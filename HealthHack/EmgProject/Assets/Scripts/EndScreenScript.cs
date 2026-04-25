using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndScreenScript : MonoBehaviour
{
    [Tooltip("TMP Text element that displays the custom message and final score")]
    public TMP_Text messageText;
    
    [Tooltip("Button that clears the screen and returns to the form")]
    public Button nextButton;

    void Start()
    {
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextClicked);
        }
    }

    public void SetupScreen(int finalScore)
    {
        if (messageText != null)
        {
            messageText.text = $"Session Complete!\n\nYour Final Score: {finalScore}\n\nThank you for your patience and for doing your absolute best during this therapy session.";
        }
    }

    private void OnNextClicked()
    {
        if (GameManager.Instance != null)
        {
            // Triggers resetting the score directly inside the GameManager logic and hides this UI
            GameManager.Instance.RestartToForm();
        }
        else
        {
            Debug.LogWarning("[EndScreenScript] GameManager missing, cannot restart properly.");
        }
    }
}
