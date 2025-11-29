using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Visual
{
    [RequireComponent(typeof(Image))]
    public class LoadingEffect : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("动画帧速 (每秒播放多少张图)")]
        public float frameRate = 20f;

        [Tooltip("是否循环播放")]
        public bool loop = true;

        [Tooltip("按顺序把序列帧图片拖到这里")]
        public List<Sprite> sprites;

        private Image targetImage;
        private float timer;
        private int currentFrame;

        private void Awake()
        {
            targetImage = GetComponent<Image>();
        }

        private void OnEnable()
        {
            // 每次面板打开时，重置动画到第一帧
            PlayAnimation();
        }

        private void PlayAnimation()
        {
            currentFrame = 0;
            timer = 0f;
            if (sprites != null && sprites.Count > 0)
            {
                targetImage.sprite = sprites[0];
            }
        }

        private void Update()
        {
            if (sprites == null || sprites.Count == 0) return;

            // 使用 unscaledDeltaTime，确保 Time.timeScale = 0 (暂停) 时动画依然流畅
            timer += Time.unscaledDeltaTime;

            // 计算每一帧持续的时间 (1 / FPS)
            float secondsPerFrame = 1f / frameRate;

            if (timer >= secondsPerFrame)
            {
                // 扣除时间，进入下一帧
                timer -= secondsPerFrame;
                currentFrame++;

                if (currentFrame >= sprites.Count)
                {
                    if (loop)
                    {
                        currentFrame = 0; // 循环
                    }
                    else
                    {
                        currentFrame = sprites.Count - 1; // 停在最后一帧
                        return;
                    }
                }

                // 替换 UI Image 的图片
                targetImage.sprite = sprites[currentFrame];
            }
        }
    }
}