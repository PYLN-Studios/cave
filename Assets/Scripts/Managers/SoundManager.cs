using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using System;

/*
This is where we will be putting all the sound types. Add anything to this part of the script! 
*/
namespace UnityEngine.SoundManager
{
    public enum SoundType
    {
        PLAYERFOOTSTEP,
        MAMMOTHFOOTSTEP,
        PAPAKAKAKA,
        DEAGLECSGO
    }

    [RequireComponent(typeof(AudioSource)), ExecuteInEditMode]
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private SoundList[] soundList;

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
            AudioClip[] clips = instance.soundList[(int)sound].Sounds;
            AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)]; 
            instance.audioSource.PlayOneShot(randomClip, volume); 
        }

        public void OnEnable()
        {
            string[] names = Enum.GetNames(typeof(SoundType));
            Array.Resize(ref soundList, names.Length);
            for (int i = 0; i < soundList.Length; i++)
            {
                soundList[i].name = names[i];
            }
        }

    }
    [Serializable]
    public struct SoundList
    {
        public AudioClip[] Sounds {get => sounds;}
        [HideInInspector] public string name;
        [SerializeField] private AudioClip[] sounds;
    }
}