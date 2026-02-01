using UnityEngine;
using TMPro;

public class RespawnMessageUI : MonoBehaviour
{
    [Tooltip("Snatcher zone this UI represents (1-4).")]
    public int zone = 1;

    [Tooltip("Shown while waiting to respawn.")]
    public GameObject respawnRoot;

    [Tooltip("Shown when no respawn is possible (game over for this player).")]
    public GameObject gameOverRoot;

    [Tooltip("Optional TMP text to show countdown.")]
    public TMP_Text countdownText;

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
        if (snatcherManager == null) return;

        var slot = snatcherManager.GetSlotByZone(zone);
        if (slot == null) return;

        if (slot.isAlive)
        {
            SetActive(respawnRoot, false);
            SetActive(gameOverRoot, false);
            return;
        }

        bool canRespawn = snatcherManager.CanRespawn(zone);
        SetActive(respawnRoot, canRespawn);
        SetActive(gameOverRoot, !canRespawn);

        if (canRespawn && countdownText != null)
        {
            float t = snatcherManager.GetRespawnTimeRemaining(zone);
            countdownText.text = Mathf.CeilToInt(t).ToString();
        }
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go == null) return;
        if (go.activeSelf != active) go.SetActive(active);
    }
}
