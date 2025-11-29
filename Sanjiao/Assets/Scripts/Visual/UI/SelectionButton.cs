using UnityEngine;
using UnityEngine.UI;

namespace Game.Visual
{
    public class SelectionButton : MonoBehaviour
    {
        [Header("UI References")]
        public Image iconImage; // 拖入按钮原本的 Image 组件
        private Button btnComp;

        private void Awake()
        {
            btnComp = GetComponent<Button>();
            if (iconImage == null) iconImage = GetComponent<Image>();
        }

        // 初始化方法：只接收最终要显示的图片
        public void Setup(bool isUnlocked, Sprite displaySprite)
        {
            // 1. 设置图片 (是数字图还是锁图，由 Panel 决定传进来)
            if (iconImage != null)
            {
                iconImage.sprite = displaySprite;
            }

            // 2. 设置交互性
            if (btnComp != null)
            {
                btnComp.interactable = isUnlocked;
            }
        }

        public Button GetButton()
        {
            return btnComp;
        }
    }
}