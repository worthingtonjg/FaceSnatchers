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

    private readonly List<WanderPoint> _points = new List<WanderPoint>();

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
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
