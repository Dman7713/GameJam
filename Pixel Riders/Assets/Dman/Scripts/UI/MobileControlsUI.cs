using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileControlsUI : MonoBehaviour
{
    // Assign these in the Inspector
    public Button accelerateButton;
    public Button decelerateButton;
    public Joystick rotationJoystick;

    private void Awake()
    {
        Debug.Log("MobileControlsUI.Awake() called. Configuring button listeners.");

        // We use EventTriggers on the buttons to detect continuous holding
        AddButtonListener(accelerateButton, 1f);
        AddButtonListener(decelerateButton, -1f);
    }
    
    private void Update()
    {
        if (rotationJoystick != null)
        {
            // Continuously update the joystick input
            float joystickValue = rotationJoystick.Horizontal;
            MobileInputManager.SetRotationJoystickInput(joystickValue);
        }
    }

    private void AddButtonListener(Button button, float inputValue)
    {
        if (button == null)
        {
            Debug.LogError($"Button for input value {inputValue} not assigned in the Inspector.");
            return;
        }

        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        // Add PointerDown event to set the input value
        EventTrigger.Entry downEntry = new EventTrigger.Entry();
        downEntry.eventID = EventTriggerType.PointerDown;
        downEntry.callback.AddListener((data) => MobileInputManager.SetDriveInput(inputValue));
        trigger.triggers.Add(downEntry);

        // Add PointerUp event to set the input back to zero
        EventTrigger.Entry upEntry = new EventTrigger.Entry();
        upEntry.eventID = EventTriggerType.PointerUp;
        upEntry.callback.AddListener((data) => MobileInputManager.SetDriveInput(0f));
        trigger.triggers.Add(upEntry);
        
        Debug.Log($"EventTrigger configured for {button.gameObject.name} with input value: {inputValue}");
    }
}
