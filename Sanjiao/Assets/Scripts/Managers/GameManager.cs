using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game;

namespace Game.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public bool HasScroll { get; private set; } = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void CollectScroll()
        {
            HasScroll = true;
            Debug.Log("GameManager: 卷轴已收集");
        }

        public void GameOver(string reason)
        {
            Debug.LogError($"GAME OVER: {reason}");
            // TODO: 弹出失败UI，重置关卡等
        }

        public void WinLevel()
        {
            Debug.Log("VICTORY: 关卡通过！");
            // TODO: 弹出胜利UI，加载下一关
        }
    }
}