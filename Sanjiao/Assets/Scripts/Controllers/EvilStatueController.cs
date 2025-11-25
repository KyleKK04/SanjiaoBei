using System.Collections;
using System.Collections.Generic;
using Game.Data;
using UnityEngine;

namespace Game.Data
{
    public class EvilStatueController : GridObject
    {
        public override void Init(int x, int y, Direction dir)
        {
            base.Init(x, y, dir);
            this.gridObjectType = GridObjectType.GhostStatue;
        }
    }
}