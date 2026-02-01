using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

public class MatchEndUI : MonoBehaviour
{
    public SnatcherManager snatcherManager;
    public GameObject root;
    public TMP_Text winnerText;
    public TMP_Text scoresText;
    [Header("Colors")]
    public Color redColor = Color.red;
    public Color yellowColor = Color.yellow;
    public Color greenColor = Color.green;
    public Color blueColor = Color.blue;

    void Awake()
    {
        if (snatcherManager == null)
        {
            snatcherManager = FindFirstObjectByType<SnatcherManager>(FindObjectsInactive.Include);
        }
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    void Update()
    {
        if (root == null) return;
        if (snatcherManager == null)
        {
            if (root.activeSelf) root.SetActive(false);
            return;
        }

        if (snatcherManager.matchStarted && snatcherManager.matchEnded)
        {
            if (!root.activeSelf) root.SetActive(true);
            if (winnerText != null)
            {
                string winnerName = GetColoredName(snatcherManager.winningZone);
                winnerText.text = $"Winner: {winnerName}";
            }
            if (scoresText != null)
            {
                scoresText.text = BuildSortedScoresText();
            }
        }
        else
        {
            if (root.activeSelf) root.SetActive(false);
        }
    }

    private string GetColoredName(int zone)
    {
        string name = snatcherManager.GetZoneName(zone);
        Color color = zone switch
        {
            1 => redColor,
            2 => yellowColor,
            3 => greenColor,
            4 => blueColor,
            _ => Color.white
        };

        string hex = ColorUtility.ToHtmlStringRGBA(color);
        return $"<color=#{hex}>{name}</color>";
    }

    private string BuildSortedScoresText()
    {
        var entries = new List<(int zone, int score)>
        {
            (1, snatcherManager.GetClaimCountForZone(1)),
            (2, snatcherManager.GetClaimCountForZone(2)),
            (3, snatcherManager.GetClaimCountForZone(3)),
            (4, snatcherManager.GetClaimCountForZone(4))
        };

        entries.Sort((a, b) => b.score.CompareTo(a.score));

        var sb = new StringBuilder();
        for (int i = 0; i < entries.Count; i++)
        {
            if (i > 0) sb.Append('\n');
            sb.Append(GetColoredName(entries[i].zone));
            sb.Append(": ");
            sb.Append(entries[i].score);
        }

        return sb.ToString();
    }
}
