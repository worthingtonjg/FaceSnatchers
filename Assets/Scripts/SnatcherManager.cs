using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SnatcherManager : MonoBehaviour
{
    [System.Serializable]
    public class SnatcherSlot
    {
        [Tooltip("Zone this snatcher owns (1-4).")]
        public int zone = 1;
        public bool isAlive = true;
        public GameObject currentHost;
        public Material material;
        public bool isHuman;
        public FaceSnatcherCamera camera;
        [Tooltip("Seconds remaining before host decay kills this snatcher.")]
        public float hostTimeRemaining;
        [Tooltip("Seconds remaining before this snatcher respawns.")]
        public float respawnTimer;
        [Tooltip("Last host this snatcher occupied (used for death audio on miss).")]
        public GameObject lastHost;
    }

    [Header("Slots")]
    public List<SnatcherSlot> slots = new List<SnatcherSlot>(4);

    [Header("Host Decay")]
    public bool hostDecayEnabled = true;
    [Min(1f)] public float hostDecaySeconds = 12f;

    [Header("Respawn")]
    [Min(0f)] public float respawnDelaySeconds = 3f;

    [Header("Match End")]
    public bool matchStarted;
    public bool matchEnded;
    public int winningZone;

    [Header("AI")]
    public GameObject aiMaskProjectilePrefab;
    public float aiShotCooldown = 1.5f;
    public float aiTargetMaxDistance = 25f;
    public float aiShotMinDistance = 3f;
    public float aiShotMaxDistance = 10f;
    public float aiMaskShootSpeed = 14f;
    public float aiMaskLifeSeconds = 6f;

    [Header("Zone Names")]
    public string zone1Name = "Red";
    public string zone2Name = "Yellow";
    public string zone3Name = "Green";
    public string zone4Name = "Blue";

    [Header("Cameras")]
    public bool autoFindCameras = true;

    void Update()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null) continue;

            if (!slot.isAlive)
            {
                TickRespawn(slot);
                continue;
            }

            if (!hostDecayEnabled) continue;
            if (slot.currentHost == null) continue;

            slot.hostTimeRemaining -= Time.deltaTime;
            if (slot.hostTimeRemaining <= 0f)
            {
                HandleHostDecay(slot);
            }
        }

        CheckForMatchEnd();
    }

    public void AssignInitialHosts(
        IReadOnlyList<GameObject> hosts,
        Material redMaterial,
        Material yellowMaterial,
        Material greenMaterial,
        Material blueMaterial,
        int humanZone,
        bool enableHumanControl)
    {
        EnsureDefaultSlots();
        AssignCamerasToSlots();

        var zoneHosts = new Dictionary<int, List<GameObject>>();
        for (int i = 0; i < hosts.Count; i++)
        {
            var host = hosts[i];
            if (host == null) continue;
            if (!host.TryGetComponent<HostWander>(out var wander)) continue;

            if (!zoneHosts.TryGetValue(wander.zone, out var list))
            {
                list = new List<GameObject>();
                zoneHosts[wander.zone] = list;
            }
            list.Add(host);
        }

        foreach (var slot in slots)
        {
            if (slot == null) continue;

            if (!zoneHosts.TryGetValue(slot.zone, out var candidates) || candidates.Count == 0)
            {
                if (slot.camera != null) slot.camera.SetTarget(null);
                continue;
            }

            int pickIndex = Random.Range(0, candidates.Count);
            var host = candidates[pickIndex];
            candidates.RemoveAt(pickIndex);

            slot.isAlive = true;
            slot.currentHost = host;
            slot.isHuman = enableHumanControl && slot.zone == humanZone;
            slot.material = GetMaterialForZone(slot.zone, redMaterial, yellowMaterial, greenMaterial, blueMaterial);

            var hostState = EnsureHostState(host);
            if (host.TryGetComponent<HostWander>(out var wander))
            {
                hostState.zone = wander.zone;
            }
            hostState.SetPossessed(slot.zone);
            slot.hostTimeRemaining = hostDecaySeconds;

            ApplyPlayerMaterial(host, slot.material);
            EnableMask(host);

            if (host.TryGetComponent<HostWander>(out var hostWander))
            {
                hostWander.enabled = false;
            }

            if (slot.isHuman)
            {
                EnsureHumanController(host);
            }
            else
            {
                EnsureAIController(host, slot.zone);
            }

            if (slot.camera != null)
            {
                slot.camera.SetTarget(host.transform);
            }
        }

        matchStarted = true;
    }

    public void OnSnatcherShot(int snatcherZone)
    {
        var slot = GetSlotByZone(snatcherZone);
        if (slot == null || slot.currentHost == null) return;

        var host = slot.currentHost;
        if (slot.isHuman && host.TryGetComponent<HostAudio>(out var hostAudio))
        {
            hostAudio.PlayLeaveHost();
        }
        slot.lastHost = host;
        var hostState = EnsureHostState(host);
        hostState.ClearPossessed();
        hostState.SetClaimed(snatcherZone);

        ApplyPlayerMaterial(host, slot.material);
        DisableMask(host);
        EnableHostWander(host, true);
        if (slot.isHuman)
        {
            DisableHumanController(host);
        }
        else
        {
            DisableAIController(host);
        }

        slot.currentHost = null;
        slot.respawnTimer = respawnDelaySeconds;

        CheckForMatchEnd();
    }

    public bool PossessHost(int attackerZone, HostState hostState)
    {
        if (hostState == null) return false;
        var attacker = GetSlotByZone(attackerZone);
        if (attacker == null || !attacker.isAlive) return false;

        if (hostState.currentSnatcherZone == attackerZone)
        {
            return false;
        }

        if (hostState.currentSnatcherZone == 0 && hostState.claimedByZone == attackerZone)
        {
            return false;
        }

        if (hostState.currentSnatcherZone != 0 && hostState.currentSnatcherZone != attackerZone)
        {
            KillSnatcher(hostState.currentSnatcherZone);
        }

        hostState.SetPossessed(attackerZone);
        attacker.currentHost = hostState.gameObject;
        attacker.hostTimeRemaining = hostDecaySeconds;

        ApplyPlayerMaterial(hostState.gameObject, attacker.material);
        EnableMask(hostState.gameObject);
        EnableHostWander(hostState.gameObject, false);

        if (attacker.isHuman)
        {
            EnsureHumanController(hostState.gameObject);
        }
        else
        {
            EnsureAIController(hostState.gameObject, attackerZone);
        }

        if (attacker.camera != null)
        {
            attacker.camera.SetTarget(hostState.transform);
        }

        return true;
    }

    public void KillSnatcher(int snatcherZone)
    {
        var slot = GetSlotByZone(snatcherZone);
        if (slot == null || !slot.isAlive) return;

        slot.isAlive = false;
        slot.hostTimeRemaining = 0f;
        slot.respawnTimer = respawnDelaySeconds;

        if (slot.currentHost != null)
        {
            if (slot.isHuman && slot.currentHost.TryGetComponent<HostAudio>(out var hostAudio))
            {
                hostAudio.PlayDeath();
            }
            if (slot.isHuman)
            {
                DisableHumanController(slot.currentHost);
            }
            else
            {
                DisableAIController(slot.currentHost);
            }
        }
        else if (slot.isHuman && slot.lastHost != null && slot.lastHost.TryGetComponent<HostAudio>(out var lastHostAudio))
        {
            lastHostAudio.PlayDeath();
        }

        slot.currentHost = null;
        slot.lastHost = null;
        if (slot.camera != null)
        {
            slot.camera.SetTarget(null);
        }
    }

    public void SetCurrentHost(int zone, GameObject newHost)
    {
        var slot = GetSlotByZone(zone);
        if (slot == null) return;

        slot.currentHost = newHost;
        if (slot.camera != null)
        {
            slot.camera.SetTarget(newHost != null ? newHost.transform : null);
        }
    }

    public SnatcherSlot GetSlotByZone(int zone)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].zone == zone) return slots[i];
        }
        return null;
    }

    public bool CanRespawn(int zone)
    {
        if (matchEnded) return false;
        var slot = GetSlotByZone(zone);
        if (slot == null) return false;
        if (slot.isAlive) return false;
        return FindUnclaimedNeutralHost() != null;
    }

    public float GetRespawnTimeRemaining(int zone)
    {
        var slot = GetSlotByZone(zone);
        if (slot == null) return 0f;
        return Mathf.Max(0f, slot.respawnTimer);
    }

    public string GetZoneName(int zone)
    {
        return zone switch
        {
            1 => zone1Name,
            2 => zone2Name,
            3 => zone3Name,
            4 => zone4Name,
            _ => $"Zone {zone}"
        };
    }

    public int GetClaimCountForZone(int zone)
    {
        return CountClaimsForZone(zone);
    }

    public int GetRemainingUnclaimedHostCount()
    {
        var hosts = FindObjectsByType<HostState>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;

        for (int i = 0; i < hosts.Length; i++)
        {
            var host = hosts[i];
            if (host == null) continue;
            if (host.isDead) continue;
            if (host.claimedByZone != 0) continue;
            if (host.currentSnatcherZone != 0) continue;
            count++;
        }

        return count;
    }

    private void EnsureDefaultSlots()
    {
        if (slots.Count >= 4) return;
        slots.Clear();
        for (int i = 1; i <= 4; i++)
        {
            slots.Add(new SnatcherSlot { zone = i });
        }
    }

    private void AssignCamerasToSlots()
    {
        if (!autoFindCameras) return;

        var cams = FindObjectsByType<FaceSnatcherCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null) continue;

            for (int c = 0; c < cams.Length; c++)
            {
                if (cams[c] != null && cams[c].zone == slot.zone)
                {
                    slot.camera = cams[c];
                    break;
                }
            }
        }
    }

    private static Material GetMaterialForZone(
        int zone,
        Material redMaterial,
        Material yellowMaterial,
        Material greenMaterial,
        Material blueMaterial)
    {
        return zone switch
        {
            1 => redMaterial,
            2 => yellowMaterial,
            3 => greenMaterial,
            4 => blueMaterial,
            _ => null
        };
    }

    private static void ApplyPlayerMaterial(GameObject host, Material mat)
    {
        if (host == null || mat == null) return;

        var renderers = host.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Transform maskRoot = FindChildByName(host.transform, "Mask");

        foreach (var r in renderers)
        {
            if (maskRoot != null && r.transform.IsChildOf(maskRoot)) continue;
            r.material = mat;
        }
    }

    private static void EnableMask(GameObject host)
    {
        var mask = FindChildByName(host.transform, "Mask");
        if (mask == null) return;
        mask.gameObject.SetActive(true);
    }

    private static void DisableMask(GameObject host)
    {
        var mask = FindChildByName(host.transform, "Mask");
        if (mask == null) return;
        mask.gameObject.SetActive(false);
    }

    private static void EnableHostWander(GameObject host, bool enabled)
    {
        if (host == null) return;
        if (host.TryGetComponent<HostWander>(out var wander))
        {
            wander.enabled = enabled;
        }
    }

    private void HandleHostDecay(SnatcherSlot slot)
    {
        var host = slot.currentHost;
        if (host == null)
        {
            slot.hostTimeRemaining = 0f;
            return;
        }

        var state = EnsureHostState(host);
        state.ClearPossessed();
        state.isDead = true;

        if (slot.isHuman)
        {
            DisableHumanController(host);
        }
        else
        {
            DisableAIController(host);
        }
        EnableMask(host);
        DisableHostSystems(host);

        slot.currentHost = null;
        slot.isAlive = false;
        slot.hostTimeRemaining = 0f;
        slot.respawnTimer = respawnDelaySeconds;

        if (slot.camera != null)
        {
            slot.camera.SetTarget(null);
        }

        // Optional: mark host dead visually or disable it later if desired.
    }

    private static void DisableHostSystems(GameObject host)
    {
        if (host == null) return;

        if (host.TryGetComponent<HostWander>(out var wander))
        {
            wander.ForceReleaseReservation();
            wander.enabled = false;
        }

        if (host.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
            agent.enabled = false;
        }

        var colliders = host.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        var renderers = host.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }
    }

    private void TickRespawn(SnatcherSlot slot)
    {
        if (respawnDelaySeconds <= 0f || slot == null) return;
        if (matchEnded) return;
        if (slot.respawnTimer < 0f) return;

        slot.respawnTimer -= Time.deltaTime;
        if (slot.respawnTimer > 0f) return;

        if (!TryRespawn(slot))
        {
            slot.respawnTimer = -1f; // no available hosts, stop trying
        }
    }

    private bool TryRespawn(SnatcherSlot slot)
    {
        var host = FindUnclaimedNeutralHost();
        if (host == null) return false;

        slot.isAlive = true;
        slot.currentHost = host.gameObject;
        slot.hostTimeRemaining = hostDecaySeconds;
        slot.respawnTimer = 0f;

        host.SetPossessed(slot.zone);

        ApplyPlayerMaterial(host.gameObject, slot.material);
        EnableMask(host.gameObject);
        EnableHostWander(host.gameObject, false);

        if (slot.isHuman)
        {
            EnsureHumanController(host.gameObject);
        }
        else
        {
            EnsureAIController(host.gameObject, slot.zone);
        }

        if (slot.camera != null)
        {
            slot.camera.SetTarget(host.transform);
        }

        return true;
    }

    private HostState FindUnclaimedNeutralHost()
    {
        var hosts = FindObjectsByType<HostState>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var candidates = new List<HostState>();

        for (int i = 0; i < hosts.Length; i++)
        {
            var host = hosts[i];
            if (host == null) continue;
            if (host.isDead) continue;
            if (host.currentSnatcherZone != 0) continue;
            if (host.claimedByZone != 0) continue;
            candidates.Add(host);
        }

        if (candidates.Count == 0) return null;

        int index = Random.Range(0, candidates.Count);
        return candidates[index];
    }

    private void CheckForMatchEnd()
    {
        if (!matchStarted) return;
        if (matchEnded) return;

        var hosts = FindObjectsByType<HostState>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool hasNeutralUnclaimed = false;

        for (int i = 0; i < hosts.Length; i++)
        {
            var host = hosts[i];
            if (host == null) continue;
            if (host.isDead) continue;
            if (host.claimedByZone != 0) continue;
            if (host.currentSnatcherZone != 0) continue;
            hasNeutralUnclaimed = true;
            break;
        }

        if (hasNeutralUnclaimed) return;

        matchEnded = true;
        winningZone = GetWinningZoneByClaims();
        Debug.Log($"{nameof(SnatcherManager)}: Match ended. Winning zone = {winningZone}");

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null || slot.isHuman) continue;
            if (slot.currentHost != null)
            {
                DisableAIController(slot.currentHost);
            }
        }
    }

    private int GetWinningZoneByClaims()
    {
        int bestZone = 0;
        int bestCount = -1;

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null) continue;
            int count = CountClaimsForZone(slot.zone);
            if (count > bestCount)
            {
                bestCount = count;
                bestZone = slot.zone;
            }
        }

        return bestZone;
    }

    private int CountClaimsForZone(int zone)
    {
        var hosts = FindObjectsByType<HostState>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        for (int i = 0; i < hosts.Length; i++)
        {
            if (hosts[i] != null && hosts[i].claimedByZone == zone) count++;
        }
        return count;
    }

    private static void EnsureHumanController(GameObject host)
    {
        if (host == null) return;
        if (!host.TryGetComponent<FaceSnatcherHumanController>(out var controller))
        {
            controller = host.AddComponent<FaceSnatcherHumanController>();
        }
        controller.enabled = true;
        controller.useNavMeshMovement = false;

        if (host.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
        {
            agent.enabled = false;
        }

    }

    private static void DisableHumanController(GameObject host)
    {
        if (host == null) return;
        if (host.TryGetComponent<FaceSnatcherHumanController>(out var controller))
        {
            controller.enabled = false;
        }

        if (host.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
        {
            agent.enabled = true;
        }
    }

    private void EnsureAIController(GameObject host, int ownerZone)
    {
        if (host == null) return;
        if (!host.TryGetComponent<FaceSnatcherAIController>(out var controller))
        {
            controller = host.AddComponent<FaceSnatcherAIController>();
        }

        controller.enabled = true;
        controller.Configure(
            ownerZone,
            this,
            aiMaskProjectilePrefab,
            aiMaskShootSpeed,
            aiMaskLifeSeconds,
            aiShotCooldown,
            aiTargetMaxDistance,
            aiShotMinDistance,
            aiShotMaxDistance);

        if (host.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
        {
            agent.isStopped = false;
        }
    }

    private static void DisableAIController(GameObject host)
    {
        if (host == null) return;
        if (host.TryGetComponent<FaceSnatcherAIController>(out var controller))
        {
            controller.enabled = false;
        }
    }

    private static HostState EnsureHostState(GameObject host)
    {
        if (host == null) return null;
        if (!host.TryGetComponent<HostState>(out var state))
        {
            state = host.AddComponent<HostState>();
        }
        return state;
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
