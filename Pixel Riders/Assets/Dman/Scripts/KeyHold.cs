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

    [System.Serializable]
    public class ImageData {
        public float xThreshold;
        public float yThreshold;
        public Image sourceImage;
        public Sprite normalSprite;
        public Sprite heldSprite;
    }

    [Header("Image References")]

    [SerializeField] private ImageData[] images;


    private PrimaryInputSystem playerInput;

    private void Start() {
        playerInput = new PrimaryInputSystem();
        playerInput.Enable();
    }

    private void Update() {
        Vector2 inputVector = playerInput.Player.Drive.ReadValue<Vector2>();

        foreach (var image in images) {

            bool isHeld = false;

            if (image.xThreshold != 0 && inputVector.x >= image.xThreshold && image.xThreshold >= 0) { isHeld = true; }
            if (image.xThreshold != 0 && inputVector.x < image.xThreshold && image.xThreshold < 0) { isHeld = true; }
            if (image.yThreshold != 0 && inputVector.y >= image.yThreshold && image.yThreshold >= 0) { isHeld = true; }
            if (image.yThreshold != 0 && inputVector.y < image.yThreshold && image.yThreshold < 0) { isHeld = true; }

            image.sourceImage.sprite = isHeld ? image.heldSprite : image.normalSprite;

        }
    }
}
