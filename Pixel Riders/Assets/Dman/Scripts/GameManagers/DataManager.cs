using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    public int coins;
    public int highScore; // New variable for high score
    public List<string> ownedSprites = new List<string>();
    public string equippedSpriteName;

    private const string CoinsKey = "PlayerCoins";
    private const string HighScoreKey = "HighScore"; // New key for high score
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
            Debug.Log(Instance);
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
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0); // Load high score
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
        PlayerPrefs.SetInt(HighScoreKey, highScore); // Save high score
        PlayerPrefs.SetString(EquippedSpriteKey, equippedSpriteName);
        PlayerPrefs.SetString(OwnedSpritesKey, string.Join(",", ownedSprites));
        PlayerPrefs.Save();
        Debug.Log("Game data saved successfully!");
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        SaveData();
    }

    public void UpdateHighScore(int currentScore)
    {
        if (currentScore > highScore)
        {
            highScore = currentScore;
            SaveData();
        }
    }

    public void EquipSprite(string spriteName)
    {
        equippedSpriteName = spriteName;
        SaveData();
    }

    public void PurchaseSprite(string spriteName, int cost)
    {
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
        highScore = 0;
        ownedSprites.Clear();
        equippedSpriteName = "Default";

        Debug.Log("Player data has been reset.");
    }
    
    // This is the new, crucial part.
    // It ensures data is saved when the application is closing, which is more reliable
    // than relying on the editor's stop button.
    private void OnApplicationQuit()
    {
        SaveData();
    }
}