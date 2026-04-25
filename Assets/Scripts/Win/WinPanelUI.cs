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

        // If this panel starts active in scene, hide it on boot.
        // When ShowWinner activates it for the first time (from inactive),
        // _showRequested is set before Awake runs so we do not auto-hide it again.
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

        if (!gameObject.activeSelf) gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    void WireButtons()
    {
        if (_wired) return;
        _wired = true;

        if (restartButton) restartButton.onClick.AddListener(Restart);
        if (exitButton) exitButton.onClick.AddListener(ExitToMenu);
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ExitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
