namespace ConsoleGameEngine;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
	public int X;
	public int Y;
}

[StructLayout(LayoutKind.Sequential)]
public struct Coord
{
	public short X;
	public short Y;

	public Coord(short X, short Y)
	{
		this.X = X;
		this.Y = Y;
	}
}

[StructLayout(LayoutKind.Sequential)]
public struct Rect
{
	public int Left;
	public int Top;
	public int Right;
	public int Bottom;
}

[StructLayout(LayoutKind.Sequential)]
public struct SmallRect
{
	public short Left;
	public short Top;
	public short Right;
	public short Bottom;
}

[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
public struct CharInfo
{
	[FieldOffset(0)] public char UnicodeChar;
	[FieldOffset(0)] public byte AsciiChar;
	[FieldOffset(2)] public short Attributes;
}

[StructLayout(LayoutKind.Sequential)]
public struct ColorRef
{
	internal uint ColorDWORD;

	internal ColorRef(Color color)
	{
		ColorDWORD = (uint)color.R + (((uint)color.G) << 8) + (((uint)color.B) << 16);
	}

	internal ColorRef(uint r, uint g, uint b)
	{
		ColorDWORD = r + (g << 8) + (b << 16);
	}

	internal Color GetColor()
	{
		return new Color((byte)(0x000000FFU & ColorDWORD),
		   (byte)((uint)(0x0000FF00U & ColorDWORD) >> 8), (byte)((uint)(0x00FF0000U & ColorDWORD) >> 16));
	}

	internal void SetColor(Color color)
	{
		ColorDWORD = (uint)color.R + (((uint)color.G) << 8) + (((uint)color.B) << 16);
	}
}

[StructLayout(LayoutKind.Sequential)]
public struct CONSOLE_SCREEN_BUFFER_INFO_EX
{
	public int cbSize;
	public Coord dwSize;
	public Coord dwCursorPosition;
	public short wAttributes;
	public SmallRect srWindow;
	public Coord dwMaximumWindowSize;

	public ushort wPopupAttributes;
	public bool bFullscreenSupported;

	internal ColorRef black;
	internal ColorRef darkBlue;
	internal ColorRef darkGreen;
	internal ColorRef darkCyan;
	internal ColorRef darkRed;
	internal ColorRef darkMagenta;
	internal ColorRef darkYellow;
	internal ColorRef gray;
	internal ColorRef darkGray;
	internal ColorRef blue;
	internal ColorRef green;
	internal ColorRef cyan;
	internal ColorRef red;
	internal ColorRef magenta;
	internal ColorRef yellow;
	internal ColorRef white;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct CONSOLE_FONT_INFO_EX
{
	public uint cbSize;
	public uint nFont;
	public Coord dwFontSize;
	public int FontFamily;
	public int FontWeight;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] // Edit sizeconst if the font name is too big
	public string FaceName;
}
