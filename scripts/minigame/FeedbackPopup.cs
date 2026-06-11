using Godot;

namespace PP3.Minigame;

public enum FeedbackType
{
    Success,
    WrongAction,
    Miss,
    Combo
}

public partial class FeedbackPopup : Node2D
{
    private Sprite2D _sprite;

    private const float AnimationDuration = 0.6f;
    private const float ScaleStart = 0.5f;
    private const float ScaleOvershoot = 1.2f;
    private const float ScaleFinal = 1.0f;

    public void Initialize(FeedbackType type, Vector2 position)
    {
        Position = position;

        string basePath = "res://assets/sprites/feedback/";
        string filename = type switch
        {
            FeedbackType.Success => "success.png",
            FeedbackType.WrongAction => "wrong_action.png",
            FeedbackType.Miss => "miss.png",
            FeedbackType.Combo => "combo.png",
            _ => ""
        };

        string path = basePath + filename;
        CallDeferred(nameof(LoadTexture), path);
    }

    private void LoadTexture(string path)
    {
        if (_sprite != null && ResourceLoader.Exists(path))
            _sprite.Texture = GD.Load<Texture2D>(path);
    }

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite");
        Scale = Vector2.One * ScaleStart;
        PlayAnimation();
    }

    private void PlayAnimation()
    {
        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Back);

        // Scale up with overshoot, then settle
        tween.TweenProperty(this, "scale", Vector2.One * ScaleOvershoot, AnimationDuration * 0.4f);
        tween.TweenProperty(this, "scale", Vector2.One * ScaleFinal, AnimationDuration * 0.2f);

        // Hold briefly, then fade out
        tween.TweenInterval(0.3f);
        tween.TweenProperty(this, "modulate:a", 0.0f, AnimationDuration * 0.4f);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
