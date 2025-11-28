using UnityEngine;

namespace Game.Data
{
    public enum Direction
    {
        up,
        down,
        left,
        right
    }

    public enum DoorType
    {
        BeginDoor, // 起点门（装饰、不可交互）
        EndDoor    // 终点门（原有逻辑）
    }
    
    // 标识格子上的物体类型
    public enum GridObjectType
    {
        None,           // 空/边界（天空，掉下去会死）
        Ground,         // 普通地面（可行走）
        Wall,           // 墙
        Statue,         // 普通雕像
        GhostStatue,    // 恶鬼雕像
        Scroll,         // 卷轴
        Door,           // 大门
        SpawnPoint,      // 玩家出生点
        Player,          //玩家
        ChantWave      //声音波
    }

    [System.Serializable] // 【重要】加上这个，才能在Inspector中显示 XY 输入框
    public struct GridCoordinates
    {
        public int x;
        public int y;

        // 【新增】定义格子像素大小，方便全局调用
        public const float UnitSize = 1f;

        public GridCoordinates(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        // 重载 + 运算符，方便坐标计算
        public static GridCoordinates operator +(GridCoordinates a, GridCoordinates b)
        {
            return new GridCoordinates(a.x + b.x, a.y + b.y);
        }

        // 方便转成 Vector3 进行物体摆放
        public Vector3 ToWorldPos()
        {
            return new Vector3(x * UnitSize, y * UnitSize, 0);
        }
        
        // 方便转成 Vector2Int (Unity原生整数坐标)
        public Vector2Int ToVector2Int()
        {
            return new Vector2Int(x, y);
        }
    }
}