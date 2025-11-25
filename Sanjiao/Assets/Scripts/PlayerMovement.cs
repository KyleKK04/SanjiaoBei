using Game.Data;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerMovement : GridObject
{   //继承GridObject类

    //刚体碰撞体动画组件
    private Rigidbody2D rb;
    private BoxCollider2D coll;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    //移动
    private float moveSpeed = 5f;   //速度
    private Vector2 movement;       //移动向量
    private bool isMoving = false;  //是否在移动
    private Vector3 targetPosition; //目标位置

    //网格
    private float gridSize = 1f;      //网格大小

    void Awake()
    {
        // 初始化父类属性
        isBlockingMovement = true;
        isMovable = false;

        // 初始化网格坐标和方向
        gridCoordinates = new GridCoordinates(0, 0);
        direction = Direction.down;
    }



    void Start()
    {
        //获取组件
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        //初始化目标位置
        targetPosition = transform.position;

    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();
        HandleMovement();
        UpdateAnimation();
        Interact();


    }
    private void HandleInput()
    {
        if (isMoving) 
            return;

        // 获取输入并尝试移动
        if (Input.GetKeyDown(KeyCode.W)) // 上
            TryMove(Direction.up);
        else if (Input.GetKeyDown(KeyCode.S)) // 下
            TryMove(Direction.down);
        else if (Input.GetKeyDown(KeyCode.A)) // 左
            TryMove(Direction.left);
        else if (Input.GetKeyDown(KeyCode.D)) // 右
            TryMove(Direction.right);
    }

    private void HandleMovement()
    {
        if (isMoving)
        {
            // 平滑移动到目标位置
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            // 检查是否到达目标位置
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;
                // 移动完成后更新网格坐标
                UpdateGridCoordinatesFromPosition();
            }
        }
    }


    void TryMove(Direction moveDirection)
    {
        // 计算目标网格坐标
        GridCoordinates targetCoord = CalculateTargetGridPosition(moveDirection);

        // 计算目标世界位置
        Vector3 worldTargetPos = new Vector3(targetCoord.x * gridSize, targetCoord.y * gridSize, transform.position.z);

     
        // 更新目标位置
        targetPosition = worldTargetPos;
        isMoving = true;

        // 更新方向
        direction = moveDirection;

        // 更新网格坐标
        gridCoordinates = targetCoord;
        
    }

    private GridCoordinates CalculateTargetGridPosition(Direction dir)
    {
        int targetX = gridCoordinates.x;
        int targetY = gridCoordinates.y;

        switch (dir)
        {
            case Direction.up:
                targetY += 1;
                break;
            case Direction.down:
                targetY -= 1;
                break;
            case Direction.left:
                targetX -= 1;
                break;
            case Direction.right:
                targetX += 1;
                break;
        }

        return new GridCoordinates(targetX, targetY);
    }

    private void UpdateGridCoordinatesFromPosition()
    {
        // 从世界坐标更新网格坐标
        gridCoordinates = new GridCoordinates(
            Mathf.RoundToInt(transform.position.x / gridSize),
            Mathf.RoundToInt(transform.position.y / gridSize)
        );
    }

    private void UpdateAnimation()
    {
        //更新动画参数
        anim.SetFloat("Horizontal", movement.x);
        anim.SetFloat("Vertical", movement.y);
        anim.SetFloat("Speed", movement.sqrMagnitude);
        //翻转角色朝向
        if (movement.x < 0)
            spriteRenderer.flipX = true;
        else if (movement.x > 0)
            spriteRenderer.flipX = false;
    }

}
