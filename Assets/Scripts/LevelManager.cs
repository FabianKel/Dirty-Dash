using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Referencias de Jugadores")]
    public GameObject player1;
    public GameObject player2;

    void Start()
    {
        if (GameDataManager.Instance != null)
        {
            Debug.Log("Asignando personajes seleccionados a los jugadores");
            SetPlayerSprite(player1, GameDataManager.Instance.p1Selected);
            SetPlayerSprite(player2, GameDataManager.Instance.p2Selected);
        }
        else
        {
            Debug.LogWarning("Modo Test: Asignando personajes por defecto");
        }
    }

    void SetPlayerSprite(GameObject playerObj, CharacterData data)
    {
        if (playerObj == null || data == null) {
            Debug.LogError($"¡No se pudo configurar el jugador, playerObj: {playerObj}, data: {data}");
            return;
        };

        SpriteRenderer sRenderer = playerObj.GetComponentInChildren<SpriteRenderer>();
        if (sRenderer != null)
        {
            sRenderer.sprite = data.ingameSprite;
        }

        Animator anim = playerObj.GetComponentInChildren<Animator>();

        if (anim != null && data.animatorController != null)
        {
            anim.runtimeAnimatorController = data.animatorController;
            Debug.Log($"Setup visual completo para {data.characterName}");
        }

        var controller = playerObj.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.selectedCharacter = data;
        }
    }
}