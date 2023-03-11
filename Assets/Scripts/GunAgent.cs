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

    private bool shotAllowed = true;

    void Start()
    {

    }

    public override void OnEpisodeBegin()
    {

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(1f);
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

    private IEnumerator Shot()
    {
        shotAllowed = false;
        laserRenderer.enabled = true;
        
        Debug.Log("Выстрел!" + laserRenderer.widthCurve);

        if (Physics.Raycast(laserFirePoint.position, laserFirePoint.forward, out RaycastHit hit))
        {
            DrawLaser(laserFirePoint.position, hit.point);
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

}
