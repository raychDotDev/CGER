namespace ConsoleGameEngine;

/// <summary> Represents an RGB color. </summary>
public class Color
{
	/// <summary> Red component. </summary>
	public byte R { get; set; }
	/// <summary> Green component. </summary>
	public byte G { get; set; }
	/// <summary> Bkue component. </summary>
	public byte B { get; set; }

	/// <summary> Creates a new Color from rgb. </summary>
	public Color(byte r, byte g, byte b)
	{
		this.R = r;
		this.G = g;
		this.B = b;
	}
}
