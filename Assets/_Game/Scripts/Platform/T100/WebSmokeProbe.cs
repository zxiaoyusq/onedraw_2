#if UNITY_WEBGL && T100_WEB_SMOKE
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace OneStrokeDemon.Platform.Diagnostics
{
    internal sealed class WebSmokeProbe : MonoBehaviour
    {
        private const string RunKey = "t100_web_smoke_run";
        private const string SaveKey = "t100_web_smoke_save_v1";
        private const int SampleRate = 22050;
        private const int ToneSampleCount = SampleRate / 2;

        private AudioSource _audioSource;
        private AudioClip _tone;
        private bool _audioPending;
        private float _audioDeadline;
        private string _reportedScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            var probeObject = new GameObject(nameof(WebSmokeProbe));
            DontDestroyOnLoad(probeObject);
            probeObject.AddComponent<WebSmokeProbe>();
        }

        private void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;

            int run = PlayerPrefs.GetInt(RunKey, 0) + 1;
            var save = new SmokeSaveData { version = 1, run = run };
            string payload = JsonUtility.ToJson(save);
            PlayerPrefs.SetInt(RunKey, run);
            PlayerPrefs.SetString(SaveKey, payload);
            PlayerPrefs.Save();

            Report("runtime", "ready");
            Report("storage", PlayerPrefs.GetString(SaveKey) == payload ? "pass" : "fail");
            Report("storageRun", run.ToString());
            Report("chinese", "标准网页中文");
            Report("input", "waiting");
            Report("audio", "waiting");
        }

        private void Update()
        {
            string activeScene = SceneManager.GetActiveScene().name;
            if (_reportedScene != activeScene)
            {
                _reportedScene = activeScene;
                Report("scene", activeScene);
            }

            if (Pointer.current?.press.wasPressedThisFrame == true)
            {
                Report("input", "pass");
                PlayTone();
            }

            if (!_audioPending)
            {
                return;
            }

            if (_audioSource.isPlaying)
            {
                _audioPending = false;
                Report("audio", "pass");
            }
            else if (Time.unscaledTime >= _audioDeadline)
            {
                _audioPending = false;
                Report("audio", "fail");
            }
        }

        private void PlayTone()
        {
            if (_tone == null)
            {
                _tone = AudioClip.Create("T100WebSmokeTone", ToneSampleCount, 1, SampleRate, false);
                var samples = new float[ToneSampleCount];
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] = Mathf.Sin(2f * Mathf.PI * 440f * i / SampleRate) * 0.1f;
                }

                _tone.SetData(samples, 0);
            }

            _audioSource.clip = _tone;
            _audioSource.Play();
            _audioPending = true;
            _audioDeadline = Time.unscaledTime + 1f;
        }

        private static void Report(string key, string value)
        {
            T100WebSmokeReport(key, value);
        }

        [DllImport("__Internal", EntryPoint = "T100WebSmokeReport")]
        private static extern void T100WebSmokeReport(string key, string value);

        [Serializable]
        private sealed class SmokeSaveData
        {
            public int version;
            public int run;
        }
    }
}
#endif
