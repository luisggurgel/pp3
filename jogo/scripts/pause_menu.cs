using Godot;

// Overlay de pausa ("PAUSADO") TRANSPARENTE: escurece levemente o jogo (que continua
// visível por trás) e mostra só o papel + título (recortados do design, sem o céu),
// com BOTÕES DE VERDADE por cima (fonte à mão, barra roxa de realce).
// Pausa a árvore do jogo. Instanciar com: AddChild(new pause_menu()).
public partial class pause_menu : CanvasLayer
{
	private static readonly Vector2 Native = new(3058, 1717);

	private FontFile _hand;
	private Label _toast;
	private Tween _toastTween;
	private Control _ui;
	private Control _frame;
	private bool _closing;

	public override void _Ready()
	{
		Layer = 128;
		ProcessMode = ProcessModeEnum.Always;
		_hand = MenuUI.LoadHand();
		GetTree().Paused = true;

		// Escurece o jogo (semi-transparente: o jogo continua visível) e bloqueia cliques.
		var dim = new ColorRect { Color = new Color(0.04f, 0.03f, 0.06f, 0.55f) };
		MenuUI.Fill(dim);
		AddChild(dim);

		// Papel + título (imagem transparente, sem céu), na proporção da arte.
		var arc = new AspectRatioContainer
		{
			Ratio = Native.X / Native.Y,
			StretchMode = AspectRatioContainer.StretchModeEnum.Fit,
		};
		MenuUI.Fill(arc);
		AddChild(arc);
		_frame = new Control();
		arc.AddChild(_frame);
		var img = new TextureRect
		{
			Texture = GD.Load<Texture2D>(MenuUI.Dir + "screen_pause_blank.png"),
			StretchMode = TextureRect.StretchModeEnum.Scale,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		MenuUI.Fill(img);
		_frame.AddChild(img);

		// Overlay nativo (escala junto com a arte).
		_ui = new Control { Size = Native, MouseFilter = Control.MouseFilterEnum.Ignore };
		_frame.AddChild(_ui);
		_frame.Resized += RescaleUi;
		RescaleUi();

		// Lista inclinada (~-3°, igual ao papel).
		var list = new Control
		{
			Size = Native,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			PivotOffset = new Vector2(1529, 880),
			RotationDegrees = -3f,
		};
		_ui.AddChild(list);

		MenuUI.TextMenuItem(list, _hand, new Vector2(1335, 492), "CONTINUAR", 60, true, Resume);
		MenuUI.TextMenuItem(list, _hand, new Vector2(1245, 612), "CONFIGURAÇÕES", 56, false, () => AddChild(new configuracoes()));
		MenuUI.TextMenuItem(list, _hand, new Vector2(1245, 724), "GALERIA", 58, false, () => AddChild(new galeria()));
		MenuUI.TextMenuItem(list, _hand, new Vector2(1245, 806), "CRÉDITOS", 58, false, () => Toast("Créditos — em breve"));
		MenuUI.TextMenuItem(list, _hand, new Vector2(1300, 1248), "IR PARA O MENU", 52, false, GoToMenu);

		// "Tirar foto" no canto inferior direito (creme com contorno, fora do papel).
		AddPhotoButton();

		_toast = MenuUI.MakeToast(_hand);
		MenuUI.PlaceBox(_toast, 0.2f, 0.82f, 0.8f, 0.9f);
		_frame.AddChild(_toast);
	}

	private void RescaleUi()
	{
		if (_frame.Size.X > 0 && _frame.Size.Y > 0)
		{
			_ui.Scale = _frame.Size / Native;
		}
	}

	private void AddPhotoButton()
	{
		var photo = new Button
		{
			Text = "Tirar foto  [F]",
			Flat = true,
			FocusMode = Control.FocusModeEnum.None,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
			Position = new Vector2(2330, 1560),
		};
		photo.AddThemeFontOverride("font", _hand);
		photo.AddThemeFontSizeOverride("font_size", 44);
		photo.AddThemeColorOverride("font_color", MenuUI.Paper);
		photo.AddThemeColorOverride("font_hover_color", new Color(1, 1, 1));
		photo.AddThemeColorOverride("font_pressed_color", MenuUI.Paper);
		photo.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.75f));
		photo.AddThemeConstantOverride("outline_size", 6);
		var empty = new StyleBoxEmpty();
		photo.AddThemeStyleboxOverride("normal", empty);
		photo.AddThemeStyleboxOverride("hover", empty);
		photo.AddThemeStyleboxOverride("pressed", empty);
		photo.AddThemeStyleboxOverride("focus", empty);
		photo.Pressed += TakePhoto;
		_ui.AddChild(photo);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey key && key.Pressed && !key.Echo)
		{
			if (key.Keycode == Key.Escape)
			{
				Resume();
				GetViewport().SetInputAsHandled();
			}
			else if (key.Keycode == Key.F)
			{
				TakePhoto();
				GetViewport().SetInputAsHandled();
			}
		}
	}

	private void Toast(string text) => MenuUI.RunToast(this, _toast, text, ref _toastTween);

	private void Resume()
	{
		if (_closing)
		{
			return;
		}
		_closing = true;
		_toastTween?.Kill();
		GetTree().Paused = false;
		QueueFree();
	}

	private void GoToMenu()
	{
		if (_closing)
		{
			return;
		}
		_closing = true;
		_toastTween?.Kill();
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn");
	}

	// Captura a tela SEM o overlay e salva em user://galeria (base da Galeria).
	private async void TakePhoto()
	{
		Audio.Instance?.Play(Audio.Sfx.Camera); // som de câmera.
		Visible = false;
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		Image img = GetViewport().GetTexture()?.GetImage();
		Visible = true;

		if (img == null)
		{
			Toast("Não foi possível tirar a foto");
			return;
		}

		DirAccess.MakeDirRecursiveAbsolute("user://galeria");
		string path = $"user://galeria/foto_{Time.GetUnixTimeFromSystem():F0}.png";
		Error err = img.SavePng(path);
		Toast(err == Error.Ok ? "Foto salva na galeria!" : "Erro ao salvar a foto");
	}

	public override void _ExitTree()
	{
		_toastTween?.Kill();
	}
}
