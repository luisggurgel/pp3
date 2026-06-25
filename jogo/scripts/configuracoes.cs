using Godot;
using System;

// Overlay de Configurações (reutilizável: abre do menu e da pausa).
// Lê/escreve no autoload Settings, aplicando ao vivo; "Aplicar" salva em disco.
public partial class configuracoes : CanvasLayer
{
	private FontFile _hand;
	private Label _toast;
	private Tween _toastTween;
	private bool _closing;

	// Valores PENDENTES: os controles só editam estes; nada é aplicado de verdade
	// até clicar em "Aplicar". "Voltar"/ESC descarta sem aplicar nem salvar.
	private float _master, _music, _sfx, _brightness;
	private bool _fullscreen;
	private int _resolution, _locale;

	public override void _Ready()
	{
		Layer = 160;
		ProcessMode = ProcessModeEnum.Always;
		_hand = MenuUI.LoadHand();

		var dim = new ColorRect { Color = new Color(0.05f, 0.04f, 0.07f, 0.55f) };
		MenuUI.Fill(dim);
		AddChild(dim);

		Panel panel = MenuUI.PaperPanel(out VBoxContainer vbox);
		MenuUI.PlaceBox(panel, 0.17f, 0.09f, 0.83f, 0.93f);
		AddChild(panel);
		vbox.AddThemeConstantOverride("separation", 10);

		Settings s = Settings.Instance;

		// Começa a partir dos valores atuais (estado pendente).
		_master = s.MasterVolume;
		_music = s.MusicVolume;
		_sfx = s.SfxVolume;
		_fullscreen = s.Fullscreen;
		_resolution = s.ResolutionIndex;
		_brightness = s.Brightness;
		_locale = s.LocaleIndex;

		vbox.AddChild(TitleHeader());
		vbox.AddChild(Separator());

		vbox.AddChild(Row("Volume geral", Slider(_master, 0f, v => _master = v)));
		vbox.AddChild(Row("Volume da música", Slider(_music, 0f, v => _music = v)));
		vbox.AddChild(Row("Volume dos efeitos", Slider(_sfx, 0f, v => _sfx = v)));
		vbox.AddChild(Separator());

		vbox.AddChild(Row("Tela", Spinner(new[] { "Janela", "Tela cheia" }, _fullscreen ? 1 : 0, i => _fullscreen = i == 1)));
		vbox.AddChild(Row("Resolução", Spinner(ResolutionNames(), _resolution, i => _resolution = i)));
		vbox.AddChild(Row("Brilho", Slider(_brightness, 0.3f, v => _brightness = v)));
		vbox.AddChild(Row("Idioma", Spinner(Settings.LocaleNames, _locale, i => _locale = i)));
		vbox.AddChild(Separator());

		var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		buttons.AddThemeConstantOverride("separation", 48);
		buttons.AddChild(MenuUI.MenuButton(_hand, "Voltar", Close, small: true));
		buttons.AddChild(MenuUI.MenuButton(_hand, "Aplicar", Apply, primary: true));
		vbox.AddChild(buttons);

		_toast = MenuUI.MakeToast(_hand);
		MenuUI.PlaceBox(_toast, 0.2f, 0.93f, 0.8f, 0.99f);
		AddChild(_toast);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey k && k.Pressed && !k.Echo && k.Keycode == Key.Escape)
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}

	// ---------- linhas / controles ----------

	private Control TitleHeader()
	{
		string path = MenuUI.Dir + "title_config.png";
		if (ResourceLoader.Exists(path))
		{
			var img = new TextureRect
			{
				Texture = GD.Load<Texture2D>(path),
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				CustomMinimumSize = new Vector2(0, 58),
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			return img;
		}
		var lbl = new Label { Text = "CONFIGURAÇÕES", HorizontalAlignment = HorizontalAlignment.Center };
		lbl.AddThemeFontOverride("font", _hand);
		lbl.AddThemeFontSizeOverride("font_size", 40);
		lbl.AddThemeColorOverride("font_color", MenuUI.Ink);
		return lbl;
	}

	private HBoxContainer Row(string label, Control control)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 14);
		var lbl = new Label { Text = label, VerticalAlignment = VerticalAlignment.Center };
		lbl.AddThemeFontOverride("font", _hand);
		lbl.AddThemeFontSizeOverride("font_size", 24);
		lbl.AddThemeColorOverride("font_color", MenuUI.Ink);
		lbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		row.AddChild(lbl);
		control.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
		row.AddChild(control);
		return row;
	}

	private ColorRect Separator()
	{
		return new ColorRect { Color = MenuUI.PaperEdge, CustomMinimumSize = new Vector2(0, 2) };
	}

	private Control Slider(float value, float min, Action<float> onChange)
	{
		var box = new HBoxContainer();
		box.AddThemeConstantOverride("separation", 10);

		var slider = new HSlider
		{
			MinValue = min,
			MaxValue = 1.0,
			Step = 0.01,
			Value = value,
			CustomMinimumSize = new Vector2(230, 22),
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		slider.AddThemeStyleboxOverride("slider", TrackStyle());
		slider.AddThemeStyleboxOverride("grabber_area", FillStyle());
		slider.AddThemeStyleboxOverride("grabber_area_highlight", FillStyle());

		var pct = new Label
		{
			Text = Pct(value),
			CustomMinimumSize = new Vector2(56, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center,
		};
		pct.AddThemeFontOverride("font", _hand);
		pct.AddThemeFontSizeOverride("font_size", 22);
		pct.AddThemeColorOverride("font_color", MenuUI.Ink);

		slider.ValueChanged += v =>
		{
			onChange((float)v);
			pct.Text = Pct((float)v);
		};
		box.AddChild(slider);
		box.AddChild(pct);
		return box;
	}

	private Control Spinner(string[] options, int current, Action<int> onChange)
	{
		var box = new HBoxContainer();
		box.AddThemeConstantOverride("separation", 6);
		int idx = Mathf.Clamp(current, 0, options.Length - 1);

		var value = new Label
		{
			Text = options[idx],
			CustomMinimumSize = new Vector2(176, 0),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
		};
		value.AddThemeFontOverride("font", _hand);
		value.AddThemeFontSizeOverride("font_size", 23);
		value.AddThemeColorOverride("font_color", MenuUI.Accent);

		Button left = Arrow("<");
		Button right = Arrow(">");
		left.Pressed += () =>
		{
			idx = (idx - 1 + options.Length) % options.Length;
			value.Text = options[idx];
			onChange(idx);
		};
		right.Pressed += () =>
		{
			idx = (idx + 1) % options.Length;
			value.Text = options[idx];
			onChange(idx);
		};

		box.AddChild(left);
		box.AddChild(value);
		box.AddChild(right);
		return box;
	}

	private Button Arrow(string text)
	{
		var b = new Button
		{
			Text = text,
			Flat = true,
			CustomMinimumSize = new Vector2(34, 0),
			FocusMode = Control.FocusModeEnum.None,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
		};
		b.AddThemeFontOverride("font", _hand);
		b.AddThemeFontSizeOverride("font_size", 26);
		b.AddThemeColorOverride("font_color", MenuUI.Ink);
		b.AddThemeColorOverride("font_hover_color", MenuUI.Accent);
		var e = new StyleBoxEmpty();
		b.AddThemeStyleboxOverride("normal", e);
		b.AddThemeStyleboxOverride("hover", e);
		b.AddThemeStyleboxOverride("pressed", e);
		b.Pressed += () => Audio.Instance?.Click();
		return b;
	}

	private static StyleBoxFlat TrackStyle()
	{
		var s = new StyleBoxFlat { BgColor = new Color(0.82f, 0.79f, 0.72f) };
		s.SetCornerRadiusAll(5);
		s.ContentMarginTop = 4;
		s.ContentMarginBottom = 4;
		return s;
	}

	private static StyleBoxFlat FillStyle()
	{
		var s = new StyleBoxFlat { BgColor = MenuUI.Accent };
		s.SetCornerRadiusAll(5);
		return s;
	}

	private static string Pct(float v) => $"{Mathf.RoundToInt(v * 100f)}%";

	private static string[] ResolutionNames()
	{
		var names = new string[Settings.Resolutions.Length];
		for (int i = 0; i < names.Length; i++)
		{
			names[i] = $"{Settings.Resolutions[i].X} x {Settings.Resolutions[i].Y}";
		}
		return names;
	}

	// ---------- ações ----------

	private void Apply()
	{
		Settings s = Settings.Instance;
		if (s == null)
		{
			return;
		}
		// Aplica TUDO de uma vez (esta é a única hora em que algo muda de verdade)
		// e salva em disco. A ordem resolução -> tela garante o tamanho certo ao
		// voltar de tela cheia para janela.
		s.SetMasterVolume(_master);
		s.SetMusicVolume(_music);
		s.SetSfxVolume(_sfx);
		s.SetResolutionIndex(_resolution);
		s.SetFullscreen(_fullscreen);
		s.SetBrightness(_brightness);
		s.SetLocaleIndex(_locale);
		s.Save();
		MenuUI.RunToast(this, _toast, "Configurações aplicadas!", ref _toastTween);
	}

	private void Close()
	{
		if (_closing)
		{
			return;
		}
		_closing = true;
		// "Voltar" descarta as alterações pendentes: não aplica nem salva.
		_toastTween?.Kill();
		QueueFree();
	}

	public override void _ExitTree()
	{
		_toastTween?.Kill();
	}
}
