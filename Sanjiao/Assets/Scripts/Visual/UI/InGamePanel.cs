using System;
using System.Collections;
using System.Collections.Generic;
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
                    await UIManager.Instance?.OpenPanelAsync("Switch");
                    LevelManager.Instance?.ClearCurrentLevel();
                    UIManager.Instance?.ClosePanel("InGame");
                    await UIManager.Instance?.SwitchPanelAsync("Switch","Select");
                }).AddTo(this);
        }
    }
}