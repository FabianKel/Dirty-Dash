using UnityEngine;
using System.Collections;

public enum PlayerState { Idle, Walk, Run, Jump, Falling }


[System.Serializable]
public class PlayerControls
{
    public KeyCode up;
    public KeyCode down;
    public KeyCode left;
    public KeyCode right;
    public KeyCode run;
}

public class PlayerController : MonoBehaviour
{

    [Header("Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    public float jumpForce = 12f;
    public float coyoteTime = 0.15f;

    [Header("Controls")]
    public PlayerControls controls;

    private PlayerState currentState;
    private Rigidbody2D rb;
    private BoxCollider2D col;
    private float coyoteCounter;
    private bool isGrounded;

    [Header("Detection")]
    public LayerMask groundLayer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        TransitionToState(PlayerState.Idle);
    }

    void Update()
    {
        CheckGround();
        HandleInputs();
        UpdateStateMachine();
    }

    void CheckGround()
    {
        isGrounded = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0f, Vector2.down, 0.1f, groundLayer);

        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }
    }

    void HandleInputs()
    {
        if (Input.GetKeyDown(controls.up) && coyoteCounter > 0)
        {
            Jump();
        }
    }

    void UpdateStateMachine()
    {
        float moveInput = 0;
        if (Input.GetKey(controls.left)) moveInput = -1;
        if (Input.GetKey(controls.right)) moveInput = 1;

        bool isRunning = Input.GetKey(controls.run);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        rb.linearVelocity = new Vector2(moveInput * currentSpeed, rb.linearVelocity.y);

        switch (currentState)
        {
            case PlayerState.Idle:
                if (moveInput != 0) TransitionToState(isRunning ? PlayerState.Run : PlayerState.Walk);
                if (rb.linearVelocity.y > 0.1f) TransitionToState(PlayerState.Jump);
                break;

            case PlayerState.Walk:
                if (moveInput == 0) TransitionToState(PlayerState.Idle);
                if (isRunning) TransitionToState(PlayerState.Run);
                if (rb.linearVelocity.y > 0.1f) TransitionToState(PlayerState.Jump);
                break;

            case PlayerState.Run:
                if (moveInput == 0) TransitionToState(PlayerState.Idle);
                if (!isRunning && moveInput != 0) TransitionToState(PlayerState.Walk);
                if (rb.linearVelocity.y > 0.1f) TransitionToState(PlayerState.Jump);
                break;

            case PlayerState.Jump:
                if (isGrounded && rb.linearVelocity.y <= 0) TransitionToState(PlayerState.Idle);
                break;
        }

        if (moveInput != 0) transform.localScale = new Vector3(Mathf.Sign(moveInput), 1, 1);
    }

    void TransitionToState(PlayerState newState)
    {
        if (currentState == newState) return;

        // triggers para animaciones
        // anim.SetTrigger(newState.ToString());

        currentState = newState;
        Debug.Log($"Jugador {gameObject.name} entró en estado: {newState}");
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        coyoteCounter = 0;
        TransitionToState(PlayerState.Jump);
    }
}