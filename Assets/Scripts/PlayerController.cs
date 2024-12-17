using System;
using System.Collections;
using System.Collections.Generic;
using GameProtos;
using Google.Protobuf;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private float speed = 3;

    private Animator anim;

    private Vector2 direction = Vector2.zero;

    [Header("Joystick")]
    public bool useJoystick = true;
    public Joystick joystick;

    // [Header("Health Bar")]
    // public GameObject healthBarPrefab;  // 血条预制体
    // public Transform uiParent;         // 血条挂载的父节点（Canvas）
    // public Camera mainCamera;          // 主摄像机

    private GameObject healthBarInstance;
    private Image healthFillImage;
    private Text nameText;

    private float maxHealth = 100f;    // 最大血量
    private float currentHealth;       // 当前血量
    public float attackRange = 0.6f;

    private float highFrequencyInterval = 0.5f; // 同步间隔（移动时,50ms）
    private float lowFrequencyInterval = 0.5f;  // 同步间隔（静止时,500ms）
    private float lastSyncTime = 0;
    private Vector2 lastPosition = Vector2.zero;
    private Player player;
    private bool isMoving = false;
    public event EventHandler<HealthChangedEventArgs> OnHealthChanged;

    public void UpdateCurrentPlayer(PlayerProto syncProto)
    {
        // 更新当前玩家的状态
        transform.position = new Vector3(syncProto.X, syncProto.Y, 0); // 同步服务器权威位置
        Debug.Log($"Updated current player position to ({syncProto.X}, {syncProto.Y})");
    }

    public void UpdateOtherPlayer(PlayerProto syncProto)
    {
        // 更新其他玩家的状态
        transform.position = new Vector3(syncProto.X, syncProto.Y, 0);
        Debug.Log($"Updated other player position to ({syncProto.X}, {syncProto.Y})");
    }

    private void Awake()
    {
        anim = GetComponent<Animator>();
        currentHealth = maxHealth; // 初始化为满血
    }

    private void Start()
    {
        if (PlayerManager.currentPlayer != null)
        {
            player = PlayerManager.currentPlayer;
            lastPosition = player.position;
        }
    }


    private Vector2 simulatedDirection;
    private float simulationInterval = 1f; // 每隔 1 秒随机生成方向
    private float lastSimulationTime;
    void SimulateInput()
    {
        simulatedDirection = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized;
        Debug.Log($"Simulated new direction: {simulatedDirection}");
    }
    void Update()
    {
        // SimulateInput();
        if (direction.magnitude > 0)
        {
            anim.SetBool("isWalking", true);
            anim.SetFloat("horizontal", direction.x);
            anim.SetFloat("vertical", direction.y);
        }
        else
        {
            anim.SetBool("isWalking", false);
        }

        if (player == null)
        {
            return;
        }

        // 动态判断当前玩家是否移动
        Vector2 currentPosition = transform.position;
        isMoving = currentPosition != lastPosition;

        // 根据移动状态决定同步间隔
        float interval = isMoving ? highFrequencyInterval : lowFrequencyInterval;

        if (Time.time - lastSyncTime >= interval)
        {
            lastSyncTime = Time.time;

            // 构建玩家状态更新消息
            PlayerStateUpdate update = new PlayerStateUpdate
            {
                Player = new PlayerProto
                {
                    Username = player.username,
                    X = transform.position.x,
                    Y = transform.position.y,
                    Lv = player.lv,
                    Exp = player.exp,
                    Hp = currentHealth
                }
            };

            Debug.Log($"Updated Player Status: name={player.username}, x={transform.position.x}, y={transform.position.x}");

            BaseMessage baseMessage = new BaseMessage
            {
                EventType = "PlayerStateUpdate", // 事件类型
                Payload = ByteString.CopyFrom(update.ToByteArray()) // 序列化 PlayerStateUpdate
            };

            // 发送状态到服务器
            NetworkManager.Instance.SendBaseMessage(baseMessage);

            // 更新最后的位置
            lastPosition = currentPosition;
        }


    }


    private void FixedUpdate()
    {
        if (joystick != null)
        {
            if (!useJoystick)
            {
                float x = Input.GetAxisRaw("Horizontal");
                float y = Input.GetAxisRaw("Vertical");
                direction = new Vector2(x, y);
            }
            else
            {
                direction = useJoystick && joystick != null ? joystick.GetInput() : Vector2.zero;
            }

            transform.Translate(direction * speed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Pickable")
        {
            collision.gameObject.SetActive(false);
            // NetworkManager.Instance.SendItemPickup(itemId);
        }
    }

    public void UseJoystick()
    {
        useJoystick = !useJoystick;
        joystick.gameObject.SetActive(useJoystick);
    }

    // 初始化血条
    public void InitializeHealthBar()
    {
        var healthBar = gameObject.GetComponent<HealthBar>();
        healthBar.Initialize(gameObject.transform, player.username, 100);

    }

    // 攻击
    public void OnAttack()
    {
        anim.SetTrigger("attack");
        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, attackRange);

        foreach (Collider2D target in targets)
        {
            if (target.CompareTag("Enemy"))
            {
                Debug.Log("Attacking target: " + target.name);
                target.GetComponent<PlayerController>()?.TakeDamage(10);
            }
        }
    }

    // 受到伤害
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        // 触发血量变化事件
        OnHealthChanged?.Invoke(this, new HealthChangedEventArgs(name, currentHealth, maxHealth));

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // 恢复血量
    public void OnHeal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(this, new HealthChangedEventArgs(player.username, currentHealth, maxHealth));
    }

    // 玩家死亡
    private void Die()
    {
        Destroy(gameObject);
        Debug.Log($"Player {gameObject.name} is dead.");

    }

    public void UpdateStatus(int level, int experience, float health)
    {
        player.lv = level;
        player.exp = experience;
        player.hp = health;

        Debug.Log($"Updated Player Status: Level={level}, Exp={experience}, HP={health}");
    }

    public void OnAction()
    {
        PlayerStateUpdate update = new PlayerStateUpdate
        {
            Player = new PlayerProto
            {
                Username = player.username,
                X = transform.position.x,
                Y = transform.position.y,
                Lv = player.lv,
                Exp = player.exp,
                Hp = currentHealth
            }
        };

        Debug.Log($"Updated Player Status: name={player.username}, x={transform.position.x}, y={transform.position.x}");

        BaseMessage baseMessage = new BaseMessage
        {
            EventType = "PlayerStateUpdate", // 事件类型
            Payload = ByteString.CopyFrom(update.ToByteArray()) // 序列化 PlayerStateUpdate
        };

        // 发送状态到服务器
        NetworkManager.Instance.SendBaseMessage(baseMessage);
    }
    // 调试
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    private void OnDestroy()
    {
        // 玩家销毁时清理血条
        if (healthBarInstance != null)
        {
            Destroy(healthBarInstance);
        }
    }
}
