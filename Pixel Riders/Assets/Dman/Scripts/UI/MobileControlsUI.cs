using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class MobileControlsUI : MonoBehaviour
{
    public EventTrigger accelerateButton;
    public EventTrigger decelerateButton;
    public Joystick rotationJoystick;

    private void Awake()
    {
        Debug.Log("MobileControlsUI.Awake() called. Configuring button listeners.");

        if (accelerateButton == null || decelerateButton == null)
        {
            Debug.LogError("Accelerate or Decelerate EventTrigger not assigned in the Inspector.");
        }
        else
        {
            // Configure Accelerate button events
            AddTrigger(accelerateButton, EventTriggerType.PointerDown, (data) => MobileInputManager.SetDriveInput(1f));
            AddTrigger(accelerateButton, EventTriggerType.PointerUp, (data) => MobileInputManager.SetDriveInput(0f));
            
            // Configure Decelerate button events
            AddTrigger(decelerateButton, EventTriggerType.PointerDown, (data) => MobileInputManager.SetDriveInput(-1f));
            AddTrigger(decelerateButton, EventTriggerType.PointerUp, (data) => MobileInputManager.SetDriveInput(0f));
        }

        if (rotationJoystick == null)
        {
            Debug.LogError("Rotation Joystick not assigned in the Inspector.");
        }
    }
    
    private void Update()
    {
        if (rotationJoystick != null)
        {
            float joystickValue = rotationJoystick.Horizontal;
            MobileInputManager.SetRotationJoystickInput(joystickValue);
            if (joystickValue != 0f)
            {
                Debug.Log($"Joystick horizontal input: {joystickValue}");
            }
        }
    }

    private void AddTrigger(EventTrigger trigger, EventTriggerType type, System.Action<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(new UnityAction<BaseEventData>(action));
        trigger.triggers.Add(entry);
        Debug.Log($"EventTrigger configured for {trigger.gameObject.name} with event type: {type}");
    }
}