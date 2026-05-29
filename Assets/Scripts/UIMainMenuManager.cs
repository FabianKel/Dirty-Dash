using UnityEngine;
using UnityEngine.SceneManagement;

public class UIMainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject characterSelectPanel;
    public GameObject confirmationPanel;
    public GameObject levelSelectPanel;       

    [Header("Confirmation Text")]
    public TMPro.TextMeshProUGUI confirmationText;

    private string confirmationTarget;

    void Start()
    {
        ShowMainMenu();
    }

    // --- Navegación Principal ---
    public void ShowMainMenu()
    {
        ResetAllPanels();
        mainMenuPanel.SetActive(true);
    }

    public void ShowLevelSelect()                 
    {
        ResetAllPanels();
        levelSelectPanel.SetActive(true);
    }

    public void ShowCharacterSelect()
    {
        ResetAllPanels();
        characterSelectPanel.SetActive(true);
    }

    public void ShowSettings()
    {
        ResetAllPanels();
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        ResetAllPanels();
        mainMenuPanel.SetActive(true);
    }

    public void RequestQuit()
    {
        ResetAllPanels();
        confirmationTarget = "Quit";
        confirmationText.text = "Are you sure you want to quit?";
        confirmationPanel.SetActive(true);
    }

    public void ConfirmAction()
    {
        if (confirmationTarget == "Quit")
        {
            Debug.Log("Quitting game...");
            Application.Quit();
        }
    }

    public void CancelAction()
    {
        ResetAllPanels();
        confirmationPanel.SetActive(false);
    }

    // --- Utilidades ---
    private void ResetAllPanels()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        characterSelectPanel.SetActive(false);
        confirmationPanel.SetActive(false);
        levelSelectPanel.SetActive(false);    
        Time.timeScale = 1f;
    }
}