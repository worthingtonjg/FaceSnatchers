using UnityEngine;

[DisallowMultipleComponent]
public class MaskProjectile : MonoBehaviour
{
    public float speed = 14f;
    public float lifeSeconds = 6f;
    public int ownerZone;
    public GameObject sourceHost;
    public SnatcherManager snatcherManager;

    private float _lifeTimer;
    private bool _resolved;
    private Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity = false;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    void Update()
    {
        if (lifeSeconds > 0f)
        {
            _lifeTimer += Time.deltaTime;
            if (_lifeTimer >= lifeSeconds)
            {
                ResolveMiss();
            }
        }
    }

    void FixedUpdate()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = transform.forward * speed;
        }
        else
        {
            transform.position += transform.forward * (speed * Time.fixedDeltaTime);
        }
    }

    public void ResetLifetime()
    {
        _lifeTimer = 0f;
    }

    void OnTriggerEnter(Collider other)
    {
        print(other.gameObject.name);
        HandleImpact(other.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleImpact(collision.gameObject);
    }

    private void HandleImpact(GameObject hitObject)
    {
        if (_resolved) return;
        if (hitObject == null) return;

        var hostState = hitObject.GetComponentInParent<HostState>();
        if (hostState != null && sourceHost != null && hostState.gameObject == sourceHost)
        {
            return;
        }

        if (hostState == null)
        {
            ResolveMiss();
            return;
        }

        bool possessed = snatcherManager != null && ownerZone != 0 && snatcherManager.PossessHost(ownerZone, hostState);
        if (!possessed)
        {
            ResolveMiss();
            return;
        }

        _resolved = true;
        Destroy(gameObject);
    }

    private void ResolveMiss()
    {
        if (_resolved) return;
        _resolved = true;

        if (snatcherManager != null && ownerZone != 0)
        {
            snatcherManager.KillSnatcher(ownerZone);
        }

        Destroy(gameObject);
    }
}
