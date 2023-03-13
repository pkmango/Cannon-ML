using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections;

public class GunAgent : Agent
{
    public float gunTurningSpeed = 5;
    [Min(0.1f)]
    public float shotDelay = 0.5f;
    [Min(0.04f)]
    public float laserVisibilityDelay = 0.1f;
    public LineRenderer laserRenderer;
    public Transform laserFirePoint;
    public float defDistanceRay = 100f;
    public LayerMask enemyLayerMask;
    [Min(1f)]
    public float enemyDetectionRadius = 6f;
    [Tooltip("Each observable enemy adds 2 values to Vector Observations > Space Size")]
    public int observedEnemiesNumber = 5; //Maximum number of simultaneously observed enemies

    private bool shotAllowed = true;

    void Awake()
    {
        
    }

    private void Start()
    {

    }

    void FixedUpdate()
    {
        
    }

    public override void OnEpisodeBegin()
    {

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //It is necessary to find all observed enemies in a given radius and transfer given number <observedEnemiesNumber>
        //in the form of flat normalized coordinates Vector2 to the sensor
        Collider[] enemiesPositions = Physics.OverlapSphere(transform.position, enemyDetectionRadius, enemyLayerMask);

        for (int i = 0; i < enemiesPositions.Length; i++)
        {
            if (i < observedEnemiesNumber)
            {
                sensor.AddObservation(GetNoramalizedVector2(enemiesPositions[i].transform.position));
            }
            else
            {
                return;
            }

        }

        // If the number of observed enemies is less than <observedEnemiesNumber>,
        // then the remaining free cells of the array are filled with zeros
        if (enemiesPositions.Length < observedEnemiesNumber)
        {
            for (int i = 0; i < (observedEnemiesNumber - enemiesPositions.Length); i++)
            {
                sensor.AddObservation(Vector2.zero);
            }
        }

        // Normalized rotation around the y-axis [0,1]
        sensor.AddObservation(transform.rotation.eulerAngles.y / 360.0f);

        sensor.AddObservation(shotAllowed);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float controlSignalRotation = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
        transform.Rotate(Vector3.up, gunTurningSpeed * controlSignalRotation);

        int controlSignalShot = actionBuffers.DiscreteActions[0];
        if (controlSignalShot == 1 && shotAllowed)
            StartCoroutine(Shot());
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        var discreteActionsOut = actionsOut.DiscreteActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        discreteActionsOut[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    private Vector2 GetNoramalizedVector2(Vector3 enemyPosition)
    {
        float normalizedValueX = (enemyPosition.x + enemyDetectionRadius) / (enemyDetectionRadius * 2f);
        float normalizedValueZ = (enemyPosition.z + enemyDetectionRadius) / (enemyDetectionRadius * 2f);

        return new Vector2(normalizedValueX, normalizedValueZ);
    }

    private IEnumerator Shot()
    {
        shotAllowed = false;
        laserRenderer.enabled = true;

        if (Physics.Raycast(laserFirePoint.position, laserFirePoint.forward, out RaycastHit hit))
        {
            DrawLaser(laserFirePoint.position, hit.point);
            DestroyEnemy(hit.transform.gameObject);
        }
        else
        {
            DrawLaser(laserFirePoint.position, laserFirePoint.forward * defDistanceRay);
        }

        yield return new WaitForSeconds(laserVisibilityDelay);
        laserRenderer.enabled = false;

        yield return new WaitForSeconds(shotDelay);
        shotAllowed = true;
    }

    private void DrawLaser (Vector3 startPos, Vector3 endPos)
    {
        laserRenderer.SetPosition(0, startPos);
        laserRenderer.SetPosition(1, endPos);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Enemy")
        {
            Debug.Log("Enemy touch");
            DestroyEnemy(other.gameObject);
        }
    }

    private void DestroyEnemy(GameObject enemy)
    {
        Destroy(enemy);
    }

}
