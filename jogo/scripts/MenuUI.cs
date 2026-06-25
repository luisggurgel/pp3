using Godot;
using System;

// Kit de UI compartilhado pelas telas em estilo "scrapbook/caderno"
// (menu principal, pausa, etc.). Centraliza cores, fonte, painel de papel,
// botões e helpers de âncora para evitar duplicação entre as telas.
public static class MenuUI
{
	public const string Dir = "res://resources/menu/";
	public const string FontPath = "res://resources/fonts/PatrickHand-Regular.ttf";

	// Paleta no tom do board (JOGAR.png).
	public static readonly Color Ink = new(0.18f, 0.15f, 0.11f);       // texto (grafite).
	public static readonly Color Accent = new(0.45f, 0.32f, 0.62f);    // roxo do destaque/hover.
	public static readonly Color Paper = new(0.962f, 0.945f, 0.882f);  // papel creme.
	public static readonly Color PaperEdge = new(0.78f, 0.74f, 0.64f); // borda do papel.
	public static readonly Color Binding = new(0.74f, 0.69f, 0.85f);   // fita/lombada lilás.

	public static FontFile LoadHand() => GD.Load<FontFile>(FontPath);

	// --- Painel de papel (caderno) com lombada + furos de espiral e um VBox
	//     interno pronto para receber os botões. O chamador ancora o painel. ---
	public static Panel PaperPanel(out VBoxContainer vbox)
	{
		var panel = new Panel();
		panel.AddThemeStyleboxOverride("panel", PaperStyle());

		var strip = new ColorRect { Color = Binding, MouseFilter = Control.MouseFilterEnum.Ignore };
		strip.AnchorTop = 0f; strip.AnchorBottom = 1f; strip.AnchorLeft = 0f; strip.AnchorRight = 0f;
		strip.OffsetLeft = 10f; strip.OffsetRight = 34f; strip.OffsetTop = 10f; strip.OffsetBottom = -10f;
		panel.AddChild(strip);

		for (int i = 0; i < 7; i++)
		{
			var hole = new Panel();
			hole.AddThemeStyleboxOverride("panel", CircleStyle());
			hole.CustomMinimumSize = new Vector2(12, 12);
			hole.AnchorLeft = 0f; hole.AnchorRight = 0f;
			float f = (i + 1) / 8f;
			hole.AnchorTop = f; hole.AnchorBottom = f;
			hole.OffsetLeft = 16f; hole.OffsetRight = 28f; hole.OffsetTop = -6f; hole.OffsetBottom = 6f;
			hole.MouseFilter = Control.MouseFilterEnum.Ignore;
			panel.AddChild(hole);
		}

		var margin = new MarginContainer();
		Fill(margin);
		margin.AddThemeConstantOverride("margin_left", 52);
		margin.AddThemeConstantOverride("margin_right", 26);
		margin.AddThemeConstantOverride("margin_top", 28);
		margin.AddThemeConstantOverride("margin_bottom", 24);
		panel.AddChild(margin);

		vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 8);
		vbox.Alignment = BoxContainer.AlignmentMode.Begin;
		margin.AddChild(vbox);
		return panel;
	}

	// --- Botão de menu: texto à mão, sem caixa; hover muda a cor e mostra a estrela. ---
	public static Button MenuButton(FontFile hand, string text, Action onPressed, bool primary = false, bool small = false)
	{
		var btn = new Button
		{
			Text = primary ? text + "  ★" : text,
			Flat = true,
			Alignment = HorizontalAlignment.Left,
			FocusMode = Control.FocusModeEnum.None,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
		};
		btn.AddThemeFontOverride("font", hand);
		btn.AddThemeFontSizeOverride("font_size", small ? 24 : 32);
		btn.AddThemeColorOverride("font_color", primary ? Accent : Ink);
		btn.AddThemeColorOverride("font_hover_color", Accent);
		btn.AddThemeColorOverride("font_pressed_color", Accent);
		btn.AddThemeColorOverride("font_focus_color", Accent);

		var empty = new StyleBoxEmpty();
		btn.AddThemeStyleboxOverride("normal", empty);
		btn.AddThemeStyleboxOverride("hover", empty);
		btn.AddThemeStyleboxOverride("pressed", empty);
		btn.AddThemeStyleboxOverride("focus", empty);

		string baseText = btn.Text;
		btn.MouseEntered += () => { if (!primary) btn.Text = "★ " + text; };
		btn.MouseExited += () => btn.Text = baseText;
		btn.Pressed += () =>
		{
			Audio.Instance?.Click();
			onPressed();
		};
		return btn;
	}

	// --- Aviso transitório (toast). ---
	public static Label MakeToast(FontFile hand)
	{
		var toast = new Label
		{
			Text = "",
			HorizontalAlignment = HorizontalAlignment.Center,
			Modulate = new Color(1, 1, 1, 0),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		toast.AddThemeFontOverride("font", hand);
		toast.AddThemeFontSizeOverride("font_size", 26);
		toast.AddThemeColorOverride("font_color", Paper);
		toast.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.7f));
		toast.AddThemeConstantOverride("outline_size", 6);
		return toast;
	}

	public static void RunToast(Node owner, Label toast, string text, ref Tween tween)
	{
		toast.Text = text;
		tween?.Kill();
		toast.Modulate = Colors.White;
		tween = owner.CreateTween();
		tween.TweenInterval(1.1);
		tween.TweenProperty(toast, "modulate:a", 0f, 0.5);
	}

	// --- estilos ---
	public static StyleBoxFlat PaperStyle()
	{
		var sb = new StyleBoxFlat { BgColor = Paper, BorderColor = PaperEdge };
		sb.SetCornerRadiusAll(14);
		sb.SetBorderWidthAll(2);
		sb.ShadowColor = new Color(0, 0, 0, 0.35f);
		sb.ShadowSize = 10;
		sb.ShadowOffset = new Vector2(6, 8);
		return sb;
	}

	public static StyleBoxFlat CircleStyle()
	{
		var sb = new StyleBoxFlat { BgColor = new Color(0.98f, 0.97f, 0.93f), BorderColor = new Color(0.45f, 0.42f, 0.38f) };
		sb.SetCornerRadiusAll(6);
		sb.SetBorderWidthAll(2);
		return sb;
	}

	// --- tela fiel ao design: a imagem do JOGAR.png como fundo ---
	// Monta: fundo escuro (letterbox) + AspectRatioContainer na proporção exata da
	// imagem + a imagem preenchendo. Devolve 'frame' (mesma área da imagem) para
	// posicionar hotspots/controles por frações que SEMPRE batem com a arte.
	public static Control DesignScreen(string imageFile, out Control frame)
	{
		var root = new Control();
		Fill(root);

		var letter = new ColorRect { Color = new Color(0.07f, 0.05f, 0.09f), MouseFilter = Control.MouseFilterEnum.Ignore };
		Fill(letter);
		root.AddChild(letter);

		var tex = GD.Load<Texture2D>(Dir + imageFile);
		var arc = new AspectRatioContainer
		{
			Ratio = (float)tex.GetWidth() / Mathf.Max(1, tex.GetHeight()),
			StretchMode = AspectRatioContainer.StretchModeEnum.Fit,
		};
		Fill(arc);
		root.AddChild(arc);

		frame = new Control();
		arc.AddChild(frame);

		var img = new TextureRect
		{
			Texture = tex,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		Fill(img);
		frame.AddChild(img);
		return root;
	}

	// Área clicável invisível sobre a arte (leve brilho no hover). Frações da imagem.
	public static Button Hotspot(Control parent, float al, float at, float ar, float ab, Action onPressed)
	{
		var btn = new Button
		{
			Flat = true,
			FocusMode = Control.FocusModeEnum.None,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
		};
		PlaceBox(btn, al, at, ar, ab);
		var empty = new StyleBoxEmpty();
		btn.AddThemeStyleboxOverride("normal", empty);
		btn.AddThemeStyleboxOverride("pressed", empty);
		btn.AddThemeStyleboxOverride("focus", empty);
		var hover = new StyleBoxFlat { BgColor = new Color(1f, 1f, 1f, 0.13f) };
		hover.SetCornerRadiusAll(10);
		btn.AddThemeStyleboxOverride("hover", hover);
		btn.Pressed += () =>
		{
			Audio.Instance?.Click();
			onPressed();
		};
		parent.AddChild(btn);
		return btn;
	}

	// Item de menu sobre a arte fiel: área clicável (clique) + barra de realce no
	// hover, no MESMO estilo roxo que o design usa para o item ativo (ex.: JOGAR).
	// 'click*' é a área clicável (generosa); 'bar*' é a barra justa ao texto.
	public static void MenuItem(Control parent,
		float clickL, float clickT, float clickR, float clickB,
		float barL, float barT, float barR, float barB,
		float tiltDeg,
		Action onPressed)
	{
		var bar = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore, Modulate = new Color(1, 1, 1, 0) };
		PlaceBox(bar, barL, barT, barR, barB);
		var sb = new StyleBoxFlat { BgColor = new Color(0.608f, 0.392f, 0.910f, 0.55f) };
		sb.SetCornerRadiusAll(16);
		bar.AddThemeStyleboxOverride("panel", sb);
		parent.AddChild(bar);

		var btn = new Button
		{
			Flat = true,
			FocusMode = Control.FocusModeEnum.None,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
		};
		PlaceBox(btn, clickL, clickT, clickR, clickB);
		var empty = new StyleBoxEmpty();
		btn.AddThemeStyleboxOverride("normal", empty);
		btn.AddThemeStyleboxOverride("hover", empty);
		btn.AddThemeStyleboxOverride("pressed", empty);
		btn.AddThemeStyleboxOverride("focus", empty);

		Tween tween = null;
		btn.MouseEntered += () =>
		{
			tween?.Kill();
			bar.PivotOffset = bar.Size / 2f;
			bar.RotationDegrees = tiltDeg; // acompanha a inclinação do papel/texto.
			bar.Scale = new Vector2(0.82f, 0.7f);
			tween = btn.CreateTween();
			tween.SetParallel(true);
			tween.TweenProperty(bar, "modulate:a", 1f, 0.13);
			tween.TweenProperty(bar, "scale", Vector2.One, 0.16)
				.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		};
		btn.MouseExited += () =>
		{
			tween?.Kill();
			tween = btn.CreateTween();
			tween.TweenProperty(bar, "modulate:a", 0f, 0.12);
		};
		btn.Pressed += () =>
		{
			Audio.Instance?.Click();
			onPressed();
		};
		parent.AddChild(btn);
	}

	// Item de menu com TEXTO REAL (fonte à mão) sobre o papel em branco, com a
	// barra roxa de realce (no hover, ou fixa se 'active'). Posicionado em px
	// nativos da imagem (o pai cuida da escala/inclinação). O botão se
	// auto-dimensiona ao texto e a barra o envolve.
	public static void TextMenuItem(Control parent, FontFile hand, Vector2 pos, string text, int fontSize, bool active, Action onPressed, bool clickSfx = true)
	{
		var item = new Control { Position = pos, MouseFilter = Control.MouseFilterEnum.Ignore };
		parent.AddChild(item);

		var btn = new Button
		{
			Text = text,
			Flat = true,
			Alignment = HorizontalAlignment.Left,
			FocusMode = Control.FocusModeEnum.None,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
		};
		btn.AddThemeFontOverride("font", hand);
		btn.AddThemeFontSizeOverride("font_size", fontSize);
		btn.AddThemeColorOverride("font_color", Ink);
		btn.AddThemeColorOverride("font_hover_color", Ink);
		btn.AddThemeColorOverride("font_pressed_color", Ink);
		var empty = new StyleBoxEmpty();
		btn.AddThemeStyleboxOverride("normal", empty);
		btn.AddThemeStyleboxOverride("hover", empty);
		btn.AddThemeStyleboxOverride("pressed", empty);
		btn.AddThemeStyleboxOverride("focus", empty);
		item.AddChild(btn);

		Vector2 sz = btn.GetMinimumSize();
		item.Size = sz;
		item.PivotOffset = sz / 2f;
		btn.AnchorRight = 1f;
		btn.AnchorBottom = 1f;

		var bar = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore, Modulate = new Color(1, 1, 1, active ? 1f : 0f) };
		bar.AnchorRight = 1f;
		bar.AnchorBottom = 1f;
		bar.OffsetLeft = -22f;
		bar.OffsetTop = -6f;
		bar.OffsetRight = 16f;
		bar.OffsetBottom = 12f;
		var sb = new StyleBoxFlat { BgColor = new Color(0.608f, 0.392f, 0.910f, 0.55f) };
		sb.SetCornerRadiusAll(16);
		bar.AddThemeStyleboxOverride("panel", sb);
		item.AddChild(bar);
		item.MoveChild(bar, 0); // atrás do texto.

		if (!active)
		{
			Tween tw = null;
			btn.MouseEntered += () =>
			{
				tw?.Kill();
				tw = btn.CreateTween();
				tw.TweenProperty(bar, "modulate:a", 1f, 0.12);
			};
			btn.MouseExited += () =>
			{
				tw?.Kill();
				tw = btn.CreateTween();
				tw.TweenProperty(bar, "modulate:a", 0f, 0.12);
			};
		}
		btn.Pressed += () =>
		{
			if (clickSfx)
			{
				Audio.Instance?.Click();
			}
			onPressed();
		};
	}

	// --- helpers de âncora (frações da tela) ---
	public static void Fill(Control c)
	{
		c.AnchorLeft = 0f; c.AnchorTop = 0f; c.AnchorRight = 1f; c.AnchorBottom = 1f;
		c.OffsetLeft = 0f; c.OffsetTop = 0f; c.OffsetRight = 0f; c.OffsetBottom = 0f;
	}

	public static void PlaceBox(Control c, float al, float at, float ar, float ab)
	{
		c.AnchorLeft = al; c.AnchorTop = at; c.AnchorRight = ar; c.AnchorBottom = ab;
		c.OffsetLeft = 0f; c.OffsetTop = 0f; c.OffsetRight = 0f; c.OffsetBottom = 0f;
	}
}
