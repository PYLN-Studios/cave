using UnityEngine;

namespace UnityEngine.SoundManager
{
    internal sealed class SfxController
    {
        private readonly AudioCatalog audioCatalog;
        private readonly SoundList[] legacySoundList;
        private readonly float defaultMinDistance;
        private readonly float defaultMaxDistance;

        private readonly AudioSource[] pool;
        private int poolIndex;

        public SfxController(
            Transform owner,
            int poolSize,
            float defaultMinDistance,
            float defaultMaxDistance,
            AudioCatalog audioCatalog,
            SoundList[] legacySoundList)
        {
            this.audioCatalog = audioCatalog;
            this.legacySoundList = legacySoundList;
            this.defaultMinDistance = defaultMinDistance;
            this.defaultMaxDistance = defaultMaxDistance;

            int resolvedPoolSize = Mathf.Max(1, poolSize);
            pool = new AudioSource[resolvedPoolSize];
            BuildPool(owner);
        }

        private void BuildPool(Transform owner)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                GameObject go = new GameObject($"3D_Audio_{i}");
                go.transform.SetParent(owner, false);

                AudioSource src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 1f;
                src.minDistance = defaultMinDistance;
                src.maxDistance = defaultMaxDistance;
                src.rolloffMode = AudioRolloffMode.Logarithmic;

                pool[i] = src;
            }
        }

        public void Play3D(SoundType sound, Vector3 position, float volume, float? minDistance, float? maxDistance)
        {
            AudioClip clip = GetRandomClip(sound);
            if (clip == null)
                return;

            AudioSource src = GetNextSource();
            src.transform.position = position;
            src.spatialBlend = 1f;
            src.minDistance = minDistance ?? defaultMinDistance;
            src.maxDistance = maxDistance ?? defaultMaxDistance;
            src.volume = volume;
            src.Stop();
            src.PlayOneShot(clip);
        }

        public void Play2D(SoundType sound, float volume)
        {
            AudioClip clip = GetRandomClip(sound);
            if (clip == null)
                return;

            AudioSource src = GetNextSource();
            src.spatialBlend = 0f;
            src.volume = volume;
            src.Stop();
            src.PlayOneShot(clip);
            src.spatialBlend = 1f;
        }

        private AudioSource GetNextSource()
        {
            AudioSource src = pool[poolIndex];
            poolIndex = (poolIndex + 1) % pool.Length;
            return src;
        }

        private AudioClip GetRandomClip(SoundType sound)
        {
            if (audioCatalog != null)
            {
                AudioClip catalogClip = audioCatalog.GetRandomClip(sound);
                if (catalogClip != null)
                    return catalogClip;
            }

            int idx = (int)sound;
            if (legacySoundList == null || idx < 0 || idx >= legacySoundList.Length)
                return null;

            AudioClip[] clips = legacySoundList[idx].Sounds;
            if (clips == null || clips.Length == 0)
                return null;

            return clips[Random.Range(0, clips.Length)];
        }
    }
}
