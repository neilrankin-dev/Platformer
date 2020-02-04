using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private float movementInputDirection;
    private float currentMovementSpeed;
    private float sprintAnimSpeed = 1.5f;
    private float walkAnimSpeed = 1.0f;

    private int amountOfJumpsLeft;
    private int facingDirection = 1; // -1 is left, and +1 is right
    

    private bool isFacingRight = true;
    private bool isWalking = false;
    private bool canJump;
    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isSprinting;
    

    private Rigidbody2D rb;
    private Animator anim;

    [Header("Movement Settings")]    
    public float walkSpeed = 10;
    public float sprintSpeed = 15;
    public float jumpForce = 16;

    [Header("Jump Settings")]
    public float groundCheckRadius;
    public float movementForceInAir;
    public float airDragMultiplier = 0.95f;
    public float variableJumpHeightMultiplier = 0.5f;
    public int amountOfExtraJumps = 1;
    public bool multiJump;
    public Transform groundCheck;
    public LayerMask whatIsGround;

    [Header("Wallslide Settings")]
    public float wallCheckDistance;
    public float wallSlideSpeed;
    public Transform wallCheck;

    [Header("Wall Jump settings")]
    public float wallHopForce;
    public float wallJumpForce;
    public bool canWallJump;
    public Vector2 wallHopDirection;
    public Vector2 wallJumpDirection;


    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        amountOfJumpsLeft = amountOfExtraJumps;
        currentMovementSpeed = walkSpeed;
        isSprinting = false;

        wallHopDirection.Normalize();
        wallJumpDirection.Normalize();
    }


    private void Update()
    {
        CheckInput();
        CheckMovementDirection();
        UpdateAnimations();
        CheckIfCanJump();
        CheckIfWallSliding();

        if (isGrounded)
        {
            CheckSprint();
        }
        else
        {
            if (Input.GetButtonUp("Sprint") && isSprinting)
            {
                isSprinting = false;
                anim.speed = walkAnimSpeed;
                currentMovementSpeed = walkSpeed;
            }
        }
    }

    private void FixedUpdate()
    {
        ApplyMovement();
        CheckSurroundings();
    }

    private void CheckSurroundings()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);

        isTouchingWall = Physics2D.Raycast(wallCheck.position, transform.right, wallCheckDistance, whatIsGround);
    }

    private void CheckIfWallSliding()
    {
        if (isTouchingWall && !isGrounded && rb.velocity.y < 0)
        {
            isWallSliding = true;
            amountOfJumpsLeft = amountOfExtraJumps;
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void CheckInput()
    {
        movementInputDirection = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump"))
        {
            Jump();
        }

        if (Input.GetButtonUp("Jump"))
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * variableJumpHeightMultiplier);
        }
    }

    private void ApplyMovement()
    {
        if (isGrounded && !canWallJump)
        {
            rb.velocity = new Vector2(movementInputDirection * currentMovementSpeed, rb.velocity.y);
        }
        else if (!isGrounded && !isWallSliding && movementInputDirection != 0 && !canWallJump)
        {
            Vector2 forceToAdd = new Vector2(movementForceInAir * movementInputDirection, 0);
            rb.AddForce(forceToAdd);

            if (Mathf.Abs(rb.velocity.x) > currentMovementSpeed)
            {
                rb.velocity = new Vector2(currentMovementSpeed * movementInputDirection, rb.velocity.y);
            }
        }
        else if (!isGrounded && !isWallSliding && movementInputDirection == 0)
        {
            rb.velocity = new Vector2(rb.velocity.x * airDragMultiplier, rb.velocity.y);
        }
        else if (canWallJump)
        {
            rb.velocity = new Vector2(movementInputDirection * currentMovementSpeed, rb.velocity.y);
        }

        if (isWallSliding)
        {
            if (rb.velocity.y < -wallSlideSpeed)
            {
                rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
            }
        }
    }

    private void Jump()
    {
        if (canJump && !isWallSliding)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            amountOfJumpsLeft -= 1;
        }
        else if (isWallSliding && movementInputDirection == 0 && canJump) //Wall hop
        {
            isWallSliding = false;
            amountOfJumpsLeft--;
            Vector2 forceToAdd = new Vector2(wallHopForce * wallHopDirection.x * -facingDirection, wallHopForce * wallHopDirection.y);
            rb.AddForce(forceToAdd, ForceMode2D.Impulse);
        }
        else if ((isWallSliding || isTouchingWall) && movementInputDirection != 0 && canJump) //Wall Jump
        {
            isWallSliding = false;
            amountOfJumpsLeft--;
            Vector2 forceToAdd = new Vector2(wallJumpForce * wallJumpDirection.x * movementInputDirection, wallJumpForce * wallJumpDirection.y);
            rb.AddForce(forceToAdd, ForceMode2D.Impulse);
        }
    }

    private void CheckSprint()
    {
        if (Input.GetButton("Sprint") && !isSprinting)
        {
            isSprinting = true;
            anim.speed = sprintAnimSpeed;
            currentMovementSpeed = sprintSpeed;
        }

        if (Input.GetButtonUp("Sprint") && isSprinting)
        {
            isSprinting = false;
            anim.speed = walkAnimSpeed;
            currentMovementSpeed = walkSpeed;
        }
    }

    private void CheckMovementDirection()
    {
        if (isFacingRight && movementInputDirection < 0)
        {
            Flip();
        }
        else if (!isFacingRight && movementInputDirection > 0)
        {
            Flip();
        }

        if (rb.velocity.x >= 0.2f || rb.velocity.x <= -0.2f)
        {
            isWalking = true;
        }
        else if (rb.velocity.x <= 0.2f || rb.velocity.x >= -0.2f)
        {
            isWalking = false;
        }
    }

    private void CheckIfCanJump()
    {
        if (isGrounded || isWallSliding)
        {
            amountOfJumpsLeft = amountOfExtraJumps;
        }

        if (multiJump)
        {
            MultiJump();
        }
        else
        {
            if (isGrounded)
            {
                canJump = true;
            }
            else
            {
                canJump = false;
            }
        }

    }

    private void MultiJump()
    {
        if (amountOfJumpsLeft <= 0)
        {
            canJump = false;
        }
        else
        {
            canJump = true;
        }
    }

    private void Flip()
    {
        if (!isWallSliding)
        {
            facingDirection *= -1;
            isFacingRight = !isFacingRight;
            transform.Rotate(0.0f, 180.0f, 0.0f);
        }
    }

    private void UpdateAnimations()
    {
        if (isWalking && isGrounded)
        {
            anim.Play("Walk");
        }
        else if (!isWalking && isGrounded)
        {
            anim.Play("Idle");
        }

        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isWallSliding", isWallSliding);
        anim.SetFloat("yVelocity", rb.velocity.y);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.DrawLine(wallCheck.position, new Vector3(wallCheck.position.x + wallCheckDistance, wallCheck.position.y, wallCheck.position.z));
    }

}
