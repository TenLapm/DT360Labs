using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public GameObject coinPrefab;
    public float spawnRadius = 10f;

    public GameObject SpawnCoin()
    {
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = new Vector3(randomCircle.x, 1.0f, randomCircle.y);

        return Instantiate(coinPrefab, spawnPos, Quaternion.identity);
    }
}