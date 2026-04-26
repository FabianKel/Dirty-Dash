using UnityEngine;
using System.Collections;

public enum PlayerState { Idle, Run, Jump, Falling, Dash }

[System.Serializable]
public class PlayerControls
{
    public KeyCode up, down, left, right, run;
}

public class PlayerController : MonoBehaviour
{
    [Header("Speed & Jump")]
    public float runSpeed = 16f;
    public float jumpForce = 14.5f;
    public float coyoteTime = 0.2f;
    public float fallMultiplier = 3.5f;
    public float lowJumpMultiplier = 3f;

    [Header("Movement Physics")]
    public float acceleration = 60f;
    public float deceleration = 80f;
    public float airControlMultiplier = 0.7f;

    [Header("Dash")]
    public float dashForce = 25f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1.2f;

    [Header("Controls & Identity")]
    public PlayerControls controls;
    public int playerIndex = 1;
    public LayerMask groundLayer;
    public GameObject blindOverlay;

    [Header("Setup del Personaje")]
    public CharacterData selectedCharacter;
    public SpriteRenderer spriteRenderer;
    public Animator animator;

    private PlayerState currentState;
    private Rigidbody2D rb;
    private BoxCollider2D col;
    private float coyoteCounter;
    private bool isGrounded, _isDashing, _canDash = true, _invertedControls = false;
    private float _speedMultiplier = 1f;

    private Coroutine _slowCoroutine, _blindCoroutine, _invertCoroutine, _boostCoroutine;

    void Awake()
    {
        if (selectedCharacter != null)
        {
            if (spriteRenderer) spriteRenderer.sprite = selectedCharacter.ingameSprite;
            if (animator && selectedCharacter.animatorController)
            {
                animator.runtimeAnimatorController = selectedCharacter.animatorController;
            } else {                 Debug.LogWarning($"Player {playerIndex} tiene CharacterData asignado pero falta SpriteRenderer o Animator para configurarlo."); }
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        ResolveBlindOverlay();
        TransitionToState(PlayerState.Idle);
    }

    void OnEnable() => PlayerRegistry.Register(this);
    void OnDisable() => PlayerRegistry.Unregister(this);

    void Update()
    {
        CheckGround();
        HandleInputs();
        ApplyBetterJump();
        UpdateStateMachine();
    }

    void CheckGround()
    {
        isGrounded = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0f, Vector2.down, 0.1f, groundLayer);
        coyoteCounter = isGrounded ? coyoteTime : coyoteCounter - Time.deltaTime;
    }

    void HandleInputs()
    {
        if (Input.GetKeyDown(controls.up) && coyoteCounter > 0) Jump();
        if (Input.GetKeyDown(controls.run) && _canDash) StartCoroutine(Dash());
    }

    void UpdateStateMachine()
    {
        if (_isDashing) return;

        float rawInput = 0;
        if (Input.GetKey(controls.left)) rawInput = -1;
        if (Input.GetKey(controls.right)) rawInput = 1;

        float moveInput = _invertedControls ? -rawInput : rawInput;
        float targetSpeed = moveInput * (runSpeed * _speedMultiplier);

        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        if (!isGrounded) accelRate *= airControlMultiplier;

        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float movement = speedDiff * accelRate * Time.deltaTime;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement, rb.linearVelocity.y);

        // Transiciones basadas en movimiento
        if (isGrounded && !_isDashing)
        {
            if (Mathf.Abs(rb.linearVelocity.x) > 0.1f) TransitionToState(PlayerState.Run);
            else TransitionToState(PlayerState.Idle);
        }
        else if (!isGrounded && rb.linearVelocity.y < -0.1f)
        {
            TransitionToState(PlayerState.Falling);
        }

        if (rawInput != 0) transform.localScale = new Vector3(Mathf.Sign(rawInput), 1, 1);
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        coyoteCounter = 0;
        TransitionToState(PlayerState.Jump);
    }

    void ApplyBetterJump()
    {
        if (rb.linearVelocity.y < 0)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(controls.up))
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
    }

    void TransitionToState(PlayerState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        Debug.Log($"Cambiando a: {newState}");
        // Actualizar animaciones
        if (animator != null)
        {
            animator.SetBool("IsRunning", newState == PlayerState.Run);
            
            if (newState == PlayerState.Jump) animator.SetTrigger("Jump");
        }
    }

    // --- EFECTOS Y DASH (Iguales) ---
    public void ApplySlow(float d, float m = 0.5f) => ResetRoutine(ref _slowCoroutine, SlowRoutine(d, m));
    public void ApplyBlind(float d) => ResetRoutine(ref _blindCoroutine, BlindRoutine(d));
    public void ApplyInvertControls(float d) => ResetRoutine(ref _invertCoroutine, InvertRoutine(d));
    public void ApplyBoost(float d, float m = 1.8f) => ResetRoutine(ref _boostCoroutine, BoostRoutine(d, m));

    private void ResetRoutine(ref Coroutine current, IEnumerator next)
    {
        if (current != null) StopCoroutine(current);
        current = StartCoroutine(next);
    }

    IEnumerator SlowRoutine(float d, float m) { _speedMultiplier = m; yield return new WaitForSeconds(d); _speedMultiplier = 1f; }
    IEnumerator InvertRoutine(float d) { _invertedControls = true; yield return new WaitForSeconds(d); _invertedControls = false; }
    IEnumerator BlindRoutine(float d)
    {
        ResolveBlindOverlay();
        if (blindOverlay == null)
        {
            Debug.LogWarning($"Player {playerIndex}: no se encontro BlindOverlay para aplicar el efecto.");
            yield break;
        }

        blindOverlay.SetActive(true);
        yield return new WaitForSeconds(d);
        if (blindOverlay) blindOverlay.SetActive(false);
    }
    IEnumerator BoostRoutine(float d, float m) { _speedMultiplier = m; yield return new WaitForSeconds(d); _speedMultiplier = 1f; }

    void ResolveBlindOverlay()
    {
        if (blindOverlay != null)
        {
            EnsureBlindCanvasCamera(blindOverlay);
            return;
        }

        // Preferred path: the camera that follows this player should contain the matching BlindOverlay.
        var follows = Object.FindObjectsByType<CameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < follows.Length; i++)
        {
            var follow = follows[i];
            if (follow == null || follow.target != transform) continue;

            blindOverlay = FindBlindOverlayInChildren(follow.transform);
            if (blindOverlay != null)
            {
                EnsureBlindCanvasCamera(blindOverlay);
                return;
            }
        }

        // Fallback: try under this player hierarchy.
        blindOverlay = FindBlindOverlayInChildren(transform);
        if (blindOverlay != null)
        {
            EnsureBlindCanvasCamera(blindOverlay);
            return;
        }

        // Last fallback: any overlay in the same scene.
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            var t = all[i];
            if (t == null || t.name != "BlindOverlay") continue;
            if (!t.gameObject.scene.IsValid() || t.gameObject.scene != gameObject.scene) continue;

            blindOverlay = t.gameObject;
            EnsureBlindCanvasCamera(blindOverlay);
            return;
        }
    }

    GameObject FindBlindOverlayInChildren(Transform root)
    {
        if (root == null) return null;

        var children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == "BlindOverlay")
                return children[i].gameObject;
        }

        return null;
    }

    void EnsureBlindCanvasCamera(GameObject overlay)
    {
        if (overlay == null) return;

        var canvas = overlay.GetComponentInParent<Canvas>(true);
        if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceCamera) return;

        var parentCamera = overlay.GetComponentInParent<Camera>(true);
        if (parentCamera != null && canvas.worldCamera != parentCamera)
            canvas.worldCamera = parentCamera;
    }

    IEnumerator Dash()
    {
        _canDash = false; _isDashing = true;
        float dir = Input.GetKey(controls.left) ? -1 : (Input.GetKey(controls.right) ? 1 : transform.localScale.x);
        float gravityBefore = rb.gravityScale;
        rb.gravityScale = 0;
        rb.linearVelocity = new Vector2(dir * dashForce, 0);
        TransitionToState(PlayerState.Dash);
        yield return new WaitForSeconds(dashDuration);
        rb.gravityScale = gravityBefore;
        _isDashing = false;
        yield return new WaitForSeconds(dashCooldown);
        _canDash = true;
    }
}
