using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Game.Visual
{
    public class EndPanel : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI text;

        [Header("Content")]
        [TextArea(3, 10)]
        public String realWinText;
        
        [TextArea(3, 10)]
        public String fakeWinText; // 帮你修正了变量名大小写 fakeWInText -> fakeWinText

        [Header("Settings")]
        [Tooltip("每个字的显示时间 (秒)")]
        public float typingSpeed = 0.1f;

        private Tweener typeTweener;

        private void OnEnable()
        {
            // 每次面板打开时清空文本，防止看到残留内容
            if (text != null) text.text = "";
        }

        /// <summary>
        /// 显示结局文本
        /// </summary>
        /// <param name="correct">true: 真结局, false: 普通结局</param>
        public void ShowEndText(bool correct)
        {
            // 1. 决定显示哪段话
            string content = correct ? realWinText : fakeWinText;

            if (text == null) return;

            // 2. 重置状态
            text.text = "";
            // 杀掉旧动画，防止冲突
            if (typeTweener != null) typeTweener.Kill();

            // 3. 计算总时长
            float duration = content.Length * typingSpeed;

            // 4. 执行打字机动画
            // 使用 DOTween.To 逐字显示，兼容性最好
            typeTweener = DOTween.To(() => text.text, x => text.text = x, content, duration)
                .SetEase(Ease.Linear)
                .SetUpdate(true); // 关键：忽略 TimeScale，确保暂停时也能播放
        }

        // 养成好习惯：销毁时清理动画
        private void OnDestroy()
        {
            if (typeTweener != null) typeTweener.Kill();
        }
    }
}