using UnityEngine;

[CreateAssetMenu(fileName = "NewConstructionMaterial", menuName = "Building/Construction Material")]
public class ConstructionMaterial : ScriptableObject
{
    public string materialName = "New Material";
    public Sprite icon;
}
