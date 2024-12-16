using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Networking;
using System;

public class LoginManager : MonoBehaviour
{
    public GameObject healthBarInstance;
    public InputField iptUserName;
    public InputField iptPwd;
    public Button loginButton;
    public Button guestLoginButton;
    public Text txtTips;

    public LoadingManager loadingManager; // 引用 LoadingManager
    public static event EventHandler<PlayerEventArgs> OnPlayerLogin;
    private string loginUrl = "http://localhost:5000/api/auth/login";
    private string guestLoginUrl = "http://localhost:5000/api/auth/guest";
    private static string tempResponse;

    public Button testButton;
    public Button testButton2;
    private float test1 = 100f;
    private float test2 = 100f;
    private void OnTest()
    {
        iptUserName.text = "player1";
        iptPwd.text = "123456";
        OnClickLogin();
    }

    private void OnTest2()
    {
        iptUserName.text = "player2";
        iptPwd.text = "123456";
        OnClickLogin();
    }
    private void Start()
    {
        testButton.onClick.AddListener(OnTest);
        testButton2.onClick.AddListener(OnTest2);
        loginButton.onClick.AddListener(OnClickLogin);
        guestLoginButton.onClick.AddListener(OnClickGuest);
        txtTips.gameObject.SetActive(false);
    }

    private void OnClickLogin()
    {
        string username = iptUserName.text.Trim();
        string password = iptPwd.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Username and password cannot be empty!");
            return;
        }

        StartCoroutine(PerformLogin(username, password));
    }

    private void OnClickGuest()
    {
        StartCoroutine(PerformGuestLogin());
    }

    private IEnumerator PerformLogin(string username, string password)
    {
        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);

        using (UnityWebRequest request = UnityWebRequest.Post(loginUrl, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                if (response.Contains("success"))
                {
                    tempResponse = response;
                    SceneManager.sceneLoaded += OnGameSceneLoaded;
                    SceneManager.LoadScene("GameScene");
                    NetworkManager.Instance.ConnectWebSocket();
                }
                else
                {
                    ShowError("Invalid username or password.");
                    Debug.LogError("Invalid username or password.");
                }
            }
            else
            {
                ShowError("Error connecting to server.");
                Debug.LogError("Error connecting to server: " + request.error);
            }
        }

    }

    private IEnumerator PerformGuestLogin()
    {
        WWWForm form = new WWWForm();

        using (UnityWebRequest request = UnityWebRequest.Post(guestLoginUrl, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                if (response.Contains("success"))
                {
                    tempResponse = response;
                    SceneManager.sceneLoaded += OnGameSceneLoaded;
                    SceneManager.LoadScene("GameScene");
                    NetworkManager.Instance.ConnectWebSocket();
                }
                else
                {
                    ShowError("Error during guest login.");
                    Debug.LogError("Error during guest login.");
                }
            }
            else
            {
                ShowError("Error connecting to server.");
                Debug.LogError("Error connecting to server: " + request.error);
            }
        }
    }
    private void ShowError(string message)
    {
        txtTips.text = message;
        txtTips.gameObject.SetActive(true);
    }

    private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameScene")
        {
            OnPlayerLogin?.Invoke(this, new PlayerEventArgs(tempResponse));
            tempResponse = null;
            SceneManager.sceneLoaded -= OnGameSceneLoaded;
        }
    }

}
