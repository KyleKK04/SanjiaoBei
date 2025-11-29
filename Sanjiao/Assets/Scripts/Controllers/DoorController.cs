
using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using DG.Tweening;
using TMPro;

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

        public void SetDoorData(int power, DoorType type)
        {
            requiredPower = power;
            doorType = type;
            // 这里可以根据 type 更换不同的 Sprite，比如起点门是灰色的，终点门是金色的
        }

        public override void Init(int x, int y, Direction dir)
        {
            base.Init(x, y, dir);
            gridObjectType = GridObjectType.Door;
            isBlockingMovement = true; // 两种门都阻挡移动
            //拿出doorSprites中的数字sprite
            var numberSprite = doorSprites[1];
            numberSprite.sprite = nubmerSprites[requiredPower-1];
        }

        public override void OnChant(int powerLevel, Direction inputDir)
        {
            // 【修改】只有终点门才响应咏唱充能
            if (doorType == DoorType.EndDoor)
            {
                if (GameManager.Instance.HasScroll && powerLevel >= requiredPower)
                {
                    isPowered = true;
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
                    
                    DOVirtual.DelayedCall(1f, () =>
                    {
                        isBlockingMovement = false;
                        doorSprites[0].sprite = openSprite;
                        doorSprites[0].DOFade(0.5f, 0.01f);
                        Debug.Log("End Door Unlocked and Opened!");
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
    }
}