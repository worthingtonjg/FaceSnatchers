using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasAlphaFader))]
public class FadeOutOnStart : MonoBehaviour
{
    private CanvasAlphaFader _fader;

    private void Awake()
    {
        _fader = GetComponent<CanvasAlphaFader>();
    }

    private void Start()
    {
        if (_fader != null)
        {
            _fader.StartFade(false);
        }
    }
}
