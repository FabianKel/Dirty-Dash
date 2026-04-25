using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class WinGoal : MonoBehaviour
{
    public bool disableOnWin = true;

    bool _triggered;

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Handle(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        Handle(other);
    }

    void Handle(Collider2D other)
    {
        if (_triggered) return;

        var player = other.GetComponent<PlayerController>();
        if (player == null) player = other.GetComponentInParent<PlayerController>();
        if (player == null && other.attachedRigidbody != null)
            player = other.attachedRigidbody.GetComponent<PlayerController>();
        if (player == null) return;

        UIManager ui = null;
        var allUi = Object.FindObjectsByType<UIManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var myScene = gameObject.scene.IsValid() ? gameObject.scene : SceneManager.GetActiveScene();

        for (int i = 0; i < allUi.Length; i++)
        {
            if (allUi[i] != null && allUi[i].gameObject.scene == myScene)
            {
                ui = allUi[i];
                break;
            }
        }

        if (ui == null && allUi.Length > 0) ui = allUi[0];
        if (ui == null)
        {
            Debug.LogWarning("WinGoal: UIManager no encontrado en escena.");
            return;
        }

        _triggered = true;
        ui.ShowWin(player.playerIndex);

        if (disableOnWin) gameObject.SetActive(false);
    }
}
