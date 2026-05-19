using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigationManager : MonoBehaviour
{
    [Header("Configuración de Escenas")]
    public string level1SceneName = "MainGame";
    public string level2SceneName = "MainGame_Level2";
    public string level3SceneName = "MainGame_Level3";
    public string mainMenuSceneName = "MainMenu";

    [Header("Fallback (si no existe GameDataManager)")]
    [Range(1, 3)] public int selectedLevel = 1;

    public void SelectLevel1() => SelectLevel(1);
    public void SelectLevel2() => SelectLevel(2);
    public void SelectLevel3() => SelectLevel(3);

    public void SelectLevel(int level)
    {
        int clamped = Mathf.Clamp(level, 1, 3);

        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.selectedLevel = clamped;
        }
        else
        {
            selectedLevel = clamped;
        }

        var ui = FindAnyObjectByType<UIMainMenuManager>();
        if (ui != null)
        {
            ui.ShowCharacterSelect();
        }
    }

    public void GoToGame()
    {
        Time.timeScale = 1f;

        int levelToLoad = selectedLevel;
        if (GameDataManager.Instance != null)
        {
            levelToLoad = GameDataManager.Instance.selectedLevel;
        }

        SceneManager.LoadScene(GetSceneNameForLevel(levelToLoad));
    }

    string GetSceneNameForLevel(int level)
    {
        switch (Mathf.Clamp(level, 1, 3))
        {
            case 2: return level2SceneName;
            case 3: return level3SceneName;
            default: return level1SceneName;
        }
    }
}

