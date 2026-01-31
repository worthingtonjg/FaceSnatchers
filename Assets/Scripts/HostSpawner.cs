using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class HostSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject hostPrefab;

    [Header("Spawn Points Root")]
    [Tooltip("Parent containing child WanderPoint components used as spawn points. If null, will auto-find 'WanderPoints'.")]
    public Transform wanderPointsRoot;

    [Header("Spawn Settings")]
    [Min(0)] public int hostCount = 50;

    [Tooltip("Optional parent for spawned hosts (keeps hierarchy clean).")]
    public Transform hostsParent;

    [Tooltip("If true, destroys previously spawned hosts under Hosts Parent (or under this spawner if no parent set).")]
    public bool clearPreviouslySpawned = true;

    [Tooltip("Random yaw rotation (0-360) applied to spawned hosts.")]
    public bool randomYaw = true;

    [Tooltip("If true, snap spawn position to NavMesh near the point (helps if points are slightly off).")]
    public bool snapToNavMesh = true;

    [Tooltip("Max distance to search for NavMesh when snapping.")]
    public float navMeshSnapRadius = 1.5f;

    [Header("Camera Assignment")]
    [Tooltip("If true, assigns each FaceSnatcher camera to a host in its zone after spawning.")]
    public bool assignCamerasOnStart = true;

    [Tooltip("If true, auto-find all FaceSnatcherCamera components in the scene.")]
    public bool autoFindCameras = true;

    [Tooltip("Explicit cameras to assign (used if auto-find is off or to override).")]
    public List<FaceSnatcherCamera> cameras = new List<FaceSnatcherCamera>();

    [Header("Player Materials")]
    public Material redMaterial;
    public Material yellowMaterial;
    public Material greenMaterial;
    public Material blueMaterial;

    [Header("Human Player")]
    [Tooltip("Zone that will be controlled by the human player (1-4).")]
    public int humanZone = 4;

    [Tooltip("If true, adds/enables FaceSnatcherHumanController on the host assigned to the human zone.")]
    public bool enableHumanControl = true;

    private readonly List<WanderPoint> _points = new List<WanderPoint>();
    private readonly List<GameObject> _spawnedHosts = new List<GameObject>();

    void Start()
    {
        Spawn();
    }

    [ContextMenu("Spawn Hosts Now")]
    public void Spawn()
    {
        if (hostPrefab == null)
        {
            Debug.LogError($"{nameof(HostSpawner)}: hostPrefab is not assigned.");
            return;
        }

        CacheSpawnPoints();
        if (_points.Count == 0)
        {
            Debug.LogError($"{nameof(HostSpawner)}: No spawn points found. Make sure your point objects have a WanderPoint component.");
            return;
        }

        if (hostsParent == null)
        {
            // Default to keeping spawned hosts grouped under the spawner
            hostsParent = this.transform;
        }

        if (clearPreviouslySpawned)
        {
            ClearSpawnedHosts();
        }

        int spawnCount = Mathf.Min(hostCount, _points.Count);
        if (hostCount > _points.Count)
        {
            Debug.LogWarning($"{nameof(HostSpawner)}: Requested {hostCount} hosts but only {_points.Count} points exist. Spawning {spawnCount}.");
        }

        // Pick unique points by shuffling a copy and taking first N
        List<WanderPoint> shuffled = new List<WanderPoint>(_points);
        Shuffle(shuffled);

        for (int i = 0; i < spawnCount; i++)
        {
            WanderPoint p = shuffled[i];
            if (p == null) continue;

            Vector3 pos = p.transform.position;

            if (snapToNavMesh && NavMesh.SamplePosition(pos, out NavMeshHit hit, navMeshSnapRadius, NavMesh.AllAreas))
            {
                pos = hit.position;
            }

            Quaternion rot = randomYaw ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) : p.transform.rotation;

            GameObject host = Instantiate(hostPrefab, pos, rot, hostsParent);
            host.name = $"{hostPrefab.name}_{i:000}";
            _spawnedHosts.Add(host);

            // Assign host's zone from the spawn point's zone (HostWander uses this to stay in-zone)
            if (host.TryGetComponent<HostWander>(out var wander))
            {
                wander.zone = p.zone;
            }
            else
            {
                Debug.LogWarning($"{nameof(HostSpawner)}: Spawned host '{host.name}' has no HostWander component; cannot assign zone.");
            }

            // If it has a NavMeshAgent, warp it to be safe (prevents "not on navmesh" issues)
            if (snapToNavMesh && host.TryGetComponent<NavMeshAgent>(out var agent))
            {
                if (agent.enabled)
                {
                    agent.Warp(pos);
                }
            }
        }

        if (assignCamerasOnStart)
        {
            AssignCamerasToHosts();
        }
    }

    private void CacheSpawnPoints()
    {
        _points.Clear();

        if (wanderPointsRoot == null)
        {
            GameObject wp = GameObject.Find("WanderPoints");
            if (wp != null) wanderPointsRoot = wp.transform;
        }

        if (wanderPointsRoot == null)
        {
            Debug.LogError($"{nameof(HostSpawner)}: wanderPointsRoot not set and no GameObject named 'WanderPoints' found.");
            return;
        }

        // Only cache actual WanderPoint components (not every child Transform)
        _points.AddRange(wanderPointsRoot.GetComponentsInChildren<WanderPoint>(true));
    }

    private void ClearSpawnedHosts()
    {
        // Destroy children under hostsParent (play mode safe; jam-friendly)
        for (int i = hostsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(hostsParent.GetChild(i).gameObject);
        }
        _spawnedHosts.Clear();
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void AssignCamerasToHosts()
    {
        if (autoFindCameras)
        {
            cameras.Clear();
            cameras.AddRange(FindObjectsOfType<FaceSnatcherCamera>(true));
        }

        if (cameras.Count == 0)
        {
            Debug.LogWarning($"{nameof(HostSpawner)}: No FaceSnatcherCamera components found to assign.");
            return;
        }

        var zoneHosts = new Dictionary<int, List<GameObject>>();

        foreach (var host in _spawnedHosts)
        {
            if (host == null) continue;
            if (!host.TryGetComponent<HostWander>(out var wander)) continue;

            if (!zoneHosts.TryGetValue(wander.zone, out var list))
            {
                list = new List<GameObject>();
                zoneHosts[wander.zone] = list;
            }
            list.Add(host);
        }

        int assigned = 0;

        foreach (var cam in cameras)
        {
            if (cam == null) continue;

            if (!zoneHosts.TryGetValue(cam.zone, out var candidates) || candidates.Count == 0)
            {
                Debug.LogWarning($"{nameof(HostSpawner)}: No hosts available in zone {cam.zone} for camera '{cam.name}'.");
                continue;
            }

            int pickIndex = Random.Range(0, candidates.Count);
            var host = candidates[pickIndex];
            candidates.RemoveAt(pickIndex);

            cam.SetTarget(host.transform);
            assigned++;

            if (host.TryGetComponent<HostWander>(out var wander))
            {
                wander.enabled = false;
            }

            ApplyPlayerMaterial(host, cam.zone);
            EnableMask(host);

            if (enableHumanControl && cam.zone == humanZone)
            {
                EnsureHumanController(host);
            }
        }

        if (assigned < 4)
        {
            Debug.LogWarning($"{nameof(HostSpawner)}: Assigned {assigned} camera(s). Expected 4 (one per zone 1-4).");
        }
    }

    private void ApplyPlayerMaterial(GameObject host, int zone)
    {
        Material mat = zone switch
        {
            1 => redMaterial,
            2 => yellowMaterial,
            3 => greenMaterial,
            4 => blueMaterial,
            _ => null
        };

        if (mat == null) return;

        var renderers = host.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Debug.LogWarning($"{nameof(HostSpawner)}: Host '{host.name}' has no Renderer to apply player material.");
            return;
        }

        Transform maskRoot = FindChildByName(host.transform, "Mask");

        foreach (var r in renderers)
        {
            if (maskRoot != null && r.transform.IsChildOf(maskRoot)) continue;
            r.material = mat;
        }
    }

    private void EnableMask(GameObject host)
    {
        var mask = FindChildByName(host.transform, "Mask");
        if (mask == null)
        {
            Debug.LogWarning($"{nameof(HostSpawner)}: Host '{host.name}' has no child named 'Mask'.");
            return;
        }

        mask.gameObject.SetActive(true);
    }

    private void EnsureHumanController(GameObject host)
    {
        if (!host.TryGetComponent<FaceSnatcherHumanController>(out var controller))
        {
            controller = host.AddComponent<FaceSnatcherHumanController>();
        }

        controller.enabled = true;
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
