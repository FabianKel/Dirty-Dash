using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject pausePanel;
    public GameObject confirmationPanel;

    [Header("Confirmation Text")]
    public TMPro.TextMeshProUGUI confirmationText;

    private string confirmationTarget;

    void Start()
    {
        ShowMainMenu();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !mainMenuPanel.activeSelf)
        {
            bool isCurrentlyPaused = pausePanel.activeSelf;
            TogglePause(!isCurrentlyPaused);
        }
    }

    public void ShowMainMenu()
    {
        ResetAllPanels();
        mainMenuPanel.SetActive(true);
    }

    public void PlayGame()
    {
        ResetAllPanels();
        Debug.Log("Game Started!");
    }

    public void ShowSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
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

    public void RequestQuit()
    {
        confirmationTarget = "Quit";
        confirmationText.text = "Are you sure you want to quit the game?";
        confirmationPanel.SetActive(true);
    }

    public void RequestMainMenu()
    {
        confirmationTarget = "MainMenu";
        confirmationText.text = "Are you sure you want to return to Main Menu?";
        confirmationPanel.SetActive(true);
    }

    public void ConfirmAction()
    {
        if (confirmationTarget == "Quit")
        {
            Application.Quit();
            Debug.Log("Quit Game");
        }
        else if (confirmationTarget == "MainMenu")
        {
            ShowMainMenu();
        }
    }

    public void CancelAction()
    {
        confirmationPanel.SetActive(false);
    }

    private void ResetAllPanels()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        pausePanel.SetActive(false);
        confirmationPanel.SetActive(false);
        Time.timeScale = 1f;
    }
}