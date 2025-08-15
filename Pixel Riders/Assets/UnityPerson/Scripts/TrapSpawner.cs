using UnityEngine;

public class TrapSpawner : MonoBehaviour {
    [SerializeField] private TrapData[] traps;            // The coin prefab
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private float spawnChance;// How often to spawn coins
    //[SerializeField] private int coinsPerSpawn = 3;            // How many coins to spawn at once
    //[SerializeField] private float spawnRadius = 20f;          // Horizontal distance from player
    //[SerializeField] private float raycastHeight = 10f;        // How high above to raycast from//
    [SerializeField] private LayerMask terrainLayer;           // The layer your Sprite Shape terrain is on
    //[SerializeField] private float minDistanceBehindPlayer = -5f; // No coins spawn behind this

    private Transform player;

    [System.Serializable]
    public class TrapData {
        public GameObject trapPrefab;
        public float spawnRandomWeight;
        public float occupiedRadius;
    }

    private void Start() {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        InvokeRepeating(nameof(SpawnTraps), 0f, spawnInterval);
    }

    private GameObject ChooseTrap() {



        return null;
    }

    void SpawnTraps(float minX, float maxX) {
        float maxSpace = Mathf.Abs(maxX - minX);

        //RaycastHit2D hit = Physics2D.Raycast(spawnOrigin, Vector2.down, raycastHeight * 2, terrainLayer);
    }
}