using Godot;

namespace PP3.Minigame;

public partial class PauseMenu : CanvasLayer
{
    [Signal]
    public delegate void ResumeRequestedEventHandler();

    [Signal]
    public delegate void RetryRequestedEventHandler();

    [Signal]
    public delegate void MainMenuRequestedEventHandler();

    private Button _retryButton;
    private Button _mainMenuButton;
    private Button _resumeButton;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _resumeButton = GetNode<Button>("VBoxContainer/ResumeButton");
        _retryButton = GetNode<Button>("VBoxContainer/RetryButton");
        _mainMenuButton = GetNode<Button>("VBoxContainer/MainMenuButton");

        _resumeButton.Pressed += OnResumePressed;
        _retryButton.Pressed += OnRetryPressed;
        _mainMenuButton.Pressed += OnMainMenuPressed;
    }

    private void OnResumePressed()
    {
        EmitSignal(SignalName.ResumeRequested);
        QueueFree();
    }

    private void OnRetryPressed()
    {
        EmitSignal(SignalName.RetryRequested);
        QueueFree();
    }

    private void OnMainMenuPressed()
    {
        EmitSignal(SignalName.MainMenuRequested);
        QueueFree();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.Escape)
        {
            OnResumePressed();
            GetViewport().SetInputAsHandled();
        }
    }
}
