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

    private static Transform FindChildByName(Transform root, string childName)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == childName) return t;
        }
        return null;
    }
}
