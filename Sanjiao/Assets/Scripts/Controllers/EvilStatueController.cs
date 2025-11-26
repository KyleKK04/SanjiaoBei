using UnityEngine;
using Game.Core;

namespace Game.Data
{
    public class EvilStatueController : GridObject
    {
        public override void Init(int x, int y, Direction dir)
        {
            base.Init(x, y, dir);
            gridObjectType = GridObjectType.GhostStatue;
            isBlockingMovement = true;
        }

        private void Update()
        {
            // 每一帧检测是否能看到玩家
            if (LevelManager.Instance != null && LevelManager.Instance.playerInstance != null)
            {
                CheckKillPlayer();
            }
        }

        private void CheckKillPlayer()
        {
            PlayerMovement player = LevelManager.Instance.playerInstance;
            GridCoordinates myPos = gridCoordinates;
            GridCoordinates playerPos = player.gridCoordinates;

            // 1. 周围四格检测
            if (Mathf.Abs(myPos.x - playerPos.x) + Mathf.Abs(myPos.y - playerPos.y) == 1)
            {
                GameManager.Instance.GameOver("Too close to Evil Statue!");
                return;
            }

            // 2. 视线检测
            if (LevelManager.Instance.CheckLineOfSight(myPos, direction, playerPos))
            {
                GameManager.Instance.GameOver("Spotted by Evil Statue!");
            }
        }

        public override void OnChant(int powerLevel, Direction inputDir)
        {
            if (powerLevel >= 3)
            {
                Debug.Log("Evil Statue Destroyed!");
                RemoveFromGrid(); // 销毁自己
            }
            else
            {
                Debug.Log("Evil Statue blocked weak chant.");
            }
        }
    }
}