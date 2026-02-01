using UnityEngine;
using UnityEngine.UI;

public class DecayOverlayUI : MonoBehaviour
{
    [Tooltip("Snatcher zone this overlay represents (1-4).")]
    public int zone = 1;

    [Tooltip("Max alpha in 0-255 range.")]
    [Range(0, 255)] public int maxAlpha = 32;

    public SnatcherManager snatcherManager;
    public Image overlayImage;

    void Awake()
    {
        if (snatcherManager == null)
        {
            snatcherManager = FindFirstObjectByType<SnatcherManager>(FindObjectsInactive.Include);
        }

        if (overlayImage == null) overlayImage = GetComponent<Image>();
    }

    void Update()
    {
        if (snatcherManager == null || overlayImage == null) return;

        var slot = snatcherManager.GetSlotByZone(zone);
        if (slot == null)
        {
            SetAlpha(0f);
            return;
        }

        if (!slot.isAlive && snatcherManager.CanRespawn(zone))
        {
            SetAlpha(maxAlpha / 255f);
            return;
        }

        if (slot.currentHost == null || !slot.isAlive)
        {
            SetAlpha(0f);
            return;
        }

        float max = Mathf.Max(0.0001f, snatcherManager.hostDecaySeconds);
        float t = Mathf.Clamp01(1f - (slot.hostTimeRemaining / max));
        float alpha = (maxAlpha / 255f) * t;
        SetAlpha(alpha);
    }

    private void SetAlpha(float alpha)
    {
        var c = overlayImage.color;
        c.a = alpha;
        overlayImage.color = c;
    }
}
