using System.Collections;
using UnityEngine;
using TMPro; // Use this if you are using TextMeshPro for your UI text

public class TutorialTrigger : MonoBehaviour
{
    
    [Header("Tutorial Content")]
    [Tooltip("The unique message that will be displayed when this specific trigger is activated.")]
    [TextArea(3, 10)] // This makes the text field in the inspector bigger
    [SerializeField] private string tutorialMessage;

    [Header("UI & Timing")]
    [Tooltip("How long this message will stay on screen before disappearing.")]
    [SerializeField] private float displayDuration = 5f;

    // This is the static reference to the single UI text element in the scene.
    private static TextMeshProUGUI tutorialTextElement;
    
    // This flag ensures the trigger only works once
    private bool hasBeenTriggered = false;

    private void Awake()
    {
        // Find the single tutorial text element in the scene ONCE.
        // This is efficient because we only search for it if we don't already have a reference.
        if (tutorialTextElement == null)
        {
            GameObject textObject = GameObject.FindGameObjectWithTag("TutorialText");
            if (textObject != null)
            {
                tutorialTextElement = textObject.GetComponent<TextMeshProUGUI>();
                // Hide it at the very start of the game
                tutorialTextElement.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError("No GameObject with the tag 'TutorialText' was found in the scene. Please create one.");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object entering is the player AND if this trigger has not been used yet
        if (other.CompareTag("Player") && !hasBeenTriggered)
        {
            // Set the flag to true so this can't run again
            hasBeenTriggered = true;
            
            // Start the process of showing our unique message
            StartCoroutine(ShowTutorialCoroutine());
        }
    }

    private IEnumerator ShowTutorialCoroutine()
    {
        if (tutorialTextElement != null)
        {
            // Set the UI text to this trigger's specific message
            tutorialTextElement.text = tutorialMessage;
            
            // Show the text
            tutorialTextElement.gameObject.SetActive(true);

            // Wait for the specified duration
            yield return new WaitForSeconds(displayDuration);

            // Hide the text again
            tutorialTextElement.gameObject.SetActive(false);
        }
    }
}
