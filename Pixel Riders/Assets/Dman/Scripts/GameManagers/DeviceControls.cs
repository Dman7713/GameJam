using UnityEngine;

public class DeviceController : MonoBehaviour
{
    [Tooltip("The parent GameObject for PC controls.")]
    [SerializeField] private GameObject _pcControls;
    [Tooltip("The parent GameObject for mobile controls.")]
    [SerializeField] private GameObject _mobileControls;

    private void Awake()
    {
        #if UNITY_ANDROID || UNITY_IOS
            // If the platform is Android or iOS, enable mobile controls
            _mobileControls.SetActive(true);
            _pcControls.SetActive(false);
            Debug.Log("Device detected: Mobile. Mobile controls enabled.");
        #else
            // For all other platforms (PC, Mac, WebGL), enable PC controls
            _pcControls.SetActive(true);
            _mobileControls.SetActive(false);
            Debug.Log("Device detected: PC/Other. PC controls enabled.");
        #endif
    }
}