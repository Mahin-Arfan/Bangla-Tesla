using UnityEngine;
using UnityEngine.SceneManagement;

public class UIScript : MonoBehaviour
{
    public GameObject mainMenuUI;
    public GameObject endMenuUI;
    public GameObject inputUI;
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

    public void ExitGame()
    {
        Application.Quit();

        // For testing in Editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void HomeButton()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
