using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    // The background image of the joystick
    [SerializeField] private RectTransform _background;
    // The handle of the joystick
    [SerializeField] private RectTransform _handle;

    [Tooltip("The maximum distance the handle can move from the center.")]
    [SerializeField] private float _joystickRadius = 50f;

    // The public property to get the horizontal input value
    public float Horizontal { get; private set; }

    private Vector2 _inputVector;

    private void Start()
    {
        if (_background == null)
        {
            _background = transform.Find("Background").GetComponent<RectTransform>();
        }
        if (_handle == null)
        {
            _handle = _background.Find("Handle").GetComponent<RectTransform>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position;
        // Convert the touch position to a local position within the joystick background
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _background,
            eventData.position,
            eventData.pressEventCamera,
            out position))
        {
            // Normalize the position vector based on the joystick radius
            position = position / _joystickRadius;
            _inputVector = position;
            _inputVector = (_inputVector.magnitude > 1.0f) ? _inputVector.normalized : _inputVector;
            
            // Move the joystick handle
            _handle.anchoredPosition = _inputVector * _joystickRadius;
            
            // Set the public horizontal value
            Horizontal = _inputVector.x;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Reset the joystick to the center when the user lifts their finger
        _inputVector = Vector2.zero;
        _handle.anchoredPosition = Vector2.zero;
        Horizontal = 0f;
    }
}