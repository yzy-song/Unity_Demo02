using System;
using System.Collections;
using System.Net.WebSockets;
using System.Threading;
using GameProtos;
using Google.Protobuf;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkManager : MonoBehaviour
{
    private static NetworkManager instance;

    public static NetworkManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("NetworkManager");
                instance = obj.AddComponent<NetworkManager>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    public IEnumerator PostRequest(string url, WWWForm form, System.Action<string> onSuccess, System.Action<string> onError)
    {
        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(request.downloadHandler.text);
            }
            else
            {
                onError?.Invoke(request.error);
            }
        }
    }

    private ClientWebSocket ws;
    // private const string serverUrl = "ws://127.0.0.1:5000";
    private const string serverUrl = "ws://192.168.125.157:5000";

    private CancellationTokenSource cancellationToken = new CancellationTokenSource();

    public void ConnectWebSocket()
    {
        ws = new ClientWebSocket();
        ws.ConnectAsync(new Uri(serverUrl), cancellationToken.Token).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("WebSocket连接失败: " + task.Exception.Message);
            }
            else
            {
                Debug.Log("WebSocket连接成功");
                ReceiveMessages();
            }
        });
    }

    private async void ReceiveMessages()
    {
        Debug.Log("ReceiveMessages");
        byte[] buffer = new byte[4096];
        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken.Token);
            if (result.MessageType == WebSocketMessageType.Binary)
            {
                HandleMessage(buffer, result.Count);
            }
        }
    }

    // private void HandleMessage(byte[] buffer, int count)
    // {
    //     var playersState = PlayersState.Parser.ParseFrom(buffer, 0, count);
    //     foreach (var player in playersState.Players)
    //     {
    //         if (player.Username != PlayerManager.CurrentPlayer.username)
    //         {
    //             Debug.Log($"Received player sync: {player.Username}, Position: ({player.X}, {player.Y})");

    //             EventManager.Invoke("PlayerSync", player);
    //         }
    //     }
    // }

    private void HandleMessage(byte[] buffer, int count)
    {
        // 解析 BaseMessage
        var baseMessage = BaseMessage.Parser.ParseFrom(buffer, 0, count);

        Debug.Log($"Event Type: {baseMessage.EventType}");
        Debug.Log($"Payload length: {baseMessage.Payload.Length}");
        // 根据事件类型解析不同的消息内容
        switch (baseMessage.EventType)
        {
            case "PlayerStateUpdate":
                var playerStateUpdate = PlayerStateUpdate.Parser.ParseFrom(baseMessage.Payload);
                var player = playerStateUpdate.Player;

                if (player.Username != PlayerManager.CurrentPlayer.username)
                {
                    Debug.Log($"Received player sync: {player.Username}, Position: ({player.X}, {player.Y})");
                    EventManager.Invoke("PlayerSync", player);
                }
                break;

            case "ItemPickup":
                var itemPickup = ItemPickupEvent.Parser.ParseFrom(baseMessage.Payload);
                Debug.Log($"Item picked up: {itemPickup.ItemId} by Player {itemPickup.PlayerId}");
                EventManager.Invoke("ItemPickup", itemPickup);
                break;

            case "ChatMessage":
                var chatMessage = ChatMessage.Parser.ParseFrom(baseMessage.Payload);
                Debug.Log($"Chat Message from {chatMessage.Sender}: {chatMessage.Content}");
                EventManager.Invoke("ChatMessage", chatMessage);
                break;

            default:
                Debug.LogWarning($"Unknown event type: {baseMessage.EventType}");
                break;
        }
    }


    public void SendBaseMessage(BaseMessage message)
    {
        if (ws != null && ws.State == WebSocketState.Open)
        {
            byte[] buffer = message.ToByteArray(); // 序列化 BaseMessage
            ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, cancellationToken.Token)
              .ContinueWith(task =>
              {
                  if (task.IsFaulted)
                  {
                      Debug.LogError("Failed to send BaseMessage: " + task.Exception.Message);
                  }
              });
        }
        else
        {
            Debug.LogError("WebSocket is not open. State: " + ws?.State);
        }
    }

    public void SendPlayerUpdate(PlayerStateUpdate update)
    {
        if (ws != null && ws.State == WebSocketState.Open)
        {
            // Debug.Log("Sending player update: " + update.Player.Username);
            byte[] buffer = update.ToByteArray();
            ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, cancellationToken.Token)
              .ContinueWith(task =>
              {
                  if (task.IsFaulted)
                  {
                      Debug.LogError("Failed to send message: " + task.Exception.Message);
                  }
                  else
                  {
                      //   Debug.Log("Message sent successfully.");
                  }
              });
        }
        else
        {
            Debug.LogError("WebSocket is not open. State: " + ws?.State);
        }
    }

    public void SendItemPickup(string itemId)
    {
        ItemProto itemProto = new ItemProto { Id = itemId };
        byte[] data = itemProto.ToByteArray();
        ws.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, cancellationToken.Token);
    }

    public void SendChatMessage(string receiver, string content)
    {
        ChatMessage chatMessage = new ChatMessage
        {
            Sender = PlayerManager.CurrentPlayer.username,
            Receiver = receiver,
            Content = content,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        byte[] data = chatMessage.ToByteArray();
        ws.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, cancellationToken.Token);
    }


    public void CloseWebSocket()
    {
        if (ws != null && ws.State == WebSocketState.Open)
        {
            ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken.Token);
        }
    }
}
