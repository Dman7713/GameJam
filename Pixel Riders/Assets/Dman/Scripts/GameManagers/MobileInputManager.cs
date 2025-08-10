using UnityEngine;

public static class MobileInputManager
{
    public static float DriveInput { get; private set; }
    public static float RotationJoystickInput { get; private set; }

    public static void SetDriveInput(float input)
    {
        DriveInput = input;
        if (input != 0f)
        {
            Debug.Log($"MobileInputManager.DriveInput set to: {input}");
        }
    }

    public static void SetRotationJoystickInput(float input)
    {
        RotationJoystickInput = input;
    }
}