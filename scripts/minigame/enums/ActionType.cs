using Godot;

namespace PP3.Minigame;

public enum ActionType
{
    Bake,     // Key A - for Bread
    Cut,      // Key S - for Cookie
    Confetti  // Key D - for Donut
}

public static class ActionTypeExtensions
{
    public static Key ToKey(this ActionType action) => action switch
    {
        ActionType.Bake => Key.A,
        ActionType.Cut => Key.S,
        ActionType.Confetti => Key.D,
        _ => throw new System.ArgumentOutOfRangeException(nameof(action))
    };

    public static ActionType GetCorrectAction(this FoodType food) => food switch
    {
        FoodType.Bread => ActionType.Bake,
        FoodType.Cookie => ActionType.Cut,
        FoodType.Donut => ActionType.Confetti,
        _ => throw new System.ArgumentOutOfRangeException(nameof(food))
    };

    public static ActionType? FromKey(Key key) => key switch
    {
        Key.A => ActionType.Bake,
        Key.S => ActionType.Cut,
        Key.D => ActionType.Confetti,
        _ => null
    };
}
