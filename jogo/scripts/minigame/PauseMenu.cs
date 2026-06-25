using Godot;

namespace PP3.Minigame;

// Pausa do minigame, no estilo scrapbook do jogo (papel + caneta).
// Véu escuro + painel de papel com "PAUSADO" e os botões à mão.
public partial class PauseMenu : CanvasLayer
{
	[Signal]
	public delegate void ResumeRequestedEventHandler();

	[Signal]
	public delegate void RetryRequestedEventHandler();

	[Signal]
	public delegate void MainMenuRequestedEventHandler();

	public override void _Ready()
	{
		Layer = 100;
		ProcessMode = ProcessModeEnum.Always; // funciona com a árvore pausada.
		FontFile hand = MenuUI.LoadHand();

		var veil = new ColorRect { Color = new Color(0.05f, 0.04f, 0.07f, 0.6f) };
		MenuUI.Fill(veil);
		AddChild(veil);

		Panel panel = MenuUI.PaperPanel(out VBoxContainer vbox);
		MenuUI.PlaceBox(panel, 0.33f, 0.2f, 0.67f, 0.8f);
		AddChild(panel);
		vbox.Alignment = BoxContainer.AlignmentMode.Center;
		vbox.AddThemeConstantOverride("separation", 12);

		var title = new Label { Text = "PAUSADO", HorizontalAlignment = HorizontalAlignment.Center };
		title.AddThemeFontOverride("font", hand);
		title.AddThemeFontSizeOverride("font_size", 60);
		title.AddThemeColorOverride("font_color", MenuUI.Accent);
		title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		vbox.AddChild(title);
		vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 18) });

		vbox.AddChild(MenuUI.MenuButton(hand, "Continuar", OnResumePressed, primary: true));
		vbox.AddChild(MenuUI.MenuButton(hand, "Tentar novamente", OnRetryPressed));
		vbox.AddChild(MenuUI.MenuButton(hand, "Menu principal", OnMainMenuPressed));
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
