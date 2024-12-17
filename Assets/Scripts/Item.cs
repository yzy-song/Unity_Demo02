using UnityEngine;

[System.Serializable]
public class Item
{
    public string Id;          // 道具唯一标识符
    public string Name;        // 道具名称
    public Vector2 Position;   // 道具位置
    public bool IsPickedUp;    // 是否被拾取

    public Item(string id, string name, Vector2 position)
    {
        Id = id;
        Name = name;
        Position = position;
        IsPickedUp = false;
    }
}
