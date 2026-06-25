using Godot;

namespace PP3.Minigame;

// Tela de fim do minigame (vitória / "tentar de novo"), no estilo scrapbook do jogo.
// Véu escuro + painel de papel com o título, o placar e a dica para reiniciar.
public partial class GameOverScreen : CanvasLayer
{
	[Signal]
	public delegate void RestartRequestedEventHandler();

	private bool _canRestart = false;
	private Control _root;

	public void Initialize(bool isVictory, int correct, int wrong, int missed)
	{
		Layer = 20;
		FontFile hand = MenuUI.LoadHand();

		_root = new Control();
		MenuUI.Fill(_root);
		AddChild(_root);

		var veil = new ColorRect { Color = new Color(0.05f, 0.04f, 0.07f, 0.8f) };
		MenuUI.Fill(veil);
		_root.AddChild(veil);

		Panel panel = MenuUI.PaperPanel(out VBoxContainer vbox);
		MenuUI.PlaceBox(panel, 0.3f, 0.16f, 0.7f, 0.84f);
		_root.AddChild(panel);
		vbox.Alignment = BoxContainer.AlignmentMode.Center;
		vbox.AddThemeConstantOverride("separation", 18);

		Color titleColor = isVictory ? new Color(0.33f, 0.55f, 0.27f) : new Color(0.72f, 0.27f, 0.27f);
		var title = new Label
		{
			Text = isVictory ? "VITÓRIA!" : "GAME OVER",
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		title.AddThemeFontOverride("font", hand);
		title.AddThemeFontSizeOverride("font_size", 72);
		title.AddThemeColorOverride("font_color", titleColor);
		vbox.AddChild(title);
		vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });

		vbox.AddChild(Stat(hand, $"Acertos:  {correct}"));
		vbox.AddChild(Stat(hand, $"Erros:  {wrong}"));
		vbox.AddChild(Stat(hand, $"Perdidos:  {missed}"));
		vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 12) });

		var hint = new Label
		{
			Text = "Pressione ESPAÇO para reiniciar",
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		hint.AddThemeFontOverride("font", hand);
		hint.AddThemeFontSizeOverride("font_size", 28);
		hint.AddThemeColorOverride("font_color", MenuUI.Accent);
		vbox.AddChild(hint);
		var blink = CreateTween().SetLoops();
		blink.TweenProperty(hint, "modulate:a", 0.35f, 0.6).SetTrans(Tween.TransitionType.Sine);
		blink.TweenProperty(hint, "modulate:a", 1.0f, 0.6).SetTrans(Tween.TransitionType.Sine);

		// Fade in.
		_root.Modulate = new Color(1, 1, 1, 0);
		var tween = CreateTween();
		tween.TweenProperty(_root, "modulate:a", 1.0f, 0.5f);
		tween.TweenCallback(Callable.From(() => _canRestart = true));
	}

	private static Label Stat(FontFile hand, string text)
	{
		var l = new Label
		{
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		l.AddThemeFontOverride("font", hand);
		l.AddThemeFontSizeOverride("font_size", 36);
		l.AddThemeColorOverride("font_color", MenuUI.Ink);
		return l;
	}

	public override void _Input(InputEvent @event)
	{
		if (!_canRestart) return;

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Space)
		{
			_canRestart = false;
			Audio.Instance?.Click();
			var tween = CreateTween();
			tween.TweenProperty(_root, "modulate:a", 0.0f, 0.3f);
			tween.TweenCallback(Callable.From(() =>
			{
				EmitSignal(SignalName.RestartRequested);
				QueueFree();
			}));
		}
	}
}
