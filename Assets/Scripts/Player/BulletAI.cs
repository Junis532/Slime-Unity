using UnityEngine;
using DG.Tweening;
using System.Collections; // Make sure this is included for Coroutines

public class BulletAI : MonoBehaviour
{
    public float moveSpeed = 20f;
    public float followDuration = 0.3f; // Duration for the "preparation" state before firing

    private Transform target;
    // private Transform player; // No longer needed for continuous position update in Update()
    private bool isFollowingPlayer = true; // Represents the "preparing to fire" state
    private Coroutine moveCoroutine;
    private Collider2D myCollider;
    // private Vector3 spawnOffset = Vector3.zero; // No longer used for position calculations here
    private bool isDestroying = false;

    // New initialization method called by BulletSpawner
    // This sets the arrow's initial position and rotation when it's spawned/enabled.
    public void InitializeBullet(Vector3 startPosition, float startAngle)
    {
        transform.position = startPosition; // Set the exact starting position
        transform.rotation = Quaternion.Euler(0, 0, startAngle); // Set the initial facing direction
        isFollowingPlayer = true; // Ensure it starts in the "preparing" state
    }

    // This method is still useful for updating rotation during the "preparation" phase,
    // as the BulletSpawner continues to sync direction.
    public void SyncSetRotation(float angle)
    {
        if (isFollowingPlayer) // Only update rotation if still in the preparation phase
            transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
    }

    void OnEnable()
    {
        transform.DOKill(); // Clear any previous DOTween animations
        isDestroying = false;
        CancelInvoke(); // Clear any previous Invoke calls

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine); // Stop any previous movement coroutine
            moveCoroutine = null;
        }

        // isFollowingPlayer will be set by InitializeBullet, but we default it here
        // in case OnEnable is called without InitializeBullet immediately following (e.g., from pool reset)
        isFollowingPlayer = true;
        target = null;
        // player = GameObject.FindGameObjectWithTag("Player")?.transform; // No longer used in Update()

        if (myCollider != null)
            myCollider.enabled = false; // Disable collider during preparation

        transform.localScale = Vector3.zero; // Start small for scaling animation

        // Auto-destroy after 10 seconds if it doesn't hit anything
        Invoke(nameof(DestroySelf), 10f);

        // Scale up animation
        transform.DOScale(0.5f, 0.3f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            if (myCollider != null)
                myCollider.enabled = true; // Enable collider once scaled up

            // After the scale-up animation, transition to actively seeking an enemy
            Invoke(nameof(SwitchToEnemy), followDuration);
        });
    }

    void Update()
    {
        // ***IMPORTANT CHANGE: Removed the continuous position update from here.***
        // The BulletSpawner is now responsible for tracking the arrow's position
        // during the 'isFollowingPlayer' (preparation) phase via SyncBowAndArrowToPlayer().
        //
        // if (isFollowingPlayer && player != null)
        // {
        //     transform.position = player.position + spawnOffset;
        // }
    }

    // Changes the bullet's state from "preparing" to "moving towards enemy"
    void SwitchToEnemy()
    {
        isFollowingPlayer = false; // Arrow is no longer just following the player
        FindClosestTarget(); // Find the target enemy

        if (target != null)
        {
            moveCoroutine = StartCoroutine(MoveTowardsTarget()); // Start moving towards the target
        }
        else
        {
            DestroySelf(); // If no target, destroy itself
        }
    }

    // Coroutine to move the bullet towards the target enemy
    System.Collections.IEnumerator MoveTowardsTarget()
    {
        while (target != null && target.gameObject.activeInHierarchy && !isDestroying)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle); // Continuously adjust direction
            transform.position += direction * moveSpeed * Time.deltaTime; // Move towards target
            yield return null; // Wait for the next frame
        }

        DestroySelf(); // Destroy when target is lost or destroyed
    }

    // Finds the closest enemy to the current bullet's position
    void FindClosestTarget()
    {
        string[] enemyTags = { "Enemy", "DashEnemy", "LongRangeEnemy", "PotionEnemy" };
        float closestDist = Mathf.Infinity;
        Transform closest = null;

        foreach (string tag in enemyTags)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject enemy in enemies)
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = enemy.transform;
                }
            }
        }

        target = closest;
    }

    // Handles collision with other 2D colliders
    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroying) return; // Prevent multiple destruction calls

        // Check if the collided object is an enemy
        if (other.CompareTag("Enemy") || other.CompareTag("DashEnemy") ||
            other.CompareTag("LongRangeEnemy") || other.CompareTag("PotionEnemy"))
        {
            EnemyHP hp = other.GetComponent<EnemyHP>();
            if (hp != null)
                hp.TakeDamage(); // Apply damage to the enemy

            DestroySelf(); // Destroy the bullet after hitting an enemy
        }
    }

    // Method to return the bullet to the object pool
    void DestroySelf()
    {
        if (isDestroying) return; // Prevent multiple destruction calls
        isDestroying = true;

        CancelInvoke(); // Cancel all Invoke calls
        transform.DOKill(); // Stop all DOTween animations

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine); // Stop the movement coroutine
            moveCoroutine = null;
        }

        GameManager.Instance.poolManager.ReturnToPool(gameObject); // Return to pool
    }
}