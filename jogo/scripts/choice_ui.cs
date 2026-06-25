using Godot;
using System.Text;

// UI de escolha mostrada no fim do diálogo: "Você ajuda a Lara no trabalho?".
// No estilo scrapbook do jogo: um bloquinho de papel (MenuUI) encostado no lado
// DIREITO da tela (não no meio) pra não cobrir a Lara nem a caixa de diálogo.
// - "Ajudar": botão normal. Ao ser clicado, dispara o sinal HelpChosen.
// - "Não ajudar": de propósito, um botão BUGADO/CORROMPIDO. Ele treme, pisca em
//   cores erradas e embaralha o próprio texto, e NÃO faz nada quando clicado.
//   Ou seja: a única saída de verdade é ajudar.
public partial class choice_ui : CanvasLayer
{
	[Signal]
	public delegate void HelpChosenEventHandler();

	// Comprimento do rótulo original do botão quebrado (define o tamanho do glitch).
	private const string RefuseLabel = "Não ajudar";
	// Caracteres usados pra "corromper" o texto do botão.
	private const string GlitchChars = "#@%&$!?*/\\<>~^=+ABCDEF0123456789▓▒░█";

	private FontFile _hand;
	private Button _refuseButton;
	private readonly RandomNumberGenerator _rng = new RandomNumberGenerator();
	private double _glitchClock;

	public override void _Ready()
	{
		_rng.Randomize();
		Layer = 100; // acima de tudo (cenário, personagem e caixa de diálogo).
		_hand = MenuUI.LoadHand();

		// Bloquinho de papel (com lombada/espiral) encostado à direita, centralizado.
		Panel panel = MenuUI.PaperPanel(out VBoxContainer box);
		panel.AnchorLeft = 1f;
		panel.AnchorRight = 1f;
		panel.AnchorTop = 0.5f;
		panel.AnchorBottom = 0.5f;
		panel.GrowHorizontal = Control.GrowDirection.Begin; // cresce pra esquerda.
		panel.GrowVertical = Control.GrowDirection.Both;     // centraliza na vertical.
		panel.OffsetLeft = -360f;
		panel.OffsetRight = -40f;
		panel.OffsetTop = -170f;
		panel.OffsetBottom = 170f;
		AddChild(panel);

		box.Alignment = BoxContainer.AlignmentMode.Center;
		box.AddThemeConstantOverride("separation", 16);

		var prompt = new Label
		{
			Text = "Você ajuda a Lara\nno trabalho?",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		prompt.AddThemeFontOverride("font", _hand);
		prompt.AddThemeFontSizeOverride("font_size", 27);
		prompt.AddThemeColorOverride("font_color", MenuUI.Ink);
		prompt.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		box.AddChild(prompt);
		box.AddChild(new Control { CustomMinimumSize = new Vector2(0, 6) });

		// Botão que funciona.
		var helpButton = MakeButton("Ajudar");
		helpButton.Pressed += OnHelpPressed;
		box.AddChild(helpButton);

		// Botão quebrado (mesmo estilo base; o glitch é aplicado em _Process).
		_refuseButton = MakeButton(RefuseLabel);
		_refuseButton.ClipText = true; // o texto embaralhado não estica o botão.
		// O clique existe, mas de propósito não leva a lugar nenhum: só "piora" o glitch.
		_refuseButton.Pressed += OnRefusePressed;
		box.AddChild(_refuseButton);
	}

	public override void _Process(double delta)
	{
		if (_refuseButton == null)
		{
			return;
		}

		_glitchClock += delta;
		if (_glitchClock < 0.05)
		{
			return;
		}
		_glitchClock = 0;

		// Embaralha o texto mantendo o mesmo comprimento do rótulo original.
		var sb = new StringBuilder(RefuseLabel.Length);
		for (int i = 0; i < RefuseLabel.Length; i++)
		{
			sb.Append(GlitchChars[_rng.RandiRange(0, GlitchChars.Length - 1)]);
		}
		_refuseButton.Text = sb.ToString();

		// Treme (rotação/escala em torno do centro) e pisca a cor: parece corrompido,
		// sem brigar com o layout do painel.
		_refuseButton.PivotOffset = _refuseButton.Size / 2f;
		_refuseButton.Rotation = _rng.RandfRange(-0.06f, 0.06f);
		_refuseButton.Scale = new Vector2(_rng.RandfRange(0.96f, 1.06f), _rng.RandfRange(0.94f, 1.08f));
		_refuseButton.Modulate = new Color(
			_rng.RandfRange(0.6f, 1f),
			_rng.RandfRange(0f, 0.5f),
			_rng.RandfRange(0f, 0.5f),
			_rng.RandfRange(0.7f, 1f));
	}

	private void OnHelpPressed()
	{
		Audio.Instance?.Click();
		EmitSignal(SignalName.HelpChosen);
	}

	private void OnRefusePressed()
	{
		// Botão quebrado: dá um "tranco" visual e ignora o clique de propósito.
		_refuseButton.Modulate = new Color(1f, 0f, 0f);
		_glitchClock = 1; // força um novo glitch já no próximo frame.
	}

	// Botão "cartão de papel": creme com borda, e no hover acende a barra roxa do jogo.
	private Button MakeButton(string text)
	{
		var btn = new Button
		{
			Text = text,
			CustomMinimumSize = new Vector2(0, 52),
			FocusMode = Control.FocusModeEnum.None, // só responde ao mouse, não ao teclado.
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		btn.AddThemeFontOverride("font", _hand);
		btn.AddThemeFontSizeOverride("font_size", 28);
		btn.AddThemeColorOverride("font_color", MenuUI.Ink);
		btn.AddThemeColorOverride("font_hover_color", MenuUI.Ink);
		btn.AddThemeColorOverride("font_pressed_color", MenuUI.Ink);
		btn.AddThemeStyleboxOverride("normal", CardStyle(new Color(0.99f, 0.98f, 0.94f), MenuUI.PaperEdge));
		btn.AddThemeStyleboxOverride("hover", CardStyle(new Color(0.608f, 0.392f, 0.910f, 0.55f), MenuUI.Accent));
		btn.AddThemeStyleboxOverride("pressed", CardStyle(new Color(0.608f, 0.392f, 0.910f, 0.72f), MenuUI.Accent));
		return btn;
	}

	private static StyleBoxFlat CardStyle(Color bg, Color border)
	{
		var sb = new StyleBoxFlat { BgColor = bg, BorderColor = border };
		sb.SetCornerRadiusAll(10);
		sb.SetBorderWidthAll(2);
		sb.SetContentMarginAll(10);
		return sb;
	}
}
