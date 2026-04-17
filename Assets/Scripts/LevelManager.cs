using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Referencias de Jugadores")]
    public GameObject player1;
    public GameObject player2;

    void Start()
    {
        // 1. Verificar que el DataManager exista (el real o el Debug)
        if (GameDataManager.Instance != null)
        {
            SetPlayerSprite(player1, GameDataManager.Instance.p1Selected);
            SetPlayerSprite(player2, GameDataManager.Instance.p2Selected);
        }
        else
        {
            Debug.LogError("No se encontró GameDataManager. ¡Asegúrate de tener el DebugDataManager activo!");
        }
    }

    void SetPlayerSprite(GameObject playerObj, CharacterData data)
    {
        if (playerObj == null || data == null) return;

        // Buscamos el SpriteRenderer en el hijo (Circle) o en el objeto mismo
        SpriteRenderer sRenderer = playerObj.GetComponentInChildren<SpriteRenderer>();

        if (sRenderer != null)
        {
            sRenderer.sprite = data.ingameSprite;
            Debug.Log($"Asignado sprite {data.characterName} a {playerObj.name}");
        }
        else
        {
            Debug.LogWarning($"No se encontró SpriteRenderer en {playerObj.name} o sus hijos.");
        }
    }
}