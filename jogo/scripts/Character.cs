using Godot;
using System;
using System.Collections.Generic;

public partial class Character : Node
{
	public enum Name
	{
		Lara,
		LaraPeixe,
		LiderDoClube,
		PaiDaLara,
		Fabio,
		Camera
	}

	// Classe auxiliar para tipar os dados do dicionário (muito mais seguro em C#)
	public class CharacterData
	{
		public string CharacterName { get; set; }
		public string Gender { get; set; }
		public SpriteFrames SpriteFrames { get; set; }
	}

	// Equivalente ao `const CHARACTER_DETAILS` do GDScript
	public static readonly Dictionary<Name, CharacterData> CharacterDetails = new Dictionary<Name, CharacterData>
	{
		{
			Name.Lara, new CharacterData
			{
				CharacterName = "Lara",
				Gender = "female",
				SpriteFrames = GD.Load<SpriteFrames>("res://assets/sprites/lara.tres")
			}
		},
		{
			Name.LaraPeixe, new CharacterData
			{
				CharacterName = "Lara Peixe",
				Gender = "female",
				SpriteFrames = GD.Load<SpriteFrames>("res://assets/sprites/laraPeixe.tres")
			}
		},
		{
			Name.LiderDoClube, new CharacterData
			{
				CharacterName = "Lider do Clube",
				Gender = "female",
				SpriteFrames = GD.Load<SpriteFrames>("res://assets/sprites/liderDoClube.tres")
			}
		},
		{
			Name.PaiDaLara, new CharacterData
			{
				CharacterName = "Pai da Lara",
				Gender = "male",
				SpriteFrames = GD.Load<SpriteFrames>("res://assets/sprites/paiLara.tres")
			}
		},
		{
			Name.Fabio, new CharacterData
			{
				CharacterName = "Fábio",
				Gender = "male",
				// Placeholder: reusa um sprite existente até o Fábio ter o dele.
				SpriteFrames = GD.Load<SpriteFrames>("res://assets/sprites/fabio.tres")
			}
		},
		{
			Name.Camera, new CharacterData
			{
				CharacterName = "Câmera",
				Gender = "male",
				// O "Câmera" é o ponto de vista do jogador: não tem corpo na tela.
				// SpriteFrames null => ChangeCharacter mantém quem já estava aparecendo.
				SpriteFrames = null
			}
		}
	};

	// Equivalente ao `static func get_enum_from_string`
	public static int GetEnumFromString(string stringValue)
	{
		// Enum.TryParse já resolve a conversão, ignorando maiúsculas/minúsculas (o parâmetro 'true')
		if (Enum.TryParse(stringValue, true, out Name parsedEnum))
		{
			return (int)parsedEnum;
		}
		else
		{
			GD.PushError($"Invalid Character name: {stringValue}");
			return -1; // Ou qualquer outro valor para indicar erro
		}
	}
}
