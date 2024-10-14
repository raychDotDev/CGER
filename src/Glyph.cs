namespace ConsoleGameEngine;
public struct Glyph
{
	public char character;
	public int foregroundColor;
	public int backgroundColor;

	public void set(char c_, int fg_, int bg_) { character = c_; foregroundColor = fg_; backgroundColor = bg_; }

	public void clear() { character = (char)0; foregroundColor = 0; backgroundColor = 0; }
}
