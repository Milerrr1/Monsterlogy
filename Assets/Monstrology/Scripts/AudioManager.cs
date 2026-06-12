using UnityEngine;

namespace Monstrology
{
    public class AudioManager : MonoBehaviour
    {
        public const string MusicVolumeKey = "MusicVolume";
        public const string SfxVolumeKey = "SfxVolume";

        private float musicVolume;
        private float sfxVolume;

        public float MusicVolume { get { return musicVolume; } }
        public float SfxVolume { get { return sfxVolume; } }

        public void Initialize()
        {
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 0.7f));
            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f));
            ApplyToSceneSources();
        }

        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
            PlayerPrefs.Save();
            ApplyToSceneSources();
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
            PlayerPrefs.Save();
            ApplyToSceneSources();
        }

        public void ApplyVolume(AudioSource source, bool music)
        {
            if (source != null)
            {
                source.volume = music ? musicVolume : sfxVolume;
            }
        }

        public void PlaySfx(AudioClip clip, Vector3 position)
        {
            if (clip != null && sfxVolume > 0f)
            {
                AudioSource.PlayClipAtPoint(clip, position, sfxVolume);
            }
        }

        private void ApplyToSceneSources()
        {
            foreach (AudioSource source in FindObjectsOfType<AudioSource>())
            {
                if (source != null)
                {
                    source.volume = source.loop ? musicVolume : sfxVolume;
                }
            }
        }
    }
}
