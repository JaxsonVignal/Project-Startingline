
using UnityEngine;
using UnityEngine.AI;

public class Citizen : MonoBehaviour
{
    public Transform[] waypoints; // Array of waypoints for the NPC to follow
    public float waypointReachThreshold = 0.5f; // Distance to consider the waypoint reached
    private int currentWaypoint = 0; // Current waypoint index

    private NavMeshAgent agent; // Call Nav Mesh Agent component

    public string[] dialogLines; // Array of dialog lines the NPC can say
    public float playerDetectionRadius = 5f; // Radius to detect the player
    private bool hasSpokenToPlayer = false; // Prevents repeated dialog in one encounter

    void Start()
    {
        agent = GetComponent<NavMeshAgent>(); // Get the NavMeshAgent component
        if (waypoints.Length > 0) // Check if waypoints are assigned
            agent.SetDestination(waypoints[currentWaypoint].position);
    }

    void Update()
    {
        if (waypoints.Length == 0) return; // Exit if no waypoints are assigned

        if (!agent.pathPending && agent.remainingDistance < waypointReachThreshold) // Check if the agent has reached the waypoint
        {
            // Move to the next waypoint
            currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
            agent.SetDestination(waypoints[currentWaypoint].position);
        }
        else if (agent.remainingDistance < waypointReachThreshold) // If the agent is close to the waypoint
        {
            currentWaypoint = (currentWaypoint + 1) % waypoints.Length; // Move to the next waypoint
            agent.SetDestination(waypoints[currentWaypoint].position); // Set the new destination
        }

        DetectPlayerAndSpeak(); // Check for player detection and speak
    }

    void DetectPlayerAndSpeak()
    {
        // Find the player in the scene (assuming the player has a tag "Player")
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // Check the distance between the NPC and the player
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceToPlayer <= playerDetectionRadius && !hasSpokenToPlayer)
        {
            SayRandomDialog();
            hasSpokenToPlayer = true; // Prevent repeated dialog
        }
        else if (distanceToPlayer > playerDetectionRadius)
        {
            hasSpokenToPlayer = false; // Reset when the player moves away
        }
    }

    void SayRandomDialog()
    {
        if (dialogLines.Length == 0) return; // Check if there are dialog lines available

        // Pick a random dialog line
        int randomIndex = Random.Range(0, dialogLines.Length);
        string dialog = dialogLines[randomIndex];

        // Display the dialog in the console till we get dialog or use a UI system
        Debug.Log($"NPC says: {dialog}");
    }
}