using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Game.Utilities; // 引用你的 Singleton 所在的命名空间

namespace Game.Core
{
    [System.Serializable]
    public struct SoundData
    {
        public string name;      // 音效名字，例如 "Walk", "Chant", "Win"
        public AudioClip clip;   // 音频文件
        [Range(0f, 1f)] public float volume; // 单独控制该音效的音量
    }

    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource; // 用于播放背景音乐
        [SerializeField] private AudioSource sfxSource; // 用于播放短音效

        [Header("Audio Library")]
        [SerializeField] private List<SoundData> soundList = new List<SoundData>();

        // 字典用于快速查找
        private Dictionary<string, SoundData> soundDict = new Dictionary<string, SoundData>();

        protected override void Awake()
        {
            base.Awake();
            
            // 初始化字典
            foreach (var sound in soundList)
            {
                if (!soundDict.ContainsKey(sound.name))
                {
                    soundDict.Add(sound.name, sound);
                }
            }
        }

        /// <summary>
        /// 播放短音效 (可以叠加)
        /// </summary>
        public void PlaySFX(string name)
        {
            if (soundDict.TryGetValue(name, out SoundData sound))
            {
                float currentSFMVolume = sfxSource.volume;
                // 【新增】防Bug机制：
                // 如果之前调用过 StopSFX 导致正在淡出，或者音量还是 0
                // 我们必须立刻杀掉动画，并把音量恢复，否则新声音会听不见
                sfxSource.DOKill(); 
                sfxSource.volume = currentSFMVolume;

                // PlayOneShot 允许声音叠加
                sfxSource.PlayOneShot(sound.clip, sound.volume);
            }
            else
            {
                Debug.LogWarning($"AudioManager: 找不到音效 [{name}]");
            }
        }

        /// <summary>
        /// 播放背景音乐 (循环)
        /// </summary>
        public void PlayBGM(string name)
        {
            if (soundDict.TryGetValue(name, out SoundData sound))
            {
                // 如果已经在播放这首，就不重置
                if (bgmSource.clip == sound.clip && bgmSource.isPlaying) return;

                bgmSource.clip = sound.clip;
                bgmSource.volume = sound.volume;
                bgmSource.loop = true;
                bgmSource.Play();
            }
            else
            {
                Debug.LogWarning($"AudioManager: 找不到BGM [{name}]");
            }
        }

        public void StopBGM(float duration = 0.5f)
        {
            // 杀掉该物体上可能正在进行的旧动画
            float currentBGMVolume = bgmSource.volume;
            bgmSource.DOKill();

            // 执行淡出：从当前音量 -> 0
            bgmSource.DOFade(0f, duration).OnComplete(() =>
            {
                bgmSource.Stop();
                bgmSource.volume = currentBGMVolume; // 【重要】停止后立刻恢复音量，为下一次播放做准备
            });
        }
        
        public void StopSFX(float duration = 0.5f)
        {
            // 杀掉该物体上可能正在进行的旧动画
            float currentSFXVolume = sfxSource.volume;
            sfxSource.DOKill();

            // 执行淡出：从当前音量 -> 0
            sfxSource.DOFade(0f, duration).OnComplete(() =>
            {
                sfxSource.Stop();
                sfxSource.volume = currentSFXVolume; // 【重要】停止后立刻恢复音量，为下一次播放做准备
            });
        }
    }
}