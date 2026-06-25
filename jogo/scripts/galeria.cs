using Godot;
using System.Collections.Generic;

// Overlay de Galeria (reutilizável: abre do menu e da pausa).
// Mostra as fotos salvas em user://galeria/ (tiradas pelo "Tirar foto" da pausa)
// como uma grade de polaroids; clicar numa foto amplia.
public partial class galeria : CanvasLayer
{
	private const string PhotoDir = "user://galeria";

	private FontFile _hand;
	private Control _viewer;
	private bool _closing;

	public override void _Ready()
	{
		Layer = 160;
		ProcessMode = ProcessModeEnum.Always;
		_hand = MenuUI.LoadHand();

		var dim = new ColorRect { Color = new Color(0.05f, 0.04f, 0.07f, 0.6f) };
		MenuUI.Fill(dim);
		AddChild(dim);

		Panel panel = MenuUI.PaperPanel(out VBoxContainer vbox);
		MenuUI.PlaceBox(panel, 0.12f, 0.08f, 0.88f, 0.94f);
		AddChild(panel);
		vbox.AddThemeConstantOverride("separation", 12);

		// Título.
		var title = new Label { Text = "GALERIA", HorizontalAlignment = HorizontalAlignment.Center };
		title.AddThemeFontOverride("font", _hand);
		title.AddThemeFontSizeOverride("font_size", 42);
		title.AddThemeColorOverride("font_color", MenuUI.Ink);
		vbox.AddChild(title);
		vbox.AddChild(new ColorRect { Color = MenuUI.PaperEdge, CustomMinimumSize = new Vector2(0, 2) });

		// Conteúdo: grade de fotos ou estado vazio.
		List<string> photos = ListPhotos();
		if (photos.Count == 0)
		{
			var empty = new Label
			{
				Text = "Nenhuma foto ainda.\nTire fotos na pausa (tecla F)!",
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				SizeFlagsVertical = Control.SizeFlags.ExpandFill,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
			};
			empty.AddThemeFontOverride("font", _hand);
			empty.AddThemeFontSizeOverride("font_size", 26);
			empty.AddThemeColorOverride("font_color", new Color(0.4f, 0.37f, 0.32f));
			vbox.AddChild(empty);
		}
		else
		{
			var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
			scroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			vbox.AddChild(scroll);

			var grid = new GridContainer { Columns = 4 };
			grid.AddThemeConstantOverride("h_separation", 16);
			grid.AddThemeConstantOverride("v_separation", 16);
			grid.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			scroll.AddChild(grid);

			foreach (string path in photos)
			{
				Texture2D tex = LoadTexture(path);
				if (tex != null)
				{
					grid.AddChild(MakePolaroid(tex));
				}
			}
		}

		// Voltar.
		var bottom = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		bottom.AddChild(MenuUI.MenuButton(_hand, "Voltar", Close, small: true));
		vbox.AddChild(bottom);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey k && k.Pressed && !k.Echo && k.Keycode == Key.Escape)
		{
			if (_viewer != null)
			{
				CloseViewer();
			}
			else
			{
				Close();
			}
			GetViewport().SetInputAsHandled();
		}
	}

	// ---------- fotos ----------

	private static List<string> ListPhotos()
	{
		var list = new List<string>();
		DirAccess dir = DirAccess.Open(PhotoDir);
		if (dir == null)
		{
			return list;
		}
		foreach (string file in dir.GetFiles())
		{
			if (file.EndsWith(".png") || file.EndsWith(".jpg"))
			{
				list.Add($"{PhotoDir}/{file}");
			}
		}
		list.Sort();
		list.Reverse(); // mais recentes primeiro.
		return list;
	}

	private static Texture2D LoadTexture(string path)
	{
		var img = new Image();
		if (img.Load(path) != Error.Ok)
		{
			return null;
		}
		return ImageTexture.CreateFromImage(img);
	}

	private Button MakePolaroid(Texture2D tex)
	{
		var card = new Button
		{
			CustomMinimumSize = new Vector2(168, 176),
			FocusMode = Control.FocusModeEnum.None,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
		};
		card.AddThemeStyleboxOverride("normal", PolaroidStyle(false));
		card.AddThemeStyleboxOverride("hover", PolaroidStyle(true));
		card.AddThemeStyleboxOverride("pressed", PolaroidStyle(true));

		var photo = new TextureRect
		{
			Texture = tex,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		photo.AnchorLeft = 0f; photo.AnchorTop = 0f; photo.AnchorRight = 1f; photo.AnchorBottom = 1f;
		photo.OffsetLeft = 12f; photo.OffsetTop = 12f; photo.OffsetRight = -12f; photo.OffsetBottom = -34f;
		card.AddChild(photo);

		card.Pressed += () => OpenViewer(tex);
		return card;
	}

	private static StyleBoxFlat PolaroidStyle(bool hover)
	{
		var sb = new StyleBoxFlat
		{
			BgColor = hover ? new Color(1f, 1f, 1f) : new Color(0.97f, 0.96f, 0.92f),
			BorderColor = hover ? MenuUI.Accent : new Color(0.8f, 0.77f, 0.7f),
		};
		sb.SetCornerRadiusAll(4);
		sb.SetBorderWidthAll(hover ? 3 : 1);
		sb.ShadowColor = new Color(0, 0, 0, 0.3f);
		sb.ShadowSize = 6;
		sb.ShadowOffset = new Vector2(3, 4);
		return sb;
	}

	// ---------- visualizador ampliado ----------

	private void OpenViewer(Texture2D tex)
	{
		if (_viewer != null)
		{
			return;
		}
		_viewer = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
		MenuUI.Fill(_viewer);

		var back = new ColorRect { Color = new Color(0f, 0f, 0f, 0.85f) };
		MenuUI.Fill(back);
		_viewer.AddChild(back);

		var big = new TextureRect
		{
			Texture = tex,
			StretchMode = TextureRect.StretchModeEnum.KeepAspect,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		MenuUI.PlaceBox(big, 0.1f, 0.08f, 0.9f, 0.9f);
		_viewer.AddChild(big);

		var hint = new Label
		{
			Text = "clique ou ESC para fechar",
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		hint.AddThemeFontOverride("font", _hand);
		hint.AddThemeFontSizeOverride("font_size", 22);
		hint.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
		MenuUI.PlaceBox(hint, 0.1f, 0.92f, 0.9f, 0.98f);
		_viewer.AddChild(hint);

		// clique em qualquer lugar fecha.
		back.GuiInput += e =>
		{
			if (e is InputEventMouseButton mb && mb.Pressed)
			{
				CloseViewer();
			}
		};
		AddChild(_viewer);
	}

	private void CloseViewer()
	{
		_viewer?.QueueFree();
		_viewer = null;
	}

	private void Close()
	{
		if (_closing)
		{
			return;
		}
		_closing = true;
		QueueFree();
	}
}
