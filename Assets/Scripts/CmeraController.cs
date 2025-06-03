using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Tooltip("Drag your Player transform here in the Inspector")]
    public Transform player;
    [Tooltip("How far ahead of the player the camera looks based on movement direction")]
    public float lookAheadDistance = 2f;
    [Tooltip("Smoothing time for camera movement (seconds)")]
    public float smoothTime = 0.3f;
    [Tooltip("Vertical offset so the player is not centered")]
    public float verticalOffset = 1f;

    private float zOffset;
    private Rigidbody2D playerRb;

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("CameraController: Player Transform is not assigned!", this);
            enabled = false;
            return;
        }

        // Preserve the initial camera Z position
        zOffset = transform.position.z;

        // Cache the player's Rigidbody2D if available
        playerRb = player.GetComponent<Rigidbody2D>();
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Determine look-ahead offset based on player's velocity
        float xOffset = 0f;
        if (playerRb != null)
        {
            float vx = playerRb.velocity.x;
            if (Mathf.Abs(vx) > 0.1f)
                xOffset = lookAheadDistance * Mathf.Sign(vx);
        }

        // Calculate target positions with vertical offset
        float targetX = player.position.x + xOffset;
        float targetY = player.position.y + verticalOffset;

        // Smooth X and snap Y to target for clarity
        float t = (smoothTime > 0f) ? Time.deltaTime / smoothTime : 1f;
        float newX = Mathf.Lerp(transform.position.x, targetX, t);
        float newY = targetY;

        transform.position = new Vector3(newX, newY, zOffset);
    }

    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.cyan;
            float xOffset = 0f;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float vx = rb.velocity.x;
                if (Mathf.Abs(vx) > 0.1f)
                    xOffset = lookAheadDistance * Mathf.Sign(vx);
            }
            Vector3 aheadPoint = new Vector3(
                player.position.x + xOffset,
                player.position.y + verticalOffset,
                transform.position.z
            );
            Gizmos.DrawWireSphere(aheadPoint, 0.2f);
        }
    }
}
