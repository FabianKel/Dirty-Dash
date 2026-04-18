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

    [Header("Identity")]
    public int playerIndex = 1; // 1 o 2

    [Header("Blind Overlay")]
    [Tooltip("Image negra que cubre la pantalla del jugador")]
    public GameObject blindOverlay;

    // Estado original 
    private PlayerState currentState;
    private Rigidbody2D rb;
    private BoxCollider2D col;
    private float coyoteCounter;
    private bool isGrounded;

    [Header("Detection")]
    public LayerMask groundLayer;

    // Efectos 
    private float _speedMultiplier = 1f;
    private bool _invertedControls = false;

    private Coroutine _slowCoroutine;
    private Coroutine _blindCoroutine;
    private Coroutine _invertCoroutine;
    private Coroutine _boostCoroutine;

    // Unity lifecycle 
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        TransitionToState(PlayerState.Idle);
    }

    void OnEnable()  => PlayerRegistry.Register(this);
    void OnDisable() => PlayerRegistry.Unregister(this);

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
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;
    }

    void HandleInputs()
    {
        if (Input.GetKeyDown(controls.up) && coyoteCounter > 0)
            Jump();
    }

    void UpdateStateMachine()
    {
        float rawInput = 0;
        if (Input.GetKey(controls.left))  rawInput = -1;
        if (Input.GetKey(controls.right)) rawInput =  1;

        // Efecto de inversión  
        float moveInput = _invertedControls ? -rawInput : rawInput;

        bool isRunning = Input.GetKey(controls.run);

        // Multiplicador de velocidad (slow / boost) 
        float currentSpeed = (isRunning ? runSpeed : walkSpeed) * _speedMultiplier;

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
                if (rb.linearVelocity.y < -0.1f) TransitionToState(PlayerState.Falling); 
                if (isGrounded && rb.linearVelocity.y <= 0) TransitionToState(PlayerState.Idle);
                break;

            case PlayerState.Falling: 
                if (isGrounded) TransitionToState(PlayerState.Idle);
                break;
        }

        // Flip de sprite usa rawInput para que siempre apunte hacia donde presionas
        if (rawInput != 0) transform.localScale = new Vector3(Mathf.Sign(rawInput), 1, 1);
    }

    void TransitionToState(PlayerState newState)
    {
        if (currentState == newState) return;
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

    // Efectos 

    public void ApplySlow(float duration, float multiplier = 0.35f)
    {
        if (_slowCoroutine != null) StopCoroutine(_slowCoroutine);
        _slowCoroutine = StartCoroutine(SlowRoutine(duration, multiplier));
    }

    IEnumerator SlowRoutine(float duration, float multiplier)
    {
        _speedMultiplier = multiplier;
        yield return new WaitForSeconds(duration);
        _speedMultiplier = 1f;
        _slowCoroutine = null;
    }

    public void ApplyBlind(float duration)
    {
        if (_blindCoroutine != null) StopCoroutine(_blindCoroutine);
        _blindCoroutine = StartCoroutine(BlindRoutine(duration));
    }

    IEnumerator BlindRoutine(float duration)
    {
        if (blindOverlay) blindOverlay.SetActive(true);
        yield return new WaitForSeconds(duration);
        if (blindOverlay) blindOverlay.SetActive(false);
        _blindCoroutine = null;
    }

    public void ApplyInvertControls(float duration)
    {
        if (_invertCoroutine != null) StopCoroutine(_invertCoroutine);
        _invertCoroutine = StartCoroutine(InvertRoutine(duration));
    }

    IEnumerator InvertRoutine(float duration)
    {
        _invertedControls = true;
        yield return new WaitForSeconds(duration);
        _invertedControls = false;
        _invertCoroutine = null;
    }

    public void ApplyBoost(float duration, float multiplier = 1.7f)
    {
        if (_boostCoroutine != null) StopCoroutine(_boostCoroutine);
        _boostCoroutine = StartCoroutine(BoostRoutine(duration, multiplier));
    }

    IEnumerator BoostRoutine(float duration, float multiplier)
    {
        _speedMultiplier = multiplier;
        yield return new WaitForSeconds(duration);
        _speedMultiplier = 1f;
        _boostCoroutine = null;
    }
}