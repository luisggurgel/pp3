using Godot;

public partial class character_sprite : Node2D // Ou o tipo correto do seu nó raiz do personagem
{
	// Precisamos referenciar o nó AnimatedSprite2D que está dentro da cena do personagem
	private AnimatedSprite2D _animatedSprite;

	public override void _Ready()
	{
		// Pegamos o nó AnimatedSprite2D na árvore (ajuste o nome se for diferente)
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	}

	// Troca o personagem na tela e já toca a animação da emoção pedida pela fala.
	public void ChangeCharacter(Character.Name characterName, Emotion emotion = Emotion.Talking)
	{
		SpriteFrames spriteFrames = Character.CharacterDetails[characterName].SpriteFrames;

		// Se esse personagem não tiver sprites (ex.: Apollo), não há o que mostrar.
		if (spriteFrames == null)
		{
			return;
		}

		// Garante que o personagem volte a aparecer caso a fala anterior fosse sem ninguém.
		Visible = true;

		_animatedSprite.SpriteFrames = spriteFrames;
		PlayEmotion(emotion);
	}

	// Esconde o personagem da tela: usado em falas/cenas sem nenhum personagem (narração).
	public void HideCharacter()
	{
		Visible = false;
		_animatedSprite?.Stop();
	}

	// Toca a animação da emoção. O nome do enum (em minúsculo) vira o nome da animação.
	public void PlayEmotion(Emotion emotion)
	{
		PlayAnimationSafe(emotion.ToString().ToLowerInvariant());
	}

	// O método play_idle_animation que também era usado na cena principal
	public void PlayIdleAnimation()
	{
		PlayAnimationSafe("idle");
	}

	// Toca a animação se ela existir; senão cai pra "talking" e, por fim, "idle".
	// Assim, pedir uma emoção que o personagem não tem nunca quebra o jogo.
	private void PlayAnimationSafe(string animationName)
	{
		if (_animatedSprite == null || _animatedSprite.SpriteFrames == null)
		{
			return;
		}

		if (_animatedSprite.SpriteFrames.HasAnimation(animationName))
		{
			_animatedSprite.Play(animationName);
		}
		else if (_animatedSprite.SpriteFrames.HasAnimation("talking"))
		{
			_animatedSprite.Play("talking");
		}
		else if (_animatedSprite.SpriteFrames.HasAnimation("idle"))
		{
			_animatedSprite.Play("idle");
		}
	}
}
