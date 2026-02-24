using UnityEngine;
using System;

namespace UnityEngine.SoundManager
{
    public enum SoundType
    {
        PLAYERFOOTSTEP,
        MAMMOTHFOOTSTEP
    }

    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private SoundList[] soundList;

        public static SoundManager instance;

        [Header("3D Audio Pool")]
        [SerializeField] private int poolSize = 16;
        [SerializeField] private float defaultMinDistance = 1.5f;
        [SerializeField] private float defaultMaxDistance = 20f;

        private AudioSource[] pool;
        private int poolIndex;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            BuildPool();
        }

        private void BuildPool()
        {
            pool = new AudioSource[poolSize];

            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject($"3D_Audio_{i}");
                go.transform.parent = transform;

                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 1f;            // 3D
                src.minDistance = defaultMinDistance;
                src.maxDistance = defaultMaxDistance;
                src.rolloffMode = AudioRolloffMode.Logarithmic;

                pool[i] = src;
            }
        }

        public AudioClip GetRandomClip(SoundType sound)
        {
            int idx = (int)sound;

            if (soundList == null || idx < 0 || idx >= soundList.Length)
                return null;

            var clips = soundList[idx].Sounds;
            if (clips == null || clips.Length == 0)
                return null;

            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        
        // Plays a 3D sound at a world position per player.
        public static void Play3D(SoundType sound, Vector3 position, float volume = 1f,
                          float? minDistance = null, float? maxDistance = null)
        {
            if (instance == null)
                    {
                        // Debug.LogError("SoundManager.instance is null. Put SoundManager in the scene.");
                        return;
                    }
                

            var clip = instance.GetRandomClip(sound);
            if (clip == null)
                    {
                        // Debug.LogWarning($"No clips assigned for {sound}");
                        return;
                    }
                

            var src = instance.pool[instance.poolIndex];
            instance.poolIndex = (instance.poolIndex + 1) % instance.pool.Length;

            src.transform.position = position;

            if (minDistance.HasValue)
                src.minDistance = minDistance.Value;
            else
                src.minDistance = instance.defaultMinDistance;

            if (maxDistance.HasValue)
                src.maxDistance = maxDistance.Value;
            else
                src.maxDistance = instance.defaultMaxDistance;

            src.volume = volume;

            src.Stop();
            src.PlayOneShot(clip);
        }

        // Old 2D Global Sound
        public static void Play2D(SoundType sound, float volume = 1f)
        {
            if (instance == null) return;
            var clip = instance.GetRandomClip(sound);
            if (clip == null) return;

            
            var src = instance.pool[instance.poolIndex];
            instance.poolIndex = (instance.poolIndex + 1) % instance.pool.Length;

            src.spatialBlend = 0f; 
            src.volume = volume;
            src.Stop();
            src.PlayOneShot(clip);

            src.spatialBlend = 1f; 
        }

        public void OnEnable()
        {
            string[] names = Enum.GetNames(typeof(SoundType));
            Array.Resize(ref soundList, names.Length);
            for (int i = 0; i < soundList.Length; i++)
                soundList[i].name = names[i];
        }
    }

    [Serializable]
    public struct SoundList
    {
        public AudioClip[] Sounds => sounds;
        [HideInInspector] public string name;
        [SerializeField] private AudioClip[] sounds;
    }
}