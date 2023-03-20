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
    public float rayDistance = 10f; // Maximum laser range
    public LayerMask enemyLayerMask;
    [Min(1f)]
    public float enemyDetectionRadius = 6f;
    [Tooltip("Each observable enemy adds 2 values to Vector Observations > Space Size")]
    public int observedEnemiesNumber = 5; // Maximum number of simultaneously observed enemies
    [HideInInspector]
    public List<GameObject> enemies = new List<GameObject>();
    public Color detectedEnemyColor = Color.white;
    [HideInInspector]
    public int currentPoints = 0;
    [HideInInspector]
    public int currentHp = 20;

    private bool shotAllowed = true;
    private Quaternion startGunRotation;
    private Coroutine shotCor; // For Shot()
    private Material enemyMaterial;
    // List of tracked targets
    private List<GameObject> observedEnemies;

    private void Start()
    {
        startGunRotation = transform.rotation;
        observedEnemies = new List<GameObject>(observedEnemiesNumber);
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
        Collider[] enemiesColliders = new Collider[observedEnemiesNumber];
        Physics.OverlapSphereNonAlloc(transform.position, enemyDetectionRadius, enemiesColliders, enemyLayerMask);

        // If there is free space, write the detected enemies to the buffer <observedEnemies>
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
            // This adds the normalized value of the angle (between 0 and 1) between the direction of the target and the current one.
            Vector3 targetDirection = enemy.transform.position - transform.position;
            float angleToTarget = Quaternion.FromToRotation(transform.forward, targetDirection).eulerAngles.y;
            sensor.AddObservation(angleToTarget / 360f);

            // Also, the agent can track the distance to the target
            float enemyDistance = Vector3.Magnitude(enemy.transform.position - transform.position);
            sensor.AddObservation(enemyDistance / enemyDetectionRadius);
        }

        // If there are unused cells in the buffer <observedEnemies>,
        // we need to write the values <-1> there so that they are clearly different from all other values
        int difference = observedEnemiesNumber - observedEnemies.Count;
        if (difference > 0)
        {
            for (int i = 0; i < difference; i++)
            {
                sensor.AddObservation(-1f);
                sensor.AddObservation(-1f);
            }
        }

        if (difference < 0)
        {
            Debug.Log("Error. <observedEnemies.Count> exceeded the expected parameters");
        }

        sensor.AddObservation(shotAllowed);
        sensor.AddObservation(observedEnemies.Count);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float controlSignalRotation = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
        transform.Rotate(Vector3.up, gunTurningSpeed * controlSignalRotation);

        // A small penalty to minimize unnecessary movements
        if (actionBuffers.ContinuousActions[0] != 0f)
            AddReward(-0.0002f);

        int controlSignalShot = actionBuffers.DiscreteActions[0];
        if (controlSignalShot == 1 && shotAllowed)
            shotCor = StartCoroutine(Shot());
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        var continuousActionsOut = actionsOut.ContinuousActions;
        discreteActionsOut.Clear();

        discreteActionsOut[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
    }

    private IEnumerator Shot()
    {
        shotAllowed = false;
        laserRenderer.enabled = true;
        
        if (Physics.Raycast(laserFirePoint.position, laserFirePoint.forward, out RaycastHit hit, rayDistance))
        {
            // Hitting the target
            if (observedEnemies.Contains(hit.transform.gameObject))
            {
                // Random hits are not rewarded
                AddReward(0.05f);
                currentPoints++;
            }

            DrawLaser(laserFirePoint.position, hit.point);
            DestroyEnemy(hit.transform.gameObject);

            if (currentPoints == winPoints)
                EndEpisode();
        }
        else
        {
            // Miss
            DrawLaser(laserFirePoint.position, laserFirePoint.forward * rayDistance + transform.position);
            AddReward(-0.005f);
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
            // The enemy touched the agent
            DestroyEnemy(other.gameObject);
            AddReward(-0.04f);
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
