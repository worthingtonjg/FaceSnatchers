using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    [Tooltip("Snatcher zone this score represents (1-4).")]
    public int zone = 1;
    public SnatcherManager snatcherManager;
    public TMP_Text label;
    public string prefix = "Score: ";

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
        int score = snatcherManager.GetClaimCountForZone(zone);
        label.text = $"{prefix}{score}";
    }
}
