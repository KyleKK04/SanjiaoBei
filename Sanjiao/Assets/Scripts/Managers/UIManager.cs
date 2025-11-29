using System.Collections.Generic;
using System.Threading.Tasks; // 必须引用
using Game.Utilities;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Game.Core
{
    [System.Serializable]
    public struct UIElement
    {
        public string UIPanelName;
        public GameObject UIPanelPrefab;
        public UIElement(string name, GameObject prefab) { UIPanelName = name; UIPanelPrefab = prefab; }
    }

    public class UIManager : Singleton<UIManager>
    {
        private float fadeDuration = 0.5f; // 稍微调慢一点看效果
        
        [Header("Settings")]
        [SerializeField] private Transform uiRoot; 
        [SerializeField] private List<UIElement> uiList = new List<UIElement>();

        private Dictionary<string, GameObject> prefabDict = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> instanceDict = new Dictionary<string, GameObject>();
        private Stack<GameObject> panelStack = new Stack<GameObject>();

        protected override void Awake()
        {
            base.Awake();
            InitializeConfigs();
            if (uiRoot == null) uiRoot = GameObject.Find("Canvas")?.transform;
        }

        private void InitializeConfigs()
        {
            foreach (var element in uiList)
            {
                if (element.UIPanelPrefab != null && !string.IsNullOrEmpty(element.UIPanelName))
                    if (!prefabDict.ContainsKey(element.UIPanelName)) prefabDict.Add(element.UIPanelName, element.UIPanelPrefab);
            }
        }

        #region 异步核心方法

        /// <summary>
        /// 异步打开面板
        /// </summary>
        public async Task OpenPanelAsync(string name, bool bringToFront = true)
        {
            GameObject panel = GetOrInstantiatePanel(name);
            if (panel == null) return;

            CanvasGroup cg = panel.GetComponent<CanvasGroup>();

            // 如果已经在显示且完全不透明，直接返回
            if (panel.activeSelf && cg.alpha >= 0.99f) return;

            if (bringToFront) panel.transform.SetAsLastSibling();

            cg.DOKill();
            panel.SetActive(true);
            cg.blocksRaycasts = true;
            cg.alpha = 0f;

            if (!panelStack.Contains(panel)) panelStack.Push(panel);

            // 【关键】等待淡入动画完成
            await cg.DOFade(1f, fadeDuration).SetUpdate(true).AsyncWaitForCompletion();
        }

        /// <summary>
        /// 异步关闭面板
        /// </summary>
        public async Task ClosePanelAsync(string name)
        {
            if (instanceDict.TryGetValue(name, out GameObject panel))
            {
                if (panel.activeSelf)
                {
                    CanvasGroup cg = panel.GetComponent<CanvasGroup>();
                    cg.DOKill();
                    cg.blocksRaycasts = false; // 立即禁止点击

                    // 【关键】等待淡出动画完成
                    await cg.DOFade(0f, fadeDuration).SetUpdate(true).AsyncWaitForCompletion();
                    
                    panel.SetActive(false);

                    if (panelStack.Count > 0 && panelStack.Peek() == panel) panelStack.Pop();
                }
            }
            // 如果面板本来就是关的，Task 会立即完成，不会阻塞
        }

        /// <summary>
        /// 异步切换面板：先彻底关闭旧的，再打开新的
        /// </summary>
        public async Task SwitchPanelAsync(string closeName, string openName)
        {
            ClosePanelAsync(closeName);
            await OpenPanelAsync(openName);
        }

        #endregion

        #region 辅助逻辑 (保持不变)
        public GameObject GetOrInstantiatePanel(string name)
        {
            if (instanceDict.TryGetValue(name, out GameObject instance)) return instance;
            if (prefabDict.TryGetValue(name, out GameObject prefab))
            {
                GameObject newPanel = Instantiate(prefab, uiRoot);
                newPanel.name = name;
                CanvasGroup cg = newPanel.GetComponent<CanvasGroup>();
                if (cg == null) cg = newPanel.AddComponent<CanvasGroup>();
                cg.alpha = 0;
                instanceDict.Add(name, newPanel);
                return newPanel;
            }
            Debug.LogError($"UIManager: Panel [{name}] not found!");
            return null;
        }
        #endregion
        
        // 为了兼容旧代码，你可以保留同步方法，内部调用异步但不等待
        public void OpenPanel(string name) => _ = OpenPanelAsync(name);
        public void ClosePanel(string name) => _ = ClosePanelAsync(name);
    }
}