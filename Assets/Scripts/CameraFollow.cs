using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target; 
    public float smoothing = 5f;
    public Vector3 offset = new Vector3(0, 0, -10);

    void LateUpdate() 
    {
        if (target == null) return;
        Vector3 targetPosition = target.position + offset;

        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothing * Time.deltaTime);
    }
}