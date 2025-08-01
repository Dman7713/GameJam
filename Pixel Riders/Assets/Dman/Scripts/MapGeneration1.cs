using UnityEngine;
using UnityEngine.U2D;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.Cinemachine;

public class MapGeneration1 : MonoBehaviour {
    [SerializeField] private SpriteShapeController _spriteShapeController;

    [SerializeField, Range(3f, 100f)] private int _levelLength = 50;
    [SerializeField, Range(1f, 50f)] private float _xMultiplier = 2f;
    [SerializeField, Range(1f, 50f)] private float _yMultiplier = 2f;
    [SerializeField, Range(0f, 1f)] private float _curveSmoothness = 0.5f;
    [SerializeField] private float _noiseStep = 0.5f;
    [SerializeField] private float _bottom = 10f;
    [SerializeField] private CinemachineCamera playerCamera;

    [Tooltip("How often the chunks update in seconds")]
    [SerializeField] private float _chunkUpdateTime = 0f;

    [Tooltip("The amount of additional distance to spawn/despawn chunks from the cameras bounds")]
    [SerializeField] private Vector2 _spawnFromCameraOffset;

    [Tooltip("If not 0, the generation will use this seed")]
    [SerializeField] private int perlinNoiseSeed = 0;

    [SerializeField] private int spawnRadius = 40;

    private enum direction {
        Right,
        Left,
    }

    private Vector3 _lastPos;
    private Camera _camera;
    private Transform _player;

    private Vector3 lastPlayerPosition;

    private int _splineIndex;
    private int _splineIncrement;
    private int _firstIncrement;

    private List<Vector3> _activeSplinePositions;


    private void ManageFarChunks() {
        if ((_player.transform.position - lastPlayerPosition).magnitude <= 10) { return; }
        lastPlayerPosition = _player.transform.position;
        for (int i = 0; i < _activeSplinePositions.Count; i++) {
            Vector3 position = _activeSplinePositions[i];
            float distance = (_player.position - position).magnitude;
            Debug.Log(distance);
            Debug.Log(spawnRadius);
            if (distance > spawnRadius) {
                //despawn the chunk
                if (_player.position.x - position.x < 0f) {
                    _spriteShapeController.spline.RemovePointAt(0);
                    _activeSplinePositions.RemoveAt(0);
                    _firstIncrement++;
                    _splineIndex--;
                    break;
                }
                
            }
        }
    }

    private void GetGenerationOpportunity(direction direction) {
        Vector3 tryPos;
        int arrayDestination = 0;
        if (direction == direction.Right) {
            Vector3 pos;
            if (_activeSplinePositions.Count == 0) {
                pos = Vector3.zero;
            }
            else {
                pos = _activeSplinePositions[_activeSplinePositions.Count - 1];
            }
            tryPos = new Vector3(pos.x + _xMultiplier, Mathf.PerlinNoise((_splineIncrement + 1) * _noiseStep, perlinNoiseSeed) * _yMultiplier, pos.z);

            arrayDestination = _activeSplinePositions.Count;
        }
        else { //Left
            Vector3 pos = _activeSplinePositions[0];
            tryPos = new Vector3(pos.x - _xMultiplier, Mathf.PerlinNoise((_firstIncrement - 1) * _noiseStep, perlinNoiseSeed) * _yMultiplier, pos.z);
            arrayDestination = 0;
        }

        if ((tryPos - _player.position).magnitude < spawnRadius) {
            GenerateChunk(tryPos, direction);
        }
    }

    private void UpdateMap() {
        //try next chunk
        ManageFarChunks();
        GetGenerationOpportunity(direction.Right);
        GetGenerationOpportunity(direction.Left);
    }

    private void GenerateChunk(Vector3 position, direction direction) {
        // Calculate point positions relative to (0,0,0) world space
        int generationIndex = _splineIndex;

        if (direction == direction.Left) {
            generationIndex = 0;
        }


        _spriteShapeController.spline.InsertPointAt(generationIndex, position);

            _spriteShapeController.spline.SetTangentMode(generationIndex, ShapeTangentMode.Continuous);
            _spriteShapeController.spline.SetLeftTangent(generationIndex, Vector3.left * _xMultiplier * _curveSmoothness);
            _spriteShapeController.spline.SetRightTangent(generationIndex, Vector3.right * _xMultiplier * _curveSmoothness);
        _lastPos = position; // Update _lastPos for the last generated point
        _activeSplinePositions.Insert(generationIndex, position);
        _splineIndex++;

        if (direction == direction.Right) {
            _splineIncrement++;
        }
        else {
            _firstIncrement--;
        }
    }

    // Use Awake to generate the map once when the game starts
    void Awake()
    {
        GenerateMap();
        if (perlinNoiseSeed == 0) {
            perlinNoiseSeed = (int)Random.Range(0f, 10000f);
        }
    }

    private void Start() {
        _camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        _activeSplinePositions = new List<Vector3>();
        _player = GameObject.FindGameObjectWithTag("Player").transform;
        lastPlayerPosition = _player.transform.position;
    }

    // You can keep OnValidate for editor-time previewing if you wish,
    // but ensure it doesn't conflict with runtime behavior.
    // For now, let's have it also call GenerateMap for consistency in the editor.
    //private void OnValidate()
    //{
    //    // Only run in editor to avoid conflicts with Awake at runtime
    //    if (!Application.isPlaying)
    //    {
    //        GenerateMap();
    //    }
    //}

    private void GenerateMap()
    {
        _spriteShapeController.spline.Clear();
        InvokeRepeating("UpdateMap", _chunkUpdateTime, _chunkUpdateTime);

        // Ensure the GameObject itself is at (0,0,0) or your desired starting point
        // This line is optional if you want to explicitly reset the object's position
        // If the map is always generated relative to (0,0,0), then the GameObject's
        // own position can be used to offset the entire map later.
        // For truly starting at (0,0), we ensure the points are relative to (0,0)
        // and the GameObject's transform is also at (0,0,0).
        transform.position = Vector3.zero; // Explicitly set GameObject position to 0,0,0

        _splineIndex = 0;
        _splineIncrement = 0;
        _firstIncrement = 0;

        //for (int i = 0; i < _levelLength; i++)
        //{
        //    // Calculate point positions relative to (0,0,0) world space
        //    position = new Vector3(i * _xMultiplier, Mathf.PerlinNoise(i * _noiseStep, 0) * _yMultiplier);
        //    _spriteShapeController.spline.InsertPointAt(i, position);

        //    if (i != 0 && i != _levelLength - 1)
        //    {
        //        _spriteShapeController.spline.SetTangentMode(i, ShapeTangentMode.Continuous);
        //        _spriteShapeController.spline.SetLeftTangent(i, Vector3.left * _xMultiplier * _curveSmoothness);
        //        _spriteShapeController.spline.SetRightTangent(i, Vector3.right * _xMultiplier * _curveSmoothness);
        //    }
        //    _lastPos = position; // Update _lastPos for the last generated point
        //}

        //// Add bottom points relative to (0,0,0) world space
        //_spriteShapeController.spline.InsertPointAt(_levelLength, new Vector3(_lastPos.x, -_bottom));
        //_spriteShapeController.spline.InsertPointAt(_levelLength + 1, new Vector3(0f, -_bottom)); // Starts at X=0, Y=-_bottom
    }
}