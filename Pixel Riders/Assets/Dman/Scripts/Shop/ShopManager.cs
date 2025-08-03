using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class ShopManager : MonoBehaviour
{
    // UI Elements
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI spriteNameText;
    public TextMeshProUGUI costText;
    
    // Button references
    public Button actionButton;
    public Image actionButtonImage;
    public TextMeshProUGUI actionButtonText;
    
    // The SpriteRenderer on the bike display in the shop
    public SpriteRenderer bikeBodyRenderer;

    // Data assets for all the possible body sprites
    public List<BikeBodySpriteSO> allSprites;
    private int currentDisplayIndex = 0;

    void Start()
    {
        if (allSprites == null || allSprites.Count == 0)
        {
            Debug.LogError("The 'All Sprites' list in the ShopManager is not assigned or is empty. Please check the Inspector.");
            return;
        }

        if (DataManager.Instance == null)
        {
            Debug.LogError("DataManager.Instance is null! Please ensure the DataManager script is on a GameObject in your starting scene.");
            return;
        }

        if (DataManager.Instance.equippedSpriteName == "Default" && !DataManager.Instance.ownedSprites.Any())
        {
            var defaultSprite = allSprites.FirstOrDefault(s => s != null && s.isDefault);
            if (defaultSprite != null)
            {
                DataManager.Instance.EquipSprite(defaultSprite.spriteName);
                DataManager.Instance.ownedSprites.Add(defaultSprite.spriteName);
            }
            else
            {
                Debug.LogError("No default sprite was found in the All Sprites list. Please set one 'isDefault' to true.");
            }
        }

        currentDisplayIndex = allSprites.FindIndex(s => s != null && s.spriteName == DataManager.Instance.equippedSpriteName);
        UpdateShopUI();
    }

    private void UpdateShopUI()
    {
        if (allSprites == null || allSprites.Count == 0)
        {
            Debug.LogError("The All Sprites list is not assigned or is empty in the ShopManager!");
            return;
        }

        if (currentDisplayIndex < 0 || currentDisplayIndex >= allSprites.Count)
        {
            Debug.LogError("Current display index " + currentDisplayIndex + " is out of bounds!");
            return;
        }

        BikeBodySpriteSO currentSprite = allSprites[currentDisplayIndex];

        if (currentSprite == null)
        {
            Debug.LogError("The sprite at index " + currentDisplayIndex + " is a null reference!");
            return;
        }

        if (coinText == null || spriteNameText == null || bikeBodyRenderer == null || costText == null || actionButtonText == null)
        {
            Debug.LogError("A UI element reference is missing in the ShopManager. Please check the Inspector.");
            return;
        }
        
        // --- ADDED DEFENSIVE CODE HERE ---
        // Ensure the scale is not zero, which would cause the sprite to disappear.
        // We set the Z scale to 1 to prevent it from collapsing.
        if (bikeBodyRenderer.transform.localScale.z == 0)
        {
            bikeBodyRenderer.transform.localScale = new Vector3(bikeBodyRenderer.transform.localScale.x, bikeBodyRenderer.transform.localScale.y, 1f);
        }
        // Ensure the Z position is not too far from the camera. A common value for 2D is 0.
        if (bikeBodyRenderer.transform.position.z != 0)
        {
            Vector3 safePosition = bikeBodyRenderer.transform.position;
            safePosition.z = 0;
            bikeBodyRenderer.transform.position = safePosition;
        }
        // --- END OF DEFENSIVE CODE ---

        coinText.text = "Coins: " + DataManager.Instance.coins.ToString();
        spriteNameText.text = currentSprite.spriteName;
        bikeBodyRenderer.sprite = currentSprite.bodySprite;

        if (currentSprite.cost == 0)
        {
            costText.text = "FREE";
        }
        else
        {
            costText.text = currentSprite.cost.ToString();
        }

        if (DataManager.Instance.ownedSprites.Contains(currentSprite.spriteName))
        {
            if (DataManager.Instance.equippedSpriteName == currentSprite.spriteName)
            {
                actionButtonText.text = "EQUIPPED";
                if (actionButtonImage != null)
                {
                    actionButtonImage.color = Color.blue;
                }
                if (actionButton != null)
                {
                    actionButton.interactable = false;
                }
            }
            else
            {
                actionButtonText.text = "EQUIP";
                if (actionButtonImage != null)
                {
                    actionButtonImage.color = Color.green;
                }
                if (actionButton != null)
                {
                    actionButton.interactable = true;
                    actionButton.onClick.RemoveAllListeners();
                    actionButton.onClick.AddListener(() => EquipSprite(currentSprite));
                }
            }
        }
        else
        {
            if (currentSprite.cost == 0)
            {
                actionButtonText.text = "EQUIP";
                if (actionButtonImage != null)
                {
                    actionButtonImage.color = Color.green;
                }
                if (actionButton != null)
                {
                    actionButton.interactable = true;
                    actionButton.onClick.RemoveAllListeners();
                    actionButton.onClick.AddListener(() => PurchaseSprite(currentSprite));
                }
            }
            else
            {
                actionButtonText.text = "PURCHASE";
                if (actionButtonImage != null)
                {
                    Color purchaseColor;
                    ColorUtility.TryParseHtmlString("#FF6464", out purchaseColor);
                    actionButtonImage.color = purchaseColor;
                }
                if (actionButton != null)
                {
                    actionButton.interactable = DataManager.Instance.coins >= currentSprite.cost;
                    actionButton.onClick.RemoveAllListeners();
                    actionButton.onClick.AddListener(() => PurchaseSprite(currentSprite));
                }
            }
        }
    }

    public void NavigateLeft()
    {
        currentDisplayIndex--;
        if (currentDisplayIndex < 0)
        {
            currentDisplayIndex = allSprites.Count - 1;
        }
        UpdateShopUI();
    }

    public void NavigateRight()
    {
        currentDisplayIndex++;
        if (currentDisplayIndex >= allSprites.Count)
        {
            currentDisplayIndex = 0;
        }
        UpdateShopUI();
    }

    private void PurchaseSprite(BikeBodySpriteSO spriteToPurchase)
    {
        DataManager.Instance.PurchaseSprite(spriteToPurchase.spriteName, spriteToPurchase.cost);
        EquipSprite(spriteToPurchase);
        UpdateShopUI();
    }

    private void EquipSprite(BikeBodySpriteSO spriteToEquip)
    {
        DataManager.Instance.EquipSprite(spriteToEquip.spriteName);
        UpdateShopUI();
    }
    
    public static void ApplyEquippedSprite(SpriteRenderer bikeBodyRenderer, List<BikeBodySpriteSO> availableSprites)
    {
        if (DataManager.Instance == null || availableSprites == null) return;
        
        string equippedName = DataManager.Instance.equippedSpriteName;
        BikeBodySpriteSO equippedSprite = availableSprites.FirstOrDefault(s => s != null && s.spriteName == equippedName);
        
        if (equippedSprite != null)
        {
            bikeBodyRenderer.sprite = equippedSprite.bodySprite;
        }
    }
}