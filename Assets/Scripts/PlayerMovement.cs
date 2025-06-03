using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Horizontal run speed")]
    public float moveSpeed = 10f;
    [Tooltip("Vertical velocity applied on jump")]
    public float jumpVelocity = 18f;

    [Header("Ground Detection")]
    [Tooltip("Child transform at feet (auto‐created if null)")]
    public Transform groundCheck;
    [Tooltip("Radius of feet‐check circle")]
    public float groundCheckRadius = 0.2f;
    [Tooltip("Which layers count as ground")]
    public LayerMask groundLayer;

    [Header("Combat Settings")]
    [Tooltip("Attack animation triggers (must match Animator state names)")]
    public string[] attackTriggers = { "Attack1", "Attack2", "Attack3" };
    [Tooltip("Seconds before combo resets if you wait too long")]
    [Range(0f, 1f)] public float comboResetTime = 0.7f;
    [Tooltip("Blend duration between animations in seconds")]
    public float transitionDuration = 0.05f;

    [Tooltip("Empty child used as the origin point for melee damage")]
    public Transform attackPoint;
    [Tooltip("Radius around attackPoint to detect enemies")]
    public float attackRange = 0.5f;
    [Tooltip("Which layers count as enemies")]
    public LayerMask enemyLayer;
    [Tooltip("Damage dealt to each enemy hit")]
    public int damageToEnemy = 1;
    [Tooltip("Fraction of the clip at which the damage should register (0–1)")]
    [Range(0f, 1f)]
    public float hitTimeFraction = 0.3f;

    Rigidbody2D rb;
    Animator anim;

    // Movement / jump
    float rawHorizontalInput;      // from keyboard/joystick
    float horizontalInput;         // final value (overridden by UI holds)
    bool jumpRequested;
    bool isGrounded;
    bool facingRight = true;

    // Combo‐attack state
    Queue<string> attackQueue = new Queue<string>();
    Coroutine comboRoutine;
    public bool isAttacking = false;
    int nextComboStep = 0;
    float lastAttackTime = -10f;

    // Mobile touch flags
    bool _holdingLeft = false;
    bool _holdingRight = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        // Auto‐create groundCheck if null
        if (groundCheck == null)
        {
            Collider2D col = GetComponent<Collider2D>();
            float yOff = -col.bounds.extents.y - 0.05f;
            var go = new GameObject("GroundCheck_Auto");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, yOff, 0);
            groundCheck = go.transform;
        }

        // Auto‐create attackPoint if null
        if (attackPoint == null)
        {
            var go = new GameObject("AttackPoint_Auto");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0.5f, 0f, 0f);
            attackPoint = go.transform;
        }

        // Configure Rigidbody2D
        rb.bodyType = RigidbodyType2D.Dynamic;
        if (rb.gravityScale <= 0f) rb.gravityScale = 1f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        // 1) Ground check
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
        anim.SetBool("grounded", isGrounded);

        // 2) Read raw horizontal input (keyboard/joystick)
        rawHorizontalInput = Input.GetAxisRaw("Horizontal");

        // 3) Determine final horizontalInput:
        //    - If holding left/right button, override rawInput
        //    - Else, use rawInput
        if (_holdingLeft)        horizontalInput = -1f;
        else if (_holdingRight)  horizontalInput =  1f;
        else                     horizontalInput =  rawHorizontalInput;

        // 4) Run animation: only if grounded, moving, and not attacking
        bool running = isGrounded && Mathf.Abs(horizontalInput) > 0.01f && !isAttacking;
        anim.SetBool("Run", running);

        // 5) Flip sprite if moving left/right and not attacking
        if (!isAttacking)
        {
            if (horizontalInput > 0.01f && !facingRight) Flip();
            else if (horizontalInput < -0.01f && facingRight) Flip();
        }

        // 6) Handle keyboard jump
        if (Input.GetButtonDown("Jump") && isGrounded && !isAttacking)
        {
            Jump();
        }

        // (KEYBOARD ATTACK REMOVED HERE—only mobile button will call EnqueueAttack())
    }

    void FixedUpdate()
    {
        // 1) If jump was requested
        if (jumpRequested)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpVelocity);
            jumpRequested = false;
        }

        // 2) Horizontal movement (zero if attacking)
        float vx = isAttacking ? 0f : horizontalInput * moveSpeed;
        rb.velocity = new Vector2(vx, rb.velocity.y);
    }

    // Public method to call from “Jump” button’s OnClick
    public void Jump()
    {
        if (isGrounded && !isAttacking)
        {
            anim.CrossFadeInFixedTime(
                Animator.StringToHash("Jump"),
                transitionDuration, 0, 0f
            );
            jumpRequested = true;
            isGrounded = false;
            anim.SetBool("grounded", false);
            AbortCombo();
        }
    }

    // Public method for Attack button’s OnClick
    public void EnqueueAttack()
    {
        if (isAttacking) return;

        // Reset combo if too slow
        if (Time.time - lastAttackTime > comboResetTime)
            nextComboStep = 0;

        attackQueue.Enqueue(attackTriggers[nextComboStep]);
        lastAttackTime = Time.time;
        nextComboStep = (nextComboStep + 1) % attackTriggers.Length;

        if (!isAttacking)
            comboRoutine = StartCoroutine(ProcessAttackQueue());
    }

    IEnumerator ProcessAttackQueue()
    {
        isAttacking = true;

        while (attackQueue.Count > 0)
        {
            string trig = attackQueue.Dequeue();
            int hash = Animator.StringToHash(trig);

            // Cross-fade into attack animation
            anim.CrossFadeInFixedTime(hash, transitionDuration, 0, 0f);

            // Wait one frame so Animator enters that state
            yield return null;

            // Grab the current clip length
            AnimatorClipInfo[] clips = anim.GetCurrentAnimatorClipInfo(0);
            float clipLength = (clips.Length > 0) ? clips[0].clip.length : 0.5f;
            float hitDelay = clipLength * hitTimeFraction;
            yield return new WaitForSeconds(hitDelay);

            // Damage all enemies in range
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                attackPoint.position,
                attackRange,
                enemyLayer
            );
            foreach (var h in hits)
            {
                SlimeController sc = h.GetComponent<SlimeController>();
                if (sc != null)
                    sc.TakeDamage(damageToEnemy);
            }

            // Wait remainder of animation
            yield return new WaitForSeconds(clipLength - hitDelay);
        }

        isAttacking = false;
        comboRoutine = null;
        attackQueue.Clear();
        nextComboStep = 0;
    }

    void AbortCombo()
    {
        if (comboRoutine != null)
            StopCoroutine(comboRoutine);

        isAttacking = false;
        comboRoutine = null;
        attackQueue.Clear();
        nextComboStep = 0;
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 s = transform.localScale;
        s.x *= -1f;
        transform.localScale = s;
    }

    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }

    // These will be wired to the Left/Right buttons via an EventTrigger component
    public void OnLeftButtonDown()  { _holdingLeft = true;  }
    public void OnLeftButtonUp()    { _holdingLeft = false; }

    public void OnRightButtonDown() { _holdingRight = true; }
    public void OnRightButtonUp()   { _holdingRight = false; }
}