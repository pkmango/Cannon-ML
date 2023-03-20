using System.Collections;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public GameObject[] respawnZones;
    public GameObject enemyPrefab;
    [Min(0.5f)]
    public float enemySpawnDelay = 1f;

    private Coroutine enemySpawnCor;

    private void Start()
    {
        enemySpawnCor = StartCoroutine(RespawnEnemy(respawnZones));
    }

    void Update()
    {
        
    }

    private IEnumerator RespawnEnemy(GameObject[] respZones)
    {
        // For continuous agent training, enemies respawn endlessly
        while (true)
        {
            GameObject randomRespZone = respZones[Random.Range(0, respawnZones.Length - 1)];
            Vector3 extents = randomRespZone.GetComponent<MeshRenderer>().bounds.extents;
            float xPos = randomRespZone.transform.position.x + Random.Range(-extents.x, extents.x);
            float zPos = randomRespZone.transform.position.z + Random.Range(-extents.z, extents.z);
            Vector3 respPoint = new Vector3(xPos, 0f, zPos);

            Instantiate(enemyPrefab, respPoint, Quaternion.identity);

            yield return new WaitForSeconds(enemySpawnDelay);
        }
        
    }
}
