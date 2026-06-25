using Godot;

namespace PP3.Minigame;

public static class PositionGrid
{
	public const int TotalPositions = 40;
	private const int Columns = 8;
	private const int Rows = 5;
	private const float MarginX = 150f;
	private const float MarginY = 150f;
	private const float ScreenWidth = 1920f;
	private const float ScreenHeight = 1080f;

	private static readonly Vector2[] _positions;

	static PositionGrid()
	{
		_positions = new Vector2[TotalPositions];
		float usableWidth = ScreenWidth - (MarginX * 2);
		float usableHeight = ScreenHeight - (MarginY * 2);
		float spacingX = usableWidth / (Columns - 1);
		float spacingY = usableHeight / (Rows - 1);

		for (int row = 0; row < Rows; row++)
		{
			for (int col = 0; col < Columns; col++)
			{
				int index = row * Columns + col;
				_positions[index] = new Vector2(
					MarginX + col * spacingX,
					MarginY + row * spacingY
				);
			}
		}
	}

	public static Vector2 GetPosition(int index)
	{
		if (index < 0 || index >= TotalPositions)
		{
			GD.PrintErr($"PositionGrid: Index {index} out of range (0-{TotalPositions - 1})");
			return new Vector2(ScreenWidth / 2, ScreenHeight / 2);
		}
		return _positions[index];
	}
}
