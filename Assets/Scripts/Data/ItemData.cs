using UnityEngine;

public enum ItemType
{
    None,
    Seed_Carrot,
    Seed_Tomato,
    Hoe
}

[CreateAssetMenu()]
public class ItemData : ScriptableObject
{
    public ItemType type = ItemType.None;
    public Sprite sprite;
    public GameObject prefab;
    public int maxCount = 1;
}
