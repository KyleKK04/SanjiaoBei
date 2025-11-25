using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Data;

namespace Game.Data
{
    public class StatueController : GridObject
    {
        public Vector2Int outDir;

        public override void Init(int x, int y, Direction dir)
        {
            base.Init(x, y, dir);
            this.gridObjectType = GridObjectType.Statue;
        }
        
        public override void Interact()
        {
            //按E键交互
            if (Input.GetKeyDown(KeyCode.E))
            {
                //TODO:待完成交互逻辑,将面朝逻辑改向玩家面朝方向
                //判定玩家位置是否在雕像周围四格
                
                
            }
        }

        /// <summary>
        /// 用于初始化雕像的出声方向
        /// </summary>
        /// <param name="dir">出声方向</param>
        public void SetOutChantDir(Vector2Int dir)
        {
            outDir = dir;
        }

        public override void OnChant(int powerLevel, Direction inputDir)
        {
            base.OnChant(powerLevel, inputDir);
            //将传入的Chant方向转变为outDir，并使powerLevel+1并输出
            Vector2Int chantDir = DirectionToVector2Int(inputDir);
            Vector2Int finalDir = new Vector2Int(outDir.x,outDir.y);
            int finalPowerLevel = powerLevel + 1;
            Debug.Log("雕像被击中，向方向 " + finalDir + " 以力量等级 " + finalPowerLevel + " 出声");
        }
    }
}