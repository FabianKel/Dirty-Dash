using UnityEngine;

public class TooltipFollower : MonoBehaviour
{
    [HideInInspector] public Transform target;
    [HideInInspector] public Vector3 offset;

    private Canvas canvas;

    void Start()
    {
        canvas = GetComponentInChildren<Canvas>();
    }

    void Update()
    {
        if (target == null) return;

        Vector3 worldPos = target.position + offset;
        transform.position = worldPos;

        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            canvas.transform.position = worldPos;
    }
}