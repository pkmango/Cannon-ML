using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public GameObject[] respawnZones;
    public GameObject enemyPrefab;
    [Min(0.5f)]
    public float enemySpawnDelay = 1f;

    private List<GameObject> enemies = new List<GameObject>();
    private Coroutine enemySpawnCor;

    private void Start()
    {
        enemySpawnCor = StartCoroutine(RespawnEnemy(respawnZones));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator RespawnEnemy(GameObject[] respZones)
    {
        while (true)
        {
            GameObject randomRespZone = respZones[Random.Range(0, respawnZones.Length - 1)];
            Vector3 extents = randomRespZone.GetComponent<MeshRenderer>().bounds.extents;
            float xPos = randomRespZone.transform.position.x + Random.Range(-extents.x, extents.x);
            float zPos = randomRespZone.transform.position.z + Random.Range(-extents.z, extents.z);
            Vector3 respPoint = new Vector3(xPos, 0f, zPos);

            enemies.Add(Instantiate(enemyPrefab, respPoint, Quaternion.identity));

            yield return new WaitForSeconds(enemySpawnDelay);
        }
        
    }
}
