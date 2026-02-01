using UnityEngine;

public class WanderPoint : MonoBehaviour
{
    [Tooltip("Optional: allow more than one host to reserve this point (leave at 1 for your rule).")]
    public int capacity = 1;

    [Tooltip("Seconds before a reservation auto-releases (0 = never).")]
    public float reserveTimeoutSeconds = 0f;

    private int _reservedCount = 0;
    private float _lastReserveTime = -1f;

    public bool IsAvailable => _reservedCount < capacity;

    public int zone;

    public bool TryReserve()
    {
        if (!IsAvailable) return false;
        _reservedCount++;
        _lastReserveTime = Time.time;
        return true;
    }

    public void Release()
    {
        _reservedCount = Mathf.Max(0, _reservedCount - 1);
        if (_reservedCount == 0) _lastReserveTime = -1f;
    }

    void Update()
    {
        if (reserveTimeoutSeconds <= 0f) return;
        if (_reservedCount <= 0) return;

        if (_lastReserveTime > 0f && Time.time - _lastReserveTime >= reserveTimeoutSeconds)
        {
            _reservedCount = 0;
            _lastReserveTime = -1f;
        }
    }
}
