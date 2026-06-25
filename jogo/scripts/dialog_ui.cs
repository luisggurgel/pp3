using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public partial class dialog_ui : Control
{
	[Signal]
	public delegate void AnimationDoneEventHandler();

	public bool AnimateText { get; private set; } = false;

	// Velocidade padrão (caracteres por segundo) quando a fala não usa [speed=...].
	private const float DefaultSpeed = 30f;

	private RichTextLabel _speakerName;
	private RichTextLabel _dialogLine;

	// Velocidade de cada caractere visível (preenchido ao processar [speed=...]).
	private float[] _speeds = Array.Empty<float>();
	// O caractere visível em cada posição (para o "blip" pular espaços/pontuação).
	private char[] _glyphs = Array.Empty<char>();
	// Quantos caracteres já "acumulamos" (com fração) pra revelar.
	private float _charProgress = 0f;

	// Controle do "blip" estilo Undertale: até onde já tocamos e um pequeno
	// intervalo mínimo entre blips (pra não virar zumbido em velocidades altas).
	private int _blipCount = 0;
	private double _blipCooldown = 0;
	private const double BlipInterval = 0.035;

	public override void _Ready()
	{
		_speakerName = GetNode<RichTextLabel>("%SpeakerName");
		_dialogLine = GetNode<RichTextLabel>("%DialogLine");
	}

	public override void _Process(double delta)
	{
		if (!AnimateText)
		{
			return;
		}

		int total = _dialogLine.GetTotalCharacterCount();

		// A velocidade vale para o próximo caractere a ser revelado.
		int index = Mathf.Clamp(_dialogLine.VisibleCharacters, 0, Math.Max(_speeds.Length - 1, 0));
		float speed = _speeds.Length > 0 ? _speeds[index] : DefaultSpeed;

		_charProgress += speed * (float)delta;
		_dialogLine.VisibleCharacters = Math.Min((int)_charProgress, total);

		PlayBlips(_dialogLine.VisibleCharacters, delta);

		if (_dialogLine.VisibleCharacters >= total)
		{
			_dialogLine.VisibleCharacters = -1; // -1 = mostra o texto inteiro.
			AnimateText = false;
			EmitSignal(SignalName.AnimationDone);
		}
	}

	// Toca o "blip" para cada nova letra revelada desde o último frame, pulando
	// espaços/quebras de linha, com um intervalo mínimo entre sons.
	private void PlayBlips(int visible, double delta)
	{
		_blipCooldown -= delta;

		if (visible <= _blipCount)
		{
			return;
		}

		bool revealedGlyph = false;
		for (int c = _blipCount; c < visible && c < _glyphs.Length; c++)
		{
			if (!char.IsWhiteSpace(_glyphs[c]))
			{
				revealedGlyph = true;
				break;
			}
		}
		_blipCount = visible;

		if (revealedGlyph && _blipCooldown <= 0)
		{
			Audio.Instance?.TextBlip();
			_blipCooldown = BlipInterval;
		}
	}

	public void ChangeLine(Character.Name? characterName, string line)
	{
		// Sem personagem (null) -> nome do falante vazio (narração).
		_speakerName.Text = characterName.HasValue
			? Character.CharacterDetails[characterName.Value].CharacterName
			: "";

		// Extrai as tags [speed=...] e monta a velocidade por caractere.
		_speeds = ParseSpeeds(line, out string cleanText, out _glyphs);

		_dialogLine.Text = cleanText;
		_dialogLine.VisibleCharacters = 0;
		_charProgress = 0f;
		_blipCount = 0;
		_blipCooldown = 0;

		AnimateText = true;
	}

	// Esvazia a caixa de diálogo (nome + texto) sem animar nada. Usado durante a
	// tela preta da intro, para a cena ser revelada em silêncio.
	public void Clear()
	{
		AnimateText = false;
		_speakerName.Text = "";
		_dialogLine.Text = "";
		_dialogLine.VisibleCharacters = -1;
	}

	public void SkipTextAnimation()
	{
		_dialogLine.VisibleCharacters = -1; // mostra tudo
		_charProgress = _speeds.Length;
		AnimateText = false;
		EmitSignal(SignalName.AnimationDone);
	}

	// Remove as tags [speed=X] / [/speed] do texto e devolve, para cada caractere
	// VISÍVEL, qual velocidade (chars/seg) deve ser usada ao revelá-lo.
	// As demais tags BBCode ([wave], [shake], ...) são mantidas no texto e não
	// contam como caractere visível (assim batem com o VisibleCharacters do Godot).
	private static float[] ParseSpeeds(string rawText, out string cleanText, out char[] glyphs)
	{
		var clean = new StringBuilder();
		var speeds = new List<float>();
		var visible = new List<char>();
		float current = DefaultSpeed;

		int i = 0;
		while (i < rawText.Length)
		{
			if (rawText[i] == '[')
			{
				int close = rawText.IndexOf(']', i);
				if (close != -1)
				{
					string tag = rawText.Substring(i + 1, close - i - 1);
					string tagLower = tag.ToLowerInvariant();

					if (tagLower.StartsWith("speed="))
					{
						string value = tag.Substring("speed=".Length);
						if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float s) && s > 0f)
						{
							current = s;
						}
						i = close + 1;
						continue; // não vai pro texto final
					}

					if (tagLower == "/speed")
					{
						current = DefaultSpeed;
						i = close + 1;
						continue;
					}

					// Outra tag BBCode: mantém no texto, mas não conta como caractere visível.
					clean.Append(rawText, i, close - i + 1);
					i = close + 1;
					continue;
				}
			}

			// Caractere visível normal.
			clean.Append(rawText[i]);
			speeds.Add(current);
			visible.Add(rawText[i]);
			i++;
		}

		cleanText = clean.ToString();
		glyphs = visible.ToArray();
		return speeds.ToArray();
	}
}
