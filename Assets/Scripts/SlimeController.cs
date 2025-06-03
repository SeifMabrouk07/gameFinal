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
    [Tooltip("Radius for melee hit detection (OverlapCircle)")]
    public float attackRange = 0.5f;
    [Tooltip("Seconds between consecutive attacks")]
    public float attackCooldown = 1f;
    [Tooltip("Damage dealt to player on contact")]
    public int damageToPlayer = 1;
    [Tooltip("Which layer(s) count as the player")]
    public LayerMask playerLayer;

    [Header("Health Settings")]
    [Tooltip("Max hit points of the slime")]
    public int maxHealth = 1;

    [Header("Turn Delay (optional)")]
    [Tooltip("Idle time before flipping direction on patrol")]
    public float turnIdleTime = 0.25f;

    // Components & state
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

        // Kinematic so we drive movement in code
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Attempt to find the player by tag (ensure your Player is tagged "Player")
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

        // If currently attacking or turning, skip detection/chase
        if (isAttacking || isTurning)
            return;

        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);

            // If inside detectRange, either chase or attempt attack
            if (dist <= detectRange)
            {
                Debug.Log("Slime: Player detected at distance " + dist.ToString("F2"));

                // Face toward player
                if (playerTransform.position.x > transform.position.x && !movingRight)
                    Flip();
                else if (playerTransform.position.x < transform.position.x && movingRight)
                    Flip();

                // First, check OverlapCircle for attackRange
                Collider2D[] hits = Physics2D.OverlapCircleAll(
                    transform.position + Vector3.up * 0.2f,
                    attackRange,
                    playerLayer
                );

                if (hits.Length > 0 && Time.time - lastAttackTime >= attackCooldown)
                {
                    // If the player’s Collider is in the attack circle and cooldown passed, attack:
                    Debug.Log("Slime: Player within attackRange, starting PerformAttack()");
                    StartCoroutine(PerformAttack());
                    return;
                }
                else if (hits.Length > 0)
                {
                    // Player is inside attackRange but cooldown not yet passed: stay Idle
                    anim.SetBool("Run", false);
                    return;
                }
                else
                {
                    // Player not yet inside attackRange (but within detectRange): chase
                    anim.SetBool("Run", true);
                    ChasePlayer();
                    return;
                }
            }
        }

        // Otherwise, player is outside detectRange (or not assigned): patrol
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
        // Move horizontally toward player's x‐position only
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

        // Pick one of "Attack", "Attack2", "Attack3" at random
        int r = Random.Range(1, 4);
        string triggerName = (r == 1) ? "Attack" : (r == 2) ? "Attack2" : "Attack3";
        Debug.Log("Slime: Triggering animation " + triggerName);
        anim.SetTrigger(triggerName);

        // Wait one frame so Animator actually enters that attack state
        yield return null;

        // Determine the length of the current animation clip
        AnimatorClipInfo[] clips = anim.GetCurrentAnimatorClipInfo(0);
        float clipLength = (clips.Length > 0) ? clips[0].clip.length : 0.5f;
        float hitDelay = clipLength * 0.3f;
        yield return new WaitForSeconds(hitDelay);

        // On the “hit frame,” do another OverlapCircle just in case
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position + Vector3.up * 0.2f,
            attackRange,
            playerLayer
        );
        if (hits.Length > 0)
            Debug.Log("Slime: Player hit detected in PerformAttack()");

        foreach (var hit in hits)
        {
            PlayerMovement pm = hit.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                Debug.Log("Slime: Calling Player.TakeDamage(" + damageToPlayer + ")");
                pm.TakeDamage(damageToPlayer);
            }
        }

        // Wait out the rest of the attack clip
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
            Debug.Log("Slime: Took damage, playing Hit");
            anim.SetTrigger("Hit");
        }
        else
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Slime: Dying");
        currentHealth = 0;
        anim.SetBool("Run", false);
        anim.SetTrigger("Death");

        bodyCollider.enabled = false;
        StopAllCoroutines();
        isAttacking = false;

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
        // Visualize detect range (blue)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        // Visualize attack range (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.2f, attackRange);

        // Visualize patrol boundaries (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            new Vector3(leftX, transform.position.y, 0f),
            new Vector3(rightX, transform.position.y, 0f)
        );
    }
}