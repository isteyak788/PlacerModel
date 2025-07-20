using UnityEngine;
using Pathfinding; // Important for A* Pathfinding Project components
using System.Collections.Generic; // Make sure this is included for List

public class AIWorkerBehavior : MonoBehaviour
{
    public Transform currentTarget; // The AI's current destination
    public float pickUpDistance = 1f; // How close AI needs to be to pick up
    public float dropOffDistance = 1f; // How close AI needs to be to drop off

    private RichAI richAI; // Changed from AIPath to RichAI
    private bool hasMaterial = false; // State variable: does the AI have material?

    // References to your scene objects (assign in Inspector)
    public Transform constructionMaterialArea;
    public Transform constructionSiteArea;

    void Start()
    {
        richAI = GetComponent<RichAI>(); // Get RichAI component
        if (richAI == null)
        {
            Debug.LogError("RichAI component not found on AIWorkerBehavior!", this);
            enabled = false; // Disable script if RichAI is missing
            return;
        }

        // Initially, the AI should go to the construction material area
        SetTarget(constructionMaterialArea);
    }

    void Update()
    {
        // If the AI has a target and is currently moving
        // RichAI uses canMove/canSearch directly, and pathPending might be a little different
        if (currentTarget != null && richAI.canMove && !richAI.pathPending)
        {
            // Check if the AI has reached its current target
            // RichAI has a 'reachedEndOfPath' property which is more robust for navmeshes
            if (richAI.reachedEndOfPath)
            {
                // Reached "Construction Material" area
                if (currentTarget == constructionMaterialArea && !hasMaterial)
                {
                    Debug.Log("AI reached material area. Picking up material.");
                    // Simulate picking up material
                    hasMaterial = true;
                    // Now go to the construction site
                    SetTarget(constructionSiteArea);
                }
                // Reached "Construction Site" area
                else if (currentTarget == constructionSiteArea && hasMaterial)
                {
                    Debug.Log("AI reached construction site. Dropping off material.");
                    // Simulate dropping off material
                    hasMaterial = false;
                    // Now go back to the construction material area
                    SetTarget(constructionMaterialArea);
                }
            }
        }
    }

    void SetTarget(Transform newTarget)
    {
        currentTarget = newTarget;
        if (richAI != null)
        {
            richAI.destination = newTarget.position;
            // RichAI typically starts pathfinding automatically when destination is set.
            // richAI.SearchPath(); // You usually don't need to call this manually with RichAI unless you disable auto-pathfinding.
            Debug.Log($"AI target set to: {newTarget.name}");
        }
    }

    // Optional: Visual Feedback (Gizmos)
    void OnDrawGizmos()
    {
        // Ensure richAI is initialized and has a path.
        // We now access the path via richAI.richPath.vectorPath.
        if (richAI != null && richAI.hasPath) //
        {
            Gizmos.color = Color.blue;
            Vector3 lastPosition = transform.position; // Start drawing from the agent's current position

            // Iterate through the path corners/waypoints that RichAI is currently following.
            // richPath.vectorPath contains the raw path nodes.
            // For RichAI, richPath.nextCorners also holds current relevant path segments,
            // but richPath.vectorPath is more comprehensive for full path visualization.
            // However, looking at the RichAI.cs file provided, it seems nextCorners is the more
            // directly relevant internal list used for current movement. Let's use that for more accurate visualization of what RichAI *is* currently doing.

            // The 'nextCorners' field is protected in RichAI, so you cannot access it directly from this script.
            // A more general way to get the *remaining* path points that the agent will traverse is using GetRemainingPath.

            List<Vector3> remainingPathPoints = new List<Vector3>();
            bool stalePath;
            richAI.GetRemainingPath(remainingPathPoints, out stalePath); //

            // If there's a path, draw it.
            if (remainingPathPoints.Count > 0)
            {
                // Draw a line from the current position to the first point
                Gizmos.DrawLine(lastPosition, remainingPathPoints[0]); //
                lastPosition = remainingPathPoints[0]; //

                // Draw lines between the rest of the points
                for (int i = 1; i < remainingPathPoints.Count; i++) //
                {
                    Gizmos.DrawLine(lastPosition, remainingPathPoints[i]); //
                    lastPosition = remainingPathPoints[i]; //
                }
            }
        }
    }
}
