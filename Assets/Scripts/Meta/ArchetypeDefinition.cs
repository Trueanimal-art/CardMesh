using UnityEngine;

[CreateAssetMenu(fileName = "ArchetypeDefinition", menuName = "WorkForUncle/Archetype Definition")]
public class ArchetypeDefinition : ScriptableObject
{
    public OfficeArchetype type;

    [Header("UI Text")]
    public string title;        // "ОФИСНЫЙ ТРУП"
    [TextArea(2, 6)] public string description;
    public string slogan;       // "Я просто жду"
}
