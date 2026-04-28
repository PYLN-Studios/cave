using System;
using FMODUnity;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace UnityEngine.SoundManager
{
    public enum SoundType
    {
        PLAYERFOOTSTEP,
        MAMMOTHFOOTSTEP
    }

    public class SoundManager : MonoBehaviour
    {
        public static SoundManager instance;

        [Header("SFX Catalog")]
        [SerializeField] private AudioCatalog audioCatalog;

        [Header("Legacy SFX Mapping (fallback)")]
        [FormerlySerializedAs("soundList")]
        [SerializeField] private SoundList[] legacySoundList;

        [Header("3D Audio Pool")]
        [SerializeField] private int poolSize = 16;
        [SerializeField] private float defaultMinDistance = 1.5f;
        [SerializeField] private float defaultMaxDistance = 20f;

        [Header("Main Menu Music")]
        [SerializeField] private string menuSceneName = "MainMenu";
        [SerializeField] private EventReference menuMusicEvent;
        [SerializeField] [Range(0f, 1f)] private float menuMusicVolume = 0.6f;

        [Header("FMOD Bus Paths")]
        [SerializeField] private string musicBusPath = "bus:/Music";
        [SerializeField] private string sfxBusPath = "bus:/SFX";

        private SfxController sfxController;
        private MusicController musicController;
        private BusController busController;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            sfxController = new SfxController(
                transform,
                poolSize,
                defaultMinDistance,
                defaultMaxDistance,
                audioCatalog,
                legacySoundList);

            musicController = new MusicController(menuSceneName, menuMusicEvent, menuMusicVolume);
            busController = new BusController(musicBusPath, sfxBusPath);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            musicController.HandleSceneMusic(SceneManager.GetActiveScene());
        }

        private void OnDestroy()
        {
            if (instance != this)
                return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (musicController != null)
                musicController.Dispose();

            instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            musicController.HandleSceneMusic(scene);
        }

        public static void Play3D(
            SoundType sound,
            Vector3 position,
            float volume = 1f,
            float? minDistance = null,
            float? maxDistance = null)
        {
            if (instance == null || instance.sfxController == null)
                return;

            instance.sfxController.Play3D(sound, position, volume, minDistance, maxDistance);
        }

        public static void Play2D(SoundType sound, float volume = 1f)
        {
            if (instance == null || instance.sfxController == null)
                return;

            instance.sfxController.Play2D(sound, volume);
        }

        public static void SetMusicVolume(float volume)
        {
            if (instance == null || instance.busController == null)
                return;

            instance.busController.SetMusicVolume(volume);
        }

        public static void SetSfxVolume(float volume)
        {
            if (instance == null || instance.busController == null)
                return;

            instance.busController.SetSfxVolume(volume);
        }

        private void OnEnable()
        {
            string[] names = Enum.GetNames(typeof(SoundType));
            Array.Resize(ref legacySoundList, names.Length);
            for (int i = 0; i < legacySoundList.Length; i++)
                legacySoundList[i].name = names[i];
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
