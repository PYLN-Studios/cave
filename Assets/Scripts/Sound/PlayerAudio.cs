using UnityEngine;
using UnityEngine.SoundManager;

public class PlayerAudio : MonoBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.spatialBlend = 1f; // Set to 3D sound
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    public void Play(SoundType soundType, float volume = 1f)
    {
        if (SoundManager.instance != null)
        {
            return;
        }
         AudioClip clip = SoundManager.instance.GetRandomClip(soundType);
        if (clip == null)
            return;

        audioSource.PlayOneShot(clip, volume);
    }
}
