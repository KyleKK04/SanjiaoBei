
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

        [Tooltip("物体的初始朝向 (对雕像、玩家出生点、恶鬼雕像有效)")] 
        public Direction initialFacing = Direction.down;

        [Tooltip("大门开启所需的最小咏唱等级 (仅对 Door 有效)")]
        public int requiredDoorPower = 3; // 默认为3
        
        // 【新增】门的类型 (仅对 Door 有效)
        public DoorType doorType = DoorType.EndDoor; 
    }

    // 关卡数据容器 (ScriptableObject)
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "Game/Level Data")]
    public class LevelSO : ScriptableObject
    {
        [Header("Map Settings")] 
        [Tooltip("地图的网格尺寸")]
        public GridCoordinates mapSize = new GridCoordinates(10, 10);

        [Header("Level Configuration")] 
        public string levelName;

        [Tooltip("该关卡是否默认解锁（例如第一关应勾选）")]
        public bool isUnlocked = false; // 【新增】解锁状态
        
        [TextArea] public string startDialogue;
        [TextArea] public string scrollDialogue;

        [Header("Map Objects")] 
        public List<LevelElement> elements = new List<LevelElement>();
        
        /// <summary>
        /// 检查给定坐标是否在地图边界内
        /// </summary>
        /// <param name="coord"></param>
        /// <returns></returns>
        public bool IsCoordinateInBounds(GridCoordinates coord)
        {
            return coord.x >= 0 && coord.x < mapSize.x &&
                   coord.y >= 0 && coord.y < mapSize.y;
        }
    }
    
    
}