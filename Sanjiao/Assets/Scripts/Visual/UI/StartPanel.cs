using System;
using System.Collections;
using System.Collections.Generic;
using Game.Core;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class StartPanel : MonoBehaviour
{
    private Button startBtn;

    private void Awake()
    {
        startBtn = GetComponentInChildren<Button>();
        startBtn.OnClickAsObservable()
            .Subscribe(async _ =>
            {
                AudioManager.Instance.PlaySFX("Click");
                await UIManager.Instance.SwitchPanelAsync("Start", "Select");
            }).AddTo(this);
    }
}
