namespace ConsoleGameEngine;

public struct Glyph
{
	public char Character;
	public int ForegroundColor;
	public int BackgroundColor;
	
	public void Set(char character)
	{
		this.Set(character);
	}

	public void Set(char character, int foregroundColor)
	{
		this.Set(character,foregroundColor,BackgroundColor);
	}

	public void Set(char character, int foregroundColor, int backgroundColor)
	{
		this.Character = character;
		this.ForegroundColor = foregroundColor;
		this.BackgroundColor = backgroundColor;
	}

	public void Clear()
	{
		this.Character = (char)0;
		this.ForegroundColor = 0;
		this.BackgroundColor = 0;
	}
}
