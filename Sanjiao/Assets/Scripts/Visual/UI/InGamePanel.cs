using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Visual
{
    public class InGamePanel : MonoBehaviour
    {
        public Button ExitButton;

        private void Awake()
        {
            ExitButton.OnClickAsObservable()
                .Subscribe(async _ =>
                {
                    AudioManager.Instance.StopBGM();
                    await UIManager.Instance?.OpenPanelAsync("Switch");
                    LevelManager.Instance?.ClearCurrentLevel();
                    UIManager.Instance?.ClosePanel("InGame");
                    await Task.Delay(12000); // 等待切换面板动画
                    await UIManager.Instance?.SwitchPanelAsync("Switch","Select");
                    AudioManager.Instance.PlayBGM("Lobby");
                }).AddTo(this);
        }
    }
}