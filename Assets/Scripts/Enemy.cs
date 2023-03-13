using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 10f;
    public float playerDetectionDistance = 10f;
    public LayerMask playerLayerMask;

    private GameObject player;
    private Vector3 moveDirection;

    void Start()
    {
        Vector3 playerPosition = GetPlayerPosition();

        // Altitude correction
        transform.position = new Vector3(transform.position.x, playerPosition.y, transform.position.z);
        moveDirection = Vector3.Normalize(playerPosition - transform.position);
    }

    void Update()
    {
        transform.Translate(speed * Time.deltaTime * moveDirection, Space.World);
    }

    private Vector3 GetPlayerPosition()
    {
        Collider[] playerCollider = new Collider[1];
        Physics.OverlapSphereNonAlloc(transform.position, playerDetectionDistance, playerCollider, playerLayerMask);
        if (playerCollider[0] != null)
        {
            player = playerCollider[0].gameObject;
            return player.transform.position;
        }
        else
        {
            Debug.Log("Player not found");
            return Vector3.zero;
        }
    }
}
