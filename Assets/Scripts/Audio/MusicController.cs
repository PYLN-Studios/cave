using System;
using FMOD.Studio;
using FMODUnity;
using UnityEngine.SceneManagement;

namespace UnityEngine.SoundManager
{
    internal sealed class MusicController : IDisposable
    {
        private readonly string menuSceneName;
        private readonly EventReference menuMusicEvent;
        private readonly float menuMusicVolume;

        private EventInstance menuMusicInstance;

        public MusicController(string menuSceneName, EventReference menuMusicEvent, float menuMusicVolume)
        {
            this.menuSceneName = menuSceneName;
            this.menuMusicEvent = menuMusicEvent;
            this.menuMusicVolume = menuMusicVolume;
        }

        public void HandleSceneMusic(Scene scene)
        {
            bool isMenuScene = string.Equals(scene.name, menuSceneName, StringComparison.OrdinalIgnoreCase);
            if (isMenuScene)
            {
                StartMenuMusicIfNeeded();
            }
            else
            {
                StopMenuMusic();
            }
        }

        private void StartMenuMusicIfNeeded()
        {
            if (menuMusicEvent.IsNull)
                return;

            if (!menuMusicInstance.isValid())
            {
                menuMusicInstance = RuntimeManager.CreateInstance(menuMusicEvent);
                menuMusicInstance.setVolume(menuMusicVolume);
            }

            menuMusicInstance.getPlaybackState(out PLAYBACK_STATE playbackState);
            if (playbackState == PLAYBACK_STATE.PLAYING || playbackState == PLAYBACK_STATE.STARTING)
                return;

            menuMusicInstance.start();
        }

        private void StopMenuMusic()
        {
            if (!menuMusicInstance.isValid())
                return;

            menuMusicInstance.getPlaybackState(out PLAYBACK_STATE playbackState);
            if (playbackState == PLAYBACK_STATE.PLAYING || playbackState == PLAYBACK_STATE.STARTING)
            {
                menuMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }
        }

        public void Dispose()
        {
            if (!menuMusicInstance.isValid())
                return;

            menuMusicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            menuMusicInstance.release();
            menuMusicInstance.clearHandle();
        }
    }
}
