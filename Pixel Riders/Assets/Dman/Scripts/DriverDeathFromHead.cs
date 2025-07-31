using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class DriverDeathFromHead : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision) // <--- Corrected method name!
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            Debug.Log("Driver has collided with the ground and is dead.");
            // Make sure GameManager.instance is correctly set up and not null
            if (GameManager.instance != null)
            {
                GameManager.instance.GameOver();
            }
            else
            {
                Debug.LogError("GameManager.instance is null! Cannot call GameOver().");
            }
        }
    }
}