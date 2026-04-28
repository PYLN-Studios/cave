using System;
using FMODUnity;
using UnityEngine;

namespace UnityEngine.SoundManager
{
    [CreateAssetMenu(fileName = "AudioCatalog", menuName = "Audio/Audio Catalog")]
    public class AudioCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public SoundType soundType;
            public AudioClip[] clips;
            public EventReference fmodEvent;
        }

        [SerializeField] private Entry[] entries;

        public AudioClip GetRandomClip(SoundType soundType)
        {
            if (entries == null || entries.Length == 0)
                return null;

            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].soundType != soundType)
                    continue;

                AudioClip[] clips = entries[i].clips;
                if (clips == null || clips.Length == 0)
                    return null;

                return clips[UnityEngine.Random.Range(0, clips.Length)];
            }

            return null;
        }

        public EventReference GetEvent(SoundType soundType)
        {
            if (entries == null || entries.Length == 0)
                return default;

            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].soundType == soundType)
                    return entries[i].fmodEvent;
            }

            return default;
        }
    }
}
