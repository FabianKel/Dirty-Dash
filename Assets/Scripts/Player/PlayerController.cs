using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem; // Importante añadir esto
using TMPro;
using UnityEngine.UI;

public enum PlayerState { Idle, Run, Jump, Falling, Dash }

[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    const float GroundProbeDistance = 0.1f;
    const float GroundSnapSkin = 0.01f;
    static readonly string[] LethalFangsTiles =
    {
        "fleshbound_fangs_and_pustules_0",
        "fleshbound_fangs_and_pustules_2"
    };

    [Header("Speed & Jump")]
    public float runSpeed = 10f;
    public float jumpForce = 14.5f;
    public float coyoteTime = 0.2f;
    public float fallMultiplier = 3.5f;
    public float lowJumpMultiplier = 3f;
    public float fastFallMultiplier = 8f;

    [Header("Movement Physics")]
    public float acceleration = 20f;
    public float deceleration = 40f;
    public float airControlMultiplier = 0.7f;

    [Header("Dash")]
    public float dashForce = 25f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1.2f;

    [Header("Platform Assist")]
    public float jumpBufferTime = 0.12f;
    public float groundProbeExtraWidth = 0.1f;
    public float groundSnapDistance = 0.18f;
    public float edgeGraceDistance = 0.16f;
    public float cornerCorrectionDistance = 0.12f;
    public float ledgeCatchVerticalWindow = 0.2f;

    [Header("Controls & Identity")]
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
    private Vector2 _spawnPosition;
    private Vector2 _currentCheckpoint;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private float edgeSupportTimer;
    private bool isGrounded, _isDashing, _canDash = true, _invertedControls = false;
    private bool _isRespawning;
    private float _speedMultiplier = 1f;
    private Collider2D _lastGroundCollider;
    private RaycastHit2D _lastGroundHit;
    private Tilemap[] _groundTilemaps = System.Array.Empty<Tilemap>();
    private AudioSource _footstepSource;
    private AudioSource _oneShotSource;
    private TextMeshProUGUI _effectStatusText;

    private Coroutine _slowCoroutine, _blindCoroutine, _invertCoroutine, _boostCoroutine;
    private Coroutine _effectStatusCoroutine;

    // Variables del Nuevo Input System
    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _dashAction;
    private InputAction _downAction;

    void Awake()
    {
        if (selectedCharacter != null)
        {
            if (spriteRenderer) spriteRenderer.sprite = selectedCharacter.ingameSprite;
            if (animator && selectedCharacter.animatorController)
            {
                animator.runtimeAnimatorController = selectedCharacter.animatorController;
            }
            else { Debug.LogWarning($"Player {playerIndex} tiene CharacterData asignado pero falta SpriteRenderer o Animator para configurarlo."); }
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();

        // Inicializar Input Actions
        _playerInput = GetComponent<PlayerInput>();
        _moveAction = _playerInput.actions["Move"];
        _jumpAction = _playerInput.actions["Jump"];
        _dashAction = _playerInput.actions["Dash"];
        _downAction = _playerInput.actions["Down"];

        _spawnPosition = rb.position;
        _currentCheckpoint = rb.position;
        CacheGroundTilemaps();
        ResolveBlindOverlay();
        ResolveEffectStatusText();
        SetupAudioSources();
        TransitionToState(PlayerState.Idle);
    }

    void OnEnable() => PlayerRegistry.Register(this);

    void OnDisable()
    {
        StopFootstepAudio();
        HideEffectStatus();
        PlayerRegistry.Unregister(this);
    }

    void Update()
    {
        CheckGround();
        HandleInputs();
        TryCornerCorrection();
        ApplyBetterJump();
        UpdateStateMachine();
        UpdateFootstepAudio();
        CheckLethalSpikeByFeetTileLookup();
        CheckLethalSpikeUnderFeet();
    }

    void OnCollisionEnter2D(Collision2D collision) => TryHandleHazard(collision.collider, collision);
    void OnCollisionStay2D(Collision2D collision) => TryHandleHazard(collision.collider, collision);
    void OnTriggerEnter2D(Collider2D other) => TryHandleHazard(other, null);
    void OnTriggerStay2D(Collider2D other) => TryHandleHazard(other, null);

    void CheckGround()
    {
        var bounds = col.bounds;
        var probeSize = new Vector2(bounds.size.x + groundProbeExtraWidth, bounds.size.y);
        var directHit = Physics2D.BoxCast(bounds.center, probeSize, 0f, Vector2.down, GroundProbeDistance, groundLayer);
        bool directGrounded = directHit.collider != null;

        if (directGrounded)
        {
            isGrounded = true;
            coyoteCounter = coyoteTime;
            edgeSupportTimer = coyoteTime;
            _lastGroundCollider = directHit.collider;
            _lastGroundHit = directHit;
            return;
        }

        edgeSupportTimer -= Time.deltaTime;
        coyoteCounter -= Time.deltaTime;

        bool assistedGrounded = TryMaintainEdgeSupport() || TrySnapToGround();
        isGrounded = assistedGrounded;

        if (assistedGrounded)
        {
            coyoteCounter = Mathf.Max(coyoteCounter, 0f);
        }
    }

    void HandleInputs()
    {
        if (jumpBufferCounter > 0f) jumpBufferCounter -= Time.deltaTime;

        if (_jumpAction.WasPressedThisFrame())
            jumpBufferCounter = jumpBufferTime;

        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            Jump();
            jumpBufferCounter = 0f;
        }

        if (_dashAction.WasPressedThisFrame() && _canDash) StartCoroutine(Dash());
    }

    void UpdateStateMachine()
    {
        if (_isDashing) return;

        float rawInput = _moveAction.ReadValue<Vector2>().x;

        float moveInput = _invertedControls ? -rawInput : rawInput;
        float targetSpeed = moveInput * (runSpeed * _speedMultiplier);

        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        if (!isGrounded) accelRate *= airControlMultiplier;

        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float movement = speedDiff * accelRate * Time.deltaTime;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement, rb.linearVelocity.y);

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
        edgeSupportTimer = 0;
        PlayJumpAudio();
        TransitionToState(PlayerState.Jump);
    }

    void ApplyBetterJump()
    {
        bool downPressed = _downAction.WasPressedThisFrame();
        bool downHeld = _downAction.IsPressed();
        bool jumpHeld = _jumpAction.IsPressed();

        if (!isGrounded && downPressed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -fastFallMultiplier);
            Debug.Log($"FastFall activado! Velocidad Y: {rb.linearVelocity.y}");
        }
        if (rb.linearVelocity.y < 0 && !isGrounded && downHeld)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fastFallMultiplier - 1) * Time.deltaTime;
        else if (rb.linearVelocity.y < 0)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        else if (rb.linearVelocity.y > 0 && !jumpHeld)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
    }

    void TransitionToState(PlayerState newState)
    {
        if (currentState == newState) return;
        currentState = newState;

        if (animator != null)
        {
            animator.SetBool("IsRunning", newState == PlayerState.Run);
            if (newState == PlayerState.Jump) animator.SetTrigger("Jump");
        }
    }

    // --- EFECTOS Y DASH ---
    public void ApplySlow(float d, float m = 0.5f)
    {
        ShowEffectStatus("Efecto Activo: Has sido ralentizado", d);
        ResetRoutine(ref _slowCoroutine, SlowRoutine(d, m));
    }

    public void ApplyBlind(float d)
    {
        ShowEffectStatus("Efecto Activo: Has sido cegado", d);
        ResetRoutine(ref _blindCoroutine, BlindRoutine(d));
    }

    public void ApplyInvertControls(float d)
    {
        ShowEffectStatus("Efecto Activo: Tus controles han sido invertidos", d);
        ResetRoutine(ref _invertCoroutine, InvertRoutine(d));
    }

    public void ApplyBoost(float d, float m = 1.8f)
    {
        ShowEffectStatus("Efecto Activo: Velocidad aumentada", d);
        ResetRoutine(ref _boostCoroutine, BoostRoutine(d, m));
    }

    public void ShowEffectStatusMessage(string message, float duration)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        ShowEffectStatus($"Efecto Activo: {message}", duration);
    }

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
        if (blindOverlay == null) yield break;

        blindOverlay.SetActive(true);
        yield return new WaitForSeconds(d);
        if (blindOverlay) blindOverlay.SetActive(false);
    }
    IEnumerator BoostRoutine(float d, float m) { _speedMultiplier = m; yield return new WaitForSeconds(d); _speedMultiplier = 1f; }

    bool TryMaintainEdgeSupport()
    {
        if (_lastGroundCollider == null) return false;
        if (edgeSupportTimer <= 0f) return false;

        Bounds groundBounds = _lastGroundCollider.bounds;
        Bounds playerBounds = col.bounds;

        float x = playerBounds.center.x;
        if (x < groundBounds.min.x - edgeGraceDistance || x > groundBounds.max.x + edgeGraceDistance)
            return false;

        float feetY = playerBounds.min.y;
        float topY = groundBounds.max.y;
        float verticalGap = feetY - topY;

        if (verticalGap > groundSnapDistance) return false;
        if (verticalGap < -ledgeCatchVerticalWindow) return false;

        return true;
    }

    bool TrySnapToGround()
    {
        var bounds = col.bounds;
        var probeSize = new Vector2(bounds.size.x + groundProbeExtraWidth, bounds.size.y);
        float snapProbeDistance = groundSnapDistance + ledgeCatchVerticalWindow;
        var hit = Physics2D.BoxCast(bounds.center, probeSize, 0f, Vector2.down, snapProbeDistance, groundLayer);

        if (hit.collider == null) return false;
        if (hit.normal.y < 0.25f) return false;
        if (rb.linearVelocity.y > 0f) return false;

        float feetY = bounds.min.y;
        float topY = hit.point.y;
        float verticalGap = feetY - topY;

        if (verticalGap > groundSnapDistance) return false;
        if (verticalGap < -ledgeCatchVerticalWindow) return false;

        float targetCenterY = topY + bounds.extents.y + GroundSnapSkin;
        rb.position = new Vector2(rb.position.x, targetCenterY);

        if (rb.linearVelocity.y < 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        _lastGroundCollider = hit.collider;
        _lastGroundHit = hit;
        return true;
    }

    void TryCornerCorrection()
    {
        if (_isDashing) return;
        if (rb.linearVelocity.y <= 0.01f) return;

        Bounds bounds = col.bounds;
        float rayDistance = Mathf.Max(ledgeCatchVerticalWindow, 0.05f);
        float inset = Mathf.Min(0.05f, bounds.extents.x * 0.4f);

        Vector2 leftProbe = new Vector2(bounds.min.x + inset, bounds.max.y);
        Vector2 rightProbe = new Vector2(bounds.max.x - inset, bounds.max.y);

        bool leftBlocked = Physics2D.Raycast(leftProbe, Vector2.up, rayDistance, groundLayer).collider != null;
        bool rightBlocked = Physics2D.Raycast(rightProbe, Vector2.up, rayDistance, groundLayer).collider != null;

        if (leftBlocked == rightBlocked) return;

        float shift = leftBlocked ? cornerCorrectionDistance : -cornerCorrectionDistance;
        if (!CanShiftWithoutGroundOverlap(shift)) return;

        rb.position = new Vector2(rb.position.x + shift, rb.position.y);
    }

    bool CanShiftWithoutGroundOverlap(float shiftX)
    {
        Bounds bounds = col.bounds;
        Vector2 nextCenter = (Vector2)bounds.center + new Vector2(shiftX, 0f);
        Vector2 testSize = new Vector2(bounds.size.x * 0.96f, bounds.size.y * 0.96f);
        return Physics2D.OverlapBox(nextCenter, testSize, 0f, groundLayer) == null;
    }

    void ResolveBlindOverlay()
    {
        if (blindOverlay != null)
        {
            EnsureBlindCanvasCamera(blindOverlay);
            return;
        }

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

        blindOverlay = FindBlindOverlayInChildren(transform);
        if (blindOverlay != null)
        {
            EnsureBlindCanvasCamera(blindOverlay);
            return;
        }

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
        if (canvas == null) return;

        EnsureCanvasCamera(canvas, overlay.GetComponentInParent<Camera>(true));
    }

    IEnumerator Dash()
    {
        _canDash = false; _isDashing = true;

        float dir = _moveAction.ReadValue<Vector2>().x;
        if (dir == 0) dir = transform.localScale.x;

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

    void TryHandleHazard(Collider2D hazard, Collision2D collision)
    {
        if (_isRespawning) return;
        if (!IsLethalSpike(hazard, collision)) return;

        HandleDeathBySpike();
    }

    bool IsLethalSpike(Collider2D hazard, Collision2D collision)
    {
        if (hazard == null) return false;
        if (HasFalseTag(hazard.transform)) return false;
        if (hazard.transform != null && HasFalseTag(hazard.transform.root)) return false;

        if (IsLethalSpikeTileAtContact(hazard, collision)) return true;
        if (IsLethalSpikeAroundContacts(collision)) return true;
        if (HasSpikeKeyword(hazard.transform)) return true;

        Transform parent = hazard.transform != null ? hazard.transform.parent : null;
        if (HasSpikeKeyword(parent)) return true;

        Transform grandParent = parent != null ? parent.parent : null;
        return HasSpikeKeyword(grandParent);
    }

    bool IsLethalSpikeTileAtContact(Collider2D hazard, Collision2D collision)
    {
        Tilemap tilemap = hazard.GetComponent<Tilemap>();
        if (tilemap == null) tilemap = hazard.GetComponentInParent<Tilemap>();
        if (tilemap == null) return false;

        Vector3[] samples = GetHazardSamples(collision);
        return HasLethalSpikeInTilemapSamples(tilemap, samples);
    }

    bool IsLethalSpikeAroundContacts(Collision2D collision)
    {
        Vector3[] samples = GetHazardSamples(collision);
        Collider2D[] nearby = Physics2D.OverlapBoxAll(col.bounds.center, col.bounds.size + new Vector3(0.2f, 0.2f, 0f), 0f, groundLayer);

        for (int i = 0; i < nearby.Length; i++)
        {
            Collider2D candidate = nearby[i];
            if (candidate == null) continue;
            if (HasFalseTag(candidate.transform)) continue;
            if (candidate.transform != null && HasFalseTag(candidate.transform.root)) continue;

            Tilemap tilemap = candidate.GetComponent<Tilemap>();
            if (tilemap == null) tilemap = candidate.GetComponentInParent<Tilemap>();
            if (tilemap == null) continue;

            if (HasLethalSpikeInTilemapSamples(tilemap, samples)) return true;
        }

        return false;
    }

    void CacheGroundTilemaps()
    {
        var allTilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        var result = new System.Collections.Generic.List<Tilemap>(allTilemaps.Length);

        for (int i = 0; i < allTilemaps.Length; i++)
        {
            Tilemap tm = allTilemaps[i];
            if (tm == null) continue;

            GameObject go = tm.gameObject;
            if (((1 << go.layer) & groundLayer.value) == 0) continue;
            if (go.GetComponent<TilemapCollider2D>() == null) continue;
            if (HasFalseTag(go.transform) || HasFalseTag(go.transform.root)) continue;

            result.Add(tm);
        }

        _groundTilemaps = result.ToArray();
    }

    Vector3[] GetHazardSamples(Collision2D collision)
    {
        if (collision != null && collision.contactCount > 0)
        {
            Vector3[] contactSamples = new Vector3[collision.contactCount + 12];
            for (int i = 0; i < collision.contactCount; i++)
                contactSamples[i] = collision.GetContact(i).point;

            Vector3[] feetSamples = BuildFeetSamples(col.bounds);
            for (int i = 0; i < feetSamples.Length; i++)
                contactSamples[collision.contactCount + i] = feetSamples[i];

            return contactSamples;
        }

        return BuildFeetSamples(col.bounds);
    }

    Vector3[] BuildFeetSamples(Bounds b)
    {
        return new Vector3[]
        {
            new Vector3(b.center.x, b.min.y + 0.02f, 0f),
            new Vector3(b.min.x + 0.03f, b.min.y + 0.02f, 0f),
            new Vector3(b.max.x - 0.03f, b.min.y + 0.02f, 0f),
            new Vector3(b.center.x, b.min.y - 0.02f, 0f),
            new Vector3(b.min.x + 0.03f, b.min.y - 0.02f, 0f),
            new Vector3(b.max.x - 0.03f, b.min.y - 0.02f, 0f),
            new Vector3(b.center.x, b.min.y - 0.08f, 0f),
            new Vector3(b.min.x + 0.03f, b.min.y - 0.08f, 0f),
            new Vector3(b.max.x - 0.03f, b.min.y - 0.08f, 0f),
            new Vector3(b.center.x, b.min.y - 0.14f, 0f),
            new Vector3(b.min.x + 0.03f, b.min.y - 0.14f, 0f),
            new Vector3(b.max.x - 0.03f, b.min.y - 0.14f, 0f),
        };
    }

    bool HasLethalSpikeInTilemapSamples(Tilemap tilemap, Vector3[] samples)
    {
        bool sawFalse = false;
        for (int i = 0; i < samples.Length; i++)
        {
            if (IsLethalSpikeAroundSample(tilemap, samples[i], ref sawFalse)) return true;
        }

        if (sawFalse) return false;
        return false;
    }

    bool IsLethalSpikeAroundSample(Tilemap tilemap, Vector3 sample, ref bool sawFalse)
    {
        const float d = 0.03f;
        Vector3[] offsets = new Vector3[]
        {
            Vector3.zero, new Vector3(d, 0f, 0f), new Vector3(-d, 0f, 0f),
            new Vector3(0f, d, 0f), new Vector3(0f, -d, 0f), new Vector3(d, d, 0f),
            new Vector3(-d, d, 0f), new Vector3(d, -d, 0f), new Vector3(-d, -d, 0f),
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3Int cell = tilemap.WorldToCell(sample + offsets[i]);
            TileBase tile = tilemap.GetTile(cell);
            if (tile == null) continue;

            string tileName = tile.name != null ? tile.name.ToLowerInvariant() : string.Empty;
            if (tileName.Contains("_false"))
            {
                sawFalse = true;
                continue;
            }

            if (tileName.Contains("spike") || tileName.Contains("pua")) return true;
            for (int k = 0; k < LethalFangsTiles.Length; k++)
            {
                if (tileName.Contains(LethalFangsTiles[k])) return true;
            }
        }

        return false;
    }

    void CheckLethalSpikeByFeetTileLookup()
    {
        if (_isRespawning) return;
        if (_groundTilemaps == null || _groundTilemaps.Length == 0) return;

        Vector3[] samples = BuildFeetSamples(col.bounds);

        for (int i = 0; i < _groundTilemaps.Length; i++)
        {
            Tilemap tm = _groundTilemaps[i];
            if (tm == null) continue;
            if (HasLethalSpikeInTilemapSamples(tm, samples))
            {
                HandleDeathBySpike();
                return;
            }
        }
    }

    void CheckLethalSpikeUnderFeet()
    {
        if (_isRespawning) return;

        Bounds b = col.bounds;
        Vector2 feetCenter = new Vector2(b.center.x, b.min.y + 0.02f);
        Vector2 feetSize = new Vector2(Mathf.Max(0.05f, b.size.x * 0.9f), 0.06f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(feetCenter, feetSize, 0f, groundLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null) continue;
            if (!IsLethalSpike(hit, null)) continue;

            HandleDeathBySpike();
            return;
        }
    }

    bool HasFalseTag(Transform t) => t != null && t.CompareTag("_FALSE");

    bool HasSpikeKeyword(Transform t)
    {
        if (t == null) return false;
        string name = t.name.ToLowerInvariant();
        return name.Contains("spike") || name.Contains("pua");
    }

    public void SetCheckpoint(Vector2 newCheckpointPosition)
    {
        _currentCheckpoint = newCheckpointPosition;
        Debug.Log($"Player {playerIndex} actualizó su checkpoint a: {_currentCheckpoint}");
    }

    void HandleDeathBySpike()
    {
        _isRespawning = true;
        StopFootstepAudio();
        HideEffectStatus();
        PlayDeathAudio();

        if (GameDataManager.Instance != null)
        {
            var stats = playerIndex == 1 ? GameDataManager.Instance.p1Stats : GameDataManager.Instance.p2Stats;
            if (stats != null)
            {
                stats.deathCounter += 1;
                if (stats.lives > 0) stats.lives -= 1;
            }
        }

        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        yield return null;

        rb.position = _currentCheckpoint;
        rb.linearVelocity = Vector2.zero;

        coyoteCounter = coyoteTime;
        edgeSupportTimer = coyoteTime;
        jumpBufferCounter = 0f;

        _isRespawning = false;
    }

    void SetupAudioSources()
    {
        _footstepSource = gameObject.AddComponent<AudioSource>();
        _footstepSource.playOnAwake = false;
        _footstepSource.loop = true;
        _footstepSource.spatialBlend = 0f;
        _footstepSource.volume = 0.55f * SettingsManager.CurrentSfxVolume;

        _oneShotSource = gameObject.AddComponent<AudioSource>();
        _oneShotSource.playOnAwake = false;
        _oneShotSource.loop = false;
        _oneShotSource.spatialBlend = 0f;
        _oneShotSource.volume = 0.75f * SettingsManager.CurrentSfxVolume;

        RefreshFootstepClip();
    }

    void RefreshFootstepClip()
    {
        if (_footstepSource == null || SceneAudioController.Instance == null) return;

        AudioClip clip = SceneAudioController.Instance.GetFootstepClip();
        if (_footstepSource.clip == clip) return;

        bool wasPlaying = _footstepSource.isPlaying;
        _footstepSource.Stop();
        _footstepSource.clip = clip;

        if (wasPlaying && clip != null)
            _footstepSource.Play();
    }

    void UpdateFootstepAudio()
    {
        if (_footstepSource == null) return;
        if (_footstepSource.clip == null) RefreshFootstepClip();

        bool shouldPlay = !_isRespawning
            && !_isDashing
            && isGrounded
            && Mathf.Abs(rb.linearVelocity.x) > 0.4f
            && _footstepSource.clip != null;

        if (!shouldPlay)
        {
            StopFootstepAudio();
            return;
        }

        _footstepSource.volume = 0.55f * SettingsManager.CurrentSfxVolume;
        _footstepSource.pitch = Mathf.Clamp(0.9f + Mathf.Abs(rb.linearVelocity.x) / Mathf.Max(runSpeed, 0.1f) * 0.15f, 0.9f, 1.2f);
        if (!_footstepSource.isPlaying)
            _footstepSource.Play();
    }

    void StopFootstepAudio()
    {
        if (_footstepSource != null && _footstepSource.isPlaying)
            _footstepSource.Stop();
    }

    void PlayJumpAudio()
    {
        if (_oneShotSource == null || SceneAudioController.Instance == null) return;
        _oneShotSource.volume = 0.75f * SettingsManager.CurrentSfxVolume;
        SceneAudioController.Instance.PlayJump(_oneShotSource);
    }

    public void PlayPickupAudio()
    {
        if (_oneShotSource == null || SceneAudioController.Instance == null) return;
        _oneShotSource.volume = Mathf.Clamp01(0.85f * 1.5f * SettingsManager.CurrentSfxVolume);
        SceneAudioController.Instance.PlayPickup(_oneShotSource);
    }

    void PlayDeathAudio()
    {
        if (_oneShotSource == null || SceneAudioController.Instance == null) return;
        _oneShotSource.volume = Mathf.Clamp01(0.95f * SettingsManager.CurrentSfxVolume);
        SceneAudioController.Instance.PlayDeath(_oneShotSource);
    }

    void ResolveEffectStatusText()
    {
        if (_effectStatusText != null)
        {
            EnsureCanvasCamera(_effectStatusText.canvas, ResolvePlayerCamera());
            return;
        }

        Canvas effectCanvas = ResolveEffectCanvas();
        if (effectCanvas == null) return;

        TextMeshProUGUI[] texts = effectCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name == "EffectStatusText")
            {
                _effectStatusText = texts[i];
                break;
            }
        }

        if (_effectStatusText == null)
            _effectStatusText = CreateEffectStatusText(effectCanvas);
    }

    Canvas ResolveEffectCanvas()
    {
        if (blindOverlay != null)
        {
            Canvas blindCanvas = blindOverlay.GetComponentInParent<Canvas>(true);
            if (blindCanvas != null)
            {
                EnsureCanvasCamera(blindCanvas, ResolvePlayerCamera());
                return blindCanvas;
            }
        }

        Camera playerCamera = ResolvePlayerCamera();
        if (playerCamera == null) return null;

        Canvas existingCanvas = playerCamera.GetComponentInChildren<Canvas>(true);
        if (existingCanvas != null)
        {
            EnsureCanvasCamera(existingCanvas, playerCamera);
            return existingCanvas;
        }

        GameObject canvasGO = new GameObject($"EffectCanvas_P{playerIndex}", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.layer = playerCamera.gameObject.layer;
        canvasGO.transform.SetParent(playerCamera.transform, false);

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = playerCamera;
        canvas.planeDistance = 1f;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    Camera ResolvePlayerCamera()
    {
        var follows = Object.FindObjectsByType<CameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < follows.Length; i++)
        {
            CameraFollow follow = follows[i];
            if (follow == null || follow.target != transform) continue;

            Camera playerCamera = follow.GetComponent<Camera>();
            if (playerCamera != null) return playerCamera;
        }

        return null;
    }

    TextMeshProUGUI CreateEffectStatusText(Canvas canvas)
    {
        GameObject go = new GameObject("EffectStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.layer = canvas.gameObject.layer;
        go.transform.SetParent(canvas.transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(18f, -18f);
        rect.sizeDelta = new Vector2(520f, 80f);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.fontSize = 17f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;

        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSharedMaterial = TMP_Settings.defaultFontAsset.material;
        }

        go.SetActive(false);
        EnsureCanvasCamera(canvas, ResolvePlayerCamera());
        return text;
    }

    void ShowEffectStatus(string message, float duration)
    {
        ResolveEffectStatusText();
        if (_effectStatusText == null) return;

        if (_effectStatusCoroutine != null)
            StopCoroutine(_effectStatusCoroutine);

        _effectStatusText.text = message;
        _effectStatusText.gameObject.SetActive(true);
        _effectStatusCoroutine = StartCoroutine(HideEffectStatusRoutine(duration));
    }

    IEnumerator HideEffectStatusRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        HideEffectStatus();
    }

    void HideEffectStatus()
    {
        if (_effectStatusText != null)
            _effectStatusText.gameObject.SetActive(false);

        _effectStatusCoroutine = null;
    }

    void EnsureCanvasCamera(Canvas canvas, Camera worldCamera)
    {
        if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceCamera || worldCamera == null) return;

        if (canvas.worldCamera != worldCamera)
            canvas.worldCamera = worldCamera;
    }
}
