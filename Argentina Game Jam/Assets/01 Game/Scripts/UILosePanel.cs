using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UILosePanel : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text messageText;

    private void Awake()
    {
        Hide();
    }

    public void Show(string message)
    {
        if (titleText != null) titleText.text = "GAME OVER";
        if (messageText != null) messageText.text = string.IsNullOrWhiteSpace(message)
            ? "You lost."
            : message;

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void OnRetryPressed()
    {
        Hide();
        LevelTransitionManager.Instance.RetryLevelFromPanel();
    }

    public void OnMainMenuPressed()
    {
        Hide();
        SceneManager.LoadScene(0);
    }
}

