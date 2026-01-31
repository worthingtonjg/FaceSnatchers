using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class FaceSnatcherHumanController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4.5f;
    public float turnSpeed = 120f;

    [Header("Mask Shot")]
    public string maskChildName = "Mask";
    [Tooltip("Prefab spawned when firing the mask.")]
    public GameObject maskProjectilePrefab;
    public float maskShootSpeed = 14f;
    public bool followMaskOnShoot = true;
    public float maskLifeSeconds = 6f;

    [Header("Input")]
    public bool useRawInput = true;

    private NavMeshAgent _agent;
    private Transform _maskSpawn;
    private FaceSnatcherCamera _camera;
    private HostState _hostState;
    private SnatcherManager _snatcherManager;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_agent != null)
        {
            _agent.updateRotation = false;
        }
    }

    void Start()
    {
        _maskSpawn = FindChildByName(transform, maskChildName);
        _camera = FindCameraFollowingThis();
        _hostState = GetComponent<HostState>();
        _snatcherManager = FindFirstObjectByType<SnatcherManager>(FindObjectsInactive.Include);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LaunchMask();
        }

        float h = useRawInput ? Input.GetAxisRaw("Horizontal") : Input.GetAxis("Horizontal");
        float v = useRawInput ? Input.GetAxisRaw("Vertical") : Input.GetAxis("Vertical");

        // Horizontal = turn in place; Vertical = move forward/back
        if (Mathf.Abs(h) > 0.001f)
        {
            transform.Rotate(0f, h * turnSpeed * Time.deltaTime, 0f);
        }

        if (Mathf.Abs(v) <= 0.001f)
        {
            if (_agent != null) _agent.velocity = Vector3.zero;
            return;
        }

        Vector3 move = transform.forward * (v * moveSpeed);

        if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
        {
            _agent.Move(move * Time.deltaTime);
        }
        else
        {
            transform.position += move * Time.deltaTime;
        }
    }

    private void LaunchMask()
    {
        if (maskProjectilePrefab == null)
        {
            Debug.LogWarning($"{nameof(FaceSnatcherHumanController)} on '{name}': maskProjectilePrefab is not assigned.");
            return;
        }

        Transform spawn = _maskSpawn != null ? _maskSpawn : transform;

        GameObject maskInstance = Instantiate(maskProjectilePrefab, spawn.position, transform.rotation);
        var projectile = maskInstance.GetComponent<MaskProjectile>();
        if (projectile == null)
        {
            projectile = maskInstance.AddComponent<MaskProjectile>();
        }

        int ownerZone = _hostState != null && _hostState.currentSnatcherZone != 0
            ? _hostState.currentSnatcherZone
            : (_camera != null ? _camera.zone : 0);

        if (_snatcherManager != null && ownerZone != 0)
        {
            _snatcherManager.OnSnatcherShot(ownerZone);
        }

        projectile.ownerZone = ownerZone;
        projectile.snatcherManager = _snatcherManager;
        projectile.sourceHost = gameObject;
        projectile.speed = maskShootSpeed;
        projectile.lifeSeconds = maskLifeSeconds;
        projectile.ResetLifetime();

        if (followMaskOnShoot)
        {
            if (_camera == null) _camera = FindCameraFollowingThis();
            if (_camera != null) _camera.SetTarget(maskInstance.transform);
        }
    }

    private FaceSnatcherCamera FindCameraFollowingThis()
    {
        var cams = FindObjectsByType<FaceSnatcherCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var cam in cams)
        {
            if (cam != null && cam.target == transform) return cam;
        }
        return null;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == childName) return t;
        }
        return null;
    }
}
