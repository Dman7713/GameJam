using UnityEngine;

using UnityEngine.U2D;

using System.Collections.Generic;



public class ProceduralGroundGenerator : MonoBehaviour

{

    public SpriteShapeController spriteShapeController;

    public float segmentLength = 5f;

    public int numberOfSegments = 50;

    public float noiseScale = 0.5f;

    public float heightMultiplier = 5f; // Max depth of the ground below Y=0

    public float startX = 0f; // <--- CHANGE THIS: Flat ground will now start at X=0

    public float seed;



    [Header("Flat Start Settings")]

    public float flatGroundLength = 15f;

    private int flatSegmentsCount;



    public GameObject[] obstaclePrefabs;

    public float obstacleSpawnChance = 0.2f;



    void Awake()

    {

        flatSegmentsCount = Mathf.CeilToInt(flatGroundLength / segmentLength);



        if (flatSegmentsCount > numberOfSegments)

        {

            Debug.LogWarning("Flat ground length (" + flatGroundLength + "m) exceeds total generated length (" + (numberOfSegments * segmentLength) + "m). Adjusting total segments to match flat ground.");

            numberOfSegments = flatSegmentsCount;

        }

    }



    void Start()

    {

        if (seed == 0)

        {

            seed = Random.Range(0f, 100000f);

        }

        Random.InitState((int)seed);



        GenerateGround();

    }



    void GenerateGround()

    {

        if (spriteShapeController == null)

        {

            Debug.LogError("SpriteShapeController not assigned! Please drag it onto the script in the Inspector.", this);

            return;

        }



        spriteShapeController.spline.Clear();



        float currentX = startX; // Starts at X=0 now

        float lastY = 0f;



        for (int i = 0; i < numberOfSegments; i++)

        {

            float targetY;



            if (i < flatSegmentsCount)

            {

                targetY = 0f; // Keep Y at 0 for flat ground

            }

            else

            {

                float noiseValue = Mathf.PerlinNoise((currentX + seed) * noiseScale, seed * noiseScale);

                targetY = -(noiseValue * heightMultiplier);



                targetY = Mathf.Lerp(lastY, targetY, 0.5f);

            }



            Vector3 newPoint = new Vector3(currentX, targetY, 0f);

            spriteShapeController.spline.InsertPointAt(i, newPoint);



            spriteShapeController.spline.SetTangentMode(i, ShapeTangentMode.Continuous);

            spriteShapeController.spline.SetLeftTangent(i, Vector3.left * segmentLength * 0.5f);

            spriteShapeController.spline.SetRightTangent(i, Vector3.right * segmentLength * 0.5f);



            lastY = targetY;



            if (i >= flatSegmentsCount && obstaclePrefabs.Length > 0 && Random.value < obstacleSpawnChance)

            {

                Vector3 obstaclePos = new Vector3(currentX, targetY + 1f, 0f);

                GameObject chosenObstacle = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];

                Instantiate(chosenObstacle, obstaclePos, Quaternion.identity, transform);

            }



            currentX += segmentLength;

        }



        spriteShapeController.RefreshSpriteShape();

    }

}