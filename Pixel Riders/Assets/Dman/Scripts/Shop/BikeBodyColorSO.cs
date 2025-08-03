using UnityEngine;

[CreateAssetMenu(fileName = "NewBikeBodySprite", menuName = "Bike/Body Sprite")]
public class BikeBodySpriteSO : ScriptableObject
{
    public string spriteName;
    public Sprite bodySprite;
    public int cost;
    public bool isDefault; // To identify the starting sprite
}