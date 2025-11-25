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
    public bool isBlockingMovement = false;
    public bool isMovable = false;

    public GridObject(int x, int y, Direction dir)
    {
        gridCoordinates = new GridCoordinates(x, y);
        direction = dir;
    }
    
    public void Interact()
    {
        //按E键交互
        if (Input.GetKeyDown(KeyCode.E))
        {
            //TODO:待完成交互逻辑   
        }
    }

    public void OnChant(int powerLevel,Direction inputDir)
    {
        //TODO:被击中时的逻辑
        
    }
}
