using UnityEngine;

[RequireComponent(typeof(Animator), typeof(CivilianMovementController))]
public class PoliceManager : MonoBehaviour
{
    public enum PoliceState { Searching, Aggro }

    [Header("Police Info")]
    public string policeName = "Officer";
    public PoliceState currentState = PoliceState.Searching;

    [Header("Detection Settings")]
    [HideInInspector] public GameObject player;
    public float sightRange = 20f;
    public float loseAggroDistance = 30f;

    [Header("Spawn Point")]
    [HideInInspector] public Transform spawnPoint;

    [Header("Search Settings")]
    public float searchMoveInterval = 5f;
    public float searchRadius = 30f;
    private float searchTimer;

    [Header("Combat Settings")]
    public float attackRange = 10f;

    [Header("Wanted System Integration")]
    public bool removeFromWantedSystemOnDestroy = true;

    private Animator animator;
    private CivilianMovementController movement;
    private bool hasEnteredAggro;

    private void Start()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<CivilianMovementController>();

        // If spawn point is set, start there
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }

        // Auto-find player if not set
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        // Start searching immediately
        MoveToRandomPosition();
    }

    private void Update()
    {
        switch (currentState)
        {
            case PoliceState.Searching:
                HandleSearching();
                CheckForPlayer();
                break;

            case PoliceState.Aggro:
                HandleCombatBehavior();
                break;
        }
    }

    // SEARCHING LOGIC
    private void HandleSearching()
    {
        searchTimer += Time.deltaTime;

        // Move to a new random position periodically
        if (searchTimer >= searchMoveInterval)
        {
            searchTimer = 0f;
            MoveToRandomPosition();
        }
    }

    private void MoveToRandomPosition()
    {
        // Generate a random point on the NavMesh within searchRadius
        Vector3 randomDirection = Random.insideUnitSphere * searchRadius;
        randomDirection += transform.position;
        randomDirection.y = transform.position.y;

        // Try to find a valid NavMesh position
        if (UnityEngine.AI.NavMesh.SamplePosition(randomDirection, out UnityEngine.AI.NavMeshHit hit, searchRadius, UnityEngine.AI.NavMesh.AllAreas))
        {
            // Create a temporary transform target for movement
            GameObject tempTarget = new GameObject("TempTarget");
            tempTarget.transform.position = hit.position;
            movement.MoveTo(tempTarget.transform);
            Destroy(tempTarget, 0.1f);

            Debug.Log($"{policeName} moving to random position.");
        }
    }

    // PLAYER DETECTION
    private void CheckForPlayer()
    {
        if (!player) return;

        // Only detect player if they are wanted
        if (WantedSystem.Instance != null && !WantedSystem.Instance.isPlayerWanted)
        {
            return; // Don't aggro if player isn't wanted
        }

        float distance = Vector3.Distance(transform.position, player.transform.position);

        // Check if player is within sight range AND has line of sight
        if (distance <= sightRange && CanSeePlayer())
        {
            EnterAggroState();
        }
    }

    private bool CanSeePlayer()
    {
        if (!player) return false;

        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 playerPos = player.transform.position + Vector3.up * 1.5f;
        Vector3 directionToPlayer = (playerPos - origin).normalized;
        float distanceToPlayer = Vector3.Distance(origin, playerPos);

        // Draw debug ray to see what we're checking
        Debug.DrawRay(origin, directionToPlayer * distanceToPlayer, Color.red, 0.1f);

        // Check if there's line of sight to player
        if (Physics.Raycast(origin, directionToPlayer, out RaycastHit hit, distanceToPlayer))
        {
            Debug.Log($"{policeName} raycast hit: {hit.transform.name} at distance {hit.distance}");

            // Check if we hit the player or a child of the player
            if (hit.transform == player.transform || hit.transform.IsChildOf(player.transform))
            {
                Debug.Log($"{policeName} CAN SEE PLAYER!");
                return true;
            }

            // Hit something else
            Debug.Log($"{policeName} LOS blocked by {hit.transform.name}");
            return false;
        }

        // If raycast didn't hit anything within range, player is visible
        Debug.Log($"{policeName} No obstruction - player visible!");
        return true;
    }

    private void EnterAggroState()
    {
        if (currentState != PoliceState.Aggro)
        {
            currentState = PoliceState.Aggro;
            hasEnteredAggro = true;
            Debug.Log($"{policeName} spotted the player! Entering AGGRO state!");

            // Ensure player becomes wanted when spotted
            if (WantedSystem.Instance != null)
            {
                WantedSystem.Instance.SetWanted(true);
            }
        }
    }

    // COMBAT LOGIC
    private void HandleCombatBehavior()
    {
        if (!player) return;

        // If player is no longer wanted, stop chasing
        if (WantedSystem.Instance != null && !WantedSystem.Instance.isPlayerWanted)
        {
            hasEnteredAggro = false;
            currentState = PoliceState.Searching;
            movement.StopMovement();
            return;
        }

        float distance = Vector3.Distance(transform.position, player.transform.position);
        bool canSeePlayer = CanSeePlayer();

        // Player escaped too far - return to searching
        if (distance > loseAggroDistance)
        {
            Debug.Log($"{policeName} lost player, returning to search.");
            hasEnteredAggro = false;
            currentState = PoliceState.Searching;
            movement.StopMovement();
            searchTimer = searchMoveInterval; // Trigger immediate new search
            return;
        }

        // Within attack range and can see player - stop and face player
        if (canSeePlayer && distance <= attackRange)
        {
            movement.StopMovement();
            FaceTarget(player.transform.position);

            // Trigger attack animation
            animator.SetTrigger("Attack");
            Debug.Log($"{policeName} ATTACKING player!");
            return;
        }

        // Chase player
        movement.MoveTo(player.transform, true);
        FaceTarget(player.transform.position);
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 5f);
        }
    }

    // Optional: Allow entering aggro from damage
    public void OnDamaged()
    {
        if (!hasEnteredAggro)
        {
            EnterAggroState();
        }
    }

    // Clean up when destroyed
    private void OnDestroy()
    {
        if (removeFromWantedSystemOnDestroy && WantedSystem.Instance != null)
        {
            WantedSystem.Instance.RemovePolice(gameObject);
        }
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        // Draw sight range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // Draw lose aggro range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseAggroDistance);

        // Draw search radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
}