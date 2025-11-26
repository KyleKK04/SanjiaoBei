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

        // 【修改】调高移动速度，让格子移动更干脆
        private float moveSpeed = 20f;

        // 【新增】是否开启平滑移动（设为 false 则为瞬移）
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
            // 只有完全停止时才接受下一次输入
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

            // 【修改】使用 GetKeyDown 代替 GetKey
            // 这样玩家必须每次按下按键才会移动一格，手感就是“一格一格”的
            if (Input.GetKeyDown(KeyCode.W)) inputDir = Direction.up;
            else if (Input.GetKeyDown(KeyCode.S)) inputDir = Direction.down;
            else if (Input.GetKeyDown(KeyCode.A)) inputDir = Direction.left;
            else if (Input.GetKeyDown(KeyCode.D)) inputDir = Direction.right;

            if (inputDir.HasValue)
            {
                // 转向逻辑
                if (direction != inputDir.Value)
                {
                    direction = inputDir.Value;
                    UpdateVisualRotation();
                    // 如果希望“转向时不移动”，保留 return
                    // 如果希望“转向并立刻移动”，注释掉 return
                    return;
                }

                // 申请移动
                if (LevelManager.Instance.RequestMove(this, inputDir.Value))
                {
                    float size = LevelManager.Instance.cellSize;
                    targetPosition = new Vector3(gridCoordinates.x * size, gridCoordinates.y * size,
                        transform.position.z);

                    // 标记开始移动
                    isMoving = true;

                    // 如果关闭了平滑移动，直接瞬移
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

            // 3. 交互输入
            if (Input.GetKeyDown(KeyCode.E))
            {
                TryInteract();
            }
        }

        private void HandleMovement()
        {
            // 只有开启平滑移动时才执行插值
            if (isMoving && useSmoothMovement)
            {
                transform.position =
                    Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

                // 距离极近时吸附并停止
                if (Vector3.Distance(transform.position, targetPosition) < 0.001f)
                {
                    transform.position = targetPosition;
                    isMoving = false;
                }
            }
        }

        private void TryInteract()
        {
            // ... (保持不变) ...
            Vector2Int dirVec = DirectionToVector2Int(direction);
            GridObject target =
                LevelManager.Instance.GetGridObject(gridCoordinates.x + dirVec.x, gridCoordinates.y + dirVec.y);
            if (target != null) target.Interact();

            Vector2Int[] offsets =
                { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };
            foreach (var off in offsets)
            {
                GridObject obj =
                    LevelManager.Instance.GetGridObject(gridCoordinates.x + off.x, gridCoordinates.y + off.y);
                if (obj != null && obj.gridObjectType == GridObjectType.Statue)
                {
                    Vector2Int rev = new Vector2Int(-off.x, -off.y);
                    obj.direction = Vector2IntToDirection(rev);
                    obj.SendMessage("UpdateVisualRotation");
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