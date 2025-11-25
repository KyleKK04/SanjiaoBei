using Game.Data;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerMovement : GridObject
{   

    private Rigidbody2D rb;
    private BoxCollider2D coll;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    //
    private float moveSpeed = 5f;   //
    private Vector2 movement;       //
    private bool isMoving = false;  //
    private Vector3 targetPosition; //

    private float gridSize = 1f;      

    void Awake()
    {
        isBlockingMovement = true;
        isMovable = false;

        gridCoordinates = new GridCoordinates(0, 0);
        direction = Direction.down;
    }



    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

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

        if (Input.GetKeyDown(KeyCode.W)) // 
            TryMove(Direction.up);
        else if (Input.GetKeyDown(KeyCode.S)) // 
            TryMove(Direction.down);
        else if (Input.GetKeyDown(KeyCode.A)) // 
            TryMove(Direction.left);
        else if (Input.GetKeyDown(KeyCode.D)) // 
            TryMove(Direction.right);
    }

    private void HandleMovement()
    {
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;
                UpdateGridCoordinatesFromPosition();
            }
        }
    }


    void TryMove(Direction moveDirection)
    {
        GridCoordinates targetCoord = CalculateTargetGridPosition(moveDirection);

        Vector3 worldTargetPos = new Vector3(targetCoord.x * gridSize, targetCoord.y * gridSize, transform.position.z);

     
        targetPosition = worldTargetPos;
        isMoving = true;

        direction = moveDirection;

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
        gridCoordinates = new GridCoordinates(
            Mathf.RoundToInt(transform.position.x / gridSize),
            Mathf.RoundToInt(transform.position.y / gridSize)
        );
    }

    private void UpdateAnimation()
    {
        anim.SetFloat("Horizontal", movement.x);
        anim.SetFloat("Vertical", movement.y);
        anim.SetFloat("Speed", movement.sqrMagnitude);
        if (movement.x < 0)
            spriteRenderer.flipX = true;
        else if (movement.x > 0)
            spriteRenderer.flipX = false;
    }

}
