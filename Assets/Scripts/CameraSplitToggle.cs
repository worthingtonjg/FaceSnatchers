using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CameraSplitToggle : MonoBehaviour
{
    [Header("Input")]
    public KeyCode toggleKey = KeyCode.C;

    [Header("Focus")]
    [Tooltip("Zone to show full-screen when toggled (1-4).")]
    public int focusZone = 4;
    public bool autoFindCameras = true;
    public List<FaceSnatcherCamera> cameras = new List<FaceSnatcherCamera>();

    private readonly List<CameraState> _states = new List<CameraState>(4);
    private bool _focused;
    private bool _warnedMissing;

    private struct CameraState
    {
        public int zone;
        public Camera cam;
        public Rect rect;
        public bool enabled;
        public float depth;
    }

    void Awake()
    {
        CacheCameraStates();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }
    }

    private void Toggle()
    {
        _focused = !_focused;
        ApplyFocusState(_focused);
    }

    private void CacheCameraStates()
    {
        _states.Clear();

        if (autoFindCameras)
        {
            cameras.Clear();
            cameras.AddRange(FindObjectsByType<FaceSnatcherCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        }

        for (int i = 0; i < cameras.Count; i++)
        {
            var snatcherCam = cameras[i];
            if (snatcherCam == null) continue;
            if (!snatcherCam.TryGetComponent(out Camera cam)) continue;

            _states.Add(new CameraState
            {
                zone = snatcherCam.zone,
                cam = cam,
                rect = cam.rect,
                enabled = cam.enabled,
                depth = cam.depth
            });
        }
    }

    private void ApplyFocusState(bool focus)
    {
        if (_states.Count == 0)
        {
            CacheCameraStates();
        }

        Camera focusCam = null;
        for (int i = 0; i < _states.Count; i++)
        {
            if (_states[i].zone == focusZone)
            {
                focusCam = _states[i].cam;
                break;
            }
        }

        if (focusCam == null)
        {
            if (!_warnedMissing)
            {
                Debug.LogWarning($"{nameof(CameraSplitToggle)}: No FaceSnatcherCamera found for zone {focusZone}.");
                _warnedMissing = true;
            }
            return;
        }

        if (focus)
        {
            for (int i = 0; i < _states.Count; i++)
            {
                var state = _states[i];
                if (state.cam == null) continue;

                if (state.cam == focusCam)
                {
                    state.cam.enabled = true;
                    state.cam.rect = new Rect(0f, 0f, 1f, 1f);
                }
                else
                {
                    state.cam.enabled = false;
                }
            }
        }
        else
        {
            for (int i = 0; i < _states.Count; i++)
            {
                var state = _states[i];
                if (state.cam == null) continue;

                state.cam.enabled = state.enabled;
                state.cam.rect = state.rect;
                state.cam.depth = state.depth;
            }
        }
    }
}
