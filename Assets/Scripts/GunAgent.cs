﻿using System.Collections.Generic;
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

    //public Transform testTarget;

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
            Vector3 targetDirection = enemy.transform.position - transform.position;
            float angleToTarget = Quaternion.FromToRotation(transform.forward, targetDirection).eulerAngles.y;
            sensor.AddObservation(angleToTarget / 360f);
            //Debug.Log(angleToTarget / 360f);
            float enemyDistance = Vector3.Magnitude(enemy.transform.position - transform.position);
            sensor.AddObservation(enemyDistance / enemyDetectionRadius);
        }

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
        //AgentTurn(actionBuffers.DiscreteActions);
        float controlSignalRotation = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
        transform.Rotate(Vector3.up, gunTurningSpeed * controlSignalRotation);

        int controlSignalShot = actionBuffers.DiscreteActions[0];
        if (controlSignalShot == 1 && shotAllowed)
            shotCor = StartCoroutine(Shot());

        if (actionBuffers.ContinuousActions[0] != 0f)
            AddReward(-0.0002f);

        //Debug.Log(GetCumulativeReward());
    }

    //private void AgentTurn(ActionSegment<int> discreteActions)
    //{
    //    float controlSignalRotation = 0;

    //    if (discreteActions[1] == 1)
    //        controlSignalRotation = 1;
    //    else if(discreteActions[1] == 2)
    //        controlSignalRotation = -1;

    //    transform.Rotate(Vector3.up, gunTurningSpeed * controlSignalRotation);
    //}

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        var continuousActionsOut = actionsOut.ContinuousActions;
        discreteActionsOut.Clear();

        discreteActionsOut[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");

        //if (Input.GetKey(KeyCode.D))
        //    discreteActionsOut[1] = 1;

        //if (Input.GetKey(KeyCode.A))
        //    discreteActionsOut[1] = 2;
    }

    private IEnumerator Shot()
    {
        shotAllowed = false;
        laserRenderer.enabled = true;
        
        if (Physics.Raycast(laserFirePoint.position, laserFirePoint.forward, out RaycastHit hit, rayDistance))
        {
            if (observedEnemies.Contains(hit.transform.gameObject))
            {
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
