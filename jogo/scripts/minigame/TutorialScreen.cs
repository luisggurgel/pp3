using Godot;

namespace PP3.Minigame;

// Tela "Como Jogar" do minigame, no estilo scrapbook do jogo (papel + caneta).
// Aparece sobre o minigame com um véu escuro, faz fade-in e some a qualquer tecla.
public partial class TutorialScreen : CanvasLayer
{
	[Signal]
	public delegate void TutorialDismissedEventHandler();

	private Control _root;
	private bool _canDismiss = false;

	private const float FadeInDuration = 0.4f;
	private const float FadeOutDuration = 0.4f;

	public override void _Ready()
	{
		Layer = 10;
		FontFile hand = MenuUI.LoadHand();

		_root = new Control();
		MenuUI.Fill(_root);
		AddChild(_root);

		// Véu escuro (o minigame fica levemente visível atrás).
		var veil = new ColorRect { Color = new Color(0.07f, 0.05f, 0.09f, 0.82f) };
		MenuUI.Fill(veil);
		_root.AddChild(veil);

		// Painel de papel centralizado.
		Panel panel = MenuUI.PaperPanel(out VBoxContainer vbox);
		MenuUI.PlaceBox(panel, 0.24f, 0.12f, 0.76f, 0.88f);
		_root.AddChild(panel);
		vbox.Alignment = BoxContainer.AlignmentMode.Center;
		vbox.AddThemeConstantOverride("separation", 16);

		vbox.AddChild(Heading(hand, "COMO JOGAR", 56, MenuUI.Accent));
		vbox.AddChild(Spacer(8));
		vbox.AddChild(Rule(hand, "A", "Assar o Pão"));
		vbox.AddChild(Rule(hand, "S", "Cortar os Cookies"));
		vbox.AddChild(Rule(hand, "D", "Confete nos Donuts"));
		vbox.AddChild(Spacer(6));
		vbox.AddChild(Hint(hand, "Passe o mouse sobre a comida e aperte a tecla exata!"));
		vbox.AddChild(Spacer(10));

		Label start = Heading(hand, "Pressione qualquer tecla para começar", 28, MenuUI.Accent);
		vbox.AddChild(start);
		Blink(start);

		// Fade-in do conjunto.
		_root.Modulate = new Color(1, 1, 1, 0);
		var tween = CreateTween();
		tween.TweenProperty(_root, "modulate:a", 1.0f, FadeInDuration);
		tween.TweenCallback(Callable.From(() => _canDismiss = true));
	}

	public override void _Input(InputEvent @event)
	{
		if (!_canDismiss) return;

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			_canDismiss = false; // Prevent double dismiss
			Audio.Instance?.Click();

			var tween = CreateTween();
			tween.TweenProperty(_root, "modulate:a", 0.0f, FadeOutDuration);
			tween.TweenCallback(Callable.From(() =>
			{
				EmitSignal(SignalName.TutorialDismissed);
				QueueFree();
			}));
		}
	}

	// ---------- helpers de estilo ----------

	private static Label Heading(FontFile hand, string text, int size, Color color)
	{
		var l = new Label
		{
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		l.AddThemeFontOverride("font", hand);
		l.AddThemeFontSizeOverride("font_size", size);
		l.AddThemeColorOverride("font_color", color);
		return l;
	}

	private static Control Hint(FontFile hand, string text)
	{
		var l = new Label
		{
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		l.AddThemeFontOverride("font", hand);
		l.AddThemeFontSizeOverride("font_size", 22);
		l.AddThemeColorOverride("font_color", new Color(MenuUI.Ink.R, MenuUI.Ink.G, MenuUI.Ink.B, 0.65f));
		return l;
	}

	// Linha de regra: "tecla" estilizada (cápsula roxa) + descrição à mão.
	private static Control Rule(FontFile hand, string key, string desc)
	{
		var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		row.AddThemeConstantOverride("separation", 16);
		row.AddChild(KeyCap(hand, key));

		var d = new Label { Text = desc, VerticalAlignment = VerticalAlignment.Center };
		d.AddThemeFontOverride("font", hand);
		d.AddThemeFontSizeOverride("font_size", 34);
		d.AddThemeColorOverride("font_color", MenuUI.Ink);
		row.AddChild(d);
		return row;
	}

	private static Control KeyCap(FontFile hand, string key)
	{
		var cap = new Panel { CustomMinimumSize = new Vector2(54, 54) };
		var sb = new StyleBoxFlat { BgColor = new Color(0.608f, 0.392f, 0.910f, 0.18f), BorderColor = MenuUI.Accent };
		sb.SetCornerRadiusAll(10);
		sb.SetBorderWidthAll(3);
		cap.AddThemeStyleboxOverride("panel", sb);

		var l = new Label
		{
			Text = key,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
		};
		MenuUI.Fill(l);
		l.AddThemeFontOverride("font", hand);
		l.AddThemeFontSizeOverride("font_size", 32);
		l.AddThemeColorOverride("font_color", MenuUI.Accent);
		cap.AddChild(l);
		return cap;
	}

	private static Control Spacer(float h) => new Control { CustomMinimumSize = new Vector2(0, h) };

	private void Blink(Control node)
	{
		var t = CreateTween().SetLoops();
		t.TweenProperty(node, "modulate:a", 0.35f, 0.6).SetTrans(Tween.TransitionType.Sine);
		t.TweenProperty(node, "modulate:a", 1.0f, 0.6).SetTrans(Tween.TransitionType.Sine);
	}
}
