using UnityEngine;
using Unity.Cinemachine;

public class CameraFollowSwitch : MonoBehaviour
{
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private Transform startFollowTarget;
    [SerializeField] private Transform playerTransform;

    private bool hasSwitched = false;

    private void Start()
    {
        if (virtualCamera != null && startFollowTarget != null)
        {
            virtualCamera.Follow = startFollowTarget;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hasSwitched && other.transform == playerTransform)
        {
            virtualCamera.Follow = playerTransform;
            hasSwitched = true;
        }
    }
}
