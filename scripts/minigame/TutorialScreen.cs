using Godot;

namespace PP3.Minigame;

public partial class TutorialScreen : CanvasLayer
{
    [Signal]
    public delegate void TutorialDismissedEventHandler();

    private Control _background;
    private bool _canDismiss = false;

    private const float FadeInDuration = 0.5f;
    private const float FadeOutDuration = 0.5f;

    public override void _Ready()
    {
        _background = GetNode<Control>("Background");

        // Start invisible and fade in
        _background.Modulate = new Color(1, 1, 1, 0);

        var tween = CreateTween();
        tween.TweenProperty(_background, "modulate:a", 1.0f, FadeInDuration);
        tween.TweenCallback(Callable.From(() => _canDismiss = true));
    }

    public override void _Input(InputEvent @event)
    {
        if (!_canDismiss) return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            _canDismiss = false; // Prevent double dismiss

            var tween = CreateTween();
            tween.TweenProperty(_background, "modulate:a", 0.0f, FadeOutDuration);
            tween.TweenCallback(Callable.From(() =>
            {
                EmitSignal(SignalName.TutorialDismissed);
                QueueFree();
            }));
        }
    }
}
