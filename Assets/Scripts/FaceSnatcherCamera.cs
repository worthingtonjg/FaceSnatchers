using UnityEngine;

[DisallowMultipleComponent]
public class FaceSnatcherCamera : MonoBehaviour
{
    [Header("Assignment")]
    [Tooltip("Zone this camera belongs to (1-4). Used to pick a host in the same zone.")]
    public int zone = 1;

    [Header("Follow")]
    public Transform target;
    public Vector3 followOffset = new Vector3(0f, 2.5f, -4f);
    public float followSmoothTime = 0.15f;
    public bool lookAtTarget = true;

    private Vector3 _velocity;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + (target.rotation * followOffset);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, followSmoothTime);

        if (lookAtTarget)
        {
            transform.rotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
