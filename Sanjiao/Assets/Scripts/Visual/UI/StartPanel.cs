using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class StartPanel : MonoBehaviour
{
    public Button startBtn;
    public Button exitBtn;
    public Sprite exitSprite;

    private void Awake()
    {
        startBtn.OnClickAsObservable()
            .Subscribe(async _ =>
            {
                AudioManager.Instance.PlaySFX("Click");
                await UIManager.Instance.SwitchPanelAsync("Start", "Select");
            }).AddTo(this);
        exitBtn.OnClickAsObservable()
            .Subscribe(async _ =>
            {
                exitBtn.image.sprite = exitSprite;
                await Task.Delay(300);
                Application.Quit();
            }).AddTo(this);
    }
}
