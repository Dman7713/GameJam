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

    // Button references
    public Button actionButton;
    public Image actionButtonImage;

    // Sprites for the different button states
    public Sprite purchaseButtonSprite;
    public Sprite equipButtonSprite;
    public Sprite equippedButtonSprite;

    // Bike Sprites to change
    public SpriteRenderer bikeBodyRenderer;

    // Data
    public List<BikeBodySpriteSO> allSprites;
    private int currentDisplayIndex = 0;

    void Start()
    {
        if (DataManager.Instance.equippedSpriteName == "Default" && !DataManager.Instance.ownedSprites.Any())
        {
            var defaultSprite = allSprites.FirstOrDefault(s => s.isDefault);
            if (defaultSprite != null)
            {
                DataManager.Instance.EquipSprite(defaultSprite.spriteName);
                DataManager.Instance.ownedSprites.Add(defaultSprite.spriteName);
            }
        }

        currentDisplayIndex = allSprites.FindIndex(s => s.spriteName == DataManager.Instance.equippedSpriteName);

        UpdateShopUI();
    }

    private void UpdateShopUI()
    {
        BikeBodySpriteSO currentSprite = allSprites[currentDisplayIndex];

        coinText.text = "Coins: " + DataManager.Instance.coins.ToString();
        spriteNameText.text = currentSprite.spriteName;
        bikeBodyRenderer.sprite = currentSprite.bodySprite;

        if (DataManager.Instance.ownedSprites.Contains(currentSprite.spriteName))
        {
            if (DataManager.Instance.equippedSpriteName == currentSprite.spriteName)
            {
                actionButtonImage.sprite = equippedButtonSprite;
                actionButton.interactable = false;
            }
            else
            {
                actionButtonImage.sprite = equipButtonSprite;
                actionButton.interactable = true;
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(() => EquipSprite(currentSprite));
            }
        }
        else
        {
            actionButtonImage.sprite = purchaseButtonSprite;
            actionButton.interactable = DataManager.Instance.coins >= currentSprite.cost;
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => PurchaseSprite(currentSprite));
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
        BikeBodySpriteSO equippedSprite = availableSprites.FirstOrDefault(s => s.spriteName == equippedName);
        
        if (equippedSprite != null)
        {
            bikeBodyRenderer.sprite = equippedSprite.bodySprite;
        }
    }
}