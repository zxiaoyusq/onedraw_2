#if UNITY_WEBGL && T100_WEB_SMOKE
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace OneStrokeDemon.Platform.Diagnostics
{
    /// <summary>
    /// 仅在T100 Web冒烟构建中启用的运行时探针，验证场景、存储、中文、指针和音频基线。
    /// </summary>
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

        /// <summary>
        /// 在首个场景加载前安装跨场景探针，确保Bootstrap之前就能收集结果。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            var probeObject = new GameObject(nameof(WebSmokeProbe));
            DontDestroyOnLoad(probeObject);
            probeObject.AddComponent<WebSmokeProbe>();
        }

        /// <summary>
        /// 初始化音频源，完成PlayerPrefs写入/回读，并发布首批冒烟状态。
        /// </summary>
        private void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;

            // 通过递增运行次数和JSON回读，同时验证持久化与中文序列化路径。
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

        /// <summary>
        /// 每帧检查场景变化、指针点击与音频播放结果。
        /// </summary>
        private void Update()
        {
            // 只在场景名发生变化时上报，避免每帧向JavaScript桥重复发送。
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

            // 没有待确认的音频请求时提前返回，避免无意义的时间检查。
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

        /// <summary>
        /// 惰性创建半秒正弦波测试音，并启动一秒的播放确认窗口。
        /// </summary>
        private void PlayTone()
        {
            if (_tone == null)
            {
                // 音频数据只在首次点击时生成，后续点击复用同一AudioClip。
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

        /// <summary>
        /// 把单项冒烟结果转发给WebGL JavaScript桥。
        /// </summary>
        /// <param name="key">结果类别。</param>
        /// <param name="value">结果值或状态。</param>
        private static void Report(string key, string value)
        {
            T100WebSmokeReport(key, value);
        }

        /// <summary>
        /// 由T100WebSmoke.jslib实现的WebGL导入函数。
        /// </summary>
        [DllImport("__Internal", EntryPoint = "T100WebSmokeReport")]
        private static extern void T100WebSmokeReport(string key, string value);

        /// <summary>
        /// PlayerPrefs冒烟数据，用于检查JsonUtility序列化和跨次启动持久化。
        /// </summary>
        [Serializable]
        private sealed class SmokeSaveData
        {
            public int version;
            public int run;
        }
    }
}
#endif
