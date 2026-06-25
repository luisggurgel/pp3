using Godot;

namespace PP3.Minigame;

public partial class GameState : RefCounted
{
    // --- Configurable Constants ---
    public const float MaxHealth = 100f;
    public const float WrongActionDamage = 10f;
    public const float MissDamage = 5f;
    public const int ComboThreshold = 5;
    public const int MaxRecoveryCombo = 10;
    public const float RecoveryPerComboLevel = 0.1f; // percentage per combo level

    // --- State ---
    public float Health { get; private set; } = MaxHealth;
    public int Combo { get; private set; } = 0;
    public bool IsComboActive => Combo >= ComboThreshold;
    public bool IsGameOver => Health <= 0f;

    public int TotalCorrect { get; private set; } = 0;
    public int TotalWrong { get; private set; } = 0;
    public int TotalMiss { get; private set; } = 0;

    // --- Events (C# events, not Godot signals since this is RefCounted) ---
    public event System.Action<float> HealthChanged;
    public event System.Action<int> ComboChanged;
    public event System.Action GameOverTriggered;

    public void RegisterCorrectAction()
    {
        TotalCorrect++;
        Combo++;

        // Health recovery during combo: 5x=0.5%, 6x=0.6%, ..., 10x+=1.0% (capped)
        if (Combo >= ComboThreshold)
        {
            float comboLevel = Mathf.Min(Combo, MaxRecoveryCombo);
            float recovery = comboLevel * RecoveryPerComboLevel;
            Health = Mathf.Min(Health + recovery, MaxHealth);
        }

        HealthChanged?.Invoke(Health);
        ComboChanged?.Invoke(Combo);
    }

    public void RegisterWrongAction()
    {
        TotalWrong++;
        Combo = 0;
        Health = Mathf.Max(Health - WrongActionDamage, 0f);

        HealthChanged?.Invoke(Health);
        ComboChanged?.Invoke(Combo);

        if (IsGameOver)
            GameOverTriggered?.Invoke();
    }

    public void RegisterMiss()
    {
        TotalMiss++;
        Combo = 0;
        Health = Mathf.Max(Health - MissDamage, 0f);

        HealthChanged?.Invoke(Health);
        ComboChanged?.Invoke(Combo);

        if (IsGameOver)
            GameOverTriggered?.Invoke();
    }

    public void Reset()
    {
        Health = MaxHealth;
        Combo = 0;
        TotalCorrect = 0;
        TotalWrong = 0;
        TotalMiss = 0;

        HealthChanged?.Invoke(Health);
        ComboChanged?.Invoke(Combo);
    }
}
