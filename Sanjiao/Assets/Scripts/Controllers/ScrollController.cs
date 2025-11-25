using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Data;

namespace Game.Data
{
    public class ScrollController : GridObject
    {
        public bool isCollected = false;
        [TextArea] public String scrollText;

        public override void Init(int x, int y, Direction dir)
        {
            base.Init(x, y, dir);
            this.gridObjectType = GridObjectType.Scroll;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.CompareTag("Player") && !isCollected)
            {
                isCollected = true;
                //TODO:显示卷轴文本
                Debug.Log("卷轴被拾取，显示文本: " + scrollText);
            }
        }
    }
}