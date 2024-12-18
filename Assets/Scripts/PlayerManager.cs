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
    public PlayerController pc;
    public static Player currentPlayer { get; private set; }
    public Dictionary<string, PlayerController> onlinePlayers = new Dictionary<string, PlayerController>();
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
        // // 登录成功后获取其他在线玩家数据
        // StartCoroutine(OnlinePlayers());

        EventManager.Subscribe<ItemProto>("ItemPickup", HandleItemPickup);
        EventManager.Subscribe<ChatMessage>("ChatMessage", DisplayChatMessage);

        // string[] args = Environment.GetCommandLineArgs();
        // foreach (var arg in args)
        // {
        //     if (arg.StartsWith("-username="))
        //     {
        //         string username = arg.Split('=')[1];
        //         PlayerManager.currentPlayer.username = username;
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
        EventManager.Subscribe<string>("LoginSuccess", InitializePlayer);
        EventManager.Subscribe<PlayerProto>("PlayerSync", OnPlayerSync);
        EventManager.Subscribe<ItemProto>("ItemPickup", HandleItemPickup);
        EventManager.Subscribe<ChatMessage>("ChatMessage", DisplayChatMessage);
    }

    private void OnDisable()
    {
        EventManager.Unsubscribe<string>("LoginSuccess", InitializePlayer);
        EventManager.Unsubscribe<PlayerProto>("PlayerSync", OnPlayerSync);
        EventManager.Unsubscribe<ItemProto>("ItemPickup", HandleItemPickup);
        EventManager.Unsubscribe<ChatMessage>("ChatMessage", DisplayChatMessage);
    }

    private void InitializePlayer(string response)
    {
        Debug.Log("Initializing current player...");
        currentPlayer = CreatePlayerFromResponse(response);
        Debug.Log($"Player {currentPlayer.username} initialized.");

        if (playerParent == null)
        {
            playerParent = GameObject.Find("GameObject").transform;
        }

        foreach (Transform child in playerParent)
        {
            child.gameObject.SetActive(true); // 启用子节点
        }
        // 获取在线玩家
        StartCoroutine(OnlinePlayers());
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
        StartCoroutine(PerformLogout(currentPlayer.sessionId));
    }

    private IEnumerator PerformLogout(string sessionId)
    {
        WWWForm form = new WWWForm();
        form.AddField("sessionId", sessionId);
        form.AddField("currentUsername", currentPlayer.username);

        yield return NetworkManager.Instance.PostRequest(
            logoutUrl,
            form,
            onSuccess: (response) =>
            {
                NetworkManager.Instance.CloseWebSocket();
                PlayerPrefs.DeleteKey("Username");
                HideAllPlayers();
                currentPlayer = null;
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
            playerParent = GameObject.Find("GameObject").transform;
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
        form.AddField("currentUsername", currentPlayer.username);

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
            if (!onlinePlayers.ContainsKey(player.username))
            {
                // 新玩家：创建游戏对象
                GameObject playerObject = Instantiate(playerPrefab, playerParent);

                // 添加血条
                var healthBar = playerObject.AddComponent<HealthBar>();
                healthBar.healthBarPrefab = healthBarPrefab;
                // healthBar.uiParent = uiParent;
                // healthBar.mainCamera = mainCamera;
                healthBar.Initialize(playerObject.transform, player.username, 100);

                playerObject.name = player.username;
                playerObject.transform.position = new Vector3(player.position.x, player.position.y, 0);

                // playerObject.GetComponent<BoxCollider2D>().isTrigger = true;
                playerObject.GetComponent<Collider2D>().tag = "Enemy";
                onlinePlayers[player.username] = playerObject.GetComponent<PlayerController>();
                Debug.Log("Added new player " + player.username);

            }
            else
            {
                // 添加血条
                var healthBar = onlinePlayers[player.username].gameObject.AddComponent<HealthBar>();
                healthBar.healthBarPrefab = healthBarPrefab;
                // healthBar.uiParent = uiParent;
                // healthBar.mainCamera = mainCamera;

                healthBar.Initialize(onlinePlayers[player.username].transform, player.username, 100);

                onlinePlayers[player.username].name = player.username;
                // 更新位置
                onlinePlayers[player.username].transform.position = new Vector3(player.position.x, player.position.y, 0);
                Debug.Log("Existed player " + player.username);
            }
        }

        pc.InitializeHealthBar();


    }

    public void OnPlayerSync(PlayerProto syncProto)
    {
        if (syncProto == null)
        {
            Debug.LogWarning("Invalid sync data received.");
            return;
        }

        if (syncProto.Username == currentPlayer.username)
        {
            // 当前玩家的同步逻辑交给 PlayerController 处理
            if (pc != null)
            {
                pc.UpdateCurrentPlayer(syncProto);
            }
        }
        else
        {
            // 其他玩家的同步逻辑
            string usernameKey = syncProto.Username.Trim().ToLower();
            if (onlinePlayers.TryGetValue(usernameKey, out PlayerController pc))
            {
                pc?.UpdateOtherPlayer(syncProto);
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

    public void TestClick()
    {

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
