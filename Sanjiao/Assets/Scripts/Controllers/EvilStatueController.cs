using UnityEngine;
using Game.Core;

namespace Game.Data
{
    public class EvilStatueController : GridObject
    {
        [Header("Sprites")]
        public Sprite UpSripte;
        public Sprite DownSprite;
        public Sprite LeftSprite;
        public Sprite RightSprite;
        public Sprite DestroyedSprite;
        
        [Header("Spotted Sprites")]
        public Sprite SpottedUpSprite;
        public Sprite SpottedDownSprite;
        public Sprite SpottedLeftSprite;
        public Sprite SpottedRightSprite;
        
        private bool isDestroyed = false;
        private bool isSpottingPlayer = false; // 当前帧是否看到玩家（用于控制Sprite）
        private bool hasTriggered = false;     // 【新增】是否已经触发过GameOver（用于逻辑锁）
        
        private SpriteRenderer spriteRenderer;

        public override void Init(int x, int y, Direction dir)
        {
            base.Init(x, y, dir);
            gridObjectType = GridObjectType.GhostStatue;
            isBlockingMovement = true;
            this.spriteRenderer = this.GetComponent<SpriteRenderer>();
            
            // 初始化状态
            hasTriggered = false;
            isDestroyed = false;
            isSpottingPlayer = false;
        }

        private void Update()
        {
            // 如果已经销毁，或者已经触发了GameOver，就不要再检测了
            if (hasTriggered || isDestroyed) 
            {
                // 即使停止检测，也要保持动画状态更新（确保显示红色的发现状态）
                UpdateAnimation();
                return;
            }

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

            bool detected = false;

            // 1. 周围四格检测
            if (Mathf.Abs(myPos.x - playerPos.x) + Mathf.Abs(myPos.y - playerPos.y) == 1)
            {
                detected = true;
            }
            // 2. 视线检测
            else if (LevelManager.Instance.CheckLineOfSight(myPos, direction, playerPos))
            {
                detected = true;
            }

            // --- 状态处理 ---
            if (detected)
            {
                isSpottingPlayer = true;
                
                // 【核心修复】加锁，只执行一次
                if (!hasTriggered)
                {
                    hasTriggered = true;
                    Debug.Log("EvilStatue: Player Spotted! Triggering GameOver.");
                    AudioManager.Instance.PlaySFX("Fail");
                    KillPlayer();
                }
            }
            else
            {
                isSpottingPlayer = false;
            }
        }
        
        public void KillPlayer()
        {
            AudioManager.Instance.PlaySFX("Fail");
            GameManager.Instance.GameOver();
        }

        public override void OnChant(int powerLevel, Direction inputDir)
        {
            // 如果已经触发了GameOver，就不要再响应被摧毁了，防止逻辑冲突
            if (hasTriggered) return;

            if (powerLevel >= 3)
            {
                Debug.Log("Evil Statue Destroyed!");
                isDestroyed = true;
                isBlockingMovement = false; // 摧毁后不再阻挡移动
                // RemoveFromGrid(); // 可以选择保留尸体（isDestroyed状态），也可以直接移除
            }
            else
            {
                Debug.Log("Evil Statue blocked weak chant.");
            }
        }

        public void UpdateAnimation()
        {
            // 如果被摧毁，显示残骸
            if (isDestroyed)
            {
                spriteRenderer.sprite = DestroyedSprite;
                return;
            }

            // 根据是否发现玩家选择图集
            Sprite targetSprite = null;

            switch (direction)
            {
                case Direction.up:
                    targetSprite = isSpottingPlayer ? SpottedUpSprite : UpSripte;
                    break;
                case Direction.down:
                    targetSprite = isSpottingPlayer ? SpottedDownSprite : DownSprite;
                    break;
                case Direction.left:
                    targetSprite = isSpottingPlayer ? SpottedLeftSprite : LeftSprite;
                    break;
                case Direction.right:
                    targetSprite = isSpottingPlayer ? SpottedRightSprite : RightSprite;
                    break;
            }

            if (targetSprite != null)
            {
                spriteRenderer.sprite = targetSprite;
            }
        }
    }
}