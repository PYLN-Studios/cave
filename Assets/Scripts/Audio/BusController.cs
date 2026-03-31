using FMOD.Studio;
using FMODUnity;
using System;

namespace UnityEngine.SoundManager
{
    internal sealed class BusController
    {
        private readonly string musicBusPath;
        private readonly string sfxBusPath;

        private Bus musicBus;
        private Bus sfxBus;
        private bool hasResolvedBuses;

        public BusController(string musicBusPath, string sfxBusPath)
        {
            this.musicBusPath = musicBusPath;
            this.sfxBusPath = sfxBusPath;
        }

        public void SetMusicVolume(float volume)
        {
            if (!TryResolveBuses())
                return;

            musicBus.setVolume(Mathf.Clamp01(volume));
        }

        public void SetSfxVolume(float volume)
        {
            if (!TryResolveBuses())
                return;

            sfxBus.setVolume(Mathf.Clamp01(volume));
        }

        private bool TryResolveBuses()
        {
            if (hasResolvedBuses)
                return musicBus.isValid() && sfxBus.isValid();

            hasResolvedBuses = true;
            try
            {
                musicBus = RuntimeManager.GetBus(musicBusPath);
                sfxBus = RuntimeManager.GetBus(sfxBusPath);
                return musicBus.isValid() && sfxBus.isValid();
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
