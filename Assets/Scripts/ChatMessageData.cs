[System.Serializable]
public class ChatMessageData
{
    public string Sender;     // 发送者用户名
    public string Receiver;   // 接收者用户名（为空时表示公屏消息）
    public string Content;    // 聊天内容
    public long Timestamp;    // 消息时间戳

    public ChatMessageData(string sender, string receiver, string content, long timestamp)
    {
        Sender = sender;
        Receiver = receiver;
        Content = content;
        Timestamp = timestamp;
    }
}
