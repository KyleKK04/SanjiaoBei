using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Game;
using Game.Utilities;

namespace Game.Core
{
    public class GameManager : Singleton<GameManager>
    {

        public bool HasScroll { get; private set; } = false;

        private void Start()
        {
            GameStart();
        }

        public void GameStart()
        {
            Debug.Log("调用开始界面");
            UIManager.Instance.OpenPanel("Start");
            AudioManager.Instance.PlayBGM("Lobby");
        }

        public void CollectScroll()
        {
            HasScroll = true;
            Debug.Log("GameManager: 卷轴已收集");
        }

        public async Task GameOver()
        {
            Debug.Log("游戏失败！！！");
            LevelManager.Instance.RestartLevel();
        }

        public void WinLevel()
        {
            Debug.Log("VICTORY: 关卡通过！");
            
            // 1. 解锁下一关
            if (LevelManager.Instance != null)
            {
                int currentIndex = LevelManager.Instance.GetCurrentLevelIndex();
                int nextIndex = currentIndex + 1;
                
                // 尝试解锁下一关
                UnlockLevel(nextIndex);
                
                // 2. 加载下一关
                // 如果你想做结算面板，可以在这里暂停，让玩家点“下一关”按钮再加载
                AudioManager.Instance.StopBGM();
                LevelManager.Instance.LoadNextLevel();
            }
        }

        public void RealWin()
        {
            
        }
        
        public void UnlockLevel(int levelIndex)
        {
            if (LevelManager.Instance == null) return;
            
            // 检查索引是否有效
            if (levelIndex >= 0 && levelIndex < LevelManager.Instance.levels.Count)
            {
                // 直接修改 SO 的数据
                LevelManager.Instance.levels[levelIndex].isUnlocked = true;
                
                Debug.Log($"GameManager: 关卡 {levelIndex} 已解锁！");
            }
        }
    }
}