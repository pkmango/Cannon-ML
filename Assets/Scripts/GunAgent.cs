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
    [Min(1)]
    public int hp = 20;
    [Min(1)]
    public int winPoints = 20;
    public LineRenderer laserRenderer;
    public Transform laserFirePoint;
    public float rayDistance = 10f;
    public LayerMask enemyLayerMask;
    [Min(1f)]
    public float enemyDetectionRadius = 6f;
    [Tooltip("Each observable enemy adds 2 values to Vector Observations > Space Size")]
    public int observedEnemiesNumber = 5; //Maximum number of simultaneously observed enemies
    [HideInInspector]
    public List<GameObject> enemies = new List<GameObject>();
    public Color detectedEnemyColor = Color.white;

    private bool shotAllowed = true;
    private Quaternion startGunRotation;
    private int currentPoints = 0;
    private int currentHp = 20;
    private Coroutine shotCor;
    private Material enemyMaterial;
    // list of tracked targets
    private List<GameObject> observedEnemies;

    private void Start()
    {
        startGunRotation = transform.rotation;
        observedEnemies = new List<GameObject>(observedEnemiesNumber);
        //Debug.Log(observedEnemies.Count);
    }

    public override void OnEpisodeBegin()
    {
        foreach(GameObject i in enemies)
        {
            DestroyEnemy(i);
        }
        enemies.Clear();

        if (shotCor != null)
            StopCoroutine(shotCor);

        laserRenderer.enabled = false;
        shotAllowed = true;
        transform.rotation = startGunRotation;
        currentPoints = 0;
        currentHp = hp;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // It is necessary to find all observed enemies in a given radius and transfer given number <observedEnemiesNumber>
        // in the form of flat normalized coordinates Vector2 to the sensor
        Collider[] enemiesColliders = new Collider[observedEnemiesNumber];
        Physics.OverlapSphereNonAlloc(transform.position, enemyDetectionRadius, enemiesColliders, enemyLayerMask);

        foreach (Collider enemyCollider in enemiesColliders)
        {
            if (enemyCollider != null && observedEnemies.Count < observedEnemiesNumber)
            {
                if (!observedEnemies.Contains(enemyCollider.gameObject))
                {
                    observedEnemies.Add(enemyCollider.gameObject);

                    enemyMaterial = enemyCollider.GetComponent<Renderer>().material;
                    enemyMaterial.color = detectedEnemyColor;
                }
            }
            else
            {
                break;
            }
        }

        foreach (GameObject enemy in observedEnemies)
        {
            sensor.AddObservation(GetNoramalizedVector2(enemy.transform.position));
        }

        int difference = observedEnemiesNumber - observedEnemies.Count;
        if (difference > 0)
        {
            for (int i = 0; i < difference; i++)
            {
                sensor.AddObservation(Vector2.zero);
            }
        }

        if (difference < 0)
        {
            Debug.Log("Error. <observedEnemies.Count> exceeded the expected parameters");
        }

        //foreach (Collider enemyCollider in enemiesColliders)
        //{

        //    if (enemyCollider != null)
        //    {
        //        enemyMaterial = enemyCollider.GetComponent<Renderer>().material;
        //        enemyMaterial.color = detectedEnemyColor;

        //        sensor.AddObservation(GetNoramalizedVector2(enemyCollider.transform.position));
        //    }
        //    else
        //    {
        //        // If the number of observed enemies is less than <observedEnemiesNumber>,
        //        // then the remaining free cells of the array are filled with zeros
        //        sensor.AddObservation(Vector2.zero);
        //    }
        //}

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
            shotCor = StartCoroutine(Shot());
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
        enemyPosition -= transform.position;
        float normalizedValueX = (enemyPosition.x + enemyDetectionRadius) / (enemyDetectionRadius * 2f);
        float normalizedValueZ = (enemyPosition.z + enemyDetectionRadius) / (enemyDetectionRadius * 2f);
        //Debug.Log(new Vector2(normalizedValueX, normalizedValueZ));
        return new Vector2(normalizedValueX, normalizedValueZ);
    }

    private IEnumerator Shot()
    {
        shotAllowed = false;
        laserRenderer.enabled = true;
        //Debug.Log("SHOT");
        if (Physics.Raycast(laserFirePoint.position, laserFirePoint.forward, out RaycastHit hit, rayDistance))
        {
            DrawLaser(laserFirePoint.position, hit.point);
            DestroyEnemy(hit.transform.gameObject);
            AddReward(0.05f);
            currentPoints++;
            if (currentPoints == winPoints)
                EndEpisode();
        }
        else
        {
            DrawLaser(laserFirePoint.position, laserFirePoint.forward * rayDistance + transform.position);
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
            DestroyEnemy(other.gameObject);
            AddReward(-0.05f);
            currentHp--;
            if (currentHp == 0)
                EndEpisode();
        }
    }

    private void DestroyEnemy(GameObject enemy)
    {
        if (observedEnemies.Contains(enemy))
            observedEnemies.Remove(enemy);

        Destroy(enemy);
    }

}
