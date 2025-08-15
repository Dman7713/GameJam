using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AllInOneCursorManager : MonoBehaviour
{
    public Texture2D defaultCursor;
    public Texture2D hoverCursor;
    public Vector2 hotspot = Vector2.zero;

    private void Start()
    {
        SetDefaultCursor();

        // Automatically hook into all existing buttons
        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None); // true = include inactive
        foreach (Button button in buttons)
        {
            AddHoverEvents(button.gameObject);
        }
    }

    void SetDefaultCursor()
    {
        Cursor.SetCursor(defaultCursor, hotspot, CursorMode.Auto);
    }

    void SetHoverCursor()
    {
        Cursor.SetCursor(hoverCursor, hotspot, CursorMode.Auto);
    }

    void AddHoverEvents(GameObject target)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = target.AddComponent<EventTrigger>();
        }

        // OnPointerEnter
        EventTrigger.Entry enterEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        enterEntry.callback.AddListener((data) => { SetHoverCursor(); });
        trigger.triggers.Add(enterEntry);

        // OnPointerExit
        EventTrigger.Entry exitEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        exitEntry.callback.AddListener((data) => { SetDefaultCursor(); });
        trigger.triggers.Add(exitEntry);
    }
}
