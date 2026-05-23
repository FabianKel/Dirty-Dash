using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private bool isActivated = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isActivated) return;

        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            player.SetCheckpoint(transform.position);
            UIManager uiManager = Object.FindFirstObjectByType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowCheckpointReached(player.playerIndex);
            }

            isActivated = true;
        }
    }
}