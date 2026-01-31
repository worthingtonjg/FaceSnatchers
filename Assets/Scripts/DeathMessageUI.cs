using UnityEngine;

public class DeathMessageUI : MonoBehaviour
{
    [Tooltip("Snatcher zone this message represents (1-4).")]
    public int zone = 1;

    [Tooltip("Root GameObject for the death message UI.")]
    public GameObject messageRoot;

    public SnatcherManager snatcherManager;

    void Awake()
    {
        if (snatcherManager == null)
        {
            snatcherManager = FindFirstObjectByType<SnatcherManager>(FindObjectsInactive.Include);
        }
    }

    void Update()
    {
        if (snatcherManager == null || messageRoot == null) return;

        var slot = snatcherManager.GetSlotByZone(zone);
        bool show = slot != null && !slot.isAlive;
        if (messageRoot.activeSelf != show)
        {
            messageRoot.SetActive(show);
        }
    }
}
