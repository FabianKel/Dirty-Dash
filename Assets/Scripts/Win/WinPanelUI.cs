using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class WinPanelUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI winnerText;
    public Button restartButton;
    public Button exitButton;
    public Button nextLevelButton;        

    [Header("Scenes")]
    public string mainMenuSceneName = "MainMenu";

    bool _wired;
    bool _showRequested;

    void Awake()
    {
        WireButtons();

        var managers = Object.FindObjectsByType<UIManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < managers.Length; i++)
        {
            if (managers[i] == null) continue;
            if (managers[i].winPanel != null) continue;
            if (managers[i].gameObject.scene.IsValid() && gameObject.scene.IsValid() && managers[i].gameObject.scene != gameObject.scene)
                continue;
            managers[i].winPanel = this;
        }

        if (!_showRequested && gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    void OnEnable()
    {
        WireButtons();
    }

    public void ShowWinner(int winnerIndex)
    {
        _showRequested = true;

        if (winnerText) winnerText.text = $"Player {winnerIndex} gana!";
        SceneAudioController.Instance?.PlayUIOpen();
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        if (nextLevelButton != null)
        {
            bool hasNextLevel = GetNextLevelIndex() != -1;
            nextLevelButton.gameObject.SetActive(hasNextLevel);
        }

        Time.timeScale = 0f;
    }

    void WireButtons()
    {
        if (_wired) return;
        _wired = true;

        if (restartButton)   restartButton.onClick.AddListener(Restart);
        if (exitButton)      exitButton.onClick.AddListener(ExitToMenu);
        if (nextLevelButton) nextLevelButton.onClick.AddListener(NextLevel);
    }

    void Restart()
    {
        SceneAudioController.Instance?.PlayUIClose();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ExitToMenu()
    {
        SceneAudioController.Instance?.PlayUIClose();
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    void NextLevel()
    {
        int next = GetNextLevelIndex();
        if (next == -1) return;

        SceneAudioController.Instance?.PlayUIClose();
        Time.timeScale = 1f;
        SceneManager.LoadScene(next);
    }

    int GetNextLevelIndex()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        int next    = current + 1;

        if (next < SceneManager.sceneCountInBuildSettings)
            return next;

        return -1; 
    }
}
