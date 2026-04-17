using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigationManager : MonoBehaviour
{
    [Header("Configuración de Escenas")]
    public string gameSceneName = "MainGame";
    public string mainMenuSceneName = "MainMenu";

    public void GoToGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

}