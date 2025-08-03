using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    public GameObject coinPrefab;
    public float spawnInterval = 1f;
    public LayerMask terrainLayer;

    [Header("Spawn Area")]
    public float forwardSpawnDistance = 20f;
    public float horizontalRange = 5f;
    public float verticalRange = 5f;
    public float raycastHeight = 10f;

    [Header("Air Coins")]
    [Range(0f, 1f)] public float airCoinChance = 0.3f;
    public float airMinHeight = 2f;
    public float airMaxHeight = 5f;

    [Header("Chunk Settings")]
    [Range(0f, 1f)] public float chunkSpawnChance = 0.4f; // 40% chance to spawn a chunk
    public int minChunkSize = 3;
    public int maxChunkSize = 8;
    public float chunkSpacing = 1.5f;

    [Header("Single Coin Settings")]
    public int minSingles = 1;
    public int maxSingles = 4;

    private Transform player;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        InvokeRepeating(nameof(SpawnCoins), 0f, spawnInterval);
    }

    void SpawnCoins()
    {
        bool spawnChunk = Random.value < chunkSpawnChance;

        if (spawnChunk)
        {
            SpawnCoinChunk();
        }
        else
        {
            int coinCount = Random.Range(minSingles, maxSingles + 1);
            for (int i = 0; i < coinCount; i++)
            {
                SpawnSingleCoin();
            }
        }
    }

    void SpawnSingleCoin()
    {
        float spawnX = player.position.x + forwardSpawnDistance + Random.Range(-horizontalRange, horizontalRange);
        float spawnY = player.position.y + Random.Range(-verticalRange, verticalRange);
        Vector2 spawnOrigin = new Vector2(spawnX, spawnY + raycastHeight);

        if (Random.value <= airCoinChance)
        {
            float airHeight = Random.Range(airMinHeight, airMaxHeight);
            Vector2 airPos = new Vector2(spawnX, spawnY + airHeight);
            Instantiate(coinPrefab, airPos, Quaternion.identity);
        }
        else
        {
            RaycastHit2D hit = Physics2D.Raycast(spawnOrigin, Vector2.down, raycastHeight * 2, terrainLayer);
            if (hit.collider != null)
            {
                Vector2 groundPos = hit.point + Vector2.up * 0.5f;
                Instantiate(coinPrefab, groundPos, Quaternion.identity);
            }
        }
    }

    void SpawnCoinChunk()
    {
        int chunkSize = Random.Range(minChunkSize, maxChunkSize + 1);
        float startX = player.position.x + forwardSpawnDistance;
        float baseY = player.position.y + Random.Range(-verticalRange, verticalRange);
        bool isAir = Random.value <= airCoinChance;

        for (int i = 0; i < chunkSize; i++)
        {
            float x = startX + i * chunkSpacing + Random.Range(-1f, 1f); // slight variation
            float y = baseY;
            Vector2 spawnOrigin = new Vector2(x, y + raycastHeight);

            if (isAir)
            {
                float airHeight = Random.Range(airMinHeight, airMaxHeight);
                Vector2 airPos = new Vector2(x, y + airHeight);
                Instantiate(coinPrefab, airPos, Quaternion.identity);
            }
            else
            {
                RaycastHit2D hit = Physics2D.Raycast(spawnOrigin, Vector2.down, raycastHeight * 2, terrainLayer);
                if (hit.collider != null)
                {
                    Vector2 groundPos = hit.point + Vector2.up * 0.5f;
                    Instantiate(coinPrefab, groundPos, Quaternion.identity);
                }
            }
        }
    }
}
