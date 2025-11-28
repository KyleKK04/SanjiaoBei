using UnityEngine;
using Game.Core;

namespace Game.Data
{
    public class EvilStatueController : GridObject
    {
        public Sprite UpSripte;
        public Sprite DownSprite;
        public Sprite LeftSprite;
        public Sprite RightSprite;
        public Sprite DestroyedSprite;
        public Sprite SpottedUpSprite;
        public Sprite SpottedDownSprite;
        public Sprite SpottedLeftSprite;
        public Sprite SpottedRightSprite;
        
        private bool isSpottingPlayer = false;
        private SpriteRenderer spriteRenderer;
        public override void Init(int x, int y, Direction dir)
        {
            base.Init(x, y, dir);
            gridObjectType = GridObjectType.GhostStatue;
            isBlockingMovement = true;
            this.spriteRenderer = this.GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            // 每一帧检测是否能看到玩家
            if (LevelManager.Instance != null && LevelManager.Instance.playerInstance != null)
            {
                CheckKillPlayer();
            }
            UpdateAnimation();
        }

        private void CheckKillPlayer()
        {
            PlayerMovement player = LevelManager.Instance.playerInstance;
            GridCoordinates myPos = gridCoordinates;
            GridCoordinates playerPos = player.gridCoordinates;

            // 1. 周围四格检测
            if (Mathf.Abs(myPos.x - playerPos.x) + Mathf.Abs(myPos.y - playerPos.y) == 1)
            {
                isSpottingPlayer = true;
                return;
            }

            // 2. 视线检测
            if (LevelManager.Instance.CheckLineOfSight(myPos, direction, playerPos))
            {
                isSpottingPlayer = true;
                return;
            }
            
            isSpottingPlayer = false;
        }
        
        public void KillPlayer()
        {
            // 这里可以调用游戏结束逻辑
            GameManager.Instance.GameOver("isSpottingPlayer = true;");
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

        public void UpdateAnimation()
        {
            if (direction == Direction.up)
            {
                if (isSpottingPlayer)
                {
                    spriteRenderer.sprite = SpottedUpSprite;
                }
                else
                {
                    spriteRenderer.sprite = UpSripte;
                }
            }
            else if (direction == Direction.down)
            {
                if (isSpottingPlayer)
                {
                    spriteRenderer.sprite = SpottedDownSprite;
                }
                else
                {
                    spriteRenderer.sprite = DownSprite;
                }
            }
            else if (direction == Direction.left)
            {
                if (isSpottingPlayer)
                {
                    spriteRenderer.sprite = SpottedLeftSprite;
                }
                else
                {
                    spriteRenderer.sprite = LeftSprite;
                }
            }
            else if (direction == Direction.right)
            {
                if (isSpottingPlayer)
                {
                    spriteRenderer.sprite = SpottedRightSprite;
                }
                else
                {
                    spriteRenderer.sprite = RightSprite;
                }
            }
        }
    }
}