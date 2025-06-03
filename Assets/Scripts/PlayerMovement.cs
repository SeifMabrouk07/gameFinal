using System;
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

    [Header("Health Settings")]
    [Tooltip("Max hit points of the player")]
    public int maxHealth = 3;

    // Public event: subscribers can listen to health changes.
    // int parameters: (currentHP, maxHP)
    public event Action<int, int> OnHealthChanged;

    Rigidbody2D rb;
    Animator anim;
    Collider2D bodyCollider;

    // Movement / jump
    float horizontalInput;
    bool jumpRequested;
    bool isGrounded;
    bool facingRight = true;

    // Combo‐attack state
    Queue<string> attackQueue = new Queue<string>();
    Coroutine comboRoutine;
    [HideInInspector] public bool isAttacking = false;
    int nextComboStep;
    float lastAttackTime;

    // Health state
    int currentHealth;
    bool isDead = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        bodyCollider = GetComponent<Collider2D>();

        // Auto‐create a groundCheck child at feet if none assigned
        if (groundCheck == null)
        {
            Collider2D col = GetComponent<Collider2D>();
            float yOff = -col.bounds.extents.y - 0.05f;
            var go = new GameObject("GroundCheck_Auto");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, yOff, 0);
            groundCheck = go.transform;
        }

        // Auto‐create an attackPoint child if none assigned
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

        // Initialize combo state
        nextComboStep = 0;
        lastAttackTime = -10f;

        // Initialize health
        currentHealth = maxHealth;
        // Immediately fire the event so UI can show starting hearts
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Update()
    {
        if (isDead)
            return;

        // 1) Ground check each frame
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
        anim.SetBool("grounded", isGrounded);

        // 2) Read horizontal input (only if not mid‐attack)
        horizontalInput = isAttacking ? 0f : Input.GetAxisRaw("Horizontal");

        // 3) Jump input (only if grounded & not attacking)
        if (Input.GetButtonDown("Jump") && isGrounded && !isAttacking)
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

        // 4) Attack input (anytime)
        if (Input.GetButtonDown("Fire1") && !isDead)
        {
            // Reset combo if too slow
            if (Time.time - lastAttackTime > comboResetTime)
                nextComboStep = 0;

            // Enqueue next attack trigger
            attackQueue.Enqueue(attackTriggers[nextComboStep]);
            lastAttackTime = Time.time;
            nextComboStep = (nextComboStep + 1) % attackTriggers.Length;

            // If not already attacking, start processing
            if (!isAttacking)
                comboRoutine = StartCoroutine(ProcessAttackQueue());
        }

        // 5) Run animation: only when grounded, moving, and not attacking
        bool running = isGrounded && Mathf.Abs(horizontalInput) > 0.01f && !isAttacking;
        anim.SetBool("Run", running);

        // 6) Flip sprite if needed (only if not attacking)
        if (!isAttacking)
        {
            if (horizontalInput > 0.01f && !facingRight) Flip();
            if (horizontalInput < -0.01f && facingRight) Flip();
        }
    }

    void FixedUpdate()
    {
        if (isDead)
            return;

        // 1) Apply jump
        if (jumpRequested)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpVelocity);
            jumpRequested = false;
        }

        // 2) Horizontal movement (zero if attacking)
        float vx = isAttacking ? 0f : horizontalInput * moveSpeed;
        rb.velocity = new Vector2(vx, rb.velocity.y);
    }

    IEnumerator ProcessAttackQueue()
    {
        isAttacking = true;

        while (attackQueue.Count > 0)
        {
            string trig = attackQueue.Dequeue();
            int hash = Animator.StringToHash(trig);

            // 1) Cross‐fade into the attack animation
            anim.CrossFadeInFixedTime(hash, transitionDuration, 0, 0f);

            // 2) Wait one frame for Animator to switch states
            yield return null;

            // 3) Grab the current clip length from Animator
            AnimatorClipInfo[] clips = anim.GetCurrentAnimatorClipInfo(0);
            float clipLength = 0.5f; // fallback length
            if (clips.Length > 0)
                clipLength = clips[0].clip.length;

            // 4) Wait until "hit frame" (fraction of clip)
            float hitDelay = clipLength * hitTimeFraction;
            yield return new WaitForSeconds(hitDelay);

            // 5) Deal damage to all enemies in range
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                attackPoint.position,
                attackRange,
                enemyLayer
            );
            foreach (var hit in hits)
            {
                SlimeController sc = hit.GetComponent<SlimeController>();
                if (sc != null)
                {
                    sc.TakeDamage(damageToEnemy);
                }
            }

            // 6) Wait the remainder of the clip
            yield return new WaitForSeconds(clipLength - hitDelay);
        }

        // 7) Reset combo state so player can move again
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

    #region Damage & Death

    // Call this when the slime hits the player, or from other damage sources
    public void TakeDamage(int amount)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Max(currentHealth - amount, 0);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Optionally play a “Hurt” animation:
            // anim.SetTrigger("Hurt");
        }
    }

    void Die()
    {
        isDead = true;
        anim.SetBool("Run", false);
        anim.SetTrigger("Die"); // matches your “Die” parameter

        // Disable collider so no further hits can register
        bodyCollider.enabled = false;

        // Stop all movement/combos
        StopAllCoroutines();
        isAttacking = false;

        // Destroy after Die animation finishes
        AnimatorClipInfo[] clips = anim.GetCurrentAnimatorClipInfo(0);
        float dieLength = 0.5f;
        if (clips.Length > 0)
            dieLength = clips[0].clip.length;

        StartCoroutine(DestroyAfterDelay(dieLength));
    }

    IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    #endregion

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 s = transform.localScale;
        s.x *= -1f;
        transform.localScale = s;

        // If the attackPoint was auto‐created, keep it in front
        if (attackPoint.name.Contains("AttackPoint_Auto"))
        {
            Vector3 ap = attackPoint.localPosition;
            ap.x = -ap.x;
            attackPoint.localPosition = ap;
        }
    }

    void OnDrawGizmos()
    {
        // Draw ground‐check circle
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(
                groundCheck.position,
                groundCheckRadius
            );
        }
        // Draw player attack range circle
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(
                attackPoint.position,
                attackRange
            );
        }
    }
}