using System.Collections;
using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class GameStartCountdown : MonoBehaviour
{
    [Header("Countdown")]
    public int countdownSeconds = 3;
    public string readyMessage = "Get Ready...";
    public float readyMessageDuration = 1f;

    [Header("UI")]
    [Tooltip("Root object to enable/disable during countdown.")]
    public GameObject uiRoot;
    public TMP_Text tmpText;

    [Header("Spawn")]
    public HostSpawner hostSpawner;

    private void Start()
    {
        if (hostSpawner == null)
        {
            hostSpawner = FindFirstObjectByType<HostSpawner>(FindObjectsInactive.Include);
        }

        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        if (uiRoot != null) uiRoot.SetActive(true);

        SetMessage(readyMessage);
        if (readyMessageDuration > 0f)
        {
            yield return new WaitForSeconds(readyMessageDuration);
        }

        for (int i = countdownSeconds; i > 0; i--)
        {
            SetMessage(i.ToString());
            yield return new WaitForSeconds(1f);
        }

        SetMessage("Go!");
        yield return new WaitForSeconds(0.35f);

        if (uiRoot != null) uiRoot.SetActive(false);

        if (hostSpawner != null)
        {
            hostSpawner.Spawn();
        }
        else
        {
            Debug.LogWarning($"{nameof(GameStartCountdown)}: No HostSpawner found to start the game.");
        }
    }

    private void SetMessage(string value)
    {
#if TMP_PRESENT
        if (tmpText != null) tmpText.text = value;
#endif
        if (tmpText != null) tmpText.text = value;
    }
}
