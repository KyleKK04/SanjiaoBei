using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Data;

/// <summary>
/// 这是所有地图物体的父类。包含DataStructure里的坐标位置，方向，是否阻挡移动，是否可移动的判断
/// </summary>
public class GridObject : MonoBehaviour
{
    public GridCoordinates gridCoordinates;
    public Direction direction;
    public GridObjectType gridObjectType;
    public bool isBlockingMovement = false;
    public bool isMovable = false;

    // 【重要】MonoBehaviour 不能使用构造函数，必须用 Init 或 Awake
    // 这个方法由 LevelManager 生成物体时调用
    public virtual void Init(int x, int y, Direction dir)
    {
        gridCoordinates = new GridCoordinates(x, y);
        
        // 根据坐标设置实际的世界坐标 (乘以80)
        transform.localPosition = gridCoordinates.ToWorldPos();
        
        // 更新视觉朝向 (这里假设后续会有专门处理旋转的代码)
        UpdateVisualRotation();
    }
    
    public virtual void Interact()
    {
        //按E键交互
        if (Input.GetKeyDown(KeyCode.E))
        {
            //TODO:待完成交互逻辑   
        }
    }

    public virtual void OnChant(int powerLevel,Direction inputDir)
    {
        //TODO:被击中时的逻辑
        
    }

    public Vector2Int DirectionToVector2Int(Direction inputDir)
    {
        //将Direction转换为Vector2Int
        switch (inputDir)
        {
            case Direction.up:
                return new Vector2Int(0, 1);
            case Direction.down:
                return new Vector2Int(0, -1);   
            case Direction.left:
                return new Vector2Int(-1, 0);
            case Direction.right:
                return new Vector2Int(1, 0);
            default:
                return Vector2Int.zero;
        }
    }
    
    // 简单的视觉旋转更新辅助方法
    protected void UpdateVisualRotation()
    {
 
    }
}
