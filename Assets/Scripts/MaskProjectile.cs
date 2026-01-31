using UnityEngine;

[DisallowMultipleComponent]
public class MaskProjectile : MonoBehaviour
{
    public float speed = 14f;
    public float lifeSeconds = 6f;

    private float _lifeTimer;

    void Update()
    {
        transform.position += transform.forward * (speed * Time.deltaTime);

        if (lifeSeconds > 0f)
        {
            _lifeTimer += Time.deltaTime;
            if (_lifeTimer >= lifeSeconds)
            {
                Destroy(gameObject);
            }
        }
    }

    public void ResetLifetime()
    {
        _lifeTimer = 0f;
    }
}
