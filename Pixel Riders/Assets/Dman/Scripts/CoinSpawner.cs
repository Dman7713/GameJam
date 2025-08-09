using UnityEngine;
using System.Collections.Generic;

public class CoinSpawner : MonoBehaviour
{
    public GameObject coinPrefab;
    public float spawnInterval = 1f;
    public LayerMask terrainLayer;

    [Header("Coin Limits")]
    public int coinLimit = 50;
    private List<GameObject> activeCoins = new List<GameObject>();
    public float destroyTimeBehindPlayer = 5f; // New variable for the destroy timer

    [Header("Spawn Area")]
    public float forwardSpawnDistance = 20f;
    public float horizontalRange = 5f;
    public float verticalRange = 5f;
    public float raycastHeight = 10f;
    public float minGroundSpawnHeight = 1f;

    [Header("Air Coins")]
    [Range(0f, 1f)] public float airCoinChance = 0.3f;
    public float airMinHeight = 2f;
    public float airMaxHeight = 5f;

    [Header("Chunk Settings")]
    [Range(0f, 1f)] public float chunkSpawnChance = 0.4f;
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

    private void Update()
    {
        CheckCoinPositions();
    }

    void CheckCoinPositions()
    {
        // Use a for loop to safely remove items from the list
        for (int i = activeCoins.Count - 1; i >= 0; i--)
        {
            GameObject coin = activeCoins[i];
            if (coin == null)
            {
                activeCoins.RemoveAt(i);
                continue;
            }

            // If the coin is behind the player and its timer hasn't started
            if (coin.transform.position.x < player.position.x)
            {
                // Check if the CoinDestroyer component is already on the coin
                CoinDestroyer destroyer = coin.GetComponent<CoinDestroyer>();
                if (destroyer == null)
                {
                    // Add the component and start the timer
                    destroyer = coin.AddComponent<CoinDestroyer>();
                    destroyer.DestroyCoinAfterDelay(destroyTimeBehindPlayer);
                }
            }
        }
    }

    void SpawnCoins()
    {
        CleanUpCoins();
        if (activeCoins.Count >= coinLimit)
        {
            return;
        }

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
        if (activeCoins.Count >= coinLimit)
        {
            return;
        }

        float spawnX = player.position.x + forwardSpawnDistance + Random.Range(-horizontalRange, horizontalRange);
        float spawnY = player.position.y + Random.Range(-verticalRange, verticalRange);
        Vector2 spawnOrigin = new Vector2(spawnX, spawnY + raycastHeight);

        GameObject newCoin;

        if (Random.value <= airCoinChance)
        {
            float airHeight = Random.Range(airMinHeight, airMaxHeight);
            Vector2 airPos = new Vector2(spawnX, spawnY + airHeight);
            newCoin = Instantiate(coinPrefab, airPos, Quaternion.identity);
        }
        else
        {
            RaycastHit2D hit = Physics2D.Raycast(spawnOrigin, Vector2.down, raycastHeight * 2, terrainLayer);
            if (hit.collider != null)
            {
                Vector2 groundPos = hit.point + Vector2.up * minGroundSpawnHeight;
                newCoin = Instantiate(coinPrefab, groundPos, Quaternion.identity);
            }
            else
            {
                // No ground found, do not spawn a coin
                return;
            }
        }

        activeCoins.Add(newCoin);
    }

    void SpawnCoinChunk()
    {
        int chunkSize = Random.Range(minChunkSize, maxChunkSize + 1);
        float startX = player.position.x + forwardSpawnDistance;
        float baseY = player.position.y + Random.Range(-verticalRange, verticalRange);
        bool isAir = Random.value <= airCoinChance;

        for (int i = 0; i < chunkSize; i++)
        {
            if (activeCoins.Count >= coinLimit)
            {
                return;
            }

            float x = startX + i * chunkSpacing + Random.Range(-1f, 1f);
            float y = baseY;
            Vector2 spawnOrigin = new Vector2(x, y + raycastHeight);
            GameObject newCoin = null;

            if (isAir)
            {
                float airHeight = Random.Range(airMinHeight, airMaxHeight);
                Vector2 airPos = new Vector2(x, y + airHeight);
                newCoin = Instantiate(coinPrefab, airPos, Quaternion.identity);
            }
            else
            {
                RaycastHit2D hit = Physics2D.Raycast(spawnOrigin, Vector2.down, raycastHeight * 2, terrainLayer);
                if (hit.collider != null)
                {
                    Vector2 groundPos = hit.point + Vector2.up * minGroundSpawnHeight;
                    newCoin = Instantiate(coinPrefab, groundPos, Quaternion.identity);
                }
            }

            if (newCoin != null)
            {
                activeCoins.Add(newCoin);
            }
        }
    }

    void CleanUpCoins()
    {
        activeCoins.RemoveAll(coin => coin == null);
    }
}

// New component to handle the timed destruction
public class CoinDestroyer : MonoBehaviour
{
    public void DestroyCoinAfterDelay(float delay)
    {
        Destroy(gameObject, delay);
    }
}