using System.Collections;
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
        if (player != null)
            player.GetComponent<GunAgent>().enemies.Add(gameObject);

        // Altitude correction
        transform.position = new Vector3(transform.position.x, playerPosition.y, transform.position.z);
        moveDirection = Vector3.Normalize(playerPosition - transform.position);

        // del
        //StartCoroutine(SetColor());
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

    //private IEnumerator SetColor()
    //{
    //    while (true)
    //    {
    //        Material thisMat = GetComponent<Renderer>().material;
    //        thisMat.color = Color.green;
    //        Debug.Log("Покрашен");
    //        yield return new WaitForSeconds(0.5f);
    //    }
        
    //}
}
