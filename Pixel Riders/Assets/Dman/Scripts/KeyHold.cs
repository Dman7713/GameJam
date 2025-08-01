using UnityEngine;
using UnityEngine.UI;

public class ArrowKeyImageSwitcher : MonoBehaviour
{
    public enum ArrowKey
    {
        Up,
        Down,
        Left,
        Right
    }

    [Header("Key Settings")]
    public ArrowKey arrowKey = ArrowKey.Up;

    [Header("Image Settings")]
    public Image targetImage;
    public Sprite normalSprite;
    public Sprite heldSprite;

    private KeyCode GetKeyCode()
    {
        switch (arrowKey)
        {
            case ArrowKey.Up: return KeyCode.UpArrow;
            case ArrowKey.Down: return KeyCode.DownArrow;
            case ArrowKey.Left: return KeyCode.LeftArrow;
            case ArrowKey.Right: return KeyCode.RightArrow;
            default: return KeyCode.None;
        }
    }

    private void Update()
    {
        if (targetImage == null || normalSprite == null || heldSprite == null)
            return;

        KeyCode selectedKey = GetKeyCode();

        if (Input.GetKey(selectedKey))
        {
            targetImage.sprite = heldSprite;
        }
        else
        {
            targetImage.sprite = normalSprite;
        }
    }
}
