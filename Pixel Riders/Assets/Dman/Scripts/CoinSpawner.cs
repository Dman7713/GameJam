using UnityEngine;

public class CoinSpawnerDebug : MonoBehaviour {
    [Header("References")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private Transform player;

    [Header("Spawn Settings")]
    [SerializeField] private float minSpawnRadius = 10f;
    [SerializeField] private float maxSpawnRadius = 50f;
    [SerializeField] private float despawnRadius = 55f;

    private GameObject activeCoin;

    private void Update() {
        if (player == null) {
            Debug.LogWarning("[CoinSpawnerDebug] No player assigned!");
            return;
        }
        if (coinPrefab == null) {
            Debug.LogWarning("[CoinSpawnerDebug] No coinPrefab assigned!");
            return;
        }

        // Despawn if too far
        if (activeCoin != null) {
            float d = Vector2.Distance(player.position, activeCoin.transform.position);
            if (d > despawnRadius) {
                Debug.Log($"[CoinSpawnerDebug] Despawning coin at distance {d:F1}");
                Destroy(activeCoin);
                activeCoin = null;
            }
        }

        // Spawn one if missing
        if (activeCoin == null) {
            Vector2 spawnPos = RandomPointInAnnulus(player.position, minSpawnRadius, maxSpawnRadius);
            Debug.Log($"[CoinSpawnerDebug] Spawning coin at {spawnPos}");
            activeCoin = Instantiate(coinPrefab, spawnPos, Quaternion.identity);
        }
    }

    private Vector2 RandomPointInAnnulus(Vector2 center, float inner, float outer) {
        float t = 2 * Mathf.PI * Random.value;
        float u = Random.value;
        float r = Mathf.Sqrt(u * (outer*outer - inner*inner) + inner*inner);
        return center + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * r;
    }

    private void OnDrawGizmosSelected() {
        if (player == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position, minSpawnRadius);
        Gizmos.DrawWireSphere(player.position, maxSpawnRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, despawnRadius);
    }
}
