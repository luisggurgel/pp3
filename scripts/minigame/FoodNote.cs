using Godot;
using System.Collections.Generic;

namespace PP3.Minigame;

public partial class FoodNote : Area2D
{
    [Signal]
    public delegate void CorrectActionEventHandler(FoodNote note);
    [Signal]
    public delegate void WrongActionEventHandler(FoodNote note);
    [Signal]
    public delegate void MissedEventHandler(FoodNote note);

    public FoodType FoodType { get; private set; }
    public bool IsMouseOver { get; private set; } = false;
    public bool IsResolved { get; private set; } = false;

    private Sprite2D _foodSprite;
    private Timer _timeoutTimer;
    private Texture2D _normalTexture;
    private float _pendingTimeout = 3.0f;
    
    private Tween _warningTween;
    private bool _hasStartedWarning = false;

    // All 3 action result textures for this food (baked, cut, confetti)
    private readonly Dictionary<ActionType, Texture2D> _actionTextures = new();

    private const float FadeOutDuration = 0.3f;

    public void Initialize(FoodType type, Vector2 position, float timeout)
    {
        FoodType = type;
        Position = position;

        string basePath = "res://assets/sprites/foods/";
        string foodName = type switch
        {
            FoodType.Bread => "bread",
            FoodType.Cookie => "cookie",
            FoodType.Donut => "donut",
            _ => ""
        };

        // Load normal texture
        string normalPath = $"{basePath}{foodName}_normal.png";
        _normalTexture = ResourceLoader.Exists(normalPath)
            ? GD.Load<Texture2D>(normalPath)
            : null;

        // Load all 3 action result textures
        var actionFiles = new Dictionary<ActionType, string>
        {
            { ActionType.Bake, $"{basePath}{foodName}_baked.png" },
            { ActionType.Cut, $"{basePath}{foodName}_cut.png" },
            { ActionType.Confetti, $"{basePath}{foodName}_confetti.png" }
        };

        foreach (var (action, path) in actionFiles)
        {
            if (ResourceLoader.Exists(path))
                _actionTextures[action] = GD.Load<Texture2D>(path);
        }

        _pendingTimeout = timeout;
    }

    public override void _Ready()
    {
        _foodSprite = GetNode<Sprite2D>("FoodSprite");
        _timeoutTimer = GetNode<Timer>("TimeoutTimer");

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        _timeoutTimer.Timeout += OnTimeout;

        if (_normalTexture != null)
            _foodSprite.Texture = _normalTexture;

        _timeoutTimer.WaitTime = _pendingTimeout;
        _timeoutTimer.OneShot = true;
        _timeoutTimer.Start();

        InputPickable = true;

        // Spawn animation (Scale from 0 to 1.5x)
        Scale = Vector2.Zero;
        var tween = CreateTween();
        tween.TweenProperty(this, "scale", new Vector2(1.5f, 1.5f), 0.4f)
            .SetTrans(Tween.TransitionType.Bounce)
            .SetEase(Tween.EaseType.Out);
    }

    public override void _Process(double delta)
    {
        if (IsResolved || _hasStartedWarning) return;

        if (_timeoutTimer != null && !_timeoutTimer.IsStopped())
        {
            if (_timeoutTimer.TimeLeft <= 0.8)
            {
                _hasStartedWarning = true;
                StartWarningAnimation();
            }
        }
    }

    private void StartWarningAnimation()
    {
        _warningTween = CreateTween();
        _warningTween.SetLoops(); // Loop infinitely
        _warningTween.TweenProperty(_foodSprite, "modulate", new Color(1.0f, 0.4f, 0.4f, 0.5f), 0.15f);
        _warningTween.TweenProperty(_foodSprite, "modulate", new Color(1.0f, 1.0f, 1.0f, 1.0f), 0.15f);
    }

    private void StopWarningAnimation()
    {
        if (_warningTween != null && _warningTween.IsValid())
        {
            _warningTween.Kill();
        }
        _foodSprite.Modulate = new Color(1, 1, 1, 1);
    }

    public float GetTimeRemaining()
    {
        if (_timeoutTimer == null || _timeoutTimer.IsStopped())
            return 0f;
        return (float)_timeoutTimer.TimeLeft;
    }

    /// <summary>
    /// Called when the player performs the CORRECT action.
    /// Shows the correct result sprite (e.g., bread_baked for bread + Bake).
    /// </summary>
    public void ResolveCorrect()
    {
        if (IsResolved) return;
        IsResolved = true;
        _timeoutTimer.Stop();

        ActionType correctAction = FoodType.GetCorrectAction();
        if (_actionTextures.TryGetValue(correctAction, out var tex))
            _foodSprite.Texture = tex;

        EmitSignal(SignalName.CorrectAction, this);
        StartFadeOut();
    }

    /// <summary>
    /// Called when the player performs the WRONG action.
    /// Shows the result of the wrong action (e.g., bread_cut if player pressed S on bread).
    /// </summary>
    public void ResolveWrong(ActionType attemptedAction)
    {
        if (IsResolved) return;
        IsResolved = true;
        _timeoutTimer.Stop();

        if (_actionTextures.TryGetValue(attemptedAction, out var tex))
            _foodSprite.Texture = tex;

        EmitSignal(SignalName.WrongAction, this);
        StartFadeOut();
    }

    private void OnMouseEntered()
    {
        IsMouseOver = true;
    }

    private void OnMouseExited()
    {
        IsMouseOver = false;
    }

    private void OnTimeout()
    {
        if (IsResolved) return;
        IsResolved = true;
        EmitSignal(SignalName.Missed, this);
        StartFadeOut();
    }

    private void StartFadeOut()
    {
        StopWarningAnimation();
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, FadeOutDuration);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
