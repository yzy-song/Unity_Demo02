public class HealthChangedEventArgs : System.EventArgs
{
    public string PlayerName { get; private set; }
    public float CurrentHealth { get; private set; }
    public float MaxHealth { get; private set; }

    public HealthChangedEventArgs(string playerName, float currentHealth, float maxHealth)
    {
        PlayerName = playerName;
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
    }
}
