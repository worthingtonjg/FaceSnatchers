using UnityEngine;

[DisallowMultipleComponent]
public class HostAudio : MonoBehaviour
{
    [Tooltip("AudioSource to play the jump/leave sound.")]
    public AudioSource audioSource;

    [Tooltip("Clip played when the snatcher leaves this host.")]
    public AudioClip leaveHostClip;

    [Tooltip("Clip played when the snatcher dies while in this host.")]
    public AudioClip deathClip;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void PlayLeaveHost()
    {
        if (audioSource == null || leaveHostClip == null) return;
        audioSource.PlayOneShot(leaveHostClip);
    }

    public void PlayDeath()
    {
        if (audioSource == null || deathClip == null) return;
        audioSource.PlayOneShot(deathClip);
    }
}
