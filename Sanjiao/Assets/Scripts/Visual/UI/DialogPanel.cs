using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Game.Data;
using Game.Core;
using UniRx;
using UnityEngine.EventSystems;

namespace Game.Visual
{
    public class DialoguePanel : MonoBehaviour
    {
        [Header("UI References")] [SerializeField]
        private Image characterImage;
        [SerializeField] private Button dialogueButton;

        [SerializeField] private TextMeshProUGUI contentText;

        [Header("Settings")] [SerializeField] private float typingSpeed = 0.05f; // 每个字的时间
        [SerializeField] private float fadeDuration = 0.3f; // 立绘淡入淡出时间

        private Queue<DialogueLine> dialogueQueue = new Queue<DialogueLine>();
        private bool isTyping = false;
        private string currentFullText = "";
        private Tweener typeTweener;

        private void OnEnable()
        {
            // 初始化状态
            if (characterImage) characterImage.color = new Color(1, 1, 1, 0); // 初始立绘透明
            contentText.text = "";
        }

        private void Awake()
        {
            dialogueButton.OnClickAsObservable().Subscribe(_ =>
            {
                if (isTyping)
                {
                    // 如果正在打字，直接显示全
                    if (typeTweener != null) typeTweener.Complete();
                    isTyping = false;
                }
                else
                {
                    // 如果打字结束，播放下一句
                    ShowNextLine();
                }
            }).AddTo(this);
        }

        /// <summary>
        /// 开始一组新的对话
        /// </summary>
        public void StartDialogue(List<DialogueLine> lines)
        {
            dialogueQueue.Clear();
            foreach (var line in lines)
            {
                dialogueQueue.Enqueue(line);
            }

            ShowNextLine();
        }

        /// <summary>
        /// 显示下一句
        /// </summary>
        private void ShowNextLine()
        {
            
            if (dialogueQueue.Count == 0)
            {
                EndDialogue();
                return;
            }
            DialogueLine line = dialogueQueue.Dequeue();
            
            StartCoroutine(AnimateLine(line));
        }

        IEnumerator AnimateLine(DialogueLine line)
        {
            isTyping = true;


            // 2. 切换立绘 (带淡入淡出效果)
            if (line.CharacterSprite != null)
            {
                // 如果当前显示的不是同一张图，或者当前是透明的，就做切换动画
                if (characterImage.sprite != line.CharacterSprite || characterImage.color.a < 0.1f)
                {
                    // 先淡出旧的(如果存在)
                    if (characterImage.color.a > 0.1f)
                        yield return characterImage.DOFade(0, fadeDuration * 0.5f).SetUpdate(true).WaitForCompletion();

                    characterImage.sprite = line.CharacterSprite;

                    // 再淡入新的
                    characterImage.DOFade(1, fadeDuration).SetUpdate(true);
                }
            }
            else
            {
                // 如果没有立绘，淡出隐藏
                characterImage.DOFade(0, fadeDuration).SetUpdate(true);
            }

            // 3. 打字机效果
            contentText.text = "";
            currentFullText = line.Content;

            // 计算总时长
            float duration = line.Content.Length * typingSpeed;

            // 杀掉旧的动画防止冲突
            if (typeTweener != null) typeTweener.Kill();

            // DOText 动画
            // --- 新代码 (通用写法) ---
            contentText.text = ""; // 先清空

// 使用 DOTween.To 逐字显示
            typeTweener = DOTween.To(() => contentText.text, x => contentText.text = x, line.Content, duration)
                .SetEase(Ease.Linear)
                .SetUpdate(true) // 忽略 TimeScale
                .OnComplete(() =>
                {
                    isTyping = false;
                });
        }

        /// <summary>
        /// 点击屏幕处理
        /// </summary>

        private void EndDialogue()
        {
            DialogueManager.Instance.EndDialogue();
        }
    }
}