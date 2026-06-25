using Godot;
using System.Collections.Generic;

namespace PP3.Minigame;

public partial class RhythmMinigame : Node2D
{
	[Export] public PackedScene FoodNoteScene { get; set; }
	[Export] public PackedScene FeedbackPopupScene { get; set; }

	// Node references
	private Sprite2D _background;
	private Sprite2D _characterSprite;
	private Sprite2D _speechBalloon;
	private Node2D _foodContainer;
	private Node2D _feedbackContainer;
	private AudioStreamPlayer _musicPlayer;
	private ProgressBar _healthBar;
	private StyleBoxFlat _healthBarBg;
	private StyleBoxFlat _healthBarFill;
	private HBoxContainer _comboContainer;
	private TextureRect _comboIcon;
	private Label _comboLabel;

	// Character expression textures (loaded from assets)
	private Texture2D _characterPerfect;
	private Texture2D _characterHappy;
	private Texture2D _characterNeutral;
	private Texture2D _characterWorried;
	private Texture2D _characterSad;

	// Speech balloon textures
	private Texture2D _balloonPositive;
	private Texture2D _balloonNegative;

	// State
	private GameState _gameState;
	private BeatmapData _beatmap;
	private int _nextNoteIndex = 0;
	private bool _isPlaying = false;
	private Tween _speechBalloonTween;

	// Elapsed time fallback (for when there is no music file)
	private double _elapsedTime = 0.0;
	private bool _hasMusicStream = false;

	// Feedback popup fixed position (right side of the health bar)
	private readonly Vector2 _feedbackPosition = new(1250, 60);

	// Resolução de design do minigame: TODO o layout (HUD, grid de comidas, fundo)
	// foi feito nessa base. Como o projeto roda noutra resolução (a das cenas de
	// diálogo), escalamos o conteúdo para caber no viewport — ver FitToViewport().
	private static readonly Vector2 DesignSize = new(1920, 1080);
	private CanvasLayer _uiLayer;

	public override void _Ready()
	{
		GD.Print("[DEBUG] RhythmMinigame._Ready() START");

		// Para a música ambiente global; o minigame toca a própria trilha.
		Audio.Instance?.StopMusic();

		// Get node references
		_background = GetNode<Sprite2D>("Background");
		_characterSprite = GetNode<Sprite2D>("UI/CharacterSprite");
		_speechBalloon = GetNode<Sprite2D>("UI/SpeechBalloon");
		_foodContainer = GetNode<Node2D>("FoodContainer");
		_feedbackContainer = GetNode<Node2D>("FeedbackContainer");
		_musicPlayer = GetNode<AudioStreamPlayer>("MusicPlayer");
		_healthBar = GetNode<ProgressBar>("UI/HealthBar");
		_comboContainer = GetNode<HBoxContainer>("UI/ComboContainer");
		_comboIcon = GetNode<TextureRect>("UI/ComboContainer/ComboIcon");
		_comboLabel = GetNode<Label>("UI/ComboContainer/ComboLabel");
		_uiLayer = GetNode<CanvasLayer>("UI");
		GD.Print("[DEBUG] Node references OK");

		// Load optional textures
		LoadTextures();
		GD.Print("[DEBUG] LoadTextures OK");

		// Setup background to fill screen
		SetupBackground();
		GD.Print("[DEBUG] SetupBackground OK");

		// Encaixa o minigame (desenhado em 1920x1080) no viewport atual, sem depender
		// da resolução global do projeto (que pertence às cenas de diálogo).
		FitToViewport();
		GetViewport().SizeChanged += FitToViewport;

		// Initialize game state
		_gameState = new GameState();
		_gameState.HealthChanged += OnHealthChanged;
		_gameState.ComboChanged += OnComboChanged;
		_gameState.GameOverTriggered += OnGameOver;
		GD.Print("[DEBUG] GameState OK");

		// Load beatmap
		_beatmap = BeatmapLoader.Load("res://assets/data/beatmap.json");
		GD.Print($"[DEBUG] Beatmap loaded: {_beatmap.Notes.Count} notes");

		// Initialize UI
		_healthBar.MaxValue = GameState.MaxHealth;
		_healthBar.Value = GameState.MaxHealth;

		// Health Bar styling
		_healthBarBg = new StyleBoxFlat
		{
			BgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f),
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8
		};
		_healthBarFill = new StyleBoxFlat
		{
			BgColor = new Color(0.2f, 0.8f, 0.2f, 1.0f),
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8
		};
		_healthBar.AddThemeStyleboxOverride("background", _healthBarBg);
		_healthBar.AddThemeStyleboxOverride("fill", _healthBarFill);

		_comboContainer.Visible = false;
		_speechBalloon.Visible = false;
		GD.Print("[DEBUG] UI initialized");

		// Show tutorial
		ShowTutorial();
		GD.Print("[DEBUG] RhythmMinigame._Ready() END");
	}

	private void LoadTextures()
	{
		string charPath = "res://assets/sprites/character/";
		string feedbackPath = "res://assets/sprites/feedback/";

		// Character expressions
		if (ResourceLoader.Exists(charPath + "perfect.png"))
			_characterPerfect = GD.Load<Texture2D>(charPath + "perfect.png");
		if (ResourceLoader.Exists(charPath + "happy.png"))
			_characterHappy = GD.Load<Texture2D>(charPath + "happy.png");
		if (ResourceLoader.Exists(charPath + "neutral.png"))
			_characterNeutral = GD.Load<Texture2D>(charPath + "neutral.png");
		if (ResourceLoader.Exists(charPath + "worried.png"))
			_characterWorried = GD.Load<Texture2D>(charPath + "worried.png");
		if (ResourceLoader.Exists(charPath + "sad.png"))
			_characterSad = GD.Load<Texture2D>(charPath + "sad.png");

		// Speech balloons
		if (ResourceLoader.Exists(feedbackPath + "balloon_positive.png"))
			_balloonPositive = GD.Load<Texture2D>(feedbackPath + "balloon_positive.png");
		if (ResourceLoader.Exists(feedbackPath + "balloon_negative.png"))
			_balloonNegative = GD.Load<Texture2D>(feedbackPath + "balloon_negative.png");

		// Set initial character expression
		if (_characterPerfect != null)
			_characterSprite.Texture = _characterPerfect;
		else if (_characterHappy != null)
			_characterSprite.Texture = _characterHappy;
	}

	/// <summary>
	/// Scales the background Sprite2D to fill the 1920x1080 viewport,
	/// regardless of the actual image dimensions.
	/// </summary>
	private void SetupBackground()
	{
		string bgPath = "res://assets/sprites/background/background.png";
		if (ResourceLoader.Exists(bgPath))
		{
			var bgTexture = GD.Load<Texture2D>(bgPath);
			_background.Texture = bgTexture;
			_background.Centered = false;
			_background.Position = Vector2.Zero;

			// Scale to fill 1920x1080
			float scaleX = 1920f / bgTexture.GetWidth();
			float scaleY = 1080f / bgTexture.GetHeight();
			_background.Scale = new Vector2(scaleX, scaleY);
		}
	}

	/// <summary>
	/// Escala e centraliza o conteúdo do minigame (desenhado para 1920x1080) para
	/// caber no viewport real, preservando a proporção (letterbox). Mantém o minigame
	/// independente da resolução global do projeto — usada pelas cenas de diálogo —,
	/// evitando que ele apareça cortado/encolhido.
	/// </summary>
	private void FitToViewport()
	{
		Vector2 vp = GetViewport().GetVisibleRect().Size;
		if (vp.X <= 0 || vp.Y <= 0) return;

		float scale = Mathf.Min(vp.X / DesignSize.X, vp.Y / DesignSize.Y);
		Vector2 offset = (vp - DesignSize * scale) * 0.5f;

		// Conteúdo em espaço 2D (fundo, comidas, feedbacks) herda o transform da raiz.
		Scale = new Vector2(scale, scale);
		Position = offset;

		// A HUD vive numa CanvasLayer, que NÃO herda o transform 2D da raiz:
		// aplicamos o mesmo ajuste manualmente para ela acompanhar.
		// (Os overlays de tutorial/game over/pause são CanvasLayers ancoradas em
		//  tela cheia, então se adaptam sozinhos e não precisam de ajuste.)
		if (_uiLayer != null)
		{
			_uiLayer.Scale = new Vector2(scale, scale);
			_uiLayer.Offset = offset;
		}
	}

	private void ShowTutorial()
	{
		var tutorialScene = GD.Load<PackedScene>("res://scenes/minigame/TutorialScreen.tscn");
		if (tutorialScene != null)
		{
			var tutorial = tutorialScene.Instantiate<TutorialScreen>();
			AddChild(tutorial);
			tutorial.TutorialDismissed += OnTutorialDismissed;
		}
		else
		{
			GD.Print("RhythmMinigame: No tutorial scene found, starting immediately.");
			StartGame();
		}
	}

	private void OnTutorialDismissed()
	{
		StartGame();
	}

	private void StartGame()
	{
		_isPlaying = true;
		_nextNoteIndex = 0;
		_elapsedTime = 0.0;

		// Try to load music (search for the actual filename)
		string[] musicPaths = {
			"res://assets/audio/MinigameDia0.mp3",
			"res://assets/audio/MinigameDia0.ogg",
			"res://assets/audio/music.mp3",
            "res://assets/audio/music.ogg"
		};
		foreach (string path in musicPaths)
		{
			if (ResourceLoader.Exists(path))
			{
				_musicPlayer.Stream = GD.Load<AudioStream>(path);
				break;
			}
		}

		if (_musicPlayer.Stream != null)
		{
			_hasMusicStream = true;
			// Roteia para o bus "Música" (criado pelo autoload Settings), para o
			// slider de volume da música controlar a trilha do minigame.
			if (AudioServer.GetBusIndex(Settings.MusicBus) >= 0)
			{
				_musicPlayer.Bus = Settings.MusicBus;
			}
			_musicPlayer.Play();
			_musicPlayer.Finished += OnMusicFinished;
			GD.Print("RhythmMinigame: Music loaded and playing.");
		}
		else
		{
			_hasMusicStream = false;
			GD.PrintErr("RhythmMinigame: No music file found! Using elapsed time fallback.");
		}
	}

	public override void _Process(double delta)
	{
		if (!_isPlaying || _gameState.IsGameOver) return;

		// Get current time from music or fallback timer
		float currentTime;
		if (_hasMusicStream)
		{
			currentTime = (float)_musicPlayer.GetPlaybackPosition();
		}
		else
		{
			_elapsedTime += delta;
			currentTime = (float)_elapsedTime;
		}

		// Spawn notes at their scheduled time
		while (_nextNoteIndex < _beatmap.Notes.Count)
		{
			var note = _beatmap.Notes[_nextNoteIndex];
			if (note.TimeSeconds <= currentTime + _beatmap.OffsetSeconds)
			{
				SpawnFoodNote(note);
				_nextNoteIndex++;
			}
			else
			{
				break;
			}
		}

		// If using fallback timer, check if all notes are done and resolved
		if (!_hasMusicStream && _nextNoteIndex >= _beatmap.Notes.Count)
		{
			// Check if all food notes have been resolved
			bool allResolved = true;
			foreach (var child in _foodContainer.GetChildren())
			{
				if (child is FoodNote foodNote && !foodNote.IsResolved)
				{
					allResolved = false;
					break;
				}
			}
			if (allResolved && _foodContainer.GetChildCount() == 0)
			{
				OnMusicFinished();
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_isPlaying || _gameState.IsGameOver) return;

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.Escape)
			{
				PauseGame();
				GetViewport().SetInputAsHandled();
				return;
			}

			ActionType? action = ActionTypeExtensions.FromKey(keyEvent.Keycode);
			if (action == null) return; // Not A, S, or D

			// Find the most urgent food note under the cursor
			FoodNote targetNote = FindFoodNoteUnderCursor();
			if (targetNote == null) return; // No food under cursor = ignored

			if (action == targetNote.FoodType.GetCorrectAction())
			{
				targetNote.ResolveCorrect();
			}
			else
			{
				targetNote.ResolveWrong(action.Value);
			}

			GetViewport().SetInputAsHandled();
		}
	}

	private void PauseGame()
	{
		var pauseScene = GD.Load<PackedScene>("res://scenes/minigame/PauseMenu.tscn");
		if (pauseScene != null)
		{
			GetTree().Paused = true;
			var pauseMenu = pauseScene.Instantiate<PauseMenu>();
			AddChild(pauseMenu);
			pauseMenu.ResumeRequested += () => GetTree().Paused = false;
			pauseMenu.RetryRequested += () => 
			{
				GetTree().Paused = false;
				GetTree().ReloadCurrentScene();
			};
			pauseMenu.MainMenuRequested += () => 
			{
				GetTree().Paused = false;
				GD.Print("Returning to Main Menu... (Main menu scene not defined yet, reloading game)");
				GetTree().ReloadCurrentScene();
			};
		}
	}

	private FoodNote FindFoodNoteUnderCursor()
	{
		FoodNote bestNote = null;
		float leastTimeRemaining = float.MaxValue;

		foreach (var child in _foodContainer.GetChildren())
		{
			if (child is FoodNote note && !note.IsResolved && note.IsMouseOver)
			{
				float timeRemaining = note.GetTimeRemaining();
				if (timeRemaining < leastTimeRemaining)
				{
					leastTimeRemaining = timeRemaining;
					bestNote = note;
				}
			}
		}

		return bestNote;
	}

	private void SpawnFoodNote(BeatmapNote note)
	{
		if (FoodNoteScene == null)
		{
			GD.PrintErr("RhythmMinigame: FoodNoteScene not assigned in inspector!");
			return;
		}

		var foodNote = FoodNoteScene.Instantiate<FoodNote>();

		// Initialize BEFORE AddChild — AddChild triggers _Ready() which needs the textures
		Vector2 pos = PositionGrid.GetPosition(note.PositionIndex);
		foodNote.Initialize(note.Type, pos, _beatmap.TimeoutSeconds);

		_foodContainer.AddChild(foodNote);

		// Connect signals
		foodNote.CorrectAction += OnCorrectAction;
		foodNote.WrongAction += OnWrongAction;
		foodNote.Missed += OnMissed;
	}

	// --- Event Handlers ---

	private void OnCorrectAction(FoodNote note)
	{
		_gameState.RegisterCorrectAction();
		SpawnFeedback(FeedbackType.Success);
		UpdateSpeechBalloon(positive: true);

		// Show combo popup when threshold is reached or exceeded
		if (_gameState.IsComboActive)
		{
			SpawnFeedback(FeedbackType.Combo);
		}
	}

	private void OnWrongAction(FoodNote note)
	{
		_gameState.RegisterWrongAction();
		SpawnFeedback(FeedbackType.WrongAction);
		UpdateSpeechBalloon(positive: false);
	}

	private void OnMissed(FoodNote note)
	{
		_gameState.RegisterMiss();
		SpawnFeedback(FeedbackType.Miss);
		UpdateSpeechBalloon(positive: false);
	}

	private void SpawnFeedback(FeedbackType type)
	{
		if (FeedbackPopupScene == null) return;

		var popup = FeedbackPopupScene.Instantiate<FeedbackPopup>();
		_feedbackContainer.AddChild(popup);
		popup.Initialize(type, _feedbackPosition);
	}

	private void UpdateSpeechBalloon(bool positive)
	{
		// Kill any existing tween
		_speechBalloonTween?.Kill();

		if (positive && _balloonPositive != null)
			_speechBalloon.Texture = _balloonPositive;
		else if (!positive && _balloonNegative != null)
			_speechBalloon.Texture = _balloonNegative;

		_speechBalloon.Visible = true;
		_speechBalloon.Modulate = new Color(1, 1, 1, 1);

		_speechBalloonTween = CreateTween();
		_speechBalloonTween.TweenInterval(1.0f);
		_speechBalloonTween.TweenProperty(_speechBalloon, "modulate:a", 0.0f, 0.3f);
		_speechBalloonTween.TweenCallback(Callable.From(() => _speechBalloon.Visible = false));
	}

	private void UpdateCharacterExpression()
	{
		float healthPercent = _gameState.Health / GameState.MaxHealth;
		Texture2D expression = null;

		if (healthPercent >= 1.0f)
			expression = _characterPerfect ?? _characterHappy;
		else if (healthPercent > 0.7f)
			expression = _characterHappy;
		else if (healthPercent > 0.4f)
			expression = _characterNeutral;
		else if (healthPercent > 0.2f)
			expression = _characterWorried;
		else
			expression = _characterSad;

		if (expression != null)
			_characterSprite.Texture = expression;
	}

	private void OnHealthChanged(float health)
	{
		_healthBar.Value = health;

		float healthPercent = health / GameState.MaxHealth;
		if (healthPercent > 0.7f)
			_healthBarFill.BgColor = new Color(0.2f, 0.8f, 0.2f, 1.0f); // Green
		else if (healthPercent > 0.4f)
			_healthBarFill.BgColor = new Color(0.9f, 0.8f, 0.1f, 1.0f); // Yellow
		else
			_healthBarFill.BgColor = new Color(0.9f, 0.2f, 0.2f, 1.0f); // Red

		UpdateCharacterExpression();
	}

	private void OnComboChanged(int combo)
	{
		if (combo >= GameState.ComboThreshold)
		{
			_comboContainer.Visible = true;
			_comboLabel.Text = $"x{combo}";
		}
		else
		{
			_comboContainer.Visible = false;
		}
	}

	private void OnGameOver()
	{
		_isPlaying = false;
		if (_hasMusicStream)
			_musicPlayer.Stop();
		
		ShowGameOverScreen(false);
	}

	private void OnMusicFinished()
	{
		if (_gameState.IsGameOver) return;
		_isPlaying = false;
		
		ShowGameOverScreen(true);
	}

	private void ShowGameOverScreen(bool isVictory)
	{
		var gameOverScene = GD.Load<PackedScene>("res://scenes/minigame/GameOverScreen.tscn");
		if (gameOverScene != null)
		{
			var gameOverScreen = gameOverScene.Instantiate<GameOverScreen>();
			AddChild(gameOverScreen);
			gameOverScreen.Initialize(isVictory, _gameState.TotalCorrect, _gameState.TotalWrong, _gameState.TotalMiss);
			gameOverScreen.RestartRequested += GoToAfterMinigameDialogue;
		}
		else
		{
			GD.PrintErr("GameOverScreen.tscn not found!");
			GoToAfterMinigameDialogue();
		}
	}

	// Fim do minigame: volta para a cena de diálogo, que toca a conversa
	// pós-minigame (a Lara agradecendo e marcando o próximo encontro).
	private void GoToAfterMinigameDialogue()
	{
		main_scene.StartChapter = main_scene.Chapter.AfterMinigame;
		carregando.NextScene = "res://scenes/main_scene.tscn";
		GetTree().ChangeSceneToFile("res://scenes/carregando.tscn");
	}
}
