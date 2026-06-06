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
        ResetAllPanels();
        mainMenuPanel.SetActive(true);
    }

    // --- Navegación Principal ---
    public void ShowMainMenu()
    {
        bool returningFromSubmenu = IsAnySubmenuOpen();
        ResetAllPanels();
        if (returningFromSubmenu)
            SceneAudioController.Instance?.PlayUIClose();
        mainMenuPanel.SetActive(true);
    }

    public void ShowLevelSelect()                 
    {
        ResetAllPanels();
        SceneAudioController.Instance?.PlayUIOpen();
        levelSelectPanel.SetActive(true);
    }

    public void ShowCharacterSelect()
    {
        ResetAllPanels();
        SceneAudioController.Instance?.PlayUIOpen();
        characterSelectPanel.SetActive(true);
    }

    public void ShowSettings()
    {
        ResetAllPanels();
        SceneAudioController.Instance?.PlayUIOpen();
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        ResetAllPanels();
        SceneAudioController.Instance?.PlayUIClose();
        mainMenuPanel.SetActive(true);
    }

    public void RequestQuit()
    {
        ResetAllPanels();
        confirmationTarget = "Quit";
        confirmationText.text = "Are you sure you want to quit?";
        SceneAudioController.Instance?.PlayUIOpen();
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
        ShowMainMenu();
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

    bool IsAnySubmenuOpen()
    {
        return (settingsPanel != null && settingsPanel.activeSelf)
            || (characterSelectPanel != null && characterSelectPanel.activeSelf)
            || (confirmationPanel != null && confirmationPanel.activeSelf)
            || (levelSelectPanel != null && levelSelectPanel.activeSelf);
    }
}
