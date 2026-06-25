using Godot;

// Tela de Carregando — ESTRITAMENTE FIEL ao JOGAR.png: a imagem exata do design
// (título + ovelhas) é o fundo, com um leve "respiro" para não ficar congelada,
// enquanto a próxima cena carrega em background (threaded), com tempo mínimo. Uso:
//   carregando.NextScene = "res://scenes/...tscn";
//   GetTree().ChangeSceneToFile("res://scenes/carregando.tscn");
public partial class carregando : Control
{
	public static string NextScene = "res://scenes/main_scene.tscn";

	private const double MinTime = 1.6;

	private Control _frame;
	private double _time;
	private bool _done;
	private bool _loadStarted;

	public override void _Ready()
	{
		AnchorRight = 1f;
		AnchorBottom = 1f;

		Control root = MenuUI.DesignScreen("screen_loading.png", out _frame);
		AddChild(root);
	}

	public override void _Process(double delta)
	{
		_time += delta;

		// "Respiro" sutil na arte toda (sem quebrar o layout do container).
		if (_frame != null)
		{
			_frame.PivotOffset = _frame.Size / 2f;
			float s = 1f + Mathf.Sin((float)_time * 2.2f) * 0.012f;
			_frame.Scale = new Vector2(s, s);
		}

		// Carregamento assíncrono da próxima cena.
		if (!_loadStarted)
		{
			_loadStarted = true;
			if (ResourceLoader.Exists(NextScene))
			{
				ResourceLoader.LoadThreadedRequest(NextScene);
			}
		}

		if (_done || !ResourceLoader.Exists(NextScene))
		{
			return;
		}

		var status = ResourceLoader.LoadThreadedGetStatus(NextScene);
		if (status == ResourceLoader.ThreadLoadStatus.Loaded && _time >= MinTime)
		{
			_done = true;
			var packed = (PackedScene)ResourceLoader.LoadThreadedGet(NextScene);
			GetTree().ChangeSceneToPacked(packed);
		}
		else if (status == ResourceLoader.ThreadLoadStatus.Failed)
		{
			_done = true;
			GD.PrintErr($"carregando: falha ao carregar {NextScene}");
			GetTree().ChangeSceneToFile(NextScene);
		}
	}
}
