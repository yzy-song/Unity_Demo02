using UnityEngine;
using GameProtos;
using Google.Protobuf; // 确保命名空间与生成的 Protobuf 类一致

public class TestProto : MonoBehaviour
{
    void Start()
    {
        // 创建 Protobuf 对象
        var player = new PlayerProto
        {
            Username = "TestPlayer",
            X = 10,
            Y = 20,
            Lv = 5,
            Exp = 100,
            Hp = 90
        };

        // 序列化为字节数组
        byte[] data = player.ToByteArray();
        Debug.Log($"Serialized Data Length: {data.Length} bytes");

        // 反序列化为对象
        var deserializedPlayer = PlayerProto.Parser.ParseFrom(data);
        Debug.Log($"Deserialized Player: {deserializedPlayer.Username}, Position: ({deserializedPlayer.X}, {deserializedPlayer.Y})");
    }
}
