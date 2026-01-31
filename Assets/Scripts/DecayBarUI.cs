using UnityEngine;
using UnityEngine.UI;

public class DecayBarUI : MonoBehaviour
{
    [System.Serializable]
    public class SlotBar
    {
        [Tooltip("Snatcher zone this bar represents (1-4).")]
        public int zone = 1;
        public Image fillImage;
    }

    public SnatcherManager snatcherManager;
    public SlotBar[] bars = new SlotBar[4];

    void Awake()
    {
        if (snatcherManager == null)
        {
            snatcherManager = FindFirstObjectByType<SnatcherManager>(FindObjectsInactive.Include);
        }
    }

    void Update()
    {
        if (snatcherManager == null || bars == null) return;

        float max = Mathf.Max(0.0001f, snatcherManager.hostDecaySeconds);

        for (int i = 0; i < bars.Length; i++)
        {
            var bar = bars[i];
            if (bar == null || bar.fillImage == null) continue;

            var slot = snatcherManager.GetSlotByZone(bar.zone);
            if (slot == null || !slot.isAlive || slot.currentHost == null)
            {
                bar.fillImage.fillAmount = 0f;
                continue;
            }

            bar.fillImage.fillAmount = Mathf.Clamp01(slot.hostTimeRemaining / max);
        }
    }
}
