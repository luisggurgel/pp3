using Godot;

namespace PP3.Minigame;

public partial class GameOverScreen : CanvasLayer
{
    [Signal]
    public delegate void RestartRequestedEventHandler();

    private bool _canRestart = false;
    private ColorRect _background;
    private Label _titleLabel;
    private Label _statsLabel;
    private VBoxContainer _vboxContainer;

    public void Initialize(bool isVictory, int correct, int wrong, int missed)
    {
        _background = GetNode<ColorRect>("Background");
        _vboxContainer = GetNode<VBoxContainer>("VBoxContainer");
        _titleLabel = GetNode<Label>("VBoxContainer/TitleLabel");
        _statsLabel = GetNode<Label>("VBoxContainer/StatsLabel");

        _titleLabel.Text = isVictory ? "VITÓRIA!" : "GAME OVER";
        _titleLabel.AddThemeColorOverride("font_color", isVictory ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.2f, 0.2f));

        _statsLabel.Text = $"Acertos: {correct}\nErros: {wrong}\nPerdidos: {missed}";

        // Fade in
        _background.Modulate = new Color(1, 1, 1, 0);
        _vboxContainer.Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(_background, "modulate:a", 1.0f, 0.5f);
        tween.TweenProperty(_vboxContainer, "modulate:a", 1.0f, 0.5f);
        tween.Chain().TweenCallback(Callable.From(() => _canRestart = true));
    }

    public override void _Input(InputEvent @event)
    {
        if (!_canRestart) return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Space)
        {
            _canRestart = false;
            var tween = CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(_background, "modulate:a", 0.0f, 0.3f);
            tween.TweenProperty(_vboxContainer, "modulate:a", 0.0f, 0.3f);
            tween.Chain().TweenCallback(Callable.From(() =>
            {
                EmitSignal(SignalName.RestartRequested);
                QueueFree();
            }));
        }
    }
}
