using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/*
This is where we will be putting all the sound types. Add anything to this part of the script! 
*/
namespace UnityEngine.SoundManager
{
    public enum SoundType
    {
        PLAYERFOOTSTEP,
        MAMMOTHFOOTSTEP,
        PAPAKAKAKA
    }

    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private AudioClip[] soundList;

        public static SoundManager instance;
    
        private AudioSource audioSource;

        private void Awake()
        {   
            instance = this;
            audioSource = GetComponent<AudioSource>();
        }
        /*private void Start()
        {
            audioSource = GetComponent<AudioSource>();
        }
        */
        

        public static void PlaySound(SoundType sound, float volume = 1f)
        {
            if (instance == null)
            {
                Debug.LogError("SoundManager.instance is NULL. Put a SoundManager in the scene (enabled).");
                return;
            }

            if (instance.audioSource == null)
            {
                Debug.LogError("SoundManager.audioSource is NULL. Add an AudioSource to the same GameObject.");
                return;
            }

            int idx = (int)sound;

            if (instance.soundList == null)
            {
                Debug.LogError("SoundManager.soundList is NULL (not assigned).");
                return;
            }

            if (idx < 0 || idx >= instance.soundList.Length)
            {
                Debug.LogError($"SoundManager.soundList is too small. Need index {idx} for {sound}. Current size={instance.soundList.Length}");
                return;
            }

            if (instance.soundList[idx] == null)
            {
                Debug.LogError($"SoundManager.soundList[{idx}] for {sound} is NULL. Assign an AudioClip in the Inspector.");
                return;
            }

            instance.audioSource.PlayOneShot(instance.soundList[idx], volume);
        }

    }
}