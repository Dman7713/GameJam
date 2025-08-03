using System.Collections;

using System.Collections.Generic;
using System.Net;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

using UnityEngine.U2D;
using UnityEngine.UIElements;



public class InfiniteMapGenerator : MonoBehaviour {

    [System.Serializable] private class TrapData {
        public GameObject trapObject;
        public float weight;
        [HideInInspector] public Vector3 positionOffset;
    }

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
    [SerializeField, Range(0f,1f)] private float curveSmoothness;
    [Header("Player & Spawn Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private float spawnRadius = 40f;
    [SerializeField] private int chunkDespawnBuffer = 1;

    [Header("Traps")]
    [Space(5)]
    [TextArea, SerializeField] private string instructions = "Add Empty Gameobject called sprite anchor inside the trap, the anchor will match the position of the ground, if you do not add one, it will default to the trap's center";
    [SerializeField] private List<TrapData> trapList;
    [SerializeField] private int minimumTrapsPerChunk = 0;
    [SerializeField] private int maximumTrapsPerChunk = 10;



    private Dictionary<int, List<Vector3>> generatedChunks = new Dictionary<int, List<Vector3>>();

    private HashSet<int> loadedChunkIndices = new HashSet<int>();

    private List<Vector3> activePoints = new List<Vector3>();
    private Dictionary<int, List<GameObject>> activeTraps = new Dictionary<int, List<GameObject>>();



    private void Start() {

        if (perlinSeed == 0)
            perlinSeed = Random.Range(0, 10000);

        if (player == null)

            player = GameObject.FindGameObjectWithTag("Player").transform;

        spriteShapeController.spline.Clear();

        foreach (TrapData trap in trapList) {
            Transform trapObj = trap.trapObject.transform;

            if (trapObj.Find("Anchor")) {
                trap.positionOffset = -trapObj.Find("Anchor").position;
            }
            else {
                Debug.LogWarning("Couldn't find anchor, using native position");
                trap.positionOffset = Vector3.zero;
            }
        }

        // Delay generation to let camera/rendering settle

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

    private TrapData ChooseTrap() {
        List <TrapData> possibleTraps = new List<TrapData>();
        foreach (TrapData trap in trapList) {
            for (int i = 0; i < trap.weight; i++) {
                possibleTraps.Add(trap);
            }
        }
        return possibleTraps[Random.Range(0, possibleTraps.Count - 1)];
    }
    private void SpawnTrap(Vector3 position, int chunkIndex) {
        position.Scale(transform.localScale);
        Vector2 newPosition = (Vector2)position;
        newPosition.y += 500;
        RaycastHit2D rayResult = Physics2D.Raycast(newPosition, Vector2.down * 200);
        if (rayResult == false) { return; }
        TrapData trapData = ChooseTrap();
        GameObject trap = Instantiate(trapData.trapObject);
        trap.transform.position = new Vector3(rayResult.point.x, rayResult.point.y, 0);
        float angle = Vector2.Angle(transform.right, rayResult.normal) - 90;
        trap.transform.Rotate(new Vector3(0, 0, angle));
        trap.transform.Translate(trapData.positionOffset);
        trap.transform.parent = transform;
        BindTrapToChunk(chunkIndex, trap);
    }

    private void BindTrapToChunk(int generationIndex, GameObject trap) {
        activeTraps[generationIndex].Add(trap);
    }

    private void RemoveTraps(int chunkIndex) {
        foreach (GameObject trap in activeTraps[chunkIndex]) {
            Destroy(trap);
        }
        activeTraps.Remove(chunkIndex);
    }

    private List<bool> CreateRoulette() {
        List<bool> roulette = new List<bool>();
        int trapsAdded = 0;
        for (int i = 0; i < chunkSize; i++) {
            bool rand = Random.Range(0f, 1f) <= 0.5f;
            roulette.Add(rand && trapsAdded <= maximumTrapsPerChunk);
            trapsAdded += rand ? 1 : 0;
        }
        
        return roulette;
    }

    private void DetermineTrapPositions(int chunkIndex) {
        List<bool> trapLayout = CreateRoulette();
        Debug.Log(CreateRoulette());
        List<Vector3> chunkPositions = generatedChunks[chunkIndex];
        activeTraps.Add(chunkIndex, new List<GameObject>());
        for (int i = 0; i < chunkSize; i++) {
            if (trapLayout[i] == false) continue;

            SpawnTrap(chunkPositions[i], chunkIndex);
            
        }
    }



    private void UpdateChunks() {
        float chunkWorldSize = chunkSize * xSpacing * transform.localScale.x;
        int playerChunk = Mathf.FloorToInt(player.position.x / chunkWorldSize);
        HashSet<int> requiredChunks = new HashSet<int>();

        float loadRange = spawnRadius / chunkWorldSize;

        for (int i = (int)(playerChunk - loadRange); i <= playerChunk + loadRange; i++) {

            requiredChunks.Add(i);

            if (!loadedChunkIndices.Contains(i)) {

                LoadChunk(i);

                loadedChunkIndices.Add(i);

                DetermineTrapPositions(i);
            }
        }

        List<int> chunksToRemove = new List<int>();

        foreach (int index in loadedChunkIndices) {

            if (!requiredChunks.Contains(index)) {

                bool isTooFarBehind = index < playerChunk - loadRange - chunkDespawnBuffer;

                bool isTooFarAhead = index > playerChunk + loadRange + chunkDespawnBuffer;

                if (isTooFarBehind || isTooFarAhead) {

                    RemoveChunk(index);

                    chunksToRemove.Add(index);

                }

            }

        }

        


        foreach (int index in chunksToRemove) {

            loadedChunkIndices.Remove(index);

        }



        UpdateSplinePoints();

    }



    private void LoadChunk(int chunkIndex) {

        List<Vector3> chunkPoints = GenerateChunkPoints(chunkIndex);

        generatedChunks[chunkIndex] = chunkPoints;

    }



    private void RemoveChunk(int chunkIndex) {

        if (generatedChunks.ContainsKey(chunkIndex)) {

            generatedChunks.Remove(chunkIndex);
            RemoveTraps(chunkIndex);

        }

    }



    private List<Vector3> GenerateChunkPoints(int chunkIndex) {

        List<Vector3> topPoints = new List<Vector3>();

        int startPointIndex = chunkIndex * chunkSize;



        for (int i = 0; i < chunkSize; i++) {

            float x = (startPointIndex + i) * xSpacing;
            x += Random.Range(-0.001f, 0.001f); // Slight variation to avoid duplicates
            float noiseY = Mathf.PerlinNoise((startPointIndex + i) * noiseStep, perlinSeed) * yMultiplier;

            float y = noiseY + baseY;

            topPoints.Add(new Vector3(x, y, 0));
        }



        if (generatedChunks.ContainsKey(chunkIndex - 1)) {

            List<Vector3> prevChunk = generatedChunks[chunkIndex - 1];

            if (prevChunk.Count >= chunkSize) {

                float lastYPrevChunk = prevChunk[chunkSize - 1].y;

                Vector3 firstTop = topPoints[0];

                topPoints[0] = new Vector3(firstTop.x, lastYPrevChunk, 0);

            }

        }



        return topPoints;

    }



    private void UpdateSplinePoints() {

        activePoints.Clear();
        List<int> sortedChunks = new List<int>(loadedChunkIndices);
        sortedChunks.Sort();
        List<Vector3> allTopPoints = new List<Vector3>();
        foreach (int chunkIndex in sortedChunks) {
            if (generatedChunks.ContainsKey(chunkIndex)) {
                allTopPoints.AddRange(generatedChunks[chunkIndex]);
            }

        }



        if (allTopPoints.Count == 0)

            return;



        activePoints.AddRange(allTopPoints);



        for (int i = allTopPoints.Count - 1; i >= 0; i--) {

            if (i != allTopPoints.Count - 1 && i > 0) { continue; }

            Vector3 top = allTopPoints[i];

            Vector3 bottom = new Vector3(top.x, bottomY, 0);

            activePoints.Add(bottom);

        }



        var spline = spriteShapeController.spline;

        spline.Clear();



        for (int i = 0; i < activePoints.Count; i++) {

            if (i == 0 || Vector3.Distance(activePoints[i], activePoints[i - 1]) > 0.01f) {

                spline.InsertPointAt(spline.GetPointCount(), activePoints[i]);

            }

        }



        int totalPoints = spline.GetPointCount();

        for (int i = 0; i < totalPoints; i++) {

            spline.SetTangentMode(i, ShapeTangentMode.Continuous);
            spline.SetLeftTangent(i, Vector3.left * curveSmoothness);
            spline.SetRightTangent(i, Vector3.right * curveSmoothness);
        }



        spline.isOpenEnded = false;



        // Force visibility refresh

        spriteShapeController.RefreshSpriteShape();

        spriteShapeController.BakeMesh();

        Canvas.ForceUpdateCanvases();

    }
}