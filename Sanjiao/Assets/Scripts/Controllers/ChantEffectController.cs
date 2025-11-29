using System;
using Game.Data;
using UnityEngine;

namespace Game.Core
{
    public class ChantEffectController : MonoBehaviour
    {
        public Sprite leftHeadSprite;
        public Sprite leftMiddleSprite;
        public Sprite leftEndSprite;
        public Sprite downHeadSprite;
        public Sprite downMiddleSprite;
        public Sprite downEndSprite;

        private SpriteRenderer spriteRenderer;
        
        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Init(Direction dir, bool isHead, bool isNextBlocking)
        {
            if (spriteRenderer == null) return;

            Sprite head = null, middle = null, end = null;
            float rotationAngle = 0f;

            switch (dir)
            {
                case Direction.left:
                    head = leftHeadSprite;
                    middle = leftMiddleSprite;
                    end = leftEndSprite;
                    rotationAngle = 0f;
                    break;
                case Direction.up:
                    head = downHeadSprite;
                    middle = downMiddleSprite;
                    end = downEndSprite;
                    rotationAngle = 0f;
                    break;
                case Direction.right:
                    head = leftEndSprite;
                    middle = leftMiddleSprite;
                    end = leftHeadSprite;
                    rotationAngle = 0f;
                    break;
                case Direction.down:
                    head = downEndSprite;
                    middle = downMiddleSprite;
                    end = downHeadSprite;
                    rotationAngle = 0f;
                    break;
            }
            
            transform.rotation = Quaternion.Euler(0, 0, rotationAngle);

            // 3. 根据逻辑优先级显示图片 (Head > End > Middle)
            if (isHead)
            {
                spriteRenderer.sprite = head;
            }
            else if (isNextBlocking)
            {
                spriteRenderer.sprite = end;
            }
            else
            {
                spriteRenderer.sprite = middle;
            }
        }

    }
}