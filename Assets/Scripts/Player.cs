using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Player
{
    public string username { get; set; }
    public string sessionId { get; private set; }
    public int lv { get; set; }
    public int exp { get; set; }
    public Vector2 logout_position { get; set; }
    public float hp { get; set; }
    public Vector2 position { get; set; }

    public Player(string name, string sid, int level, int experience, float health, Vector2 logoutPosition)
    {
        username = name;
        sessionId = sid;
        lv = level;
        exp = experience;
        logout_position = logoutPosition;
        hp = health;
    }

    // 保存玩家信息到 PlayerPrefs
    public void SaveToPrefs()
    {
        PlayerPrefs.SetString("Username", username);
        PlayerPrefs.SetString("SessionId", sessionId);
        PlayerPrefs.SetInt("Level", lv);
        PlayerPrefs.SetInt("Experience", exp);
        PlayerPrefs.SetFloat("Health", hp);
        PlayerPrefs.SetString("LogoutPosition", $"{logout_position.x},{logout_position.y}");
    }

    // 从 PlayerPrefs 加载玩家信息
    public static Player LoadFromPrefs()
    {
        string username = PlayerPrefs.GetString("Username", "");
        string sessionId = PlayerPrefs.GetString("SessionId", "");
        int level = PlayerPrefs.GetInt("Level", 0);
        int experience = PlayerPrefs.GetInt("Experience", 0);
        float hp = PlayerPrefs.GetFloat("Health", 100f);
        string[] position = PlayerPrefs.GetString("LogoutPosition", "0,0").Split(',');
        Vector2 logoutPosition = new Vector2(float.Parse(position[0]), float.Parse(position[1]));

        return new Player(username, sessionId, level, experience, hp, logoutPosition);
    }


}
