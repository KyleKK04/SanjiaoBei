using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UniRx;
using DG.Tweening;
using Game.Core;
using UnityEngine.UI;

namespace Game.Visual
{


    public class SelectionPanel : MonoBehaviour
    {
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject selectionButtonPrefab;
        [SerializeField] private Transform gridContainer; // Grid Layout Group 的物体
        [Header("Level Sprites")]
        [Tooltip("请按顺序拖入15张关卡数字图片 (Level 1 ~ 15)")]
        public List<Sprite> levelNumberSprites;

        private List<Button> generatedButtons = new List<Button>();
        
        [Tooltip("未解锁时显示的通用锁图片")]
        public Sprite lockedSprite;

        private void Awake()
        {
            exitButton.OnClickAsObservable()
                .Subscribe(async _ =>
                {
                   await UIManager.Instance?.SwitchPanelAsync("Select", "Start");
                }).AddTo(this);
        }

        private void Start()
        {
            GenerateSelectionButtons();
        }

        private void GenerateSelectionButtons()
        {
            if (LevelManager.Instance == null) return;

            var levels = LevelManager.Instance.levels; // 实际的数据列表
            int totalDisplayCount = 15; // 强制生成 15 个按钮

            for (int i = 0; i < totalDisplayCount; i++)
            {
                // 实例化
                GameObject btnObj = Instantiate(selectionButtonPrefab, gridContainer);
                SelectionButton btnScript = btnObj.GetComponent<SelectionButton>();
                Button btnComp = btnScript.GetButton();

                // 1. 判断解锁状态
                // 只有当 LevelManager 里有这一关的数据，且 isUnlocked 为 true 时，才算解锁
                bool hasData = i < levels.Count;
                bool isUnlocked = hasData && levels[i].isUnlocked;

                // 2. 决定显示哪张图片
                Sprite targetSprite;
                if (isUnlocked)
                {
                    // 防止图片没配够15张报错，加个保护
                    if (i < levelNumberSprites.Count)
                        targetSprite = levelNumberSprites[i];
                    else
                        targetSprite = lockedSprite; // 缺图就显示锁，或者用默认图
                }
                else
                {
                    targetSprite = lockedSprite;
                }

                // 3. 初始化按钮
                if (btnScript != null)
                {
                    btnScript.Setup(isUnlocked, targetSprite);
                }

                // 4. 绑定点击事件
                if (isUnlocked)
                {
                    int levelIndex = i; // 闭包捕获
                    btnComp.OnClickAsObservable()
                        .Subscribe(async _ =>
                        {
                            LevelManager.Instance.LoadLevel(levelIndex);
                        })
                        .AddTo(this);
                }
                else
                {
                    btnComp.interactable = false;
                }

                generatedButtons.Add(btnComp);
            }
        }
    }
}