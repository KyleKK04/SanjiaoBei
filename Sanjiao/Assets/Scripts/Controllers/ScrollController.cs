
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
        //创建15关的对话列表
        private List<DialogueLine> level1DialogueLines = new List<DialogueLine>();
        private List<DialogueLine> level2DialogueLines = new List<DialogueLine>();
        private List<DialogueLine> level3DialogueLines = new List<DialogueLine>();
        private List<DialogueLine> level4DialogueLines = new List<DialogueLine>();
        private List<DialogueLine> level5DialogueLines = new List<DialogueLine>();
        private List<DialogueLine> level6DialogueLines = new List<DialogueLine>();
        private List<DialogueLine> level7DialogueLines = new List<DialogueLine>();
        private List<DialogueLine> level8DialogueLines = new List<DialogueLine>();
        private List<DialogueLine> level9DialogueLines = new List<DialogueLine>();
        private List<DialogueLine> level10DialogueLines = new List<DialogueLine>();
        private List<DialogueLine> level11DialogueLines = new List<DialogueLine>();
        private List<DialogueLine> level12DialogueLines = new List<DialogueLine>();
        private List<DialogueLine> level13DialogueLines = new List<DialogueLine>();
        private List<DialogueLine> level14DialogueLines = new List<DialogueLine>();
        private List<DialogueLine> level15DialogueLines = new List<DialogueLine>();
        

        public override void Init(int x, int y, Direction dir)
        {
            base.Init(x, y, dir);
            gridObjectType = GridObjectType.Scroll;
            isBlockingMovement = false; // 允许玩家走上来
            SetText();
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
                AudioManager.Instance.PlaySFX("PickScroll");
                ShowDialog();                
                
            }
        }

        private void Update()
        {

        }

        private void ShowDialog()
        {
            // 根据当前关卡索引显示对应的对话
            if (currentIndex == 0) DialogueManager.Instance.ShowDialogue(level1DialogueLines);
            if (currentIndex == 1) DialogueManager.Instance.ShowDialogue(level2DialogueLines);
            if (currentIndex == 2) DialogueManager.Instance.ShowDialogue(level3DialogueLines);
            if (currentIndex == 3) DialogueManager.Instance.ShowDialogue(level4DialogueLines);
            if (currentIndex == 4) DialogueManager.Instance.ShowDialogue(level5DialogueLines);
            if (currentIndex == 5) DialogueManager.Instance.ShowDialogue(level6DialogueLines);
            if (currentIndex == 6) DialogueManager.Instance.ShowDialogue(level7DialogueLines);
            if (currentIndex == 7) DialogueManager.Instance.ShowDialogue(level8DialogueLines);
            if (currentIndex == 8) DialogueManager.Instance.ShowDialogue(level9DialogueLines);
            if (currentIndex == 9) DialogueManager.Instance.ShowDialogue(level10DialogueLines);
            if (currentIndex == 10) DialogueManager.Instance.ShowDialogue(level11DialogueLines);
            if (currentIndex == 11) DialogueManager.Instance.ShowDialogue(level12DialogueLines);
            if (currentIndex == 12) DialogueManager.Instance.ShowDialogue(level13DialogueLines);
            if (currentIndex == 13) DialogueManager.Instance.ShowDialogue(level14DialogueLines);
            if (currentIndex == 14) DialogueManager.Instance.ShowDialogue(level15DialogueLines);
            
        }
        
        private void SetText()
        {
            level1DialogueLines.Clear(); // 【新增】防止重复添加
            DialogueLine line1 = new DialogueLine();
            line1.Content = "神说，要有光，便有了光。";
            line1.CharacterSprite = null;
            level1DialogueLines.Add(line1);
            Debug.Log($"Scroll Dialogue Init. Count: {level1DialogueLines.Count}");
            
            DialogueLine line2 = new DialogueLine();
            line2.Content = "圣灵所说，凡有耳的，便应当听。";
            line2.CharacterSprite = null;
            level2DialogueLines.Add(line2);
            
            DialogueLine line3 = new DialogueLine();
            line3.Content = "我知道你的行为，爱心，信心，勤劳，忍耐。又知道你末后所行的善事，比起初所行的更多。";
            line3.CharacterSprite = null;
            level3DialogueLines.Add(line3);
            
            DialogueLine line4 = new DialogueLine();
            line4.Content = "我必快来，你要持守你所有的，免得人夺去你的冠冕。";
            line4.CharacterSprite = null;
            level4DialogueLines.Add(line4);
            
            DialogueLine line5 = new DialogueLine();
            line5.Content = "你要儆醒，坚固那剩下将要衰微的。";
            line5.CharacterSprite = null;
            level5DialogueLines.Add(line5);
            
            DialogueLine line6 = new DialogueLine();
            line6.Content = "若不儆醒，我必临到你那里如同贼一样。我几时临到，你也决不能知道。";
            line6.CharacterSprite = null;
            level6DialogueLines.Add(line6);
            
            DialogueLine line7 = new DialogueLine();
            line7.Content = "凡我所疼爱的，我就责备管教他。所以你要发热心，也要悔改。";
            line7.CharacterSprite = null;
            level7DialogueLines.Add(line7);
            
            DialogueLine line8 = new DialogueLine();
            line8.Content = "你既遵守我忍耐的道，我必在普天下人受试炼的时候，保守你免去你的试炼。";
            line8.CharacterSprite = null;
            level8DialogueLines.Add(line8);
            
            DialogueLine line9 = new DialogueLine();
            line9.Content = "你说，我是富足，已经发了财，一样都不缺。却不知道你是那困苦，可怜，贫穷，瞎眼，赤身的。";
            line9.CharacterSprite = null;
            level9DialogueLines.Add(line9);
            
            DialogueLine line10 = new DialogueLine();
            line10.Content = "天挪移，好像书卷被卷起来。山岭海岛都被挪移离开本位。";
            line10.CharacterSprite = null;
            level10DialogueLines.Add(line10);
            
            DialogueLine line11 = new DialogueLine();
            line11.Content = "在天上，地上，地底下，没有能展开能观看那书卷的，因为没有配展开，配观看那书卷的。";
            line11.CharacterSprite = null;
            level11DialogueLines.Add(line11);
            
            DialogueLine line12 = new DialogueLine();
            line12.Content = "我又看见，且听见，宝座与活物并长老的周围，有许多天使的声音。他们的数目有千千万万。";
            line12.CharacterSprite = null;
            level12DialogueLines.Add(line12);
            
            DialogueLine line13 = new DialogueLine();
            line13.Content = "他们大声说，曾被杀的羔羊，是配得权柄，丰富，智慧，能力，尊贵，荣耀，颂赞的。";
            line13.CharacterSprite = null;
            level13DialogueLines.Add(line13);
            
            DialogueLine line14 = new DialogueLine();
            line14.Content = "他们不再饥，不再渴。日头和炎热，也必不伤害他们。";
            line14.CharacterSprite = null;
            level14DialogueLines.Add(line14);
            
            DialogueLine line15 = new DialogueLine();
            line15.Content = "颂赞，荣耀，智慧，感谢，尊贵，权柄，大力，都归与我们的神，直到永永远远。阿门。";
            line15.CharacterSprite = null;
            level15DialogueLines.Add(line15);
        }
    }
}