// --- PlayerMovement.cs ---

using Game.Core;
using Game.Data;
using UnityEngine;

namespace Game.Core
{
    public class PlayerMovement : GridObject
    {
        private Animator anim;
        private SpriteRenderer spriteRenderer;

        private float moveSpeed = 20f;
        public bool useSmoothMovement = true;

        private bool isMoving = false;
        private Vector3 targetPosition;

        void Awake()
        {
            isBlockingMovement = true;
            gridObjectType = GridObjectType.Player;
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
            if (!isMoving)
            {
                HandleInput();
            }

            HandleMovement();
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
                    UpdateVisualRotation();
                    return;
                }

                if (LevelManager.Instance.RequestMove(this, inputDir.Value))
                {
                    float size = LevelManager.Instance.cellSize;
                    targetPosition = new Vector3(gridCoordinates.x * size, gridCoordinates.y * size,
                        transform.position.z);

                    isMoving = true;

                    if (!useSmoothMovement)
                    {
                        transform.position = targetPosition;
                        isMoving = false;
                    }
                }
            }

            // 2. 咏唱输入
            if (Input.GetKeyDown(KeyCode.Q))
            {
                LevelManager.Instance.CastChant(gridCoordinates, direction);
            }

            if (Input.GetKeyUp(KeyCode.Q))
            {
                LevelManager.Instance.StopChant();
            }

            // 3. 交互输入
            if (Input.GetKeyDown(KeyCode.E))
            {
                TryInteract();
            }
        }

        private void HandleMovement()
        {
            if (isMoving && useSmoothMovement)
            {
                transform.position =
                    Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

                if (Vector3.Distance(transform.position, targetPosition) < 0.001f)
                {
                    transform.position = targetPosition;
                    isMoving = false;
                }
            }
        }

        // 【修改】只交互正前方，且针对雕像直接执行转向逻辑
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
                // 【逻辑修改】如果是雕像，直接让它转过来面向玩家
                if (target.gridObjectType == GridObjectType.Statue)
                {
                    // 玩家面朝 (x, y)，雕像就要面朝 (-x, -y) 才能对视
                    Vector2Int facePlayerVec = new Vector2Int(-dirVec.x, -dirVec.y);
                    
                    // 修改数据
                    target.direction = Vector2IntToDirection(facePlayerVec);
                    
                    // 刷新视觉 (使用 SendMessage 调用 protected 方法)
                    target.SendMessage("UpdateVisualRotation", SendMessageOptions.DontRequireReceiver);
                    
                    Debug.Log($"已将前方雕像 ({targetX},{targetY}) 转向面向玩家。");
                }
                else
                {
                    // 如果是大门或其他物体，保持调用 Interact() 接口 (用于触发通关等)
                    target.Interact();
                }
            }
        }

        private void UpdateAnimation()
        {
            if (anim == null) return;
            anim.SetBool("IsMoving", isMoving);
        }
    }
}