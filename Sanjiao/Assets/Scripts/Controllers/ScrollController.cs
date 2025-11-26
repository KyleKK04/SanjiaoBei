using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Data;
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
            this.gridObjectType = GridObjectType.Scroll;
        }
        private void Awake()
        {
            GetText(textFile);

            // 初始时隐藏文本面板
            if (textPanel != null)
                textPanel.SetActive(false);
        }

        private void OnEnable()
        {
            textFinished = true;
            StartCoroutine(SetTextUI());
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.CompareTag("Player") && !isCollected)
            {
                isCollected = true;
       
                //TODO:显示卷轴文本
                Debug.Log("卷轴被拾取，显示文本: " + scrollText);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Mouse0) && isCollected ) { 
                if (textFinished && index == textList.Count)
                {
                    // 文本显示完毕，关闭UI
                    if (textPanel != null)
                        textPanel.SetActive(false);

                    uiTextBox.text = "";
                    textLabel.text = "";
                    isCollected = false;
                    //关闭文本框的逻辑
                }
                else if (textFinished)
                {
                    StartCoroutine(SetTextUI());
                }
                else
                {
                    cancelTyping = true;
                }

            }
        }

        void GetText(TextAsset file)//获取文本
        {
            textList.Clear();
            index = 0;

            var lineData = file.text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (var line in lineData)
            {
                textList.Add(line);
            }
        }

        IEnumerator SetTextUI()
        {
            textFinished = false;
            textLabel.text = "";

            int letter = 0;
            while (!cancelTyping && letter < textList[index].Length - 1)
            {
                textLabel.text += textList[index][letter];
                letter++;
                yield return new WaitForSeconds(textspeed);
            }
            textLabel.text = textList[index];
            cancelTyping = false;
            textFinished = true;
            index++;
        }


    }
}