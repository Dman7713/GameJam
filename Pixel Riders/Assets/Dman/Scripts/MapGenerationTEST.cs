using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class InfiniteMapGenerator : MonoBehaviour
{
    [Header("SpriteShape")]
    [SerializeField] private SpriteShapeController spriteShapeController;

    [Header("Generation Settings")]
    [SerializeField] private int chunkSize = 20;
    [SerializeField] private float xSpacing = 1.5f;
    [SerializeField] private float yMultiplier = 3f;
    [SerializeField] private float baseY = 1.5f;
    [SerializeField] private float bottomY = 0f;
    [SerializeField] private float noiseStep = 0.2f;
    [SerializeField] private int perlinSeed = 0;

    [Header("Player & Spawn Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private float spawnRadius = 40f;
    [SerializeField] private int chunkDespawnBuffer = 1;

    private Dictionary<int, List<Vector3>> generatedChunks = new Dictionary<int, List<Vector3>>();
    private HashSet<int> loadedChunkIndices = new HashSet<int>();
    private List<Vector3> activePoints = new List<Vector3>();

    private void Start()
    {
        if (perlinSeed == 0)
            perlinSeed = Random.Range(0, 10000);

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        spriteShapeController.spline.Clear();
        UpdateChunks();
    }

    private void Update()
    {
        UpdateChunks();
    }

    private void UpdateChunks()
    {
        float chunkWorldSize = chunkSize * xSpacing * transform.localScale.x;
        int playerChunk = Mathf.FloorToInt(player.position.x / chunkWorldSize);

        HashSet<int> requiredChunks = new HashSet<int>();
        float loadRange = spawnRadius / chunkWorldSize;

        for (int i = (int)(playerChunk - loadRange); i <= playerChunk + loadRange; i++)
        {
            requiredChunks.Add(i);
            if (!loadedChunkIndices.Contains(i))
            {
                LoadChunk(i);
                loadedChunkIndices.Add(i);
            }
        }

        List<int> chunksToRemove = new List<int>();
        foreach (int index in loadedChunkIndices)
        {
            if (!requiredChunks.Contains(index))
            {
                bool isTooFarBehind = index < playerChunk - loadRange - chunkDespawnBuffer;
                bool isTooFarAhead = index > playerChunk + loadRange + chunkDespawnBuffer;

                if (isTooFarBehind || isTooFarAhead)
                {
                    RemoveChunk(index);
                    chunksToRemove.Add(index);
                }
            }
        }

        foreach (int index in chunksToRemove)
        {
            loadedChunkIndices.Remove(index);
        }

        UpdateSplinePoints();
    }

    private void LoadChunk(int chunkIndex)
    {
        List<Vector3> chunkPoints = GenerateChunkPoints(chunkIndex);
        generatedChunks[chunkIndex] = chunkPoints;
    }

    private void RemoveChunk(int chunkIndex)
    {
        if (generatedChunks.ContainsKey(chunkIndex))
        {
            generatedChunks.Remove(chunkIndex);
        }
    }

    private List<Vector3> GenerateChunkPoints(int chunkIndex)
    {
        List<Vector3> topPoints = new List<Vector3>();
        int startPointIndex = chunkIndex * chunkSize;

        for (int i = 0; i < chunkSize; i++)
        {
            float x = (startPointIndex + i) * xSpacing;
            x += Random.Range(-0.001f, 0.001f); // Slight variation to avoid duplicates

            float noiseY = Mathf.PerlinNoise((startPointIndex + i) * noiseStep, perlinSeed) * yMultiplier;
            float y = noiseY + baseY;

            topPoints.Add(new Vector3(x, y, 0));
        }

        // Smooth connection to previous chunk
        if (generatedChunks.ContainsKey(chunkIndex - 1))
        {
            List<Vector3> prevChunk = generatedChunks[chunkIndex - 1];
            if (prevChunk.Count >= chunkSize)
            {
                float lastYPrevChunk = prevChunk[chunkSize - 1].y;
                Vector3 firstTop = topPoints[0];
                topPoints[0] = new Vector3(firstTop.x, lastYPrevChunk, 0);
            }
        }

        return topPoints;
    }

    private void UpdateSplinePoints()
    {
        activePoints.Clear();

        List<int> sortedChunks = new List<int>(loadedChunkIndices);
        sortedChunks.Sort();

        List<Vector3> allTopPoints = new List<Vector3>();

        foreach (int chunkIndex in sortedChunks)
        {
            if (generatedChunks.ContainsKey(chunkIndex))
            {
                allTopPoints.AddRange(generatedChunks[chunkIndex]);
            }
        }

        if (allTopPoints.Count == 0)
            return;

        // Add top points
        activePoints.AddRange(allTopPoints);

        // Add bottom points in reverse order to close the shape
        for (int i = allTopPoints.Count - 1; i >= 0; i--)
        {
            Vector3 top = allTopPoints[i];
            Vector3 bottom = new Vector3(top.x, bottomY, 0);
            activePoints.Add(bottom);
        }

        var spline = spriteShapeController.spline;
        spline.Clear();

        for (int i = 0; i < activePoints.Count; i++)
        {
            // Avoid very close points
            if (i == 0 || Vector3.Distance(activePoints[i], activePoints[i - 1]) > 0.01f)
            {
                spline.InsertPointAt(spline.GetPointCount(), activePoints[i]);
            }
            else
            {
                Debug.LogWarning($"Skipped point {i} due to proximity.");
            }
        }

        int totalPoints = spline.GetPointCount();

        for (int i = 0; i < totalPoints; i++)
        {
            spline.SetTangentMode(i, ShapeTangentMode.Linear); // Linear is safest for closed shapes
        }

        spline.isOpenEnded = false; // ✅ Closes the shape
        spriteShapeController.RefreshSpriteShape();
        spriteShapeController.BakeMesh();
    }
}
