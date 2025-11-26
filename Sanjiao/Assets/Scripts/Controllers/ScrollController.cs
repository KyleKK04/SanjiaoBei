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
        public bool isCollected = false;

        [TextArea] public String scrollText;

        public TextAsset textFile; // 指向文本文件的引用
        public Text uiTextBox; // 指向UI文本框的引用
        public Text textLabel;
        public GameObject textPanel; // 用于显示/隐藏文本面板
        public int index;
        public float textspeed = 0.05f;

        bool textFinished = false;
        bool cancelTyping = false;

        List<string> textList = new List<string>();

      
        public override void Init(int x, int y, Direction dir)
        {
            base.Init(x, y, dir);
            gridObjectType = GridObjectType.Scroll;
            isBlockingMovement = false; // 允许玩家走上来
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                OnCollected();
            }
        }

        // 如果不用 Trigger，也可以在 PlayerMovement 进入格子时调用
        public void OnCollected()
        {
            if(!isCollected)
            {
                isCollected = true;
                GameManager.Instance.CollectScroll();
                 
                // 原有的 UI 显示逻辑
                // Debug.Log("Scroll Collected!");
                 
                // 视觉上隐藏卷轴，但保留对象以显示 UI
                GetComponent<SpriteRenderer>().enabled = false;
                // 从网格逻辑中移除
                if(LevelManager.Instance != null) 
                    LevelManager.Instance.UpdateGrid(gridCoordinates.x, gridCoordinates.y, null);
            }
        }
    }
}