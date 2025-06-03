using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(Collider2D))]
public class SlimeController : MonoBehaviour
{
    [Header("Patrol Settings")]
    [Tooltip("Leftmost X coordinate the slime can patrol to")]
    public float leftX = -3f;
    [Tooltip("Rightmost X coordinate the slime can patrol to")]
    public float rightX = 3f;
    [Tooltip("Speed of patrol (and chase) in units/sec")]
    public float moveSpeed = 2f;

    [Header("Detection & Attack Settings")]
    [Tooltip("Distance at which the slime sees and stops patrolling")]
    public float detectRange = 3f;
    [Tooltip("Radius for melee hit detection")]
    public float attackRange = 0.5f;
    [Tooltip("Seconds between consecutive attacks")]
    public float attackCooldown = 1f;
    [Tooltip("Damage dealt to the player")]
    public int damageToPlayer = 1;
    [Tooltip("Which layer(s) count as the player")]
    public LayerMask playerLayer;

    [Header("Health Settings")]
    [Tooltip("Max hit points of the slime")]
    public int maxHealth = 1;

    [Header("Turn Delay (optional)")]
    [Tooltip("Idle time before flipping direction on patrol")]
    public float turnIdleTime = 0.25f;

    Rigidbody2D rb;
    Animator anim;
    Collider2D bodyCollider;

    int currentHealth;
    bool movingRight = true;
    bool isAttacking = false;
    float lastAttackTime = -999f;
    bool isTurning = false;

    Transform playerTransform;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        bodyCollider = GetComponent<Collider2D>();
        currentHealth = maxHealth;

        // Drive movement by code
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Find the player by tag; ensure your Player GameObject is tagged "Player"
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            playerTransform = playerGO.transform;

        // Start in Idle (Run = false)
        anim.SetBool("Run", false);
    }

    void Update()
    {
        if (currentHealth <= 0)
            return;

        // If attacking or turning, skip detection/chase
        if (isAttacking || isTurning)
            return;

        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);

            if (dist <= detectRange)
            {
                // Face the player
                if (playerTransform.position.x > transform.position.x && !movingRight)
                    Flip();
                else if (playerTransform.position.x < transform.position.x && movingRight)
                    Flip();

                // Check if player inside attackRange via OverlapCircle
                Collider2D[] hits = Physics2D.OverlapCircleAll(
                    transform.position + Vector3.up * 0.2f,
                    attackRange,
                    playerLayer
                );

                if (hits.Length > 0 && Time.time - lastAttackTime >= attackCooldown)
                {
                    // Player in melee range: attack
                    StartCoroutine(PerformAttack());
                    return;
                }
                else if (hits.Length > 0)
                {
                    // Player is inside attackRange but still on cooldown: stay Idle
                    anim.SetBool("Run", false);
                    return;
                }
                else
                {
                    // Player in detectRange but outside attackRange: chase
                    anim.SetBool("Run", true);
                    ChasePlayer();
                    return;
                }
            }
        }

        // Outside detectRange: patrol
        Patrol();
    }

    #region Patrol & Chase Logic

    void Patrol()
    {
        anim.SetBool("Run", true);

        float step = moveSpeed * Time.deltaTime;
        Vector3 pos = transform.position;

        if (movingRight)
        {
            pos.x += step;
            if (pos.x >= rightX)
            {
                pos.x = rightX;
                StartCoroutine(DoTurn());
            }
        }
        else
        {
            pos.x -= step;
            if (pos.x <= leftX)
            {
                pos.x = leftX;
                StartCoroutine(DoTurn());
            }
        }

        transform.position = pos;
    }

    void ChasePlayer()
    {
        float step = moveSpeed * Time.deltaTime;
        Vector3 pos = transform.position;

        if (playerTransform.position.x > transform.position.x)
            pos.x += step;
        else
            pos.x -= step;

        transform.position = pos;
    }

    IEnumerator DoTurn()
    {
        isTurning = true;
        anim.SetBool("Run", false);
        yield return new WaitForSeconds(turnIdleTime);
        movingRight = !movingRight;
        Flip();
        isTurning = false;
    }

    #endregion

    #region Attack Logic

    IEnumerator PerformAttack()
    {
        isAttacking = true;
        anim.SetBool("Run", false);

        // Trigger the attack animation (Enemy Attack1 state)
        anim.SetTrigger("Attack");

        // Wait one frame for Animator to enter the attack state
        yield return null;

        // Get the current clip length
        AnimatorClipInfo[] clips = anim.GetCurrentAnimatorClipInfo(0);
        float clipLength = (clips.Length > 0) ? clips[0].clip.length : 0.5f;
        float hitDelay = clipLength * 0.3f;
        yield return new WaitForSeconds(hitDelay);

        // On the “hit frame,” do an OverlapCircle to damage the player if still in range
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position + Vector3.up * 0.2f,
            attackRange,
            playerLayer
        );
        foreach (var hit in hits)
        {
            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damageToPlayer);
            }
        }

        // Wait out the remainder of the attack clip
        yield return new WaitForSeconds(clipLength - hitDelay);

        lastAttackTime = Time.time;
        isAttacking = false;
        anim.SetBool("Run", false);
    }

    #endregion

    #region Damage & Death

    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0)
            return;

        currentHealth -= amount;
        if (currentHealth > 0)
        {
            anim.SetTrigger("Hit");
        }
        else
        {
            Die();
        }
    }

    void Die()
    {
        currentHealth = 0;
        anim.SetBool("Run", false);
        anim.SetTrigger("Death");

        bodyCollider.enabled = false;
        StopAllCoroutines();
        isAttacking = false;

        // Destroy after death animation finishes
        AnimatorClipInfo[] clips = anim.GetCurrentAnimatorClipInfo(0);
        float dieLength = (clips.Length > 0) ? clips[0].clip.length : 0.5f;
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
        movingRight = !movingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    void OnDrawGizmosSelected()
    {
        // Detect range (blue)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        // Attack range (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.2f, attackRange);

        // Patrol boundaries (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            new Vector3(leftX, transform.position.y, 0f),
            new Vector3(rightX, transform.position.y, 0f)
        );
    }
}