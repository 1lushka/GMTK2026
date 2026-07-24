using UnityEngine;

[CreateAssetMenu(fileName = "NewAbility", menuName = "Abilities/Ability Definition")]
public class AbilityDefinition : ScriptableObject
{
    public string abilityId;          
    public string displayName;
    [TextArea] public string description;
    public bool enabledByDefault = true;
}