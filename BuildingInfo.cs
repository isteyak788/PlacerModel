using UnityEngine;
using System.Collections.Generic;

public enum BuildingCategory
{
    Residential, // House, Castle
    Agriculture, // Farm, Barn
    Industry,    // Factory, Mills
    Defense,     // Fences, Walls
    Resource     // Mines
}

[CreateAssetMenu(fileName = "NewBuildingInfo", menuName = "Building/Building Info")]
public class BuildingInfo : ScriptableObject
{
    public string buildingName = "New Building";
    public BuildingCategory category; // New: Assign a category to each building
    public Vector2Int size = new Vector2Int(1, 1); // e.g., 1x3, 2x2. X for width, Y for height. (For grid-based systems)
    public Sprite buildingIcon;
    public GameObject buildingPrefab; // The actual building to be instantiated
    public GameObject buildingGhostPrefab; // A transparent version for placement preview

    [Tooltip("Capacity for families this building can house. 0 if not a residential building.")]
    public int familyCapacity = 0; 

    // NEW: Physical dimensions for collision detection (half-extents of the bounding box)
    [Tooltip("Half-extents of the building's bounding box for collision checks. Get this from the buildingPrefab's collider.bounds.extents.")]
    public Vector3 buildingBoundsExtents = new Vector3(0.5f, 0.5f, 0.5f); // Default to half a unit cube

    public List<ConstructionMaterialCost> materials = new List<ConstructionMaterialCost>();
}

[System.Serializable]
public class ConstructionMaterialCost
{
    public ConstructionMaterial material;
    public int amount;
}
