using UnityEngine;
using Unity.Cinemachine;

public class CameraZoomOnAir : MonoBehaviour
{
    [Header("References")]
    public CinemachineCamera cinemachineCamera;

    [Header("Zoom Settings")]
    public float zoomOutSize = 7f;      // Zoomed-out orthographic size (in air)
    public float zoomInSize = 5f;       // Normal orthographic size (grounded)
    public float zoomSpeed = 5f;        // Speed of zoom interpolation

    [HideInInspector] public bool isGrounded = true;

    void Awake()
    {
        if (cinemachineCamera == null)
            cinemachineCamera = GetComponent<CinemachineCamera>();
    }

    void Update()
    {
        float targetSize = isGrounded ? zoomInSize : zoomOutSize;

        // Smoothly interpolate orthographic size
        cinemachineCamera.Lens.OrthographicSize = Mathf.Lerp(
            cinemachineCamera.Lens.OrthographicSize,
            targetSize,
            Time.deltaTime * zoomSpeed);
    }
}
