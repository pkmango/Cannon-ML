using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 10f;

    private GameObject player;
    private Vector3 moveDirection;

    void Start()
    {
        player = GameObject.FindWithTag("Player");

        // Altitude correction
        transform.position = new Vector3(transform.position.x, player.transform.position.y, transform.position.z);
        moveDirection = Vector3.Normalize(player.transform.position - transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(speed * Time.deltaTime * moveDirection, Space.World);
    }
}
