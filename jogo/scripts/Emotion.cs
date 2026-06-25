// Emoções que uma fala pode pedir.
// O nome de cada valor (em minúsculo) precisa bater com o nome da animação
// dentro do SpriteFrames do personagem. Ex.: Emotion.Angry -> animação "angry".
// Quando o personagem não estiver falando, usamos sempre "idle".
public enum Emotion
{
	Idle,
	Talking,
	Dissapointed,
	Worried,
	Angry,
	Flushed,

	// Bônus: a Lara Peixe tem essas duas animações nos assets dela.
	Crying,
	Ashamed,
	Cry
}
