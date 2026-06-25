using Godot;
using System.Collections.Generic;

public partial class main_scene : Node2D
{
	// Tipo alterado para bater com a classe em minúsculo
	private character_sprite _characterSprite;
	private dialog_ui _dialogUi;
	private TextureRect _background;
	private int _dialogIndex = 0;

	// Falas que estão de fato rodando (cópia do roteiro abaixo; pode crescer em
	// tempo de execução quando uma escolha adiciona um ramo, ex.: "ajudar a Lara").
	private List<DialogLine> _lines;

	// Controle da escolha do fim do diálogo.
	private choice_ui _choiceUi;        // UI dos botões enquanto está na tela.
	private bool _awaitingChoice;       // true = esperando o jogador clicar num botão.
	private bool _choiceResolved;       // true = escolha já feita, diálogo segue.
	private bool _helpBranchActive;     // true = ramo "Ajudar" em andamento (termina no minigame).
	private bool _minigameLaunched;     // trava p/ não disparar o minigame mais de uma vez.

	// Capítulos do roteiro. A cena pode abrir direto em qualquer um (usado pelo
	// minigame pra voltar no capítulo certo, e pra testar capítulos isolados).
	public enum Chapter { Intro, AfterMinigame, Day2 }

	// Ponto de entrada: qual capítulo tocar ao abrir a cena. O minigame liga isto
	// ao terminar; é consumido (volta a Intro) assim que a cena começa.
	public static Chapter StartChapter = Chapter.Intro;

	private Chapter _chapter;           // capítulo em andamento.
	private bool _day2Started;          // trava p/ não disparar a transição do Dia 2 mais de uma vez.
	private bool _transitioning;        // true durante o cartão "DIA 2" (bloqueia avançar).

	// Agora cada fala carrega: quem fala, a emoção, o texto e (opcional) o cenário.
	// - Emotion: animação tocada enquanto fala (volta pra idle ao terminar).
	// - Scenario: deixe de fora pra manter o cenário; defina pra trocar o fundo.
	private readonly DialogLine[] _dialogLines = new DialogLine[]
	{
		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Scenario = Scenario.Name.CasaLara,
			Text = "Mas pai, eu já tinha dito que ia sair com meu namorado hoje! O sr. sempre coloca os seus compromissos acima dos meus!"
		},
		new DialogLine
		{
			Speaker = Character.Name.PaiDaLara,
			Emotion = Emotion.Angry,
			Text = "[shake rate=30.0 level=10 connected=1]ISSO TÁ FORA DE DEBATE![/shake]\n[shake rate=30.0 level=10 connected=1]SE EU FALEI, TÁ FALADO, E PONTO FINAL![/shake]"
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Cry,
			EmotionAfter = Emotion.Cry,
			Text = "[speed=3.0]:([/speed]"
		},

		// Cena sem nenhum personagem: deixe o Speaker de fora (fica null).
		new DialogLine
		{
			Emotion = Emotion.Talking,
			Text = "[speed=2.0]...[/speed]",
			// Antes do "Oi bebê": batida na porta e depois a porta abrindo/fechando.
			PlaySfx = new[] { Audio.Sfx.Knock, Audio.Sfx.Door }
		},
				new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "[wave]Oi bebê :)[/wave]"
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Ashamed,
			Text = "Desculpa isso."
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "Meu dia tá um caos."
		},	

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Scenario = Scenario.Name.CasaLara,
			Text = "[tornado]Tipo.[/tornado]"
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "[shake]CAOS[/shake] mesmo."
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Ashamed,
			Text = "Nossa eu tava [wave]morrendo de saudade[/wave] de você."
		},

		// --- Lara mostra a presilha, fica empolgada com o cantor e depois desanima ---

		// (Lara mostra a presilha que o protagonista deu a ela.)
		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "Ah olha."
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "Tô usando aquela presilha que você me deu."
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "Mesmo ela apertando minha cabeça igual um instrumento medieval de tortura."
		},

		// (Ela ri.)
		// Ela para por um segundo, olha pro protagonista e sorri menor agora.
		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Ashamed,
			Text = "[speed=2.0]...[/speed]"
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Ashamed,
			Text = "Mas eu tava com saudade de você."
		},

		// Pausa curta.
		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Idle,
			Text = "[speed=2.0]...[/speed]"
		},

		// (Aí ela lembra do cantor e se empolga.)
		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "[shake]AH E FALANDO EM SAUDADE.[/shake]"
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "Lembra daquele cantor que eu falei que eu casaria com ele?"
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "[shake]ELE VEM PRA CÁ.[/shake]"
		},

		// (Ela aponta aleatoriamente pra cima.)
		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "Tipo."
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "Aqui."
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "Na nossa cidade."
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Ashamed,
			Text = "Isso é muito perigoso pra mim emocionalmente."
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "A gente devia ir."
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "Ia ser muito [wave]top[/wave]."
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "Enfim, eu tenho que voltar ao trabalho, é muita coisa pra fazer."
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Ashamed,
			Text = "Acho que você já entendeu que a nossa saída hoje não vai rolar, né?"
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Ashamed,
			Text = "Você viu como ele tava bravo?"
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Crying,
			EmotionAfter = Emotion.Cry,
			// Ao terminar esta fala, abre a escolha: o câmera ajuda a Lara ou não?
			TriggerChoice = true,
			Text = "E nem é a primeira vez que ele me deixa só cuidando de tudo… [speed=10.0]Às vezes dá vontade de sumir.[/speed]"
		},
	};

	// Ramo "Ajudar": acrescentado ao diálogo quando o jogador clica em "Ajudar".
	// (Se ele clicar em "Não ajudar", nada acontece — o botão é bugado de propósito.)
	private readonly DialogLine[] _helpResponse = new DialogLine[]
	{
		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			EmotionAfter = Emotion.Ashamed,
			Text = "[shake]Você vai me ajudar?!! Meu heroiii[/shake]"
		},

		// (Lara beija o câmera.)
		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Ashamed,
			Text = "[wave]*Beija o câmera*[/wave]"
		},
	};

	// Conversa PÓS-MINIGAME: tocada quando a cena reabre após o minigame terminar
	// (a Lara agradecendo e já marcando o próximo encontro). É ligada pelo minigame
	// através de main_scene.StartAfterMinigame.
	private readonly DialogLine[] _afterMinigameLines = new DialogLine[]
	{
		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Scenario = Scenario.Name.CasaLara,
			Text = "Cê me salvou hoje, pra variar né?"
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Flushed,
			EmotionAfter = Emotion.Flushed,
			Text = "Acho que se você não existisse seria meu fim!\n[wave]*risos*[/wave]"
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "Ah, e falando em fim…"
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "A gente [shake]PRECISA[/shake] fazer alguma coisa pra comemorar o fim do ensino Médio, né? Sim ou claro?"
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "Tava até pensando em marcar um encontro na pracinha pra gente definir qual vai ser o grande evento que vamos fazer."
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Flushed,
			Text = "Amanhã você consegue?"
		},

		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "Naquele horário de sempre? Eu te mando um sapo."
		},
	};

	// DIA 2 — Praça da Gentilândia. Câmera é o ponto de vista do jogador (sprite
	// null: não tem corpo, e quem estava na tela continua aparecendo). Fábio usa um
	// sprite-placeholder por enquanto.
	private readonly DialogLine[] _day2Lines = new DialogLine[]
	{
		// Cena: a praça absurdamente colorida e viva (narração, sem ninguém na tela).
		new DialogLine
		{
			Speaker = null,
			Scenario = Scenario.Name.Gentilandia,
			Text = "A praça da Gentilândia é [wave]extremamente colorida[/wave]. Pessoas passam correndo, sorrindo, dançando."
		},
		new DialogLine
		{
			Speaker = null,
			Text = "Tudo parece [wave]exageradamente vivo[/wave]."
		},

		// Fábio entra em cena pegando a bala do chão.
		new DialogLine
		{
			Speaker = Character.Name.LiderDoClube,
			Emotion = Emotion.Idle,
			Text = "[wave]*Fábio pega uma bala amassada no chão.*[/wave]"
		},

		// Câmera (POV): Fábio continua na tela, só muda o nome do falante.
		new DialogLine
		{
			Speaker = null,
			Text = "Tu vai comer isso mesmo?"
		},
		new DialogLine
		{
			Speaker = Character.Name.LiderDoClube,
			Emotion = Emotion.Talking,
			Text = "Claro."
		},
		new DialogLine
		{
			Speaker = Character.Name.LiderDoClube,
			Emotion = Emotion.Talking,
			Text = "[wave]*Ele abre a bala.*[/wave]"
		},
		new DialogLine
		{
			Speaker = Character.Name.LiderDoClube,
			Emotion = Emotion.Talking,
			Text = "Uma vez meu tio atropelou um cara e só descobriu porque o corpo ficou preso no carro."
		},
		new DialogLine
		{
			Speaker = Character.Name.LiderDoClube,
			Emotion = Emotion.Talking,
			Text = "[wave]*Ele coloca a bala na boca.*[/wave]"
		},
		new DialogLine
		{
			Speaker = Character.Name.LiderDoClube,
			Emotion = Emotion.Talking,
			Text = "Ele dirigiu uns vinte minutos assim."
		},

		// Lara aparece.
		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "Oi gentee."
		},

		// Silêncio: os dois se viram pra ela (narração curta, sem ninguém na tela).
		new DialogLine
		{
			Speaker = null,
			Text = "[speed=18.0]Silêncio curto. Os dois se viram e olham pra ela.[/speed]"
		},

		// Fábio continua a história como se nada tivesse acontecido.
		new DialogLine
		{
			Speaker = Character.Name.Fabio,
			Emotion = Emotion.Talking,
			Text = "...aí ele vendeu o carro."
		},
		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "Nossa."
		},
		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "[wave]*Ela senta.*[/wave]"
		},
		new DialogLine
		{
			Speaker = Character.Name.LaraPeixe,
			Emotion = Emotion.Talking,
			Text = "Vocês começaram sem mim."
		},
	};


	public override void _Ready()
	{
		// Como o seu nó na árvore se chama "CharacterSprite" (sem o %), buscamos pelo caminho relativo se ele for filho direto da MainScene
		_characterSprite = GetNode<character_sprite>("%CharacterSprite");
		_dialogUi = GetNode<dialog_ui>("%DialogUI");

		// O fundo é o TextureRect dentro do nó "Scenario" da cena principal.
		_background = GetNode<TextureRect>("%Scenario/TextureRect");

		_dialogUi.AnimationDone += OnAnimationDone;

		// Escolhe o capítulo a tocar e consome o flag (para que um "JOGAR" normal
		// depois volte ao início).
		_chapter = StartChapter;
		StartChapter = Chapter.Intro;
		DialogLine[] script = _chapter switch
		{
			Chapter.AfterMinigame => _afterMinigameLines,
			Chapter.Day2 => _day2Lines,
			_ => _dialogLines,
		};

		// Trabalhamos sobre uma cópia mutável do roteiro (ramos de escolha entram nela).
		_lines = new List<DialogLine>(script);
		_dialogIndex = 0;

		if (_chapter == Chapter.Intro)
		{
			// Dia 1 abre com o cartão "DIA 1": tudo em SILÊNCIO e o diálogo só começa
			// quando a introdução terminar.
			StartDay1Intro();
		}
		else
		{
			// Música ambiente: Dia 2 tem a trilha feliz; demais, a do Dia 1.
			Audio.Instance?.PlayMusic(_chapter == Chapter.Day2 ? Audio.Track.Day2 : Audio.Track.Day1);
			ProcessCurrentLine();
		}
	}

	// Intro do Dia 1 (estilo Smiling Friends): tela preta + "DIA 1", em silêncio.
	// O diálogo e a música só começam quando o cartão terminar.
	private void StartDay1Intro()
	{
		_transitioning = true; // bloqueia avançar enquanto a intro roda.

		var card = new day_title_card { DayText = "DIA 1", FadeIn = false };
		card.Blackout += () => PrepareSceneFor(_lines[0]); // prepara a cena atrás do preto.
		card.Finished += () =>
		{
			// Intro acabou: agora sim começa o diálogo e a música.
			_transitioning = false;
			Audio.Instance?.PlayMusic(Audio.Track.Day1);
			ProcessCurrentLine();
		};
		AddChild(card);
	}

	// Prepara o cenário/personagem de uma fala SEM começar a falar (caixa de diálogo
	// vazia, em silêncio). Usado por trás da tela preta da intro.
	private void PrepareSceneFor(DialogLine line)
	{
		if (line.Scenario.HasValue && _background != null)
		{
			_background.Texture = Scenario.Load(line.Scenario.Value);
		}
		if (_characterSprite != null && line.Speaker.HasValue)
		{
			_characterSprite.ChangeCharacter(line.Speaker.Value, Emotion.Idle);
		}
		else
		{
			_characterSprite?.HideCharacter();
		}
		_dialogUi.Clear();
	}

	private void OnAnimationDone()
	{
		DialogLine line = _lines[_dialogIndex];

		// Atualiza a emoção pós-fala do personagem (se houver alguém na cena).
		if (_characterSprite != null && line.Speaker.HasValue)
		{
			// Terminou de falar: toca a emoção pós-fala escolhida, ou idle se não houver.
			if (line.EmotionAfter.HasValue)
			{
				_characterSprite.PlayEmotion(line.EmotionAfter.Value);
			}
			else
			{
				_characterSprite.PlayIdleAnimation();
			}
		}

		// Ponto de decisão: ao terminar de mostrar a fala, abre os botões de escolha.
		if (line.TriggerChoice && !_choiceResolved && _choiceUi == null)
		{
			ShowChoice();
		}
	}

	public override void _Input(InputEvent @event)
	{
		// Durante o cartão "DIA 2" (transição), ignora qualquer input.
		if (_transitioning)
		{
			return;
		}

		// ESC abre a pausa (overlay que pausa a árvore do jogo).
		if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape)
		{
			AddChild(new pause_menu());
			GetViewport().SetInputAsHandled();
			return;
		}

		if (!@event.IsActionPressed("next_line"))
		{
			return;
		}

		// Enquanto a escolha está na tela, nada de avançar pelo teclado/clique:
		// o jogador precisa decidir clicando num botão (e "next_line" também é o clique).
		if (_awaitingChoice)
		{
			return;
		}

		if (_dialogUi.AnimateText)
		{
			_dialogUi.SkipTextAnimation();
			return;
		}

		// No ponto de decisão (antes de escolher), o avanço fica travado.
		DialogLine line = _lines[_dialogIndex];
		if (line.TriggerChoice && !_choiceResolved)
		{
			return;
		}

		if (_dialogIndex < _lines.Count - 1)
		{
			_dialogIndex++;
			ProcessCurrentLine();
		}
		else if (_helpBranchActive && !_minigameLaunched)
		{
			// Fim do ramo "Ajudar": o câmera vai de fato ajudar a Lara no trabalho,
			// então entramos no minigame de ritmo (vindo do projeto pp3).
			LaunchMinigame();
		}
		else if (_chapter == Chapter.AfterMinigame && !_day2Started)
		{
			// Fim da conversa pós-minigame: abre o Dia 2 (cartão "DIA 2" + nova cena).
			StartDay2Transition();
		}
	}

	// Mostra o cartão "DIA 2" (tela preta estilo Smiling Friends) e, sob a tela
	// preta, troca o roteiro/cena para o Dia 2 — tudo na MESMA cena, sem recarregar.
	private void StartDay2Transition()
	{
		_day2Started = true;
		_transitioning = true;

		var card = new day_title_card { DayText = "DIA 2" };
		card.Blackout += OnDay2Blackout;
		card.Finished += () => _transitioning = false;
		AddChild(card);
	}

	private void OnDay2Blackout()
	{
		// Tela 100% preta: troca o conteúdo sem o jogador ver o "corte".
		_chapter = Chapter.Day2;
		_lines = new List<DialogLine>(_day2Lines);
		_dialogIndex = 0;
		Audio.Instance?.PlayMusic(Audio.Track.Day2);
		ProcessCurrentLine();
	}

	// Troca a cena atual pelo minigame de ritmo. Usar ChangeSceneToFile isola
	// totalmente o minigame do fluxo de diálogo: nenhum nó, herança ou input é
	// compartilhado entre as duas cenas, evitando conflitos.
	private void LaunchMinigame()
	{
		_minigameLaunched = true;
		// Passa pela tela de carregando antes do minigame.
		carregando.NextScene = "res://scenes/minigame/RhythmMinigame.tscn";
		GetTree().ChangeSceneToFile("res://scenes/carregando.tscn");
	}

	// Mostra os botões de escolha e trava o avanço normal do diálogo.
	private void ShowChoice()
	{
		_awaitingChoice = true;
		_choiceUi = new choice_ui();
		_choiceUi.HelpChosen += OnHelpChosen;
		AddChild(_choiceUi);
	}

	// Jogador clicou em "Ajudar": fecha os botões e emenda a reação da Lara no diálogo.
	private void OnHelpChosen()
	{
		_awaitingChoice = false;
		_choiceResolved = true;

		if (_choiceUi != null)
		{
			_choiceUi.HelpChosen -= OnHelpChosen;
			_choiceUi.QueueFree();
			_choiceUi = null;
		}

		_lines.AddRange(_helpResponse);
		_helpBranchActive = true; // ao fim deste ramo, abre o minigame.
		_dialogIndex++;
		ProcessCurrentLine();
	}

	private void ProcessCurrentLine()
	{
		DialogLine line = _lines[_dialogIndex];

		// Troca a música ambiente em tempo real, se esta fala pedir (sem reiniciar a cena).
		if (line.ChangeMusic.HasValue)
		{
			Audio.Instance?.PlayMusic(line.ChangeMusic.Value);
		}

		// Toca efeitos sonoros desta fala (em sequência), sem parar a música.
		if (line.PlaySfx != null && line.PlaySfx.Length > 0)
		{
			Audio.Instance?.PlaySequence(line.PlaySfx);
		}

		// Troca o cenário, se esta fala pedir um.
		if (line.Scenario.HasValue && _background != null)
		{
			_background.Texture = Scenario.Load(line.Scenario.Value);
		}

		// Mostra o texto na UI (nome do falante vazio quando não há personagem).
		_dialogUi.ChangeLine(line.Speaker, line.Text);

		// Troca o personagem na tela e toca a emoção desta fala.
		// Sem Speaker (null) -> esconde o personagem (cena/narração sem ninguém).
		if (_characterSprite != null)
		{
			if (line.Speaker.HasValue)
			{
				_characterSprite.ChangeCharacter(line.Speaker.Value, line.Emotion);
			}
			else
			{
				_characterSprite.HideCharacter();
			}
		}
	}

	public override void _ExitTree()
	{
		if (_dialogUi != null)
		{
			_dialogUi.AnimationDone -= OnAnimationDone;
		}

		if (_choiceUi != null)
		{
			_choiceUi.HelpChosen -= OnHelpChosen;
		}
	}
}
