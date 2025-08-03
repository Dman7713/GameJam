using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// This script handles playing hover and click sound effects for all buttons on a Canvas.
/// It finds all Button components within the Canvas and adds an EventTrigger for hover sounds.
/// Click sounds are handled by directly subscribing to each button's onClick event.
/// The sounds are played through a single AudioSource on the Canvas itself.
/// </summary>
public class ButtonSoundEffects : MonoBehaviour
{
    // The AudioClips to be played for the hover and click events.
    public AudioClip hoverSound;
    public AudioClip clickSound;

    // The AudioSource component on the Canvas that will play the sounds.
    // This must be manually assigned in the Inspector.
    public AudioSource audioSource;
    
    // The delay in seconds before the button's original action is executed after a click.
    // This gives the click sound time to play. Adjust this value in the Inspector.
    public float clickSoundDelay = 0.2f;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// This is where we find all the buttons and set up their event triggers.
    /// </summary>
    void Start()
    {
        // Check if an AudioSource has been assigned.
        if (audioSource == null)
        {
            Debug.LogError("ButtonSoundEffects: AudioSource is not assigned. Please assign it in the Inspector.");
            return;
        }

        // Find all Button components that are children of this GameObject (the Canvas).
        List<Button> buttons = new List<Button>(GetComponentsInChildren<Button>());

        // Iterate through each button and set up the event triggers.
        foreach (Button button in buttons)
        {
            // Get the EventTrigger component. If it doesn't exist, add it.
            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }

            // Create and add the PointerEnter event for the hover sound.
            EventTrigger.Entry entryHover = new EventTrigger.Entry();
            entryHover.eventID = EventTriggerType.PointerEnter;
            // The listener calls our OnHoverEnter method.
            entryHover.callback.AddListener((data) => { OnHoverEnter(); });
            trigger.triggers.Add(entryHover);

            // Now, we need to handle the button's original click functionality.
            // We'll store the original listeners, clear them, and add our own delayed action.
            
            // This is a temporary list to hold all the actions subscribed to the button's onClick event.
            // We use a List to handle cases where a button has multiple functions assigned to it.
            List<UnityAction> originalActions = new List<UnityAction>();
            for(int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                // We're capturing the method and target for each persistent listener.
                var target = button.onClick.GetPersistentTarget(i);
                var methodName = button.onClick.GetPersistentMethodName(i);
                originalActions.Add(() =>
                {
                    // Find the component and method and invoke it.
                    // This is a simple but functional way to re-trigger the original action.
                    Component component = target as Component;
                    if (component != null)
                    {
                        component.SendMessage(methodName, SendMessageOptions.DontRequireReceiver);
                    }
                });
            }

            // Clear the button's original listeners.
            button.onClick.RemoveAllListeners();

            // Add our new listener, which will play the sound and then trigger the original actions.
            button.onClick.AddListener(() => StartCoroutine(PlayClickAndExecute(originalActions)));
        }
    }
    
    /// <summary>
    /// Coroutine to play the click sound and then execute the button's original actions after a delay.
    /// </summary>
    /// <param name="originalActions">A list of the original UnityAction callbacks to execute.</param>
    private IEnumerator PlayClickAndExecute(List<UnityAction> originalActions)
    {
        // Play the click sound.
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        // Wait for the specified delay.
        yield return new WaitForSeconds(clickSoundDelay);

        // Execute all the original button actions.
        foreach (UnityAction action in originalActions)
        {
            action.Invoke();
        }
    }

    /// <summary>
    /// Plays the hover sound through the single AudioSource.
    /// This method is called by the EventTrigger on each button.
    /// </summary>
    public void OnHoverEnter()
    {
        // Check if a hover sound has been assigned and the AudioSource is available.
        if (hoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }
    }
}
