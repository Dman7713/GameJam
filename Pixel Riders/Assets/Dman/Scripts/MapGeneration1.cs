using UnityEngine;
using UnityEngine.U2D;
using System.Collections; // Not strictly needed for this version, but good to keep if you plan to use coroutines

public class MapGeneration1 : MonoBehaviour
{
    [SerializeField] private SpriteShapeController _spriteShapeController;

    [SerializeField, Range(3f, 100f)] private int _levelLength = 50;
    [SerializeField, Range(1f, 50f)] private float _xMultiplier = 2f;
    [SerializeField, Range(1f, 50f)] private float _yMultiplier = 2f;
    [SerializeField, Range(0f, 1f)] private float _curveSmoothness = 0.5f;
    [SerializeField] private float _noiseStep = 0.5f;
    [SerializeField] private float _bottom = 10f;

    private Vector3 _lastPos;

    // Use Awake to generate the map once when the game starts
    void Awake()
    {
        GenerateMap();
    }

    // You can keep OnValidate for editor-time previewing if you wish,
    // but ensure it doesn't conflict with runtime behavior.
    // For now, let's have it also call GenerateMap for consistency in the editor.
    private void OnValidate()
    {
        // Only run in editor to avoid conflicts with Awake at runtime
        if (!Application.isPlaying)
        {
            GenerateMap();
        }
    }

    private void GenerateMap()
    {
        _spriteShapeController.spline.Clear();

        // Ensure the GameObject itself is at (0,0,0) or your desired starting point
        // This line is optional if you want to explicitly reset the object's position
        // If the map is always generated relative to (0,0,0), then the GameObject's
        // own position can be used to offset the entire map later.
        // For truly starting at (0,0), we ensure the points are relative to (0,0)
        // and the GameObject's transform is also at (0,0,0).
        transform.position = Vector3.zero; // Explicitly set GameObject position to 0,0,0

        Vector3 currentPos; // Renamed to avoid confusion with _lastPos being for the spline point

        for (int i = 0; i < _levelLength; i++)
        {
            // Calculate point positions relative to (0,0,0) world space
            currentPos = new Vector3(i * _xMultiplier, Mathf.PerlinNoise(i * _noiseStep, 0) * _yMultiplier);
            _spriteShapeController.spline.InsertPointAt(i, currentPos);

            if (i != 0 && i != _levelLength - 1)
            {
                _spriteShapeController.spline.SetTangentMode(i, ShapeTangentMode.Continuous);
                _spriteShapeController.spline.SetLeftTangent(i, Vector3.left * _xMultiplier * _curveSmoothness);
                _spriteShapeController.spline.SetRightTangent(i, Vector3.right * _xMultiplier * _curveSmoothness);
            }
            _lastPos = currentPos; // Update _lastPos for the last generated point
        }

        // Add bottom points relative to (0,0,0) world space
        _spriteShapeController.spline.InsertPointAt(_levelLength, new Vector3(_lastPos.x, -_bottom));
        _spriteShapeController.spline.InsertPointAt(_levelLength + 1, new Vector3(0f, -_bottom)); // Starts at X=0, Y=-_bottom
    }
}