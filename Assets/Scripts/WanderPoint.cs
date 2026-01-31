using UnityEngine;

public class WanderPoint : MonoBehaviour
{
    [Tooltip("Optional: allow more than one host to reserve this point (leave at 1 for your rule).")]
    public int capacity = 1;

    private int _reservedCount = 0;

    public bool IsAvailable => _reservedCount < capacity;

    public int zone;

    public bool TryReserve()
    {
        if (!IsAvailable) return false;
        _reservedCount++;
        return true;
    }

    public void Release()
    {
        _reservedCount = Mathf.Max(0, _reservedCount - 1);
    }
}
