using Godot;

// Tela inicial ("O Aniversário") — fiel ao JOGAR.png, mas com BOTÕES DE VERDADE:
// o fundo é a imagem do design com o texto do menu apagado (screen_menu_blank.png),
// e os itens são botões reais (fonte à mão) por cima, com a barra roxa de realce.
// Posições em px nativos da imagem (3051x1716); um overlay escala junto com a arte
// e uma "lista" rotacionada aplica a inclinação do papel (~-4,9°).
public partial class main_menu : Control
{
	// Resolução nativa da imagem do design (px) — base para posicionar os botões.
	private static readonly Vector2 Native = new(3051, 1716);

	private FontFile _hand;
	private Label _toast;
	private Tween _toastTween;
	private Control _ui;
	private Control _frame;

	public override void _Ready()
	{
		AnchorRight = 1f;
		AnchorBottom = 1f;
		_hand = MenuUI.LoadHand();

		Audio.Instance?.PlayMusic(Audio.Track.Menu);

		// Fundo = imagem do design com o texto apagado.
		Control root = MenuUI.DesignScreen("screen_menu_blank.png", out _frame);
		AddChild(root);

		// Overlay em coordenadas nativas, escalado junto com a imagem.
		_ui = new Control { Size = Native, MouseFilter = MouseFilterEnum.Ignore };
		_frame.AddChild(_ui);
		_frame.Resized += RescaleUi;
		RescaleUi();

		// Lista inclinada (gira em torno do centro do bloco para casar com o papel).
		var list = new Control
		{
			Size = Native,
			MouseFilter = MouseFilterEnum.Ignore,
			PivotOffset = new Vector2(560, 820),
			RotationDegrees = -4.9f,
		};
		_ui.AddChild(list);

		// Botões reais (posição = canto sup-esq do texto, em px nativos).
		MenuUI.TextMenuItem(list, _hand, new Vector2(430, 500), "JOGAR", 60, true, OnPlayPressed, clickSfx: false);
		MenuUI.TextMenuItem(list, _hand, new Vector2(335, 624), "CONFIGURAÇÕES", 58, false, () => AddChild(new configuracoes()));
		MenuUI.TextMenuItem(list, _hand, new Vector2(340, 730), "GALERIA", 58, false, () => AddChild(new galeria()));
		MenuUI.TextMenuItem(list, _hand, new Vector2(345, 814), "CRÉDITOS", 58, false, () => Toast("Créditos — em breve"));
		MenuUI.TextMenuItem(list, _hand, new Vector2(352, 1118), "Sair :c", 48, false, OnQuitPressed);

		_toast = MenuUI.MakeToast(_hand);
		MenuUI.PlaceBox(_toast, 0.3f, 0.86f, 0.97f, 0.94f);
		_frame.AddChild(_toast);
	}

	private void RescaleUi()
	{
		if (_frame.Size.X > 0 && _frame.Size.Y > 0)
		{
			_ui.Scale = _frame.Size / Native;
		}
	}

	private void Toast(string text) => MenuUI.RunToast(this, _toast, text, ref _toastTween);

	private void OnPlayPressed()
	{
		Audio.Instance?.Play(Audio.Sfx.Start); // som do botão "Start".
		// JOGAR -> tela de carregando -> diálogo (que encadeia o minigame ao "Ajudar").
		carregando.NextScene = "res://scenes/main_scene.tscn";
		GetTree().ChangeSceneToFile("res://scenes/carregando.tscn");
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}

	public override void _ExitTree()
	{
		_toastTween?.Kill();
	}
}
