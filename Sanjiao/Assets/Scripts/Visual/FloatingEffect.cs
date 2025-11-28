using UnityEngine;
using DG.Tweening; // 引入 DOTween 命名空间
namespace Game.Visual
{
    public class FloatingEffect : MonoBehaviour
    {
        [Header("Settings")]
        public float floatDistance = 0.2f; // 上下浮动的距离
        public float duration = 1.0f;      // 单次浮动的时间
        public Ease easeType = Ease.InOutSine; // 缓动类型，InOutSine 最像呼吸感

        private void Start()
        {
            // 获取当前 Y 轴位置，移动到 (当前Y + 距离)，无限循环，悠悠球模式(往返)
            transform.DOMoveY(transform.position.y + floatDistance, duration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(easeType);
        }

        private void OnDestroy()
        {
            // 养成好习惯：物体销毁时杀掉该物体上的所有 Tween，防止报错
            transform.DOKill();
        }
    }
}