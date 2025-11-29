using System.Collections;
using System.Collections.Generic;
using Game.Data;
using UnityEngine;

namespace Game.Core
{

    public class ObstacleController : GridObject
    {
        public override void Init(int x, int y, Direction dir)
        {
            base.Init(x, y, dir);
            isMovable = false;
            isBlockingMovement = true;
            gridObjectType = GridObjectType.Obstacle;
        }
    }
}