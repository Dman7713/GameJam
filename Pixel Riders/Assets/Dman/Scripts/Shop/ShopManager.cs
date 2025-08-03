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
    // --- NEW: Public variable for the cost text ---
    public TextMeshProUGUI costText;

    // Button references (the Button component is what handles the click)
    public Button actionButton;
    // This is the Image component on the button that we will change the sprite of
    public Image actionButtonImage;

    // Sprites for the different button states
    public Sprite purchaseButtonSprite;
    public Sprite equipButtonSprite;
    public Sprite equippedButtonSprite;

    // The SpriteRenderer on the bike display in the shop
    public SpriteRenderer bikeBodyRenderer;

    // Data assets for all the possible body sprites
    public List<BikeBodySpriteSO> allSprites;
    private int currentDisplayIndex = 0;

    void Start()
    {
        // A defensive check to ensure the list is assigned.
        if (allSprites == null || allSprites.Count == 0)
        {
            Debug.LogError("The 'All Sprites' list in the ShopManager is not assigned or is empty. Please check the Inspector.");
            return;
        }

        // Final check for the DataManager instance to give a clear error message.
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
        // First, perform a null check to ensure the list is valid
        if (allSprites == null || allSprites.Count == 0)
        {
            Debug.LogError("The All Sprites list is not assigned or is empty in the ShopManager!");
            return;
        }
        
        if (currentDisplayIndex < 0 || currentDisplayIndex >= allSprites.Count)
        {
            // The LogError method was missing the "Debug." prefix.
            Debug.LogError("Current display index " + currentDisplayIndex + " is out of bounds!");
            return;
        }

        BikeBodySpriteSO currentSprite = allSprites[currentDisplayIndex];

        // Ensure the current sprite is not null before trying to use it.
        if (currentSprite == null)
        {
            Debug.LogError("The sprite at index " + currentDisplayIndex + " is a null reference!");
            return;
        }

        // Check if UI references are missing
        if (coinText == null || spriteNameText == null || bikeBodyRenderer == null || costText == null)
        {
            Debug.LogError("A UI element reference is missing in the ShopManager. Please check the Inspector.");
            return;
        }
        
        // Update basic UI elements
        coinText.text = "Coins: " + DataManager.Instance.coins.ToString();
        spriteNameText.text = currentSprite.spriteName;
        bikeBodyRenderer.sprite = currentSprite.bodySprite;

        // --- NEW: Logic to display the cost or "FREE" ---
        if (currentSprite.cost == 0)
        {
            costText.text = "FREE";
        }
        else
        {
            costText.text = currentSprite.cost.ToString();
        }
        // --- END NEW LOGIC ---

        // --- Logic for the Action Button's state and visual ---
        if (DataManager.Instance.ownedSprites.Contains(currentSprite.spriteName))
        {
            // Player owns this sprite
            if (DataManager.Instance.equippedSpriteName == currentSprite.spriteName)
            {
                // This sprite is currently equipped
                if (actionButtonImage != null)
                {
                    actionButtonImage.sprite = equippedButtonSprite;
                    // --- NEW: Set the color to white to prevent the faded effect of a non-interactable button.
                    actionButtonImage.color = Color.white;
                }
                if (actionButton != null)
                {
                    actionButton.interactable = false;
                }
            }
            else
            {
                // Player owns it but it's not equipped
                if (actionButtonImage != null)
                {
                    actionButtonImage.sprite = equipButtonSprite;
                    // Ensure the button is not faded when it is interactable.
                    actionButtonImage.color = Color.white;
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
            // Player does not own this sprite
            // --- NEW: Check if the cost is 0 to show the 'Equip' button instead of 'Purchase'
            if (currentSprite.cost == 0)
            {
                if (actionButtonImage != null)
                {
                    actionButtonImage.sprite = equipButtonSprite;
                    actionButtonImage.color = Color.white;
                }
                if (actionButton != null)
                {
                    actionButton.interactable = true;
                    actionButton.onClick.RemoveAllListeners();
                    // --- FIXED: Call PurchaseSprite to both equip and add the free item to ownedSprites.
                    actionButton.onClick.AddListener(() => PurchaseSprite(currentSprite));
                }
            }
            else
            {
                // The item has a cost, so we show the 'Purchase' button
                if (actionButtonImage != null)
                {
                    actionButtonImage.sprite = purchaseButtonSprite;
                    actionButtonImage.color = Color.white;
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
        EquipSprite(spriteToPurchase); // Automatically equip after purchase
        UpdateShopUI();
    }

    private void EquipSprite(BikeBodySpriteSO spriteToEquip)
    {
        DataManager.Instance.EquipSprite(spriteToEquip.spriteName);
        UpdateShopUI();
    }
    
    // This is the static method your DriverController will call to update the bike's sprite
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
