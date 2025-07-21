using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Button
using System.Collections.Generic; // Required for List

public class BuildingPlacer : MonoBehaviour
{
    // --- Panel Toggle Configuration ---
    // This is an "expandable field" (a List of a [System.Serializable] class)
    [System.Serializable]
    public class PanelToggleConfig
    {
        [Tooltip("The UI Button that will toggle the associated panel.")]
        public Button toggleButton;
        [Tooltip("The GameObject panel that will be activated/deactivated.")]
        public GameObject panelToToggle;
    }

    [Tooltip("Configure your UI buttons and the panels they toggle.")]
    public List<PanelToggleConfig> panelToggleConfigs = new List<PanelToggleConfig>();

    // --- Building Type Configuration ---
    [System.Serializable]
    public class BuildingType
    {
        public string name; // Name for display in the UI (optional)
        [Tooltip("The BuildingInfo ScriptableObject for this building type.")]
        public BuildingInfo buildingInfo; // CHANGED: Now references BuildingInfo SO
        [Tooltip("The UI Button that, when clicked, starts placing this building.")]
        public Button uiButton;
    }

    [Tooltip("Configure your building types here. Each entry needs a BuildingInfo SO and a UI button.")]
    public List<BuildingType> buildingTypes = new List<BuildingType>();

    // --- Internal State Variables ---
    private BuildingInfo currentBuildingInfo; // CHANGED: Stores the selected BuildingInfo SO
    private GameObject ghostBuildingInstance; // A transparent "ghost" of the building to be placed
    private bool isPlacingBuilding = false; // True when the player is in building placement mode

    // --- Visuals and Layers ---
    [Tooltip("Assign a transparent material for valid placement.")]
    public Material validPlacementMaterial;
    [Tooltip("Assign a red, transparent material for invalid placement.")]
    public Material invalidPlacementMaterial;

    [Tooltip("Set this to the layer your ground or placable surfaces are on.")]
    public LayerMask groundLayer;
    [Tooltip("Layer for already placed buildings to check for overlaps. Create a new layer called 'PlacedBuilding' and assign it to your placed buildings.")]
    public LayerMask placedBuildingsLayer; // NEW: For collision detection

    [Tooltip("Offset the ghost building slightly above the ground to prevent Z-fighting.")]
    public float placementYOffset = 0.05f;

    // --- Placement Options ---
    public bool snapToGrid = false;
    public float gridSize = 1f;

    // --- Unity Lifecycle Methods ---

    void Start()
    {
        // Hook up panel toggle buttons
        foreach (PanelToggleConfig config in panelToggleConfigs)
        {
            if (config.toggleButton != null && config.panelToToggle != null)
            {
                GameObject panel = config.panelToToggle; // Capture for lambda
                config.toggleButton.onClick.AddListener(() => TogglePanel(panel));
                // Ensure panels are initially hidden
                config.panelToToggle.SetActive(false);
            }
            else
            {
                Debug.LogWarning("A PanelToggleConfig entry has unassigned button or panel. Please check the Inspector.");
            }
        }

        // Hook up building selection buttons
        foreach (BuildingType buildingType in buildingTypes)
        {
            if (buildingType.uiButton != null && buildingType.buildingInfo != null)
            {
                BuildingInfo infoToPlace = buildingType.buildingInfo; // Capture for lambda
                buildingType.uiButton.onClick.AddListener(() => StartPlacingBuilding(infoToPlace));
            }
            else
            {
                Debug.LogWarning($"UI Button or BuildingInfo not assigned for building type: {buildingType.name}. Please check the Inspector.");
            }
        }

        // Clean up any leftover ghost building if the scene was not properly reset
        if (ghostBuildingInstance != null)
        {
            Destroy(ghostBuildingInstance);
            ghostBuildingInstance = null;
        }
    }

    void Update()
    {
        if (isPlacingBuilding && currentBuildingInfo != null)
        {
            // Raycast to find the ground position under the mouse cursor
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
            {
                Vector3 placementPosition = hit.point + Vector3.up * placementYOffset;

                // Optional: Snap to grid
                if (snapToGrid)
                {
                    placementPosition.x = Mathf.Round(placementPosition.x / gridSize) * gridSize;
                    placementPosition.y = Mathf.Round(placementPosition.y / gridSize) * gridSize;
                    placementPosition.z = Mathf.Round(placementPosition.z / gridSize) * gridSize;
                }

                // Create ghost building if it doesn't exist
                if (ghostBuildingInstance == null)
                {
                    // Use buildingGhostPrefab if provided, otherwise use buildingPrefab
                    GameObject prefabToInstantiate = currentBuildingInfo.buildingGhostPrefab != null ?
                                                     currentBuildingInfo.buildingGhostPrefab :
                                                     currentBuildingInfo.buildingPrefab;

                    if (prefabToInstantiate != null)
                    {
                        ghostBuildingInstance = Instantiate(prefabToInstantiate);
                        SetGhostMaterial(ghostBuildingInstance, validPlacementMaterial);
                        DisableGhostComponents(ghostBuildingInstance);
                    }
                    else
                    {
                        Debug.LogError($"Ghost or main prefab is missing for {currentBuildingInfo.buildingName}. Cannot create ghost.");
                        CancelBuildingPlacement();
                        return;
                    }
                }
                ghostBuildingInstance.transform.position = placementPosition;
                ghostBuildingInstance.SetActive(true);

                // --- Placement Validation ---
                bool isValidPlacement = IsPlacementValid(placementPosition);
                SetGhostMaterial(ghostBuildingInstance, isValidPlacement ? validPlacementMaterial : invalidPlacementMaterial);

                // Left-click to place building (continuous placement)
                if (Input.GetMouseButtonDown(0) && isValidPlacement) // 0 is left-click
                {
                    PlaceBuilding(placementPosition);
                }
            }
            else
            {
                // If mouse is not over ground, hide the ghost building
                if (ghostBuildingInstance != null)
                {
                    ghostBuildingInstance.SetActive(false);
                }
            }

            // Right-click to cancel placement
            if (Input.GetMouseButtonDown(1)) // 1 is right-click
            {
                CancelBuildingPlacement();
            }
        }
    }

    // --- Public Methods (Called by UI or other scripts) ---

    // Called when a building UI button is clicked to start placement mode
    public void StartPlacingBuilding(BuildingInfo info)
    {
        currentBuildingInfo = info;
        isPlacingBuilding = true;

        // Destroy any existing ghost building to prepare for a new one
        if (ghostBuildingInstance != null)
        {
            Destroy(ghostBuildingInstance);
            ghostBuildingInstance = null;
        }

        Debug.Log("Started placing: " + info.buildingName);

        // Close all panels after selecting a building
        foreach (PanelToggleConfig config in panelToggleConfigs)
        {
            if (config.panelToToggle != null && config.panelToToggle.activeSelf)
            {
                config.panelToToggle.SetActive(false);
            }
        }
    }

    // --- Private Helper Methods ---

    // Instantiates the actual building
    private void PlaceBuilding(Vector3 position)
    {
        if (currentBuildingInfo != null && currentBuildingInfo.buildingPrefab != null)
        {
            // Instantiate the building
            GameObject newBuilding = Instantiate(currentBuildingInfo.buildingPrefab, position, Quaternion.identity);

            // IMPORTANT: Assign the "PlacedBuilding" layer to the newly placed building
            // This is crucial for the IsPlacementValid collision check to work correctly.
            newBuilding.layer = LayerMask.NameToLayer("PlacedBuilding");
            // Also set layer for all children
            SetLayerRecursively(newBuilding, LayerMask.NameToLayer("PlacedBuilding"));


            Debug.Log("Building placed: " + currentBuildingInfo.buildingName);
        }
    }

    // Sets the layer for a GameObject and all its children
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }


    // Cancels the current building placement mode
    private void CancelBuildingPlacement()
    {
        isPlacingBuilding = false;
        currentBuildingInfo = null; // Clear the selected BuildingInfo

        if (ghostBuildingInstance != null)
        {
            Destroy(ghostBuildingInstance);
            ghostBuildingInstance = null;
        }
        Debug.Log("Building placement cancelled.");
    }

    // Toggles the visibility of a given UI panel GameObject
    public void TogglePanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(!panel.activeSelf); // Toggle visibility

            // If a panel is being closed, and we are placing a building, cancel placement
            if (!panel.activeSelf && isPlacingBuilding)
            {
                CancelBuildingPlacement();
            }
        }
    }

    // Sets the material of the ghost building and its children
    private void SetGhostMaterial(GameObject ghost, Material material)
    {
        Renderer[] renderers = ghost.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r.sharedMaterial != material) // Only update if different to avoid constant changes
            {
                r.material = material; // Use .material to create an instance, .sharedMaterial affects prefab
            }
        }
    }

    // Disables colliders and other components on the ghost building
    private void DisableGhostComponents(GameObject ghost)
    {
        // Disable all colliders on the ghost and its children
        Collider[] colliders = ghost.GetComponentsInChildren<Collider>(true);
        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }

        // You might also want to disable scripts, rigidbodies, etc., that shouldn't run on the ghost
        // Be careful not to disable components essential for visual representation (e.g., MeshRenderer)
        MonoBehaviour[] scripts = ghost.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour s in scripts)
        {
            // Avoid disabling the BuildingPlacer script itself if it's somehow a child, or other critical scripts
            if (s != this)
            {
                s.enabled = false;
            }
        }

        Rigidbody[] rigidbodies = ghost.GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = true; // Make kinematic to prevent physics interactions
            rb.detectCollisions = false; // Disable collision detection
        }
    }

    // --- Placement Validation Logic ---
    private bool IsPlacementValid(Vector3 position)
    {
        if (currentBuildingInfo == null || currentBuildingInfo.buildingPrefab == null)
        {
            Debug.LogError("Current Building Info or its Prefab is null during validation.");
            return false;
        }

        // Use the buildingBoundsExtents from the BuildingInfo for the overlap check
        Vector3 halfExtents = currentBuildingInfo.buildingBoundsExtents;

        // Fallback: If extents are zero, try to get from ghost's collider (less reliable as ghost might not have the correct collider)
        if (halfExtents == Vector3.zero)
        {
            Debug.LogWarning($"BuildingInfo for {currentBuildingInfo.buildingName} has zero bounds extents. Attempting to use ghost collider bounds. Please set 'Building Bounds Extents' in the BuildingInfo ScriptableObject for accurate validation.");
            if (ghostBuildingInstance != null)
            {
                Collider ghostCollider = ghostBuildingInstance.GetComponent<Collider>();
                if (ghostCollider != null)
                {
                    halfExtents = ghostCollider.bounds.extents;
                }
            }
            if (halfExtents == Vector3.zero) return true; // If still zero, assume valid to prevent blocking entirely
        }

        // Check for overlaps with other colliders on the 'placedBuildingsLayer'
        // Physics.OverlapBox returns all colliders that overlap with the defined box.
        // The ghost building itself should be ignored.
        // The ground layer should also be ignored (it's not in placedBuildingsLayer anyway).
        Collider[] hitColliders = Physics.OverlapBox(
            position, // Center of the box
            halfExtents, // Half-extents of the box
            Quaternion.identity, // Rotation of the box (assuming buildings are placed without rotation for now)
            placedBuildingsLayer // Only check against this layer
        );

        // If any collider is hit, and it's not the ghost itself, then placement is invalid.
        foreach (Collider col in hitColliders)
        {
            // Ensure we're not colliding with the ghost building itself
            // (e.g., if the ghost has multiple colliders or a parent/child structure)
            if (col.transform.root != ghostBuildingInstance.transform.root)
            {
                Debug.Log($"Overlap detected with existing building: {col.gameObject.name}");
                return false; // Overlap detected, invalid placement
            }
        }

        // Add more complex validation here if needed (e.g., terrain type, buildable area boundaries)

        return true; // No overlaps found, placement is valid
    }
}
