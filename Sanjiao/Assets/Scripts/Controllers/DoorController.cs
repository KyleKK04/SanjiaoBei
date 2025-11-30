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
        }

        private void ShowDialog()
        {
            if (LevelManager.Instance.GetCurrentLevelIndex() == 0)
            {
                DialogueManager.Instance.ShowDialogue(level1Dialog);
            }
        }
    }
}