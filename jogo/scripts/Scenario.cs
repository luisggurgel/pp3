using Godot;
using System.Collections.Generic;

// Registro de cenários (imagens de fundo).
// Para adicionar um cenário novo: coloque o .png em res://resources/,
// adicione um valor no enum Name e o caminho no dicionário Paths.
public static class Scenario
{
	public enum Name
	{
		CasaLara,
		Gentilandia,
		Escola,
		Xdd

	}

	private static readonly Dictionary<Name, string> Paths = new Dictionary<Name, string>
	{
		{ Name.CasaLara, "res://resources/cenario casa lara.png" },
		{ Name.Gentilandia, "res://resources/gentilandia.png" },
		{ Name.Escola, "res://resources/escola.jpg" },
		{ Name.Xdd, "res://resources/xdd.png" }
	};

	public static Texture2D Load(Name name)
	{
		return GD.Load<Texture2D>(Paths[name]);
	}
}
