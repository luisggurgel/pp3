using Godot;

// Cartão de título de "dia" no estilo abertura do Smiling Friends: uma tela preta
// cobrindo tudo e o nome do dia (ex.: "DIA 2") no centro. Aparece, segura um
// instante e some, revelando a cena nova por trás.
//
// Uso:
//   var card = new day_title_card { DayText = "DIA 2" };
//   card.Blackout += () => { /* troca o conteúdo enquanto está 100% preto */ };
//   card.Finished += () => { /* opcional: liberar input, etc. */ };
//   AddChild(card);
public partial class day_title_card : CanvasLayer
{
	// Emitido quando a tela está 100% preta: hora de trocar o que está por trás
	// (cenário/personagens/roteiro) sem o jogador ver o "corte".
	[Signal]
	public delegate void BlackoutEventHandler();

	// Emitido ao terminar a animação (o cartão já se removeu da árvore).
	[Signal]
	public delegate void FinishedEventHandler();

	// Texto exibido (defina antes de AddChild).
	public string DayText = "DIA";

	// Quando true (transição entre cenas), a tela escurece a partir do que está na
	// tela. Quando false (intro logo na abertura), já começa 100% preta — assim não
	// há um "flash" da cena antes de escurecer.
	public bool FadeIn = true;

	public override void _Ready()
	{
		Layer = 150; // acima do diálogo.
		ProcessMode = ProcessModeEnum.Always;

		var black = new ColorRect { Color = Colors.Black, Modulate = new Color(1, 1, 1, FadeIn ? 0f : 1f) };
		MenuUI.Fill(black);
		AddChild(black);

		var label = new Label
		{
			Text = DayText,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Modulate = new Color(1, 1, 1, 0),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		MenuUI.Fill(label);
		label.AddThemeFontOverride("font", MenuUI.LoadHand());
		label.AddThemeFontSizeOverride("font_size", 130);
		label.AddThemeColorOverride("font_color", Colors.White);
		AddChild(label);

		var t = CreateTween();
		if (FadeIn)
		{
			t.TweenProperty(black, "modulate:a", 1f, 0.4);                     // escurece a tela
		}
		t.TweenCallback(Callable.From(() => EmitSignal(SignalName.Blackout)));  // troca o conteúdo atrás
		t.TweenProperty(label, "modulate:a", 1f, 0.35);                         // "DIA 2" aparece
		t.TweenInterval(1.5);                                                   // segura
		t.TweenProperty(label, "modulate:a", 0f, 0.35);                         // o texto some
		t.TweenProperty(black, "modulate:a", 0f, 0.6);                          // revela a cena nova
		t.TweenCallback(Callable.From(() =>
		{
			EmitSignal(SignalName.Finished);
			QueueFree();
		}));
	}
}
