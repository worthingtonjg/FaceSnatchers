using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class HostWander : MonoBehaviour
{
    [Header("Wander Points Root")]
    [Tooltip("Parent containing child WanderPoint components. If null, will auto-find 'WanderPoints'.")]
    public Transform wanderPointsRoot;

    [Header("Selection Rules")]
    public float minDestinationDistance = 6f;

    [Tooltip("How many attempts to find an available + far-enough point.")]
    public int pickAttempts = 25;

    [Header("Timing")]
    public float waitMinSeconds = 0.3f;
    public float waitMaxSeconds = 1.2f;

    [Header("Failure Handling")]
    [Tooltip("If we can't reach or arrive within this time, we give up, release the point, and pick a new one.")]
    public float travelTimeoutSeconds = 6f;

    [Tooltip("Require complete path to the reserved point.")]
    public bool requireCompletePath = true;

    [Header("Zone")]
    [Tooltip("Host will only reserve/visit WanderPoints whose zone matches this value.")]
    public int zone;

    private NavMeshAgent _agent;
    private readonly List<WanderPoint> _points = new();
    private WanderPoint _reservedPoint;
    private Coroutine _loop;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        CachePoints();
        _agent.avoidancePriority = Random.Range(30, 70); // helps with crowding
        _loop = StartCoroutine(WanderLoop());
    }

    void OnDisable()
    {
        ReleaseReservedPoint();
        if (_loop != null) StopCoroutine(_loop);
        _loop = null;
    }

    private void CachePoints()
    {
        _points.Clear();

        if (wanderPointsRoot == null)
        {
            GameObject go = GameObject.Find("WanderPoints");
            if (go != null) wanderPointsRoot = go.transform;
        }

        if (wanderPointsRoot == null)
        {
            Debug.LogError($"{nameof(HostWander)} on '{name}': wanderPointsRoot not set and no GameObject named 'WanderPoints' found.");
            return;
        }

        _points.AddRange(wanderPointsRoot.GetComponentsInChildren<WanderPoint>(true));

        if (_points.Count == 0)
        {
            Debug.LogError($"{nameof(HostWander)} on '{name}': No WanderPoint components found under '{wanderPointsRoot.name}'. " +
                           $"Add the WanderPoint script to your destination empties.");
        }
    }

    private IEnumerator WanderLoop()
    {
        yield return null;

        if (_points.Count == 0) yield break;

        // Ensure agent starts on navmesh
        if (_agent.enabled && !_agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 5f, NavMesh.AllAreas))
                _agent.Warp(hit.position);
        }

        while (true)
        {
            if (!_agent.enabled || !_agent.isOnNavMesh)
            {
                yield return null;
                continue;
            }

            // Pick + reserve an available point IN THIS HOST'S ZONE
            if (!TryPickAndReservePointInZone(out _reservedPoint))
            {
                yield return null;
                continue;
            }

            Vector3 dest = _reservedPoint.transform.position;

            // Optional: verify path is complete
            if (requireCompletePath && !HasCompletePath(dest))
            {
                ReleaseReservedPoint();
                yield return null;
                continue;
            }

            _agent.isStopped = false;
            _agent.SetDestination(dest);

            // Travel with timeout
            float timer = 0f;

            while (true)
            {
                if (!_agent.enabled || !_agent.isOnNavMesh) break;
                if (_agent.pathPending) { yield return null; continue; }

                // Path invalid/partial? bail
                if (requireCompletePath && _agent.pathStatus != NavMeshPathStatus.PathComplete) break;

                // Arrived?
                if (_agent.remainingDistance <= _agent.stoppingDistance + 0.2f) break;

                timer += Time.deltaTime;
                if (timer >= travelTimeoutSeconds) break;

                yield return null;
            }

            // Stop and release point so someone else can use it
            _agent.ResetPath();
            _agent.isStopped = true;
            ReleaseReservedPoint();

            // Pause
            yield return new WaitForSeconds(Random.Range(waitMinSeconds, waitMaxSeconds));
        }
    }

    private bool TryPickAndReservePointInZone(out WanderPoint reserved)
    {
        reserved = null;
        if (_points.Count == 0) return false;

        // Pass 1: must match zone + available + min distance
        for (int i = 0; i < pickAttempts; i++)
        {
            var candidate = _points[Random.Range(0, _points.Count)];
            if (candidate == null) continue;
            if (candidate.zone != zone) continue;
            if (!candidate.IsAvailable) continue;

            float dist = Vector3.Distance(transform.position, candidate.transform.position);
            if (dist < minDestinationDistance) continue;

            if (candidate.TryReserve())
            {
                reserved = candidate;
                return true;
            }
        }

        // Pass 2: still must match zone + available, but ignore min distance
        for (int i = 0; i < pickAttempts; i++)
        {
            var candidate = _points[Random.Range(0, _points.Count)];
            if (candidate == null) continue;
            if (candidate.zone != zone) continue;
            if (!candidate.IsAvailable) continue;

            if (candidate.TryReserve())
            {
                reserved = candidate;
                return true;
            }
        }

        return false;
    }

    private bool HasCompletePath(Vector3 destination)
    {
        var path = new NavMeshPath();
        bool ok = _agent.CalculatePath(destination, path);
        return ok && path.status == NavMeshPathStatus.PathComplete;
    }

    private void ReleaseReservedPoint()
    {
        if (_reservedPoint != null)
        {
            _reservedPoint.Release();
            _reservedPoint = null;
        }
    }
}
