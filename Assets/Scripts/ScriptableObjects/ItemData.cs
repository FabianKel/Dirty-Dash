using UnityEngine;

[CreateAssetMenu(fileName = "NuevoItem", menuName = "DirtyDash/Item")]
public class ItemData : ScriptableObject
{
    [Header("Información Básica")]
    public string itemName;
    public Sprite itemSprite;
    [TextArea] public string description;

    [Header("Combate y Salud (Opcional)")]
    [Tooltip("Cantidad de daño que inflige. Dejar en 0 si no hace daño.")]
    public int damage = 0;

    [Tooltip("Cantidad de vida que restaura. Dejar en 0 si no cura.")]
    public int heal = 0;

    [Header("Efectos y Acciones (Opcional)")]
    [Tooltip("Nombre técnico del efecto (ej: 'Stun', 'Slow', 'SpeedUp').")]
    public string effectName;
    public float effectDuration = 0f;

    [Tooltip("Define qué hace el objeto (ej: 'Tirar', 'ColocarTrampa', 'Escudo').")]
    public string actionType;

    // Métodos de utilidad para saber qué tipo de item es
    public bool IsHealingItem => heal > 0;
    public bool IsDamageItem => damage > 0;
    public bool HasEffect => !string.IsNullOrEmpty(effectName);
}