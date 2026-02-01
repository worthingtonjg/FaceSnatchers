using UnityEngine;
using TMPro;

public class RemainingHostsUI : MonoBehaviour
{
    public SnatcherManager snatcherManager;
    public TMP_Text label;
    public string prefix = "Hosts Remaining: ";

    void Awake()
    {
        if (snatcherManager == null)
        {
            snatcherManager = FindFirstObjectByType<SnatcherManager>(FindObjectsInactive.Include);
        }
    }

    void Update()
    {
        if (snatcherManager == null || label == null) return;
        int remaining = snatcherManager.GetRemainingUnclaimedHostCount();
        label.text = $"{prefix}{remaining}";
    }
}
