using System;
using System.Collections;
using DG.Tweening; // 必须引入，用于协程
using Game.Core;
using Game.Data;
using UnityEditor.Tilemaps;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Core
{
    public class PlayerMovement : GridObject
    {
        private Animator anim;
        private SpriteRenderer spriteRenderer;

        [Tooltip("普通移动一格所需时间 (越小越快)")]
        public float moveDuration = 0.15f; 
        [Tooltip("推动雕像一格所需时间 (越慢越有重量感)")]
        public float pushDuration = 0.4f; 
        public bool useSmoothMovement = true;

        private bool isMoving = false;
        private bool isPraying = false;
        private bool isPushing = false;
        public bool canMove = true;
        [SerializeField]
        private bool isChanting = false;
        private Vector3 targetPosition;

        [Header("Sprites")]
        public Sprite UpSprite;
        public Sprite DownSprite;
        public Sprite LeftSprite;
        public Sprite RightSprite;
        
        [Header("Push Sprites")]
        public Sprite PushUpSprite;
        public Sprite PushDownSprite;
        public Sprite PushLeftSprite;
        public Sprite PushRightSprite;
        
        [Header("Pray Sprites")]
        public Sprite PrayUpSprite;
        public Sprite PrayDownSprite;
        public Sprite PrayLeftSprite;
        public Sprite PrayRightSprite;
        
        [Header("Chant Sprites")]
        public Sprite ChantUpSprite;
        public Sprite ChantDownSprite;
        public Sprite ChantLeftSprite;
        public Sprite ChantRightSprite;

        void Awake()
        {
            isBlockingMovement = true;
            gridObjectType = GridObjectType.Player;
            canMove = true;
        }

        public override void Init(int x, int y, Direction dir)
        {
            base.Init(x, y, dir);
            targetPosition = transform.position;
        }

        void Start()
        {
            anim = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            // 如果正在祈祷，暂时不接受移动输入，防止动作被打断
            if (!isMoving && !isPraying)
            {
                HandleInput();
            }
            UpdateAnimation();
        }

        private void HandleInput()
        {
            // 1. 移动输入
            Direction? inputDir = null;

            if (Input.GetKeyDown(KeyCode.W)) inputDir = Direction.up;
            else if (Input.GetKeyDown(KeyCode.S)) inputDir = Direction.down;
            else if (Input.GetKeyDown(KeyCode.A)) inputDir = Direction.left;
            else if (Input.GetKeyDown(KeyCode.D)) inputDir = Direction.right;

            if (inputDir.HasValue)
            {
                if (direction != inputDir.Value)
                {
                    direction = inputDir.Value;
                    UpdateVisualRotation(); // 如果父类有旋转逻辑，这里确保只改变变量
                    return;
                }

                if(!canMove || UIManager.Instance.IsBusy)
                {
                    return;
                }
                
                // --- 新增：推箱子预判逻辑 ---
                // 在请求移动前，先看看前方是不是雕像
                Vector2Int dirVec = DirectionToVector2Int(inputDir.Value);
                int targetX = gridCoordinates.x + dirVec.x;
                int targetY = gridCoordinates.y + dirVec.y;
                GridObject potentialTarget = LevelManager.Instance.GetGridObject(targetX, targetY);
                bool isTargetStatue = potentialTarget != null && potentialTarget.gridObjectType == GridObjectType.Statue;
                // ---------------------------

                if (LevelManager.Instance.RequestMove(this, inputDir.Value))
                {
                    float size = LevelManager.Instance.cellSize;
                    targetPosition = new Vector3(gridCoordinates.x * size, gridCoordinates.y * size,
                        transform.position.z);

                    isMoving = true;
                    
                    // 如果移动成功，且前方本来是雕像，说明正在推雕像
                    if (isTargetStatue)
                    {
                        isPushing = true;
                        String randomPushSFX = "Pushing" + Random.Range(1, 3).ToString();
                        AudioManager.Instance.PlaySFX(randomPushSFX);
                        MoveTo(targetPosition, pushDuration);
                    }
                    else
                    {
                        isPushing = false;
                        MoveTo(targetPosition, moveDuration);
                    }

                }
            }

            // 2. 咏唱输入
            if (Input.GetKeyDown(KeyCode.Q))
            {
                LevelManager.Instance.CastChant(gridCoordinates, direction);
                isChanting = true;
            }


            if (Input.GetKeyUp(KeyCode.Q))
            {
                LevelManager.Instance.StopChant();
                isChanting = false;
            }

            // 3. 交互输入
            if (Input.GetKeyDown(KeyCode.E))
            {
                TryInteract();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                LevelManager.Instance?.RestartLevel();
            }
        }

        /*private void HandleMovement()
        {
            if (isMoving && useSmoothMovement)
            {
                // 【核心修改】根据当前是否在推东西，决定移动速度
                // 如果在推，用较慢的 pushSpeed，否则用较快的 moveSpeed
                float currentSpeed = isPushing ? pushSpeed : moveSpeed;

                transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);

                if (Vector3.Distance(transform.position, targetPosition) < 0.001f)
                {
                    transform.position = targetPosition;
                    isMoving = false;
                    isPushing = false; // 移动结束，停止推动姿态
                }
            }
        }*/
        
        private void MoveTo(Vector3 targetPos, float duration)
        {
            isMoving = true;
            String randomWalkSFX = "Walk" + Random.Range(1, 4).ToString();
            AudioManager.Instance.PlaySFX(randomWalkSFX);
            transform.DOMove(targetPos, duration)
                .SetEase(Ease.Linear) // 线性移动最适合格子游戏
                .OnComplete(() => {
                    isMoving = false;
                    isPushing = false; // 移动结束后退出推动姿态
                });
        }

        private void TryInteract()
        {
            // 1. 获取正前方的坐标
            Vector2Int dirVec = DirectionToVector2Int(direction);
            int targetX = gridCoordinates.x + dirVec.x;
            int targetY = gridCoordinates.y + dirVec.y;

            // 2. 获取该位置的物体
            GridObject target = LevelManager.Instance.GetGridObject(targetX, targetY);
            
            if (target != null) 
            {
                Debug.Log("交互成功 with " + target.gridObjectType.ToString());

                // --- 新增：如果是雕像，触发祈祷动作 ---
                if (target.gridObjectType == GridObjectType.Statue)
                {
                    StartCoroutine(PerformPrayAction());
                }
                else
                {
                    target.Interact();
                }
            }
        }

        // --- 新增：祈祷协程 ---
        IEnumerator PerformPrayAction()
        {
            isPraying = true;
            // 保持祈祷姿势 0.5 秒 (或者等待动画播放完毕)
            yield return new WaitForSeconds(0.5f);
            String randomPraySFX = "Praying" + Random.Range(1, 3).ToString();
            AudioManager.Instance.PlaySFX(randomPraySFX);
            Vector2Int dirVec = DirectionToVector2Int(direction);
            int targetX = gridCoordinates.x + dirVec.x;
            int targetY = gridCoordinates.y + dirVec.y;
            GridObject target = LevelManager.Instance.GetGridObject(targetX, targetY);
            target.Interact();
            isPraying = false;
        }

        protected void UpdateVisualRotation()
        {
            transform.rotation = Quaternion.identity; // 强制保持不旋转，只切换图片
        }

        private void UpdateAnimation()
        {
            // 根据状态选择对应的精灵图集
            Sprite targetUp, targetDown, targetLeft, targetRight;

            // 优先级：祈祷 > 推动 > 普通移动/待机
            if (isPraying)
            {
                targetUp = PrayUpSprite;
                targetDown = PrayDownSprite;
                targetLeft = PrayLeftSprite;
                targetRight = PrayRightSprite;
            }
            else if (isPushing)
            {
                targetUp = PushUpSprite;
                targetDown = PushDownSprite;
                targetLeft = PushLeftSprite;
                targetRight = PushRightSprite;
            }
            else if(isChanting)
            {
                targetUp = ChantUpSprite;
                targetDown = ChantDownSprite;
                targetLeft = ChantLeftSprite;
                targetRight = ChantRightSprite;
            }
            else
            {
                targetUp = UpSprite;
                targetDown = DownSprite;
                targetLeft = LeftSprite;
                targetRight = RightSprite;
            }

            // 根据方向应用精灵
            switch (direction)
            {
                case Direction.up:
                    spriteRenderer.sprite = targetUp;
                    break;
                case Direction.down:
                    spriteRenderer.sprite = targetDown;
                    break;
                case Direction.left:
                    spriteRenderer.sprite = targetLeft;
                    break;
                case Direction.right:
                    spriteRenderer.sprite = targetRight;
                    break;
            }
        }
    }
}