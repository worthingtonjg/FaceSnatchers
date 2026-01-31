using UnityEngine;

[DisallowMultipleComponent]
public class HostState : MonoBehaviour
{
    [Tooltip("Zone this host belongs to (1-4).")]
    public int zone;

    [Tooltip("Zone of the snatcher currently possessing this host (0 = none).")]
    public int currentSnatcherZone;

    [Tooltip("Zone of the snatcher who last claimed this host (0 = none).")]
    public int claimedByZone;

    public bool isDead;

    public bool IsPossessed => currentSnatcherZone != 0;
    public bool IsClaimed => claimedByZone != 0;

    public void SetPossessed(int snatcherZone)
    {
        currentSnatcherZone = snatcherZone;
    }

    public void ClearPossessed()
    {
        currentSnatcherZone = 0;
    }

    public void SetClaimed(int snatcherZone)
    {
        claimedByZone = snatcherZone;
    }
}
