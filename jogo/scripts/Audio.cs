using Godot;
using System.Collections.Generic;

// Autoload (singleton) de áudio do jogo.
// - Música (com loop) tocada no bus "Música", com crossfade suave ao trocar.
// - Efeitos (SFX) tocados no bus "Efeitos" via um pequeno pool (permite sobrepor).
// Acesso de qualquer lugar via Audio.Instance.
public partial class Audio : Node
{
	public static Audio Instance { get; private set; }

	public enum Track { Menu, Day1, Day2 }
	public enum Sfx { Click, Start, Camera, Text, Knock, Door }

	private const string Dir = "res://resources/audio/";
	private static readonly Dictionary<Track, string> MusicFiles = new()
	{
		{ Track.Menu, Dir + "music_menu.mp3" },
		{ Track.Day1, Dir + "music_day1.mp3" },
		{ Track.Day2, Dir + "music_day2.mp3" },
	};
	private static readonly Dictionary<Sfx, string> SfxFiles = new()
	{
		{ Sfx.Click, Dir + "sfx_click.mp3" },
		{ Sfx.Start, Dir + "sfx_start.mp3" },
		{ Sfx.Camera, Dir + "sfx_camera.mp3" },
		{ Sfx.Text, Dir + "sfx_text.wav" },
		{ Sfx.Knock, Dir + "sfx_knock.mp3" },
		{ Sfx.Door, Dir + "sfx_door.mp3" },
	};

	private AudioStreamPlayer _musicA;
	private AudioStreamPlayer _musicB;
	private AudioStreamPlayer _activeMusic;
	private Track? _currentTrack;
	private Tween _fadeTween;

	private AudioStreamPlayer[] _sfxPool;
	private int _sfxIndex;

	public override void _EnterTree() => Instance = this;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always; // continua tocando mesmo com o jogo pausado.
		EnsureBuses();

		_musicA = NewPlayer(Settings.MusicBus);
		_musicB = NewPlayer(Settings.MusicBus);
		_activeMusic = _musicA;

		_sfxPool = new AudioStreamPlayer[5];
		for (int i = 0; i < _sfxPool.Length; i++)
		{
			_sfxPool[i] = NewPlayer(Settings.SfxBus);
		}
	}

	private AudioStreamPlayer NewPlayer(string bus)
	{
		var p = new AudioStreamPlayer
		{
			Bus = AudioServer.GetBusIndex(bus) >= 0 ? bus : "Master",
			ProcessMode = ProcessModeEnum.Always,
		};
		AddChild(p);
		return p;
	}

	private static void EnsureBuses()
	{
		// O autoload Settings também cria; garantimos aqui caso o Audio rode antes.
		if (AudioServer.GetBusIndex(Settings.MusicBus) < 0)
		{
			AudioServer.AddBus();
			AudioServer.SetBusName(AudioServer.BusCount - 1, Settings.MusicBus);
		}
		if (AudioServer.GetBusIndex(Settings.SfxBus) < 0)
		{
			AudioServer.AddBus();
			AudioServer.SetBusName(AudioServer.BusCount - 1, Settings.SfxBus);
		}
	}

	// --- Música ---

	// Troca a música ambiente (com crossfade). Se já for a mesma tocando, não faz nada
	// (não reinicia). Pode ser chamado a qualquer momento, sem reiniciar a cena.
	public void PlayMusic(Track track)
	{
		if (_currentTrack == track && _activeMusic.Playing)
		{
			return;
		}
		_currentTrack = track;

		var stream = GD.Load<AudioStream>(MusicFiles[track]);
		SetLoop(stream, true);

		AudioStreamPlayer from = _activeMusic;
		AudioStreamPlayer to = _activeMusic == _musicA ? _musicB : _musicA;
		to.Stream = stream;
		to.VolumeDb = -40f;
		to.Play();

		_fadeTween?.Kill();
		_fadeTween = CreateTween();
		_fadeTween.TweenProperty(to, "volume_db", 0f, 0.6);
		_fadeTween.Parallel().TweenProperty(from, "volume_db", -40f, 0.6);
		_fadeTween.TweenCallback(Callable.From(() => from.Stop()));
		_activeMusic = to;
	}

	public void StopMusic()
	{
		_currentTrack = null;
		_fadeTween?.Kill();
		_musicA.Stop();
		_musicB.Stop();
	}

	// --- Efeitos ---

	public void Play(Sfx sfx, float volumeDb = 0f)
	{
		var stream = GD.Load<AudioStream>(SfxFiles[sfx]);
		SetLoop(stream, false);
		AudioStreamPlayer p = _sfxPool[_sfxIndex];
		_sfxIndex = (_sfxIndex + 1) % _sfxPool.Length;
		p.Stream = stream;
		p.VolumeDb = volumeDb;
		p.Play();
	}

	public void Click() => Play(Sfx.Click);

	// "Blip" suave a cada letra que vai aparecendo (estilo Undertale), bem baixinho.
	public void TextBlip() => Play(Sfx.Text, -10f);

	// Toca uma sequência de efeitos, um logo após o outro terminar (ex.: bater na
	// porta e depois a porta abrir/fechar). A MÚSICA continua tocando normalmente,
	// pois os efeitos saem no bus de Efeitos, separado do bus de Música.
	public void PlaySequence(params Sfx[] sequence)
	{
		if (sequence == null || sequence.Length == 0)
		{
			return;
		}
		PlaySequenceFrom(sequence, 0);
	}

	private void PlaySequenceFrom(Sfx[] sequence, int index)
	{
		if (index >= sequence.Length)
		{
			return;
		}

		var stream = GD.Load<AudioStream>(SfxFiles[sequence[index]]);
		Play(sequence[index]);

		if (index + 1 < sequence.Length)
		{
			// Agenda o próximo som para logo depois deste terminar.
			double wait = stream.GetLength() + 0.05;
			SceneTreeTimer timer = GetTree().CreateTimer(wait, processAlways: true);
			timer.Timeout += () => PlaySequenceFrom(sequence, index + 1);
		}
	}

	private static void SetLoop(AudioStream stream, bool loop)
	{
		if (stream is AudioStreamMP3 mp3)
		{
			mp3.Loop = loop;
		}
		else if (stream is AudioStreamOggVorbis ogg)
		{
			ogg.Loop = loop;
		}
	}
}
