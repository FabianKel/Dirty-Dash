using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "DirtyDash/Character")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public Sprite lobbySprite;     // El que se ve en la selección
    public Sprite ingameSprite;    // El sprite estático para la demo

    [Header("Futuro - Animaciones")]
    public RuntimeAnimatorController animatorController;
}