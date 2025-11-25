using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    // 单个地图元素的数据结构
    [System.Serializable]
    public class LevelElement
    {
        [Tooltip("物体在网格中的坐标")] 
        public GridCoordinates position;

        [Tooltip("物体的类型")] 
        public GridObjectType type;

        [Tooltip("物体的初始朝向 (对雕像、玩家出生点有效)")] 
        public Direction initialFacing = Direction.down;
    }

    // 关卡数据容器 (ScriptableObject)
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "Game/Level Data")]
    public class LevelSO : ScriptableObject
    {
        [Header("Map Settings")] 
        [Tooltip("地图的网格尺寸 (比如 10x10 表示这也是个10行10列的图)")]
        public GridCoordinates mapSize = new GridCoordinates(10, 10);

        [Header("Level Configuration")] 
        public string levelName;

        [TextArea] 
        [Tooltip("进入关卡时的剧情文本")]
        public string startDialogue;

        [TextArea] 
        [Tooltip("拾取卷轴时的剧情文本")]
        public string scrollDialogue;

        [Header("Map Objects")] 
        [Tooltip("地图上所有的物体列表")]
        public List<LevelElement> elements = new List<LevelElement>();

        // 辅助方法：验证坐标是否在 mapSize 范围内
        public bool IsCoordinateInBounds(GridCoordinates coord)
        {
            return coord.x >= 0 && coord.x < mapSize.x &&
                   coord.y >= 0 && coord.y < mapSize.y;
        }
    }
}