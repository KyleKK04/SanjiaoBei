
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Data;
using Game.Core;
using UnityEngine.UI;

namespace Game.Data
{
    public class ScrollController : GridObject
    {
        [Header("Status")]
        public bool isCollected = false;

        [Header("Data")]
        [TextArea] public string scrollText = "You found a scroll...";
        public TextAsset textFile; // 可选：从文件读取

        [Header("UI References")]
        public GameObject textPanel; // UI面板 (Image + Text)
        public Text textLabel;       // 用于显示文字的 Text 组件
        
        [Header("Settings")]
        public float textSpeed = 0.05f;

        private bool textFinished = false;
        private bool cancelTyping = false;
        private List<string> textList = new List<string>();
        private int currentIndex = 0;

        public override void Init(int x, int y, Direction dir)
        {
            base.Init(x, y, dir);
            gridObjectType = GridObjectType.Scroll;
            isBlockingMovement = false; // 允许玩家走上来
            
            // 初始化隐藏UI
            if (textPanel != null) textPanel.SetActive(false);
            
            // 准备文本
            PrepareText();
        }

        private void PrepareText()
        {
            textList.Clear();
            if (textFile != null)
            {
                var lines = textFile.text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                textList.AddRange(lines);
            }
            else
            {
                // 如果没有文件，使用 inspector 里的字符串
                if (!string.IsNullOrEmpty(scrollText))
                {
                    textList.Add(scrollText);
                }
            }
        }

        // 被 LevelManager 调用
        public void OnCollected()
        {
            if (!isCollected)
            {
                isCollected = true;
                GameManager.Instance.CollectScroll();

                Debug.Log("Scroll Collected!");

                // 1. 隐藏卷轴图片 (不再在场景中显示)
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = false;

                // 2. 启动 UI 流程
                if (textPanel != null && textLabel != null && textList.Count > 0)
                {
                    textPanel.SetActive(true);
                    currentIndex = 0;
                    StartCoroutine(SetTextUI());
                }
                else
                {
                    // 如果没有UI配置，直接自我销毁
                    Debug.LogWarning("Scroll has no UI assigned or no text!");
                    Destroy(gameObject);
                }
            }
        }

        private void Update()
        {
            // 只有当面板激活且被收集时，才侦听点击
            if (isCollected && textPanel != null && textPanel.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    if (textFinished)
                    {
                        // 当前行打字完毕，去下一行
                        currentIndex++;
                        if (currentIndex >= textList.Count)
                        {
                            // 全部读完，关闭
                            CloseUI();
                        }
                        else
                        {
                            // 播放下一行
                            StartCoroutine(SetTextUI());
                        }
                    }
                    else
                    {
                        // 正在打字，加速显示（直接显示全）
                        cancelTyping = true;
                    }
                }
            }
        }

        private void CloseUI()
        {
            if (textPanel != null) textPanel.SetActive(false);
            // 对话结束，彻底销毁卷轴物体
            Destroy(gameObject);
        }

        IEnumerator SetTextUI()
        {
            textFinished = false;
            cancelTyping = false;
            textLabel.text = "";

            string currentLine = textList[currentIndex];

            for (int i = 0; i < currentLine.Length; i++)
            {
                // 如果用户点击了，取消打字，直接显示全部
                if (cancelTyping)
                {
                    textLabel.text = currentLine;
                    break;
                }

                textLabel.text += currentLine[i];
                yield return new WaitForSeconds(textSpeed);
            }

            // 确保最后是完整的文字
            textLabel.text = currentLine;
            textFinished = true;
            cancelTyping = false;
        }
    }
}