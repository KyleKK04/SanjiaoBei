using UnityEngine;
using Game.Core;

namespace Game.Data
{
    public class StatueController : GridObject
    {
        public override void Init(int x, int y, Direction dir)
        {
            base.Init(x, y, dir);
            gridObjectType = GridObjectType.Statue;
            isBlockingMovement = true;
            isMovable = true; // 标记为可移动
        }

        public override void Interact()
        {
            // 简单的旋转交互
            // 注意：PlayerMovement 中已经实现了“周围四格自动转向玩家”的逻辑
            // 这里可以实现“对着按E旋转”的补充逻辑
            int d = (int)direction;
            d = (d + 1) % 4; // 简单顺时针
            // 实际上 Editor 里是 Face to Player，这里可以留空或做特殊处理
        }

        // 当被 LevelManager 推动时调用此方法更新视觉
        public void OnPush(Direction pushDir)
        {
            float size = LevelManager.Instance.cellSize;
            // 这里的 gridCoordinates 已经被 Manager 更新过了
            Vector3 targetPos = new Vector3(gridCoordinates.x * size, gridCoordinates.y * size, 0);
            
            // 简单处理：直接瞬移或使用协程平滑移动
            // 为了代码简洁这里直接设置位置，建议在 Update 中用插值实现平滑
            transform.position = targetPos; 
        }

        public override void OnChant(int powerLevel, Direction inputDir)
        {
            base.OnChant(powerLevel, inputDir);
            // 视觉反馈：雕像发光或播放音效
            Debug.Log($"Statue boosted chant! Power {powerLevel} -> {powerLevel+1}");
        }
    }
}