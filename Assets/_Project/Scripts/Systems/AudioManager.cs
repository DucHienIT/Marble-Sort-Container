using UnityEngine;
using MarbleSort.Config;

namespace MarbleSort.Systems
{
    /// <summary>
    /// Synthesises every sound at runtime (no audio assets). Blips, chimes, a win arpeggio and a
    /// soft looping pad. Author this on a scene object; reference it from GameManager.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private AudioSource _sfx;
        private AudioSource _music;

        private AudioClip _tap, _intake, _deliver, _complete, _win, _lose, _rattle;
        private AudioClip _pad;

        private const int SR = 44100;

        private void Awake()
        {
            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;

            _tap = Blip(0.10f, 520f, 720f, 0.5f);
            _intake = Blip(0.09f, 380f, 300f, 0.45f);
            _deliver = Chime(new float[] { 660f, 990f }, 0.16f, 0.5f);
            _complete = Chime(new float[] { 523f, 659f, 784f, 1046f }, 0.32f, 0.55f);
            _win = Chime(new float[] { 523f, 659f, 784f, 1046f, 1318f }, 0.7f, 0.6f);
            _lose = Blip(0.4f, 300f, 90f, 0.5f);
            _rattle = Noise(0.12f, 0.25f);
            _pad = Pad(4f);
        }

        // ---- public sfx --------------------------------------------------
        public void PlayTap() { Sfx(_tap, 1f); }
        public void PlayIntake() { Sfx(_intake, 0.8f); }
        public void PlayDeliver() { Sfx(_deliver, 0.7f); }
        public void PlayComplete() { Sfx(_complete, 0.9f); }
        public void PlayWin() { Sfx(_win, 1f); }
        public void PlayLose() { Sfx(_lose, 0.9f); }
        public void PlayRattle() { Sfx(_rattle, 0.5f); }

        /// <summary>Ball release blip whose pitch rises with progress for "juice".</summary>
        public void PlayRelease(int index, int total)
        {
            float t = total <= 1 ? 0f : index / (float)(total - 1);
            _sfx.pitch = Mathf.Lerp(0.9f, 1.5f, t);
            Sfx(_intake, 0.35f);
            _sfx.pitch = 1f;
        }

        public void StartMusic()
        {
            if (!SaveSystem.MusicOn) { _music.Stop(); return; }
            _music.clip = _pad;
            _music.volume = Tune.MusicVolume;
            if (!_music.isPlaying) _music.Play();
        }

        public void StopMusic() { _music.Stop(); }
        public void RefreshMusic() { if (SaveSystem.MusicOn) StartMusic(); else StopMusic(); }

        private void Sfx(AudioClip clip, float vol)
        {
            if (clip == null || !SaveSystem.SfxOn) return;
            _sfx.PlayOneShot(clip, vol * Tune.SfxVolume);
        }

        // ---- synthesis ---------------------------------------------------

        private AudioClip Blip(float dur, float startFreq, float endFreq, float amp)
        {
            int n = Mathf.CeilToInt(SR * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float k = i / (float)n;
                float freq = Mathf.Lerp(startFreq, endFreq, k);
                float env = Mathf.Exp(-6f * k);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * amp;
            }
            return Make("blip", data);
        }

        private AudioClip Chime(float[] freqs, float dur, float amp)
        {
            int n = Mathf.CeilToInt(SR * dur);
            var data = new float[n];
            float per = dur / freqs.Length;
            for (int f = 0; f < freqs.Length; f++)
            {
                int start = Mathf.FloorToInt(f * per * SR);
                int end = Mathf.Min(n, Mathf.FloorToInt((f * per + per * 2.2f) * SR));
                for (int i = start; i < end; i++)
                {
                    float lt = (i - start) / (float)SR;
                    float env = Mathf.Exp(-5f * (i - start) / (float)(end - start));
                    data[i] += Mathf.Sin(2f * Mathf.PI * freqs[f] * lt) * env * amp * 0.6f;
                }
            }
            return Make("chime", data);
        }

        private AudioClip Noise(float dur, float amp)
        {
            int n = Mathf.CeilToInt(SR * dur);
            var data = new float[n];
            var rng = new System.Random(1234);
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)n;
                float env = Mathf.Exp(-8f * k);
                data[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * env * amp;
            }
            return Make("noise", data);
        }

        private AudioClip Pad(float dur)
        {
            int n = Mathf.CeilToInt(SR * dur);
            var data = new float[n];
            float[] freqs = { 130.8f, 164.8f, 196f }; // C E G
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float window = Mathf.Sin(Mathf.PI * i / n); // fade in/out so the loop doesn't click
                float s = 0f;
                for (int f = 0; f < freqs.Length; f++)
                    s += Mathf.Sin(2f * Mathf.PI * freqs[f] * t) * 0.16f;
                s += Mathf.Sin(2f * Mathf.PI * 65.4f * t) * 0.10f; // sub
                data[i] = s * (0.5f + 0.5f * window);
            }
            return Make("pad", data);
        }

        private AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
