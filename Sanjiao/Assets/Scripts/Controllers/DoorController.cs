using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using DG.Tweening;
using TMPro;
using UnityEditor.Rendering;

namespace Game.Data
{
    public class DoorController : GridObject
    {
        public int requiredPower = 3;
        public DoorType doorType = DoorType.EndDoor; // 【新增】
        private bool isPowered = false;
        public List<SpriteRenderer> doorSprites;
        public List<Sprite> nubmerSprites; // 用于显示数字的Sprite列表
        public Sprite openSprite;
        
        [Header("Dialog Texts")]
        private List<DialogueLine> level1Dialog = new List<DialogueLine>();
        private List<DialogueLine> level2Dialog = new List<DialogueLine>();
        private List<DialogueLine> level3Dialog = new List<DialogueLine>();
        private List<DialogueLine> level4Dialog = new List<DialogueLine>();
        private List<DialogueLine> level5Dialog = new List<DialogueLine>();
        private List<DialogueLine> level6Dialog = new List<DialogueLine>();
        private List<DialogueLine> level7Dialog = new List<DialogueLine>();
        private List<DialogueLine> level8Dialog = new List<DialogueLine>();
        private List<DialogueLine> level9Dialog = new List<DialogueLine>();
        private List<DialogueLine> level10Dialog = new List<DialogueLine>();
        private List<DialogueLine> level11Dialog = new List<DialogueLine>();
        private List<DialogueLine> level12Dialog = new List<DialogueLine>();
        private List<DialogueLine> level13Dialog = new List<DialogueLine>();
        private List<DialogueLine> level14Dialog = new List<DialogueLine>();
        private List<DialogueLine> level15Dialog = new List<DialogueLine>();

        public void SetDoorData(int power, DoorType type)
        {
            requiredPower = power;
            doorType = type;
            // 这里可以根据 type 更换不同的 Sprite，比如起点门是灰色的，终点门是金色的
            if (doorSprites != null && doorSprites.Count >= 2)
            {
                var numberSpriteRenderer = doorSprites[1];

                if (doorType == DoorType.EndDoor)
                {
                    // 终点门：显示数字
                    numberSpriteRenderer.gameObject.SetActive(true); // 确保显示
                    if (requiredPower > 0 && requiredPower <= nubmerSprites.Count)
                    {
                        numberSpriteRenderer.sprite = nubmerSprites[requiredPower - 1];
                    }
                    SetText();
                }
                else
                {
                    // 起点门：隐藏数字图片
                    numberSpriteRenderer.gameObject.SetActive(false); 
                    // 或者: numberSpriteRenderer.sprite = null;
                }
            }
        }

        public override void Init(int x, int y, Direction dir)
        {
            base.Init(x, y, dir);
            gridObjectType = GridObjectType.Door;
            isBlockingMovement = true; // 两种门都阻挡移动
        }

        public override void OnChant(int powerLevel, Direction inputDir)
        {
            // 【修改】只有终点门才响应咏唱充能
            if (doorType == DoorType.EndDoor)
            {
                if (GameManager.Instance.HasScroll && powerLevel >= requiredPower)
                {
                    isPowered = true;
                    AudioManager.Instance.PlaySFX("GateActive");
                    Debug.Log("End Door Powered Up!");
                }
            }
            else
            {
                // 起点门只是单纯的阻挡咏唱，不发生逻辑
                Debug.Log("Chant hit Begin Door (Blocked).");
            }
        }

        public override void Interact()
        {
            // 【修改】只有终点门可以交互
            if (doorType == DoorType.EndDoor)
            {
                if (GameManager.Instance.HasScroll && isPowered)
                {
                    //使Door缓慢透明并消失并不再BlockMovement
                    foreach (SpriteRenderer sprite in doorSprites)
                    {
                        sprite.DOFade(0, 0.5f);
                    }
                    
                    ShowDialog();
                    
                    DOVirtual.DelayedCall(1f, () =>
                    {
                        isBlockingMovement = false;
                        doorSprites[0].sprite = openSprite;
                        doorSprites[0].DOFade(0.7f, 0.01f);
                        
                        if (LevelManager.Instance.GetCurrentLevelIndex() == 14) 
                        {
                            LevelManager.Instance.OpenBeginDoor();
                        }
                    });
                }
                else
                {
                    Debug.Log($"Door Locked. Scroll:{GameManager.Instance.HasScroll}, Powered:{isPowered}");
                }
            }
            else
            {
                // 起点门无法交互
                Debug.Log("This is the entrance (Begin Door), cannot interact.");
            }
        }

        public void ForceOpen()
        {
            foreach (SpriteRenderer sprite in doorSprites)
            {
                sprite.DOFade(0, 0.5f);
            }
            
            DOVirtual.DelayedCall(1f, () =>
            {
                isBlockingMovement = false;
                doorSprites[0].sprite = openSprite;
                doorSprites[0].DOFade(0.7f, 0.01f);
                Debug.Log("Door Force Opened!");
            });
        }
        
        private void SetText()
        {
            DialogueLine line1 = new DialogueLine();
            line1.Content = "主啊，我无时无刻地聆听着你。请您告诉我，我将往何处去？";
            line1.CharacterSprite = DialogueManager.Instance.angel;
            level1Dialog.Add(line1);
            
            DialogueLine line2 = new DialogueLine();
            line2.Content = "（走向终点大门即可通关。）";
            line2.CharacterSprite = null;
            level1Dialog.Add(line2);
            
            DialogueLine line3 = new DialogueLine();
            line3.Content = "主啊，我是如此的相信您。请指引我，指引我到光明的远方去。";
            line3.CharacterSprite = DialogueManager.Instance.angel;
            level2Dialog.Add(line3);
            
            DialogueLine line4 = new DialogueLine();
            line4.Content = "主啊，我知道的，您在考验我的赤诚，倾听我的声音是否如光般纯洁无暇。";
            line4.CharacterSprite = DialogueManager.Instance.angel;
            level4Dialog.Add(line4);
            
            DialogueLine line5 = new DialogueLine();
            line5.Content = "请您放心，我会将您的教诲传得更远，更远。";
            line5.CharacterSprite = DialogueManager.Instance.angel;
            level4Dialog.Add(line5);
            
            DialogueLine line6 = new DialogueLine();
            line6.Content = "我已经没有耐心再次告诫你，你的阻挡是无用的。";
            line6.CharacterSprite = DialogueManager.Instance.angel;
            level5Dialog.Add(line6);
            
            DialogueLine line7 = new DialogueLine();
            line7.Content = "…";
            line7.CharacterSprite = DialogueManager.Instance.devil;
            level5Dialog.Add(line7);
            
            DialogueLine line8 = new DialogueLine();
            line8.Content = "（瞟了一眼）";
            line8.CharacterSprite = DialogueManager.Instance.angel;
            level6Dialog.Add(line8);
            
            DialogueLine line9 = new DialogueLine();
            line9.Content = "（怡然自得）";
            line9.CharacterSprite = DialogueManager.Instance.devil;
            level6Dialog.Add(line9);
            
            DialogueLine line10 = new DialogueLine();
            line10.Content = "（啧！欸，眼不见心不烦…眼不见心不烦…眼不见心不烦…）";
            line10.CharacterSprite = DialogueManager.Instance.angel;
            level6Dialog.Add(line10);
            
            DialogueLine line11 = new DialogueLine();
            line11.Content = "喂，好好学生。你的功课做的很足嘛。";
            line11.CharacterSprite = DialogueManager.Instance.devil;
            level8Dialog.Add(line11);
            
            DialogueLine line12 = new DialogueLine();
            line12.Content = "！注意你的行为！你刚才可是把我吓得不轻啊！";
            line12.CharacterSprite = DialogueManager.Instance.angel;
            level8Dialog.Add(line12);
            
            DialogueLine line13 = new DialogueLine();
            line13.Content = "你大概不知道吧？你很好骗哦？";
            line13.CharacterSprite = DialogueManager.Instance.devil;
            level8Dialog.Add(line13);
            
            DialogueLine line14 = new DialogueLine();
            line14.Content = "神自会承载信者。";
            line14.CharacterSprite = DialogueManager.Instance.angel;
            level8Dialog.Add(line14);
            
            DialogueLine line15 = new DialogueLine();
            line15.Content = "我一定要到达最光明处…";
            line15.CharacterSprite = DialogueManager.Instance.angel;
            level10Dialog.Add(line15);
            
            DialogueLine line16 = new DialogueLine();
            line16.Content = "祈祷真的有用吗？还是只是自己对外界的逃避？";
            line16.CharacterSprite = DialogueManager.Instance.devil;
            level10Dialog.Add(line16);
            
            DialogueLine line17 = new DialogueLine();
            line17.Content = "秩序与规则，是神的教诲，还是束缚思想的锁链？";
            line17.CharacterSprite = DialogueManager.Instance.devil;
            level10Dialog.Add(line17);
            
            DialogueLine line18 = new DialogueLine();
            line18.Content = "你爱世人，是因为神说要爱，还是因为你本就想爱？";
            line18.CharacterSprite = DialogueManager.Instance.devil;
            level10Dialog.Add(line18);
            
            DialogueLine line19 = new DialogueLine();
            line19.Content = "难道你从未思考过？";
            line19.CharacterSprite = DialogueManager.Instance.devil;
            level10Dialog.Add(line19);
            
            DialogueLine line20 = new DialogueLine();
            line20.Content = "…………";
            line20.CharacterSprite = DialogueManager.Instance.angel;
            level10Dialog.Add(line20);
            
            DialogueLine line21 = new DialogueLine();
            line21.Content = "即使累成这样，你还要继续前进吗？仅仅只是为了从虚无里获得“救赎”？";
            line21.CharacterSprite = DialogueManager.Instance.devil;
            level12Dialog.Add(line21);
            
            DialogueLine line22 = new DialogueLine();
            line22.Content = "难道你还没有意识到吗？我们一路走来，解开谜题的是谁的智慧？是神暂时借给你的，还是你自己的？";
            line22.CharacterSprite = DialogueManager.Instance.devil;
            level12Dialog.Add(line22);
            
            DialogueLine line23 = new DialogueLine();
            line23.Content = "帮助你踏过深渊的是谁的勇气？是神推动你的，还是你内心滋生的？面对恐惧时，是谁在战斗？";
            line23.CharacterSprite = DialogueManager.Instance.devil;
            level12Dialog.Add(line23);
            
            DialogueLine line24 = new DialogueLine();
            line24.Content = "你又有什么资格说我？";
            line24.CharacterSprite = DialogueManager.Instance.angel;
            level12Dialog.Add(line24);
            
            DialogueLine line25 = new DialogueLine();
            line25.Content = "难道我没有资格说你吗？！你难道不知道你的本我，你的身体残破到什么地步了吗？！";
            line25.CharacterSprite = DialogueManager.Instance.devil;
            level12Dialog.Add(line25);
            
            DialogueLine line26 = new DialogueLine();
            line26.Content = "你还没意识到吗？我就是你，所谓的神的好好学生。";
            line26.CharacterSprite = DialogueManager.Instance.devil;
            level12Dialog.Add(line26);
            
            DialogueLine line27 = new DialogueLine();
            line27.Content = "是，我知道你想说，你在这条路上走了太久太久了，但是其实无论何时都不算晚！";
            line27.CharacterSprite = DialogueManager.Instance.devil;
            level12Dialog.Add(line27);
            
            DialogueLine line28 = new DialogueLine();
            line28.Content = "够了！！！";
            line28.CharacterSprite = DialogueManager.Instance.angel;
            level12Dialog.Add(line28);
            
            DialogueLine line29 = new DialogueLine();
            line29.Content = "…";
            line29.CharacterSprite = DialogueManager.Instance.devil;
            level13Dialog.Add(line29);
            
            DialogueLine line30 = new DialogueLine();
            line30.Content = "…";
            line30.CharacterSprite = DialogueManager.Instance.devil;
            level14Dialog.Add(line30);
            
            DialogueLine line31 = new DialogueLine();
            line31.Content = "之前，你总是把自己人生的责任，都推给了一个想象中的、全知全能的存在，却从未真正为自己活过一刻。";
            line31.CharacterSprite = DialogueManager.Instance.devil;
            level14Dialog.Add(line31);
            
            DialogueLine line32 = new DialogueLine();
            line32.Content = "难道不是吗？";
            line32.CharacterSprite = DialogueManager.Instance.devil;
            level14Dialog.Add(line32);
            
            DialogueLine line33 = new DialogueLine();
            line33.Content = "…………";
            line33.CharacterSprite = DialogueManager.Instance.angel;
            level14Dialog.Add(line33);
            
            DialogueLine line34 = new DialogueLine();
            line34.Content = "你相信你自己吗？我指的是原原本本，没有所谓的主帮助的自己？";
            line34.CharacterSprite = DialogueManager.Instance.devil;
            level14Dialog.Add(line34);
            
            DialogueLine line35 = new DialogueLine();
            line35.Content = "如果现在的你对自己哪怕存在一点点信任，为了自己活下去，可以吗？";
            line35.CharacterSprite = DialogueManager.Instance.devil;
            level14Dialog.Add(line35);
            
            DialogueLine line36 = new DialogueLine();
            line36.Content = "现在，到你做出选择的时候了。";
            line36.CharacterSprite = null;
            level15Dialog.Add(line36);
            
            DialogueLine line37 = new DialogueLine();
            line37.Content = "没错，我说的，是你。";
            line37.CharacterSprite = null;
            level15Dialog.Add(line37);
        }

        private void ShowDialog()
        {
            if (LevelManager.Instance.GetCurrentLevelIndex() == 0) DialogueManager.Instance.ShowDialogue(level1Dialog);
            if (LevelManager.Instance.GetCurrentLevelIndex() == 1) DialogueManager.Instance.ShowDialogue(level2Dialog);
            if (LevelManager.Instance.GetCurrentLevelIndex() == 2) DialogueManager.Instance.ShowDialogue(level3Dialog);
            if (LevelManager.Instance.GetCurrentLevelIndex() == 3) DialogueManager.Instance.ShowDialogue(level4Dialog);
            if (LevelManager.Instance.GetCurrentLevelIndex() == 4) DialogueManager.Instance.ShowDialogue(level5Dialog);
            if (LevelManager.Instance.GetCurrentLevelIndex() == 5) DialogueManager.Instance.ShowDialogue(level6Dialog);
            if (LevelManager.Instance.GetCurrentLevelIndex() == 6) DialogueManager.Instance.ShowDialogue(level7Dialog);
            if (LevelManager.Instance.GetCurrentLevelIndex() == 7) DialogueManager.Instance.ShowDialogue(level8Dialog);
            if (LevelManager.Instance.GetCurrentLevelIndex() == 8) DialogueManager.Instance.ShowDialogue(level9Dialog);
            if (LevelManager.Instance.GetCurrentLevelIndex() == 9) DialogueManager.Instance.ShowDialogue(level10Dialog);
            if (LevelManager.Instance.GetCurrentLevelIndex() == 10) DialogueManager.Instance.ShowDialogue(level11Dialog);
            if (LevelManager.Instance.GetCurrentLevelIndex() == 11) DialogueManager.Instance.ShowDialogue(level12Dialog);
            if (LevelManager.Instance.GetCurrentLevelIndex() == 12) DialogueManager.Instance.ShowDialogue(level13Dialog);
            if (LevelManager.Instance.GetCurrentLevelIndex() == 13) DialogueManager.Instance.ShowDialogue(level14Dialog);
            if (LevelManager.Instance.GetCurrentLevelIndex() == 14) DialogueManager.Instance.ShowDialogue(level15Dialog);
        }
    }
}