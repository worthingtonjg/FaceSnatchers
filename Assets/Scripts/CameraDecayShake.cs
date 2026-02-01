using UnityEngine;

[DefaultExecutionOrder(100)]
public class CameraDecayShake : MonoBehaviour
{
    [Tooltip("Snatcher zone this camera represents (1-4).")]
    public int zone = 1;

    [Header("Trigger")]
    [Range(0f, 1f)] public float startAtPercent = 0.5f;

    [Header("Shake")]
    public float positionAmplitude = 0.08f;
    public float frequency = 8f;

    public SnatcherManager snatcherManager;

    private Vector3 _lastOffset;
    private float _seed;

    void Awake()
    {
        if (snatcherManager == null)
        {
            snatcherManager = FindFirstObjectByType<SnatcherManager>(FindObjectsInactive.Include);
        }
        _seed = Random.value * 1000f;
    }

    void LateUpdate()
    {
        if (snatcherManager == null) return;

        var slot = snatcherManager.GetSlotByZone(zone);
        if (slot == null || !slot.isAlive || slot.currentHost == null)
        {
            ClearShake();
            return;
        }

        float max = Mathf.Max(0.0001f, snatcherManager.hostDecaySeconds);
        float remaining = slot.hostTimeRemaining;
        float ratio = remaining / max;

        if (ratio > startAtPercent)
        {
            ClearShake();
            return;
        }

        // Remove previous offset to avoid drift.
        transform.position -= _lastOffset;

        float t = Time.time * frequency;
        float x = (Mathf.PerlinNoise(_seed, t) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(_seed + 10f, t) - 0.5f) * 2f;

        _lastOffset = new Vector3(x, y, 0f) * positionAmplitude;
        transform.position += _lastOffset;
    }

    private void ClearShake()
    {
        if (_lastOffset.sqrMagnitude > 0f)
        {
            transform.position -= _lastOffset;
            _lastOffset = Vector3.zero;
        }
    }
}
