using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public GameObject healthBarPrefab;
    public Transform uiParent;
    public Camera mainCamera;

    private GameObject healthBarInstance;
    private Image healthFillImage;        // 血量条的填充部分
    private Text nameText;                // 显示玩家姓名

    // private Transform playerTransform;    // 玩家对象的 Transform
    public PlayerController controller;
    public void Initialize(Transform player, string playerName, float maxHealth)
    {
        controller = player.GetComponent<PlayerController>();

        // EventManager.Subscribe("HealthChangedEventArgs", UpdateHealthBar);
        controller.OnHealthChanged += UpdateHealthBar;
        // 动态实例化血条
        healthBarInstance = Instantiate(healthBarPrefab, uiParent);
        healthFillImage = healthBarInstance.transform.Find("Fill").GetComponent<Image>();
        nameText = healthBarInstance.transform.Find("Name").GetComponent<Text>();

        // 设置初始状态
        nameText.text = playerName;
        healthFillImage.fillAmount = 1;
    }

    private void UpdateHealthBar(object sender, HealthChangedEventArgs e)
    {
        // 更新血条
        healthFillImage.fillAmount = e.CurrentHealth / e.MaxHealth;
        Debug.Log($"Updated {e.PlayerName}'s health to {e.CurrentHealth}/{e.MaxHealth}");
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main; // 动态获取主摄像机
            if (mainCamera == null)
            {
                Debug.LogWarning("MainCamera not found for HealthBar.");
                return;
            }
        }
        if (controller && healthBarInstance)
        {
            // 将血条位置同步到玩家头顶
            Vector3 worldPosition = controller.transform.position + Vector3.up;
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

            if (screenPosition.z > 0) // 玩家在摄像机前方
            {
                healthBarInstance.transform.position = screenPosition;
                healthBarInstance.SetActive(true);
            }
            else
            {
                healthBarInstance.SetActive(false); // 玩家不在视野内时隐藏血条
            }
        }
    }

    private void OnDestroy()
    {
        if (healthBarInstance)
        {
            controller.OnHealthChanged -= UpdateHealthBar;
            Destroy(healthBarInstance); // 玩家销毁时清理血条
        }
    }
}
