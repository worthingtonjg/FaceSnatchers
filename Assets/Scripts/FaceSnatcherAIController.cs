using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class FaceSnatcherAIController : MonoBehaviour
{
    [Header("Owner")]
    public int ownerZone;
    public SnatcherManager snatcherManager;

    [Header("Movement")]
    public float moveSpeed = 4.5f;
    public float turnSpeed = 180f;
    public float desiredStopDistance = 1.5f;

    [Header("Targeting")]
    public float retargetInterval = 0.6f;
    public float maxTargetDistance = 25f;

    [Header("Shooting")]
    public GameObject maskProjectilePrefab;
    public float maskShootSpeed = 14f;
    public float maskLifeSeconds = 6f;
    public float shotCooldown = 1.5f;
    public float shotMinDistance = 3f;
    public float shotMaxDistance = 10f;
    public LayerMask lineOfSightMask = ~0;
    [Tooltip("Half-width for multi-ray line of sight checks.")]
    public float lineOfSightHalfWidth = 0.4f;
    public bool drawLineOfSightRays = true;
    public Color lineOfSightHitColor = Color.green;
    public Color lineOfSightBlockedColor = Color.red;

    private NavMeshAgent _agent;
    private HostState _currentTarget;
    private float _retargetTimer;
    private float _shotTimer;
    private Transform _maskSpawn;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        if (snatcherManager == null)
        {
            snatcherManager = FindFirstObjectByType<SnatcherManager>(FindObjectsInactive.Include);
        }

        _maskSpawn = FindChildByName(transform, "Mask");
        _shotTimer = shotCooldown;

        if (_agent != null)
        {
            _agent.updateRotation = false;
            _agent.speed = moveSpeed;
            _agent.stoppingDistance = desiredStopDistance;
        }
    }

    void Update()
    {
        if (snatcherManager == null) return;
        if (snatcherManager.matchEnded) return;
        if (ownerZone == 0) return;

        _retargetTimer -= Time.deltaTime;
        _shotTimer -= Time.deltaTime;

        if (_retargetTimer <= 0f || _currentTarget == null || !IsTargetValid(_currentTarget))
        {
            _currentTarget = FindBestTarget();
            _retargetTimer = retargetInterval;
        }

        if (_currentTarget == null) return;

        Vector3 toTarget = _currentTarget.transform.position - transform.position;
        float dist = toTarget.magnitude;

        if (toTarget.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.SetDestination(_currentTarget.transform.position);
        }
        else
        {
            Vector3 move = transform.forward * (moveSpeed * Time.deltaTime);
            transform.position += move;
        }

        if (_shotTimer <= 0f && dist >= shotMinDistance && dist <= shotMaxDistance)
        {
            TryShoot();
        }
    }

    public void Configure(
        int zone,
        SnatcherManager manager,
        GameObject projectilePrefab,
        float projectileSpeed,
        float projectileLife,
        float cooldown,
        float maxDistance,
        float minShotDistance,
        float maxShotDistance)
    {
        ownerZone = zone;
        snatcherManager = manager;
        maskProjectilePrefab = projectilePrefab;
        maskShootSpeed = projectileSpeed;
        maskLifeSeconds = projectileLife;
        shotCooldown = cooldown;
        maxTargetDistance = maxDistance;
        shotMinDistance = minShotDistance;
        shotMaxDistance = maxShotDistance;
        _shotTimer = shotCooldown;
    }

    private void TryShoot()
    {
        if (maskProjectilePrefab == null) return;
        if (_currentTarget == null || !IsTargetValid(_currentTarget)) return;

        if (!HasLineOfSightToTarget(_currentTarget))
        {
            return;
        }

        snatcherManager.OnSnatcherShot(ownerZone);

        Transform spawn = _maskSpawn != null ? _maskSpawn : transform;
        GameObject maskInstance = Instantiate(maskProjectilePrefab, spawn.position, transform.rotation);

        var projectile = maskInstance.GetComponent<MaskProjectile>();
        if (projectile == null)
        {
            projectile = maskInstance.AddComponent<MaskProjectile>();
        }

        projectile.ownerZone = ownerZone;
        projectile.snatcherManager = snatcherManager;
        projectile.sourceHost = gameObject;
        projectile.speed = maskShootSpeed;
        projectile.lifeSeconds = maskLifeSeconds;
        projectile.ResetLifetime();

        _shotTimer = shotCooldown;
    }

    private HostState FindBestTarget()
    {
        var hosts = FindObjectsByType<HostState>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        HostState best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < hosts.Length; i++)
        {
            var host = hosts[i];
            if (!IsTargetValid(host)) continue;

            float dist = Vector3.Distance(transform.position, host.transform.position);
            if (dist > maxTargetDistance) continue;

            if (dist < bestDist)
            {
                bestDist = dist;
                best = host;
            }
        }

        return best;
    }

    private bool IsTargetValid(HostState host)
    {
        if (host == null) return false;
        if (host.currentSnatcherZone != 0) return false;
        if (host.claimedByZone == ownerZone) return false;
        return true;
    }

    private bool HasLineOfSightToTarget(HostState host)
    {
        if (host == null) return false;

        Vector3 origin = _maskSpawn != null ? _maskSpawn.position : transform.position + Vector3.up * 0.5f;
        Vector3 target = host.transform.position + Vector3.up * 0.5f;
        Vector3 dir = target - origin;

        if (dir.sqrMagnitude < 0.01f) return true;

        Vector3 forward = dir.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 offset = right * lineOfSightHalfWidth;

        bool center = RayHitsHost(origin, forward, dir.magnitude, host);
        bool rightRay = RayHitsHost(origin + offset, forward, dir.magnitude, host);
        bool leftRay = RayHitsHost(origin - offset, forward, dir.magnitude, host);

        if (drawLineOfSightRays)
        {
            Debug.DrawRay(origin, forward * dir.magnitude, center ? lineOfSightHitColor : lineOfSightBlockedColor, 0f, false);
            Debug.DrawRay(origin + offset, forward * dir.magnitude, rightRay ? lineOfSightHitColor : lineOfSightBlockedColor, 0f, false);
            Debug.DrawRay(origin - offset, forward * dir.magnitude, leftRay ? lineOfSightHitColor : lineOfSightBlockedColor, 0f, false);
        }

        return center && rightRay && leftRay;
    }

    private bool RayHitsHost(Vector3 origin, Vector3 direction, float distance, HostState expectedHost)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance, lineOfSightMask, QueryTriggerInteraction.Ignore))
        {
            var hitHost = hit.collider.GetComponentInParent<HostState>();
            return hitHost != null && hitHost == expectedHost;
        }
        return false;
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
