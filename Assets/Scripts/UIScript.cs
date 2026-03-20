using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIScript : MonoBehaviour
{
    [Header("Score UI")]
    public TextMeshProUGUI scoreText;      // Drag your current score text here
    public TextMeshProUGUI highScoreText;

    [Header("UI GameObjects")]
    public GameObject mainMenuUI;
    public GameObject endMenuUI;
    public GameObject inputUI;
    public FloatingText[] floatingScorePool;

    private GameManagerScript gameManagerScript;

    void Start()
    {
        gameManagerScript = GetComponent<GameManagerScript>();
        inputUI.SetActive(false);
    }

    public void PlayGame()
    {
        mainMenuUI.SetActive(false);
        inputUI.SetActive(true);
        gameManagerScript.gameStarted = true;
    }
    public void UpdateScoreUI(int currentScore, int currentHighScore)
    {
        if (scoreText != null)
        {
            scoreText.text = "SCORE: " + currentScore.ToString("D5");
        }

        if (highScoreText != null)
        {
            highScoreText.text = "HIGH SCORE: " + currentHighScore.ToString("D5");
        }
    }

    public void SpawnFloatingScore(int pointsAdded)
    {
        foreach (FloatingText ft in floatingScorePool)
        {
            if (!ft.gameObject.activeInHierarchy)
            {
                // We found a free text object! Play it and stop searching.
                ft.SetupAndPlay("+" + pointsAdded.ToString());
                return;
            }
        }
    }

    public void ExitGame()
    {
        Application.Quit();

        // For testing in Editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void GameOver()
    {
        inputUI.SetActive(false);
        endMenuUI.SetActive(true);
    }

    public void HomeButton()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
