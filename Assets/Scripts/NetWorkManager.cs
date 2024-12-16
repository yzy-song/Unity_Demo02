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

    private void HandleMessage(byte[] buffer, int count)
    {
        var playersState = PlayersState.Parser.ParseFrom(buffer, 0, count);
        foreach (var player in playersState.Players)
        {
            if (player.Username != PlayerManager.CurrentPlayer.username)
            {
                Debug.Log($"Received player sync: {player.Username}, Position: ({player.X}, {player.Y})");

                EventManager.Invoke("PlayerSync", player);
            }
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


    public void CloseWebSocket()
    {
        if (ws != null && ws.State == WebSocketState.Open)
        {
            ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken.Token);
        }
    }
}
