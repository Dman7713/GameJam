using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class InfiniteMapGenerator : MonoBehaviour {

    [Header("SpriteShape")]
    [SerializeField] private SpriteShapeController spriteShapeController;

    [Header("Generation Settings")]
    [SerializeField] private int chunkSize = 20;
    [SerializeField] private float xSpacing = 1.5f;
    [SerializeField] private float yMultiplier = 3f;
    [SerializeField] private float baseY = 10f;
    [SerializeField] private float bottomY = 0;
    [SerializeField] private float noiseStep = 0.2f;
    [SerializeField] private int perlinSeed = 0;
    [SerializeField, Range(0f, 1f)] private float curveSmoothness;

    [Header("Player & Spawn Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private float spawnRadius = 40f;
    [SerializeField] private int chunkDespawnBuffer = 1;

    private Dictionary<int, List<Vector3>> topPointsByChunk = new Dictionary<int, List<Vector3>>();
    private HashSet<int> loadedChunkIndices = new HashSet<int>();

    private void Start() {
        if (perlinSeed == 0) perlinSeed = Random.Range(0, 10000);
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (spriteShapeController == null) {
            Debug.LogError("SpriteShapeController not assigned!");
            enabled = false;
            return;
        }

        spriteShapeController.spline.Clear();
        StartCoroutine(DelayedInitialGeneration());
        StartCoroutine(ForceVisibilityFix());
    }

    private IEnumerator DelayedInitialGeneration() {
        yield return new WaitForEndOfFrame();
        UpdateChunks();
    }

    private IEnumerator ForceVisibilityFix() {
        yield return new WaitForSeconds(1f);
        UpdateChunks();
    }

    private void Update() {
        UpdateChunks();
    }

    private void UpdateChunks() {
        if (player == null) return;

        float chunkWidth = chunkSize * xSpacing * transform.localScale.x;
        int playerChunk = Mathf.FloorToInt(player.position.x / chunkWidth);
        int radius = Mathf.CeilToInt(spawnRadius / chunkWidth);

        // Load needed
        var needed = new HashSet<int>();
        for (int i = playerChunk - radius; i <= playerChunk + radius; i++) {
            needed.Add(i);
            if (!loadedChunkIndices.Contains(i)) {
                LoadChunk(i);
                loadedChunkIndices.Add(i);
            }
        }

        // Unload out-of-range
        var toUnload = new List<int>();
        foreach (int idx in loadedChunkIndices) {
            if (!needed.Contains(idx) &&
               (idx < playerChunk - radius - chunkDespawnBuffer ||
                idx > playerChunk + radius + chunkDespawnBuffer)) {
                RemoveChunk(idx);
                toUnload.Add(idx);
            }
        }
        foreach (int idx in toUnload) loadedChunkIndices.Remove(idx);

        UpdateSplinePoints();
    }

    private void LoadChunk(int chunkIndex) {
        topPointsByChunk[chunkIndex] = GenerateChunkPoints(chunkIndex);
    }

    private void RemoveChunk(int chunkIndex) {
        topPointsByChunk.Remove(chunkIndex);
    }

    private List<Vector3> GenerateChunkPoints(int chunkIndex) {
        var pts = new List<Vector3>();
        int start = chunkIndex * chunkSize;
        for (int i = 0; i < chunkSize; i++) {
            float x = (start + i) * xSpacing + Random.Range(-0.001f, 0.001f);
            float y = Mathf.PerlinNoise((start + i) * noiseStep, perlinSeed) * yMultiplier + baseY;
            pts.Add(new Vector3(x, y, 0));
        }
        // stitch to previous chunk
        if (topPointsByChunk.ContainsKey(chunkIndex - 1) &&
            topPointsByChunk[chunkIndex - 1].Count == chunkSize) {
            var prev = topPointsByChunk[chunkIndex - 1];
            pts[0] = new Vector3(pts[0].x, prev[chunkSize - 1].y, 0);
        }
        return pts;
    }

    private void UpdateSplinePoints() {
        var allPts = new List<Vector3>();
        var sorted = new List<int>(loadedChunkIndices);
        sorted.Sort();
        foreach (int idx in sorted)
            if (topPointsByChunk.ContainsKey(idx))
                allPts.AddRange(topPointsByChunk[idx]);

        if (allPts.Count == 0) return;

        var spline = spriteShapeController.spline;
        spline.Clear();

        // Top edge
        for (int i = 0; i < allPts.Count; i++) {
            if (i == 0 || Vector3.Distance(allPts[i], allPts[i - 1]) > 0.01f)
                spline.InsertPointAt(spline.GetPointCount(), allPts[i]);
        }
        // Bottom edge
        var last = allPts[allPts.Count - 1];
        var first = allPts[0];
        spline.InsertPointAt(spline.GetPointCount(), new Vector3(last.x, bottomY, 0));
        spline.InsertPointAt(spline.GetPointCount(), new Vector3(first.x, bottomY, 0));

        // Tangents
        int total = spline.GetPointCount();
        for (int i = 0; i < total; i++) {
            spline.SetTangentMode(i, ShapeTangentMode.Continuous);
            spline.SetLeftTangent(i, Vector3.left * curveSmoothness);
            spline.SetRightTangent(i, Vector3.right * curveSmoothness);
        }
        spline.isOpenEnded = false;

        spriteShapeController.RefreshSpriteShape();
        spriteShapeController.BakeMesh();
        Canvas.ForceUpdateCanvases();
    }
}
