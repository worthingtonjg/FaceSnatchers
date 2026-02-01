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

    [Header("Decay Shake")]
    public bool useDecayShake = true;
    [Range(0f, 1f)] public float decayShakeStartPercent = 0.5f;
    public float decayShakeAmplitude = 0.08f;
    public float decayShakeFrequency = 8f;

    private Vector3 _velocity;
    private Vector3 _shakeOffset;
    private float _shakeSeed;
    private SnatcherManager _snatcherManager;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + (target.rotation * followOffset);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, followSmoothTime);

        if (lookAtTarget)
        {
            transform.rotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        }

        ApplyDecayShake();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void ApplyDecayShake()
    {
        if (!useDecayShake) return;

        if (_snatcherManager == null)
        {
            _snatcherManager = FindFirstObjectByType<SnatcherManager>(FindObjectsInactive.Include);
            _shakeSeed = Random.value * 1000f;
        }

        if (_snatcherManager == null) return;
        if (target == null) return;

        var slot = _snatcherManager.GetSlotByZone(zone);
        if (slot == null || !slot.isAlive || slot.currentHost == null)
        {
            ClearShake();
            return;
        }

        float max = Mathf.Max(0.0001f, _snatcherManager.hostDecaySeconds);
        float ratio = slot.hostTimeRemaining / max;

        if (ratio > decayShakeStartPercent)
        {
            ClearShake();
            return;
        }

        // Remove previous offset to avoid drift.
        transform.position -= _shakeOffset;

        float t = Time.time * decayShakeFrequency;
        float x = (Mathf.PerlinNoise(_shakeSeed, t) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(_shakeSeed + 10f, t) - 0.5f) * 2f;
        _shakeOffset = new Vector3(x, y, 0f) * decayShakeAmplitude;
        transform.position += _shakeOffset;
    }

    private void ClearShake()
    {
        if (_shakeOffset.sqrMagnitude > 0f)
        {
            transform.position -= _shakeOffset;
            _shakeOffset = Vector3.zero;
        }
    }
}
