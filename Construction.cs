using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Required for OrderBy

public class Construction : MonoBehaviour
{
    [Header("Activation Settings")]
    [Tooltip("The parent object containing all the construction parts.")]
    public Transform parentObject;
    [Tooltip("Time in seconds it takes to activate all children.")]
    public float totalActivationTime = 5f;

    [Header("Slide Animation Settings")]
    [Tooltip("Offset applied to the starting position for the slide animation.")]
    public Vector3 slideStartOffset = new Vector3(0, 5, 0);
    [Tooltip("Duration of the slide animation for each part.")]
    public float slideAnimationDuration = 0.5f;
    [Tooltip("Animation curve for the slide movement.")]
    public AnimationCurve slideAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private List<Transform> childrenToActivate = new List<Transform>();
    private float timePerChild;
    private int activatedCount = 0;

    void Start()
    {
        if (parentObject == null)
        {
            Debug.LogError("Construction Script: Parent Object is not assigned. Please assign the parent object in the Inspector.", this);
            return;
        }

        // Deactivate all children initially and store their original positions
        foreach (Transform child in parentObject)
        {
            child.gameObject.SetActive(false);
            childrenToActivate.Add(child);
        }

        // Sort children by their Y-axis position (lowest first)
        childrenToActivate = childrenToActivate.OrderBy(child => child.position.y).ToList();

        if (childrenToActivate.Count == 0)
        {
            Debug.LogWarning("Construction Script: No children found under the parent object.", this);
            return;
        }

        timePerChild = totalActivationTime / childrenToActivate.Count;

        StartCoroutine(ActivateChildrenGradually());
    }

    IEnumerator ActivateChildrenGradually()
    {
        foreach (Transform child in childrenToActivate)
        {
            Vector3 originalPosition = child.position;
            Vector3 startPosition = originalPosition + slideStartOffset;

            child.position = startPosition;
            child.gameObject.SetActive(true);

            // Start slide animation
            yield return StartCoroutine(SlideAnimation(child, startPosition, originalPosition));

            activatedCount++;
            yield return new WaitForSeconds(timePerChild - slideAnimationDuration); // Wait for the remaining time
        }

        Debug.Log("Construction Complete! All parts activated in " + totalActivationTime + " seconds.");
    }

    IEnumerator SlideAnimation(Transform targetTransform, Vector3 startPos, Vector3 endPos)
    {
        float timer = 0f;
        while (timer < slideAnimationDuration)
        {
            float progress = timer / slideAnimationDuration;
            float curveValue = slideAnimationCurve.Evaluate(progress);
            targetTransform.position = Vector3.Lerp(startPos, endPos, curveValue);

            timer += Time.deltaTime;
            yield return null;
        }
        targetTransform.position = endPos; // Ensure it ends exactly at the target position
    }

    // You can add a public method to reset the construction if needed
    public void ResetConstruction()
    {
        StopAllCoroutines(); // Stop any ongoing activation
        foreach (Transform child in parentObject)
        {
            child.gameObject.SetActive(false);
        }
        activatedCount = 0;
        // Re-start the process
        StartCoroutine(ActivateChildrenGradually());
    }
}
