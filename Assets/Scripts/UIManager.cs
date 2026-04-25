using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject pausePanel;
    public GameObject confirmationPanel;
    public WinPanelUI winPanel;

    [Header("Confirmation Text")]
    public TMPro.TextMeshProUGUI confirmationText;

    private string confirmationTarget;
    private bool _gameOver;

    void Awake()
    {
        ResolveWinPanel();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        
    }

    void Update()
    {
        if (_gameOver) return;

        if (Input.GetKeyDown(KeyCode.Escape) && !mainMenuPanel.activeSelf)
        {
            bool isCurrentlyPaused = pausePanel.activeSelf;
            TogglePause(!isCurrentlyPaused);
        }
    }

    public void TogglePause(bool isPaused)
    {
        pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void ResumeGame()
    {
        TogglePause(false);
    }

 

    private void ResetAllPanels()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        pausePanel.SetActive(false);
        confirmationPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void ShowWin(int winnerIndex)
    {
        if (_gameOver) return;
        _gameOver = true;

        ResolveWinPanel();

        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);
        confirmationPanel.SetActive(false);

        if (winPanel != null) winPanel.ShowWinner(winnerIndex);

        foreach (var pc in Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            pc.enabled = false;
    }

    void ResolveWinPanel()
    {
        if (winPanel != null && winPanel.gameObject != null && winPanel.gameObject.scene.IsValid() && winPanel.gameObject.scene == gameObject.scene)
            return;

        winPanel = null;

        var panels = Object.FindObjectsByType<WinPanelUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        // Prefer same scene first (avoids picking a prefab-stage or DDOL panel).
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] == null) continue;
            if (!panels[i].gameObject.scene.IsValid()) continue;
            if (panels[i].gameObject.scene != gameObject.scene) continue;
            winPanel = panels[i];
            break;
        }

        // Fallback: any scene panel.
        if (winPanel == null)
        {
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] == null) continue;
                if (!panels[i].gameObject.scene.IsValid()) continue;
                winPanel = panels[i];
                break;
            }
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _gameOver = false;
        Time.timeScale = 1f;
        ResolveWinPanel();
        if (winPanel != null) winPanel.gameObject.SetActive(false);
    }
}
