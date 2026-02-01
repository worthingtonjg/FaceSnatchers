using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public class CanvasAlphaFader : MonoBehaviour
{
    [Header("Target")]
    public Graphic target;

    [Header("Fade")]
    [Tooltip("Alpha change per second.")]
    public float fadeSpeed = 1f;
    public bool fadeOnEnable = true;
    public bool fadeIn = true;
    public bool disableWhenInvisible = false;

    private float _targetAlpha;

    private void Awake()
    {
        if (target == null)
        {
            target = GetComponent<Graphic>();
        }
    }

    private void OnEnable()
    {
        if (fadeOnEnable)
        {
            StartFade(fadeIn);
        }
    }

    private void Update()
    {
        if (target == null) return;

        float current = target.color.a;
        if (Mathf.Approximately(current, _targetAlpha)) return;

        float next = Mathf.MoveTowards(current, _targetAlpha, fadeSpeed * Time.deltaTime);
        SetAlpha(next);

        if (disableWhenInvisible && Mathf.Approximately(next, 0f))
        {
            gameObject.SetActive(false);
        }
    }

    public void StartFade(bool toVisible)
    {
        _targetAlpha = toVisible ? 1f : 0f;
    }

    public void SetImmediate(bool visible)
    {
        _targetAlpha = visible ? 1f : 0f;
        SetAlpha(_targetAlpha);
    }

    private void SetAlpha(float a)
    {
        Color c = target.color;
        c.a = a;
        target.color = c;
    }
}
