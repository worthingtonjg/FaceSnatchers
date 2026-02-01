using UnityEngine;
using TMPro;

public class MatchEndUI : MonoBehaviour
{
    public SnatcherManager snatcherManager;
    public GameObject root;
    public TMP_Text winnerText;

    void Awake()
    {
        if (snatcherManager == null)
        {
            snatcherManager = FindFirstObjectByType<SnatcherManager>(FindObjectsInactive.Include);
        }
    }

    void Update()
    {
        if (snatcherManager == null || root == null) return;

        if (snatcherManager.matchEnded)
        {
            if (!root.activeSelf) root.SetActive(true);
            if (winnerText != null)
            {
                winnerText.text = $"Winner: Zone {snatcherManager.winningZone}";
            }
        }
        else
        {
            if (root.activeSelf) root.SetActive(false);
        }
    }
}
