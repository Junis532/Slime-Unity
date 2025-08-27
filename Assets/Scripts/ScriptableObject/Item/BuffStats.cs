using UnityEngine;

[CreateAssetMenu(fileName = "New Buff", menuName = "Buff/Buff")]
public class BuffStats : ScriptableObject
{
    public int BuffID;
    public string BuffName;
    public string description;
    public Sprite icon;

}
