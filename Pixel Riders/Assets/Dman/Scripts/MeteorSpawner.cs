// MeteorSpawner.cs
// Attach this script to an empty GameObject in your scene.
// This script will handle spawning meteors at regular intervals.

using UnityEngine;

public class MeteorSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    [Tooltip("The prefab of the meteor to be spawned.")]
    public GameObject meteorPrefab;
    [Tooltip("The player's transform to track for spawning.")]
    public Transform playerTransform;
    [Tooltip("The radius around the player where meteors can spawn.")]
    public float spawnRadius = 50f;
    [Tooltip("The time in seconds between each meteor spawn.")]
    public float spawnInterval = 5f;

    private const float SPAWN_HEIGHT = 50f;

    private void Start()
    {
        // Find the player object by tag. This is a common way to get a reference.
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            // Begin the spawning process after a short initial delay.
            InvokeRepeating("SpawnMeteor", 1f, spawnInterval);
        }
        else
        {
            Debug.LogError("Player object with tag 'Player' not found! Meteor spawning disabled.");
            enabled = false; // Disable the script if the player isn't found.
        }
    }

    private void SpawnMeteor()
    {
        if (playerTransform == null)
        {
            return;
        }

        // Generate a random position within a circle around the player.
        Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 spawnPosition = new Vector3(playerTransform.position.x + randomCircle.x, SPAWN_HEIGHT, 0);

        // Instantiate the meteor prefab at the calculated position.
        Instantiate(meteorPrefab, spawnPosition, Quaternion.identity);
    }
}