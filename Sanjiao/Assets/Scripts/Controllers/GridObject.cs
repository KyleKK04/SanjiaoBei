using System;
using UnityEngine;

namespace Game.Data
{
    public class GridObject : MonoBehaviour
    {
        public GridCoordinates gridCoordinates;
        public Direction direction;
        public GridObjectType gridObjectType;
        public bool isBlockingMovement = false;
        public bool isMovable = false;

        private void Awake()
        {
            Init(this.gridCoordinates.x, this.gridCoordinates.y, this.direction);
        }

        public virtual void Init(int x, int y, Direction dir)
        {
            gridCoordinates = new GridCoordinates(x, y);
            direction = dir;
            
            // 假设 LevelManager.Instance.cellSize 是网格大小
            float size = 1f; 
            if(Game.Core.LevelManager.Instance != null) size = Game.Core.LevelManager.Instance.cellSize;
            
            transform.position = new Vector3(x * size, y * size, 0);
        }

        public virtual void Interact() { }

        public virtual void OnChant(int powerLevel, Direction inputDir) { }

        public Vector2Int DirectionToVector2Int(Direction inputDir)
        {
            switch (inputDir)
            {
                case Direction.up: return new Vector2Int(0, 1);
                case Direction.down: return new Vector2Int(0, -1);
                case Direction.left: return new Vector2Int(-1, 0);
                case Direction.right: return new Vector2Int(1, 0);
                default: return Vector2Int.zero;
            }
        }

        public Direction Vector2IntToDirection(Vector2Int input)
        {
            if (input.x == 0 && input.y == 1) return Direction.up;
            if (input.x == 0 && input.y == -1) return Direction.down;
            if (input.x == -1 && input.y == 0) return Direction.left;
            if (input.x == 1 && input.y == 0) return Direction.right;
            return Direction.down;
        }
        

        protected void UpdateVisualRotation()
        {
            /*// 简单的 Z 轴旋转实现
            float angle = 0;
            switch (direction)
            {
                case Direction.up: angle = 0; break; // 假设图片默认朝上
                case Direction.left: angle = 90; break;
                case Direction.down: angle = 180; break;
                case Direction.right: angle = -90; break;
            }
            transform.rotation = Quaternion.Euler(0, 0, angle);*/
        }
        
        // 销毁自身并清理网格引用
        protected void RemoveFromGrid()
        {
            if(Game.Core.LevelManager.Instance != null)
            {
                Game.Core.LevelManager.Instance.UpdateGrid(gridCoordinates.x, gridCoordinates.y, null);
            }
            Destroy(gameObject);
        }
    }
}