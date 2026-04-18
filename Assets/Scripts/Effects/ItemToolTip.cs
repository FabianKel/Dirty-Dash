using UnityEngine;
using TMPro;
using System.Collections;

public class ItemTooltip : MonoBehaviour
{
    [Header("Tooltip")]
    [Tooltip("Mensaje que aparece cuando el jugador pasa sobre el item")]
    public string message = "¡Efecto aplicado!";

    [Tooltip("Cuántos segundos se muestra el mensaje")]
    public float displayDuration = 2f;

    [Tooltip("Qué tan arriba del jugador aparece el texto")]
    public Vector3 offset = new Vector3(0.5f, 1.5f, 0f);

    [Header("Prefab")]
    [Tooltip("Prefab con un TextMeshPro en World Space")]
    public GameObject tooltipPrefab;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        ShowTooltip(other.transform);
    }

    void ShowTooltip(Transform playerTransform)
    {
        if (tooltipPrefab == null)
        {
            Debug.LogWarning("ItemTooltip: falta asignar tooltipPrefab.");
            return;
        }

        // Instanciar junto al jugador
        Vector3 spawnPos = playerTransform.position + offset;
        GameObject tooltipGO = Instantiate(tooltipPrefab, spawnPos, Quaternion.identity);
        TooltipFollower follower = tooltipGO.GetComponent<TooltipFollower>();
        if (follower != null)
        {
            follower.target = playerTransform;
            follower.offset = offset;
        }

        // Asignar texto
        TextMeshProUGUI tmp = tooltipGO.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text = message;

        // Destruir después de displayDuration segundos
        Destroy(tooltipGO, displayDuration);
    }
}