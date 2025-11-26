using UnityEngine;
using Game.Core;

namespace Game.Data
{
    public class DoorController : GridObject
    {
        public int requiredPower = 3;
        private bool isPowered = false;

        public void SetRequiredPower(int power)
        {
            requiredPower = power;
            // 更新头顶文字显示 power
        }

        public override void Init(int x, int y, Direction dir)
        {
            base.Init(x, y, dir);
            gridObjectType = GridObjectType.Door;
            isBlockingMovement = true;
        }

        public override void OnChant(int powerLevel, Direction inputDir)
        {
            if (GameManager.Instance.HasScroll && powerLevel >= requiredPower)
            {
                isPowered = true;
                Debug.Log("Door Powered Up!");
                // 视觉变化：发光
                GetComponent<SpriteRenderer>().color = Color.yellow;
            }
        }

        public override void Interact()
        {
            if (GameManager.Instance.HasScroll && isPowered)
            {
                // 调用 LevelManager 加载下一关
                LevelManager.Instance.LoadNextLevel(); 
            }
            else
            {
                Debug.Log($"Door Locked. Scroll:{GameManager.Instance.HasScroll}, Powered:{isPowered}");
            }
        }
    }
}