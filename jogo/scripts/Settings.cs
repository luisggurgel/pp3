using Godot;

// Autoload (singleton) de configurações do jogo.
// - Garante os buses de áudio (Master + Música + Efeitos).
// - Mantém um overlay global de brilho (escurece a tela toda).
// - Carrega/salva em user://settings.cfg e aplica tudo no início do jogo.
// Acesso de qualquer lugar via Settings.Instance.
public partial class Settings : Node
{
	public static Settings Instance { get; private set; }

	private const string SavePath = "user://settings.cfg";
	public const string MusicBus = "Música";
	public const string SfxBus = "Efeitos";

	// Opções fixas.
	public static readonly Vector2I[] Resolutions =
	{
		new(1280, 720), new(1600, 900), new(1920, 1080)
	};
	public static readonly string[] Locales = { "pt", "en" };
	public static readonly string[] LocaleNames = { "Português", "English" };

	// Valores atuais (0..1 nos volumes/brilho).
	public float MasterVolume = 0.8f;
	public float MusicVolume = 0.7f;
	public float SfxVolume = 0.8f;
	public bool Fullscreen = false;
	public int ResolutionIndex = 1;
	public float Brightness = 1.0f;
	public int LocaleIndex = 0;

	private ColorRect _brightnessOverlay;

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		EnsureBuses();
		CreateBrightnessOverlay();
		LoadFromDisk();
		ApplyAll();
	}

	private void EnsureBuses()
	{
		if (AudioServer.GetBusIndex(MusicBus) < 0)
		{
			AudioServer.AddBus();
			AudioServer.SetBusName(AudioServer.BusCount - 1, MusicBus);
		}
		if (AudioServer.GetBusIndex(SfxBus) < 0)
		{
			AudioServer.AddBus();
			AudioServer.SetBusName(AudioServer.BusCount - 1, SfxBus);
		}
	}

	private void CreateBrightnessOverlay()
	{
		var layer = new CanvasLayer { Layer = 200 }; // acima de tudo.
		AddChild(layer);
		_brightnessOverlay = new ColorRect
		{
			Color = new Color(0, 0, 0, 0),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_brightnessOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		layer.AddChild(_brightnessOverlay);
	}

	// ---------- aplicação dos valores ----------

	public void SetMasterVolume(float v)
	{
		MasterVolume = Mathf.Clamp(v, 0f, 1f);
		AudioServer.SetBusVolumeDb(0, ToDb(MasterVolume));
	}

	public void SetMusicVolume(float v)
	{
		MusicVolume = Mathf.Clamp(v, 0f, 1f);
		int i = AudioServer.GetBusIndex(MusicBus);
		if (i >= 0)
		{
			AudioServer.SetBusVolumeDb(i, ToDb(MusicVolume));
		}
	}

	public void SetSfxVolume(float v)
	{
		SfxVolume = Mathf.Clamp(v, 0f, 1f);
		int i = AudioServer.GetBusIndex(SfxBus);
		if (i >= 0)
		{
			AudioServer.SetBusVolumeDb(i, ToDb(SfxVolume));
		}
	}

	private static float ToDb(float linear) => linear <= 0.0005f ? -80f : Mathf.LinearToDb(linear);

	public void SetFullscreen(bool fs)
	{
		Fullscreen = fs;
		if (fs)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
		}
		else
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
			ApplyResolution();
		}
	}

	public void SetResolutionIndex(int idx)
	{
		ResolutionIndex = Mathf.Clamp(idx, 0, Resolutions.Length - 1);
		ApplyResolution();
	}

	private void ApplyResolution()
	{
		if (Fullscreen)
		{
			return; // em tela cheia a resolução acompanha o monitor.
		}
		// Passo 1: sai de maximizado/tela cheia (modo janela "normal").
		if (DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Windowed)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		}
		// Passo 2: redimensiona no PRÓXIMO frame. No Windows, mudar o modo e o
		// tamanho no mesmo frame costuma fazer o WindowSetSize ser ignorado — por
		// isso a resolução "não mudava".
		Callable.From(ResizeWindowNow).CallDeferred();
	}

	private void ResizeWindowNow()
	{
		if (Fullscreen)
		{
			return;
		}
		Vector2I r = Resolutions[ResolutionIndex];
		// Garante que a janela pode encolher até a menor resolução suportada.
		DisplayServer.WindowSetMinSize(Resolutions[0]);
		DisplayServer.WindowSetSize(r);
		Vector2I screen = DisplayServer.ScreenGetSize();
		DisplayServer.WindowSetPosition((screen - r) / 2);
	}

	public void SetBrightness(float b)
	{
		Brightness = Mathf.Clamp(b, 0.3f, 1f);
		if (_brightnessOverlay != null)
		{
			_brightnessOverlay.Color = new Color(0, 0, 0, 1f - Brightness);
		}
	}

	public void SetLocaleIndex(int idx)
	{
		LocaleIndex = Mathf.Clamp(idx, 0, Locales.Length - 1);
		TranslationServer.SetLocale(Locales[LocaleIndex]);
	}

	public void ApplyAll()
	{
		SetMasterVolume(MasterVolume);
		SetMusicVolume(MusicVolume);
		SetSfxVolume(SfxVolume);
		SetFullscreen(Fullscreen);
		SetBrightness(Brightness);
		SetLocaleIndex(LocaleIndex);
	}

	// ---------- persistência ----------

	public void Save()
	{
		var cfg = new ConfigFile();
		cfg.SetValue("audio", "master", MasterVolume);
		cfg.SetValue("audio", "music", MusicVolume);
		cfg.SetValue("audio", "sfx", SfxVolume);
		cfg.SetValue("video", "fullscreen", Fullscreen);
		cfg.SetValue("video", "resolution", ResolutionIndex);
		cfg.SetValue("video", "brightness", Brightness);
		cfg.SetValue("game", "locale", LocaleIndex);
		cfg.Save(SavePath);
	}

	private void LoadFromDisk()
	{
		var cfg = new ConfigFile();
		if (cfg.Load(SavePath) != Error.Ok)
		{
			return; // primeira execução: usa os padrões.
		}
		MasterVolume = (float)cfg.GetValue("audio", "master", MasterVolume).AsSingle();
		MusicVolume = (float)cfg.GetValue("audio", "music", MusicVolume).AsSingle();
		SfxVolume = (float)cfg.GetValue("audio", "sfx", SfxVolume).AsSingle();
		Fullscreen = cfg.GetValue("video", "fullscreen", Fullscreen).AsBool();
		ResolutionIndex = cfg.GetValue("video", "resolution", ResolutionIndex).AsInt32();
		Brightness = (float)cfg.GetValue("video", "brightness", Brightness).AsSingle();
		LocaleIndex = cfg.GetValue("game", "locale", LocaleIndex).AsInt32();
	}
}
