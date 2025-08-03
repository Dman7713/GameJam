using UnityEngine;
using System.Collections.Generic;

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
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        LoadData();
    }

    private void LoadData()
    {
        coins = PlayerPrefs.GetInt(CoinsKey, 0);
        equippedSpriteName = PlayerPrefs.GetString(EquippedSpriteKey, "Default");
        
        string ownedSpritesJson = PlayerPrefs.GetString(OwnedSpritesKey, "");
        if (!string.IsNullOrEmpty(ownedSpritesJson))
        {
            // Simple way to handle the list of strings
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
        if (coins >= cost && !ownedSprites.Contains(spriteName))
        {
            coins -= cost;
            ownedSprites.Add(spriteName);
            SaveData();
        }
    }
}