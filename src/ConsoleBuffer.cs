namespace ConsoleGameEngine;

using System;
using System.IO;
using Microsoft.Win32.SafeHandles;

class ConsoleBuffer
{
	private CharInfo[] CharInfoBuffer { get; set; }
	private SafeFileHandle FileHandle;

	readonly int width, height;

	public ConsoleBuffer(int width, int height)
	{
		this.width = width;
		this.height = height;

		FileHandle = WinAPIWrapper.CreateFile("CONOUT$", 0x40000000, 2, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);

		if (!FileHandle.IsInvalid)
		{
			CharInfoBuffer = new CharInfo[this.width * this.height];
		}
	}

	/// <param name="buffer"></param>
	/// <param name="backgroundColor"> Background color of the buffer</param>
	public void SetBuffer(Glyph[,] buffer, int backgroundColor)
	{
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				int i = (y * width) + x;

				if (buffer[x, y].Character == 0)
					buffer[x, y].BackgroundColor = backgroundColor;

				CharInfoBuffer[i].Attributes = (short)(buffer[x, y].ForegroundColor | (buffer[x, y].BackgroundColor << 4));
				CharInfoBuffer[i].UnicodeChar = buffer[x, y].Character;
			}
		}
	}

	public bool Blit()
	{
		SmallRect rect = new SmallRect() { Left = 0, Top = 0, Right = (short)width, Bottom = (short)height };

		return WinAPIWrapper.WriteConsoleOutputW(FileHandle, CharInfoBuffer,
			new Coord() { X = (short)width, Y = (short)height },
			new Coord() { X = 0, Y = 0 }, ref rect);
	}
}
