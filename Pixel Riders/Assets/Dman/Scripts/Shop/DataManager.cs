using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Added for convenience

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    public int coins;
    public List<string> ownedSprites = new List<string>();
    public string equippedSpriteName;

    private const string CoinsKey = "PlayerCoins";
    private const string EquippedSpriteKey = "EquippedSprite";
    private const string OwnedSpritesKey = "OwnedSprites";

    void Awake()
    {
        // Check if an instance already exists
        if (Instance == null)
        {
            // If not, set this as the instance
            Instance = this;
            // And ensure it persists across scene changes
            DontDestroyOnLoad(gameObject);
            LoadData();
        }
        else
        {
            // If an instance already exists, it means we are a duplicate,
            // so we destroy this new object.
            Destroy(gameObject);
        }
    }

    private void LoadData()
    {
        coins = PlayerPrefs.GetInt(CoinsKey, 0);
        equippedSpriteName = PlayerPrefs.GetString(EquippedSpriteKey, "Default");
        
        string ownedSpritesJson = PlayerPrefs.GetString(OwnedSpritesKey, "");
        if (!string.IsNullOrEmpty(ownedSpritesJson))
        {
            ownedSprites.AddRange(ownedSpritesJson.Split(','));
        }
    }

    public void SaveData()
    {
        PlayerPrefs.SetInt(CoinsKey, coins);
        PlayerPrefs.SetString(EquippedSpriteKey, equippedSpriteName);
        PlayerPrefs.SetString(OwnedSpritesKey, string.Join(",", ownedSprites));
        PlayerPrefs.Save();
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        SaveData();
    }

    public void EquipSprite(string spriteName)
    {
        equippedSpriteName = spriteName;
        SaveData();
    }

    public void PurchaseSprite(string spriteName, int cost)
    {
        // The ShopManager now handles the logic for checking if the item is owned and
        // if the player has enough coins. This method simply performs the transaction.
        coins -= cost;
        ownedSprites.Add(spriteName);
        SaveData();
    }

    public void ResetDataForTesting()
    {
        // Clear all saved data
        PlayerPrefs.DeleteAll();

        // Reset the variables to their default state
        coins = 0;
        ownedSprites.Clear();
        equippedSpriteName = "Default";

        Debug.Log("Player data has been reset.");
    }
}
