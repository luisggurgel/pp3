// Representa UMA fala do diálogo.
// Aqui você controla, por fala: quem fala, com qual emoção, o texto
// e (opcionalmente) para qual cenário a cena deve mudar.
public class DialogLine
{
	// Quem está falando. Deixe null para uma cena/narração sem nenhum
	// personagem na tela (o sprite some e o nome do falante fica vazio).
	public Character.Name? Speaker { get; set; } = null;

	// Emoção da fala. Enquanto o texto aparece, toca essa animação.
	public Emotion Emotion { get; set; } = Emotion.Talking;

	// Emoção depois de terminar de falar. Deixe null para voltar pra "idle".
	public Emotion? EmotionAfter { get; set; } = null;

	// O texto da fala (aceita BBCode, ex.: [shake]...[/shake]).
	public string Text { get; set; }

	// Cenário desta fala. Deixe null para manter o cenário atual,
	// ou escolha um Scenario.Name para trocar o fundo.
	public Scenario.Name? Scenario { get; set; } = null;

	// Quando true, ao terminar de mostrar esta fala a cena abre os botões de
	// escolha (ex.: "ajudar a Lara?") em vez de simplesmente avançar.
	public bool TriggerChoice { get; set; } = false;

	// Troca a música ambiente do "dia" ao chegar nesta fala (em tempo real, sem
	// reiniciar a cena). Deixe null para manter a música atual.
	// Ex.: ChangeMusic = Audio.Track.Day2
	public Audio.Track? ChangeMusic { get; set; } = null;

	// Efeitos sonoros tocados ao chegar nesta fala, em sequência (um após o outro),
	// SEM parar a música. Deixe null para nenhum.
	// Ex.: PlaySfx = new[] { Audio.Sfx.Knock, Audio.Sfx.Door }
	public Audio.Sfx[] PlaySfx { get; set; } = null;
}
