using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game;
using Game.Data;


namespace Game.Core
{
    public class WallController : GridObject
    {
        public override void Init(int x, int y, Direction dir)
        {
            base.Init(x, y, dir);
            isMovable = false;
            isBlockingMovement = true;
        }
    }
}