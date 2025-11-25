using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    public class LevelSO : ScriptableObject
    {
        /// <summary>
        /// 单个地图元素的数据结构
        /// </summary>
        [System.Serializable] // 这个标签让该类可以在 Inspector 面板中显示和编辑
        public class LevelElement
        {
            [Tooltip("物体在网格中的坐标 (X, Y)")] public GridCoordinates position;

            [Tooltip("物体的类型")] public GridObjectType type;

            [Tooltip("物体的初始朝向 (对雕像、玩家出生点有效)")] public Direction initialFacing = Direction.down; // 默认为下
        }

        /// <summary>
        /// 关卡数据容器
        /// 对应策划案：用于存储每一关的配置（共15关）
        /// </summary>
        [CreateAssetMenu(fileName = "NewLevelData", menuName = "Game/Level Data")]
        public class LevelData : ScriptableObject
        {
            [Header("Map Settings")] [Tooltip("地图的网格尺寸 (宽, 高)")]
            public GridCoordinates mapSize = new GridCoordinates(10, 10);

            [Header("Level Configuration")] [Tooltip("关卡名称/ID")]
            public string levelName;

            [TextArea] [Tooltip("进入关卡时的剧情文本 (策划案提到的上方对话框)")]
            public string startDialogue;

            [TextArea] [Tooltip("拾取卷轴时的剧情文本 (下方提示字)")]
            public string scrollDialogue;

            [Header("Map Objects")] [Tooltip("地图上所有的物体列表")]
            public List<LevelElement> elements = new List<LevelElement>();

            // 辅助方法：用于在编辑器中快速验证数据
            public bool IsCoordinateInBounds(Vector2Int coord)
            {
                return coord.x >= 0 && coord.x < mapSize.x &&
                       coord.y >= 0 && coord.y < mapSize.y;
            }
        }
    }
}