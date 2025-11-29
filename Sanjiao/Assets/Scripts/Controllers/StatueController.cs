using DG.Tweening;
using UnityEngine;
using Game.Core;

namespace Game.Data
{
    public class StatueController : GridObject
    {
        public Sprite upSprite;
        public Sprite downSprite;
        public Sprite leftSprite;
        public Sprite rightSprite;
        
        private SpriteRenderer spriteRenderer;
        public override void Init(int x, int y, Direction dir)
        {
            base.Init(x, y, dir);
            gridObjectType = GridObjectType.Statue;
            isBlockingMovement = true;
            isMovable = true; // 标记为可移动
            this.spriteRenderer = this.GetComponent<SpriteRenderer>();
            UpdateAnimation();
        }

        public override void Interact()
        {
            base.Interact();
            
            // 1. 获取玩家实例
            if (LevelManager.Instance != null && LevelManager.Instance.playerInstance != null)
            {
                GridObject player = LevelManager.Instance.playerInstance;

                // 2. 计算方向向量：目标 = 玩家坐标 - 雕像坐标
                // 例如：玩家在(0,0)，雕像在(0,1)。向量 = (0, -1)，即向下看
                int dx = player.gridCoordinates.x - this.gridCoordinates.x;
                int dy = player.gridCoordinates.y - this.gridCoordinates.y;
                Vector2Int lookVec = new Vector2Int(dx, dy);

                // 3. 转换并更新方向
                this.direction = Vector2IntToDirection(lookVec);
                
                Debug.Log($"雕像转向了玩家，方向变更为: {this.direction}");
            }
            
            UpdateAnimation();
        }

        // 当被 LevelManager 推动时调用此方法更新视觉
        public void OnPush(Vector3 targetPos, float duration)
        {
            transform.DOMove(targetPos, duration).SetEase(Ease.Linear);
        }

        public override void OnChant(int powerLevel, Direction inputDir)
        {
            base.OnChant(powerLevel, inputDir);
            // 视觉反馈：雕像发光或播放音效
            Debug.Log($"Statue boosted chant! Power {powerLevel} -> {powerLevel+1}");
        }

        private void UpdateAnimation()
        {
            switch (direction)
            {
                case Direction.up:
                    spriteRenderer.sprite = upSprite;
                    break;
                case Direction.down:
                    spriteRenderer.sprite = downSprite;
                    break;
                case Direction.left:
                    spriteRenderer.sprite = leftSprite;
                    break;
                case Direction.right:
                    spriteRenderer.sprite = rightSprite;
                    break;
            }
        }
    }
}