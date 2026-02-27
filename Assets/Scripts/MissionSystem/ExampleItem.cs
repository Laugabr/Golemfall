using UnityEngine;

[CreateAssetMenu(fileName = "ExampleItem", menuName = "Scriptable Objects/ExampleItem")]
public class ExampleItem : ScriptableObject
{
    public string itemId;
    public string itemName;
    public int amount;
}
