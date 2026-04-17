using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public enum SharedXAxisMode { Leader, Average }

    public Transform target;
    public float smoothSpeed = 5f;
    public Vector2 offset = new Vector2(3f, 1f);

    [Header("Split-Screen Sync (optional)")]
    public bool syncXWithOtherPlayer = true;
    public SharedXAxisMode sharedXMode = SharedXAxisMode.Leader;

    [Tooltip("If empty, will try to auto-find Player1/Player2 by name.")]
    public Transform sharedXTargetA;

    [Tooltip("If empty, will try to auto-find Player1/Player2 by name.")]
    public Transform sharedXTargetB;

    [Tooltip("If players are too far apart on X, stop syncing so each camera still sees its player.")]
    public float desyncThreshold = 25f;

    void Awake()
    {
        ResolveSharedTargetsIfNeeded();
    }

    void LateUpdate()
    {
        if (target == null) return;
        ResolveSharedTargetsIfNeeded();

        float desiredX = target.position.x;
        if (syncXWithOtherPlayer && sharedXTargetA != null && sharedXTargetB != null)
        {
            float ax = sharedXTargetA.position.x;
            float bx = sharedXTargetB.position.x;
            if (Mathf.Abs(ax - bx) <= desyncThreshold)
            {
                desiredX = sharedXMode == SharedXAxisMode.Leader
                    ? Mathf.Max(ax, bx)
                    : (ax + bx) * 0.5f;
            }
        }

        Vector3 desired = new Vector3(
            desiredX + offset.x,
            target.position.y + offset.y,
            transform.position.z);
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }

    void ResolveSharedTargetsIfNeeded()
    {
        if (sharedXTargetA != null && sharedXTargetB != null) return;

        var p1 = GameObject.Find("Player1");
        var p2 = GameObject.Find("Player2");
        if (p1 == null || p2 == null) return;

        if (sharedXTargetA == null) sharedXTargetA = p1.transform;
        if (sharedXTargetB == null) sharedXTargetB = p2.transform;
    }
}
