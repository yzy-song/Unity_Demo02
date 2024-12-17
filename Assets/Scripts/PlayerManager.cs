using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using GameProtos;
using System;

public class PlayerManager : MonoBehaviour
{
    private string logoutUrl = "http://localhost:5000/api/auth/logout";
    private string onlinePlayersUrl = "http://localhost:5000/api/game/onlinePlayers";
    private string heartbeatUrl = "http://localhost:5000/api/game/heartbeat";
    public int heartbeatInterval = 30;
    public GameObject healthBarPrefab;
    public Transform uiParent;
    public Camera mainCamera;
    public GameObject playerPrefab;
    public Transform playerParent;
    public static Player CurrentPlayer { get; private set; }
    public Dictionary<string, GameObject> otherPlayers = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> items = new Dictionary<string, GameObject>();

    public static PlayerManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject);
    }
    private void Start()
    {
        Application.runInBackground = true;
        // 登录成功后获取其他在线玩家数据
        StartCoroutine(OnlinePlayers());

        EventManager.Subscribe<ItemProto>("ItemPickup", HandleItemPickup);
        EventManager.Subscribe<ChatMessage>("ChatMessage", DisplayChatMessage);
        // string[] args = Environment.GetCommandLineArgs();
        // foreach (var arg in args)
        // {
        //     if (arg.StartsWith("-username="))
        //     {
        //         string username = arg.Split('=')[1];
        //         PlayerManager.CurrentPlayer.username = username;
        //         Debug.Log($"Username set to: {username}");
        //     }
        // }
    }

    private void HandleItemPickup(ItemProto itemProto)
    {
        if (items.ContainsKey(itemProto.Id))
        {
            Destroy(items[itemProto.Id]);
            items.Remove(itemProto.Id);
            Debug.Log($"Item {itemProto.Id} picked up.");
        }
    }

    private void DisplayChatMessage(ChatMessage message)
    {
        string chatContent = string.IsNullOrEmpty(message.Receiver)
            ? $"{message.Sender}: {message.Content}"
            : $"[私聊] {message.Sender} -> {message.Receiver}: {message.Content}";

        Debug.Log(chatContent); // 可扩展为 UI 显示
    }

    private void OnEnable()
    {
        LoginManager.OnPlayerLogin += OnPlayerLogin;
    }

    private void OnDisable()
    {
        LoginManager.OnPlayerLogin -= OnPlayerLogin;
    }

    private void OnPlayerLogin(object sender, PlayerEventArgs e)
    {
        string response = e.Response;
        CurrentPlayer = CreatePlayerFromResponse(response);

        ShowAllPlayers();

        Debug.Log($"Player initialized: {CurrentPlayer.username}");

    }

    private Player CreatePlayerFromResponse(string response)
    {
        string username = ExtractJsonField(response, "userName");
        string sessionId = ExtractJsonField(response, "sessionId");
        int level = int.Parse(ExtractJsonField(response, "level", "0"));
        int experience = int.Parse(ExtractJsonField(response, "experience", "0"));
        float health = float.Parse(ExtractJsonField(response, "health", "100"));

        string pos = ExtractJsonField(response, "position");
        float x = float.Parse(ExtractJsonField(pos, "x", "0"));
        float y = float.Parse(ExtractJsonField(pos, "y", "0"));
        Vector3 position = new Vector3(x, y, 0);
        Player player = new Player(username, sessionId, level, experience, health, position);
        player.SaveToPrefs();
        return player;
    }

    private string ExtractJsonField(string json, string fieldName, string defaultValue = "")
    {
        int startIndex = json.IndexOf($"\"{fieldName}\":") + fieldName.Length + 3;
        if (startIndex < fieldName.Length + 3) return defaultValue;

        int endIndex = json.IndexOf(",", startIndex);
        if (endIndex == -1) endIndex = json.IndexOf("}", startIndex);

        if (endIndex == -1) return defaultValue;

        string value = json.Substring(startIndex, endIndex - startIndex).Trim('\"');
        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }

    public void Logout()
    {
        StartCoroutine(PerformLogout(CurrentPlayer.sessionId));
    }

    private IEnumerator PerformLogout(string sessionId)
    {
        WWWForm form = new WWWForm();
        form.AddField("sessionId", sessionId);
        form.AddField("currentUsername", CurrentPlayer.username);

        yield return NetworkManager.Instance.PostRequest(
            logoutUrl,
            form,
            onSuccess: (response) =>
            {
                NetworkManager.Instance.CloseWebSocket();
                PlayerPrefs.DeleteKey("Username");
                HideAllPlayers();
                CurrentPlayer = null;
                SceneManager.LoadScene("LoginScene");
            },
            onError: (error) =>
            {
                Debug.LogError("Logout error: " + error);
            }
        );
    }

    public void HideAllPlayers()
    {
        if (playerParent == null) return;

        foreach (Transform child in playerParent)
        {
            child.gameObject.SetActive(false); // 禁用子节点
        }
        Debug.Log("All players under playerParent hidden.");
    }

    public void ShowAllPlayers()
    {
        if (playerParent == null)
        {
            playerParent = GameObject.Find("PlayerParent").transform;
        }

        foreach (Transform child in playerParent)
        {
            child.gameObject.SetActive(true); // 启用子节点
        }

        Debug.Log("All players under playerParent shown.");
    }

    private IEnumerator OnlinePlayers()
    {
        WWWForm form = new WWWForm();
        form.AddField("currentUsername", CurrentPlayer.username);

        yield return NetworkManager.Instance.PostRequest(
            onlinePlayersUrl,
            form,
            onSuccess: (response) =>
            {
                // 更新场景中的玩家
                OnlinePlayersData playersData = JsonUtility.FromJson<OnlinePlayersData>(response);
                UpdatePlayersInScene(playersData.players);

            },
            onError: (error) =>
            {
                Debug.LogError("Get online players error: " + error);
            }
        );
    }

    private void UpdatePlayersInScene(List<PlayerData> players)
    {
        foreach (PlayerData player in players)
        {
            if (!otherPlayers.ContainsKey(player.username))
            {
                // 新玩家：创建游戏对象
                GameObject playerObject = Instantiate(playerPrefab, playerParent);

                // 添加血条
                var healthBar = playerObject.AddComponent<HealthBar>();
                healthBar.healthBarPrefab = healthBarPrefab;
                healthBar.uiParent = uiParent;
                healthBar.mainCamera = mainCamera;
                healthBar.Initialize(playerObject.transform, player.username, 100);

                playerObject.name = player.username;
                playerObject.transform.position = new Vector3(player.position.x, player.position.y, 0);

                // playerObject.GetComponent<BoxCollider2D>().isTrigger = true;
                playerObject.GetComponent<Collider2D>().tag = "Enemy";
                otherPlayers[player.username] = playerObject;
                Debug.Log("Added new player " + player.username);

                // 订阅 PlayerSync 事件
                EventManager.Subscribe<PlayerProto>("PlayerSync", playerProtoData =>
                {
                    OnPlayerSync(playerProtoData);
                });
            }
            else
            {
                // 添加血条
                var healthBar = otherPlayers[player.username].AddComponent<HealthBar>();
                healthBar.healthBarPrefab = healthBarPrefab;
                healthBar.uiParent = uiParent;
                healthBar.mainCamera = mainCamera;

                healthBar.Initialize(otherPlayers[player.username].transform, player.username, 100);

                otherPlayers[player.username].name = player.username;
                // 更新位置
                otherPlayers[player.username].transform.position = new Vector3(player.position.x, player.position.y, 0);
                Debug.Log("Existed player " + player.username);
            }
        }
    }

    public void OnPlayerSync(PlayerProto syncProto)
    {
        if (syncProto == null)
        {
            Debug.LogWarning("Invalid sync data received.");
            return;
        }

        if (syncProto.Username == CurrentPlayer.username)
        {
            // 当前玩家的同步逻辑交给 PlayerController 处理
            // var pc = CurrentPlayer.GetComponent<PlayerController>();
            // if (pc != null)
            // {
            //     pc.UpdateCurrentPlayer(syncProto);
            // }
        }
        else
        {
            // 其他玩家的同步逻辑
            string usernameKey = syncProto.Username.Trim().ToLower();
            if (otherPlayers.TryGetValue(usernameKey, out GameObject playerObject))
            {
                var pc = playerObject.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.UpdateOtherPlayer(syncProto);
                    Debug.Log("Updated player " + usernameKey);
                }
                else
                {
                    Debug.Log("PlayerController not found for username: " + usernameKey);
                }
            }
            else
            {
                Debug.LogWarning($"Received sync for unknown player: {syncProto.Username}");
            }
        }
    }

    private IEnumerator SendHeartbeat()
    {
        while (true)
        {
            yield return new WaitForSeconds(heartbeatInterval);

            WWWForm form = new WWWForm();
            form.AddField("sessionId", PlayerPrefs.GetString("SessionId"));

            using (UnityWebRequest request = UnityWebRequest.Post(heartbeatUrl, form))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Heartbeat failed: " + request.error);
                }
            }
        }
    }

}


// 在线玩家数据结构
[System.Serializable]
public class OnlinePlayersData
{
    public List<PlayerData> players;
}

// 单个玩家数据结构
[System.Serializable]
public class PlayerData
{
    public string username;
    public int lv;
    public int exp;
    public Vector2 logout_position;
    public Vector2 position;
    public float hp;
}
