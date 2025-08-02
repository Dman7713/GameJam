using UnityEngine;
using UnityEngine.UI;

public class ArrowKeyImageSwitcher : MonoBehaviour
{
    public enum ArrowKey
    {
        Up,
        Down,
        Left,
        Right,
        Space,
    }

    [Header("Key Settings")]
    [EnumButtons]
    public ArrowKey arrowKey = ArrowKey.Up;

    [Header("Image Settings")]
    public Image targetImage;
    public Sprite normalSprite;
    public Sprite heldSprite;

    private (KeyCode, KeyCode) GetKeyCode()
    {
        switch (arrowKey)
        {
            case ArrowKey.Up: return (KeyCode.UpArrow, KeyCode.W);
            case ArrowKey.Down: return ((KeyCode.DownArrow, KeyCode.S));
            case ArrowKey.Left: return (KeyCode.LeftArrow, KeyCode.A);
            case ArrowKey.Right: return (KeyCode.RightArrow, KeyCode.D);
            case ArrowKey.Space: return (KeyCode.Space, KeyCode.LeftShift);
            default: return (KeyCode.None, KeyCode.None);
        }
    }

    private void Update()
    {
        if (targetImage == null || normalSprite == null || heldSprite == null)
            return;

        (KeyCode, KeyCode) selectedKey = GetKeyCode();


        if (Input.GetKey(selectedKey.Item1) || Input.GetKey(selectedKey.Item2))
        {
            targetImage.sprite = heldSprite;
        }
        else
        {
            targetImage.sprite = normalSprite;
        }
    }
}
