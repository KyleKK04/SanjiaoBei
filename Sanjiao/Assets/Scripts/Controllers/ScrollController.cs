
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
        [Header("Status")] public bool isCollected = false;

        [Header("Data")] [TextArea] public string scrollText = "You found a scroll...";
        public TextAsset textFile; // 可选：从文件读取

        [Header("UI References")] public GameObject textPanel; // UI面板 (Image + Text)
        public Text textLabel; // 用于显示文字的 Text 组件

        [Header("Settings")] public float textSpeed = 0.05f;

        private bool textFinished = false;
        private bool cancelTyping = false;
        private List<string> textList = new List<string>();
        private int currentIndex = 0;
        private List<DialogueLine> dialogueLines = new List<DialogueLine>();

        public override void Init(int x, int y, Direction dir)
        {
            base.Init(x, y, dir);
            gridObjectType = GridObjectType.Scroll;
            isBlockingMovement = false; // 允许玩家走上来
            setText();
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
                DialogueManager.Instance.ShowDialogue(dialogueLines);
                
                
            }
        }

        private void Update()
        {

        }


        private void setText()
        {
            dialogueLines.Clear(); // 【新增】防止重复添加
            DialogueLine line1 = new DialogueLine();
            line1.Content = "神说，要有光，便有了光。";
            line1.CharacterSprite = null;
            dialogueLines.Add(line1);
            Debug.Log($"Scroll Dialogue Init. Count: {dialogueLines.Count}");
        }
    }
}