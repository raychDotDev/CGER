using System.Text;
using System.Drawing;

namespace CGER;

public class ConsoleRenderer
{
	private readonly IntPtr stdInputHandle = WinAPIWrapper.GetStdHandle(-10);
	private readonly IntPtr stdOutputHandle = WinAPIWrapper.GetStdHandle(-11);
	private readonly IntPtr stdErrorHandle = WinAPIWrapper.GetStdHandle(-12);
	private readonly IntPtr consoleHandle = WinAPIWrapper.GetConsoleWindow();

	/// <summary> The active color palette. </summary> <see cref="Color"/>
	public Color[] Palette { get; private set; }

	/// <summary> The current size of the font. </summary> <see cref="Point"/>
	public Point FontSize { get; private set; }

	/// <summary> The dimensions of the window in characters. </summary> <see cref="Point"/>
	public Point WindowSize { get; private set; }

	private Glyph[,] GlyphBuffer { get; set; }
	private int BackgroundColor { get; set; }
	private ConsoleBuffer ConsoleBuffer { get; set; }
	private bool Borderless { get; set; }

	/// <summary> Creates a new ConsoleEngine. </summary>
	/// <param name="width">Target window width.</param>
	/// <param name="height">Target window height.</param>
	/// <param name="fontWidth">Target font width.</param>
	/// <param name="fontHeight">Target font height.</param>
	public ConsoleRenderer(int width, int height, int fontWidth, int fontHeight, string title = "Untitiled")
	{
		if (width < 1 || height < 1) throw new ArgumentOutOfRangeException();
		if (fontWidth < 1 || fontHeight < 1) throw new ArgumentOutOfRangeException();

		Console.Title = title;
		Console.CursorVisible = false;

		Console.SetWindowPosition(0, 0);

		Console.SetWindowSize(width, height);
		Console.SetBufferSize(width, height);

		ConsoleBuffer = new ConsoleBuffer(width, height);

		WindowSize = new Point(width, height);
		FontSize = new Point(fontWidth, fontHeight);

		GlyphBuffer = new Glyph[width, height];
		for (int y = 0; y < GlyphBuffer.GetLength(1); y++)
			for (int x = 0; x < GlyphBuffer.GetLength(0); x++)
				GlyphBuffer[x, y] = new Glyph();

		SetBackgroundColor(0);
		SetPalette(Palettes.Default);

		WinAPIWrapper.SetConsoleMode(stdInputHandle, 0x0080);

		IntPtr handle = WinAPIWrapper.GetConsoleWindow();
		IntPtr sysMenu = WinAPIWrapper.GetSystemMenu(handle, false);

		if (handle != IntPtr.Zero)
		{
			WinAPIWrapper.DeleteMenu(sysMenu, WinAPIWrapper.SC_CLOSE, 0x0);
			WinAPIWrapper.DeleteMenu(sysMenu, WinAPIWrapper.SC_MINIMIZE, 0x0);
			WinAPIWrapper.DeleteMenu(sysMenu, WinAPIWrapper.SC_MAXIMIZE, 0x0);
			WinAPIWrapper.DeleteMenu(sysMenu, WinAPIWrapper.SC_SIZE, 0x0);
			
		}

		ConsoleFont.SetFont(stdOutputHandle, (short)fontWidth, (short)fontHeight);
	}

	public void SetPixel(Point position, int foregroundColor, char character)
	{
		SetPixel(position, foregroundColor, BackgroundColor, character);
	}

	public void SetPixel(Point position, int foregroundColor, int backgroundColor, char character)
	{
		if (position.X >= GlyphBuffer.GetLength(0) || position.Y >= GlyphBuffer.GetLength(1)
			|| position.X < 0 || position.Y < 0) return;
		GlyphBuffer[position.X, position.Y].Set(character, foregroundColor, backgroundColor);
	}

	/// <summary>
	/// returns gylfh at point given
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public Glyph? GetGlyph(Point position)
	{
		if (position.X > 0 && position.X < GlyphBuffer.GetLength(0) && position.Y > 0 && position.Y < GlyphBuffer.GetLength(1))
			return GlyphBuffer[position.X, position.Y];
		else return null;
	}


	/// <summary> Sets the console's color palette </summary>
	/// <param name="colors"></param>
	/// <exception cref="ArgumentException"/> <exception cref="ArgumentNullException"/>
	public void SetPalette(Color[] colors)
	{
		if (colors.Length > 16) throw new ArgumentException("Windows command prompt only support 16 colors.");
		Palette = colors ?? throw new ArgumentNullException();

		for (int i = 0; i < colors.Length; i++)
		{
			ConsoleColorPalette.SetColor(i, colors[i]);
		}
	}

	/// <summary> Sets the console's background color to one in the active palette. </summary>
	/// <param name="color">Index of background color in palette.</param>
	public void SetBackgroundColor(int color = 0)
	{
		if (color > 16 || color < 0) throw new IndexOutOfRangeException();
		BackgroundColor = color;
	}

	/// <summary>Gets Background</summary>
	/// <returns>Returns the background</returns>
	public int GetBackgroundColor()
	{
		return BackgroundColor;
	}

	/// <summary> Clears the screenbuffer. </summary>
	public void ClearBuffer()
	{
		for (int y = 0; y < GlyphBuffer.GetLength(1); y++)
			for (int x = 0; x < GlyphBuffer.GetLength(0); x++)
				GlyphBuffer[x, y].Clear();
	}

	/// <summary> Blits the screenbuffer to the Console window. </summary>
	public void DisplayBuffer()
	{
		ConsoleBuffer.SetBuffer(GlyphBuffer, BackgroundColor);
		ConsoleBuffer.Blit();
	}

	/// <summary> Sets the window to borderless mode. </summary>
	public void GoBorderless()
	{
		Borderless = true;

		int GWL_STYLE = -16;                // hex konstant för stil-förändring
		int WS_BORDERLESS = 0x00080000;     // helt borderless

		Rect rect = new Rect();
		Rect desktopRect = new Rect();

		//TODO: make dis shiet use IPlatformAPIWrapper instead of native win32 api
		WinAPIWrapper.GetWindowRect(consoleHandle, ref rect);
		IntPtr desktopHandle = WinAPIWrapper.GetDesktopWindow();
		WinAPIWrapper.MapWindowPoints(desktopHandle, consoleHandle, ref rect, 2);
		WinAPIWrapper.GetWindowRect(desktopHandle, ref desktopRect);

		Point wPos = new Point(
			(desktopRect.Right / 2) - ((WindowSize.X * FontSize.X) / 2),
			(desktopRect.Bottom / 2) - ((WindowSize.Y * FontSize.Y) / 2));

		WinAPIWrapper.SetWindowLong(consoleHandle, GWL_STYLE, WS_BORDERLESS);
		WinAPIWrapper.SetWindowPos(consoleHandle, -2, wPos.X, wPos.Y, rect.Right - 8, rect.Bottom - 8, 0x0040);

		WinAPIWrapper.DrawMenuBar(consoleHandle);
	}
	
	#region Primitives

	/// <summary> Draws a single pixel to the screenbuffer. calls new method with Background as the bgColor </summary>
	/// <param name="v">The Point that should be drawn to.</param>
	/// <param name="color">The color index.</param>
	/// <param name="c">The character that should be drawn with.</param>
	public void SetPixel(Point v, int color, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		SetPixel(v, color, BackgroundColor, (char)c);
	}

	/// <summary> Overloaded Method Draws a single pixel to the screenbuffer with custom bgColor. </summary>
	/// <param name="v">The Point that should be drawn to.</param>
	/// <param name="fgColor">The foreground color index.</param>
	/// <param name="bgColor">The background color index.</param>
	/// <param name="c">The character that should be drawn with.</param>
	public void SetPixel(Point v, int fgColor, int bgColor, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		SetPixel(v, fgColor, bgColor, (char)c);
	}

	/// <summary> Draws a frame using boxdrawing symbols, calls new method with Background as the bgColor. </summary>
	/// <param name="pos">Top Left corner of box.</param>
	/// <param name="end">Bottom Right corner of box.</param>
	/// <param name="color">The specified color index.</param>
	public void DrawFrame(Point pos, Point end, int color)
	{
		DrawFrame(pos, end, color, BackgroundColor);
	}

	/// <summary> Draws a frame using boxdrawing symbols. </summary>
	/// <param name="pos">Top Left corner of box.</param>
	/// <param name="end">Bottom Right corner of box.</param>
	/// <param name="fgColor">The specified color index.</param>
	/// <param name="bgColor">The specified background color index.</param>
	public void DrawFrame(Point pos, Point end, int fgColor, int bgColor)
	{
		for (int i = 1; i < end.X - pos.X; i++)
		{
			SetPixel(new Point(pos.X + i, pos.Y), fgColor, bgColor, ConsoleCharacter.BoxDrawingL_H);
			SetPixel(new Point(pos.X + i, end.Y), fgColor, bgColor, ConsoleCharacter.BoxDrawingL_H);
		}

		for (int i = 1; i < end.Y - pos.Y; i++)
		{
			SetPixel(new Point(pos.X, pos.Y + i), fgColor, bgColor, ConsoleCharacter.BoxDrawingL_V);
			SetPixel(new Point(end.X, pos.Y + i), fgColor, bgColor, ConsoleCharacter.BoxDrawingL_V);
		}

		SetPixel(new Point(pos.X, pos.Y), fgColor, bgColor, ConsoleCharacter.BoxDrawingL_DR);
		SetPixel(new Point(end.X, pos.Y), fgColor, bgColor, ConsoleCharacter.BoxDrawingL_DL);
		SetPixel(new Point(pos.X, end.Y), fgColor, bgColor, ConsoleCharacter.BoxDrawingL_UR);
		SetPixel(new Point(end.X, end.Y), fgColor, bgColor, ConsoleCharacter.BoxDrawingL_UL);
	}

	/// <summary> Writes plain text to the buffer, calls new method with Background as the bgColor. </summary>
	/// <param name="pos">The position to write to.</param>
	/// <param name="text">String to write.</param>
	/// <param name="color">Specified color index to write with.</param>
	public void DrawText(Point pos, string text, int color)
	{
		DrawText(pos, text, color, BackgroundColor);
	}

	/// <summary> Writes plain text to the buffer. </summary>
	/// <param name="pos">The position to write to.</param>
	/// <param name="text">String to write.</param>
	/// <param name="fgColor">Specified color index to write with.</param>
	/// <param name="bgColor">Specified background color index to write with.</param>
	public void DrawText(Point pos, string text, int fgColor, int bgColor)
	{
		for (int i = 0; i < text.Length; i++)
		{
			SetPixel(new Point(pos.X + i, pos.Y), fgColor, bgColor, text[i]);
		}
	}

	/// <summary>  Writes text to the buffer in a FIGlet font, calls new method with Background as the bgColor. </summary>
	/// <param name="pos">The Top left corner of the text.</param>
	/// <param name="text">String to write.</param>
	/// <param name="font">FIGLET font to write with.</param>
	/// <param name="color">Specified color index to write with.</param>
	/// <see cref="FigletFont"/>
	public void DrawTextFiglet(Point pos, string text, FigletFont font, int color)
	{
		DrawTextFiglet(pos, text, font, color, BackgroundColor);
	}

	/// <summary>  Writes text to the buffer in a FIGlet font. </summary>
	/// <param name="pos">The Top left corner of the text.</param>
	/// <param name="text">String to write.</param>
	/// <param name="font">FIGLET font to write with.</param>
	/// <param name="fgColor">Specified color index to write with.</param>
	/// <param name="bgColor">Specified background color index to write with.</param>
	/// <see cref="FigletFont"/>
	public void DrawTextFiglet(Point pos, string text, FigletFont font, int fgColor, int bgColor)
	{
		if (text == null) throw new ArgumentNullException(nameof(text));
		if (Encoding.UTF8.GetByteCount(text) != text.Length) throw new ArgumentException("String contains non-ascii characters");

		int sWidth = FigletFont.GetStringWidth(font, text);

		for (int line = 1; line <= font.Height; line++)
		{
			int runningWidthTotal = 0;

			for (int c = 0; c < text.Length; c++)
			{
				char character = text[c];
				string fragment = FigletFont.GetCharacter(font, character, line);
				for (int f = 0; f < fragment.Length; f++)
				{
					if (fragment[f] != ' ')
					{
						SetPixel(new Point(pos.X + runningWidthTotal + f, pos.Y + line - 1), fgColor, bgColor, fragment[f]);
					}
				}
				runningWidthTotal += fragment.Length;
			}
		}
	}

	/// <summary> Draws an Arc, calls new method with Background as the bgColor. </summary>
	/// <param name="pos">Center of Arc.</param>
	/// <param name="radius">Radius of Arc.</param>
	/// <param name="color">Specified color index.</param>
	/// <param name="arc">angle in degrees, 360 if not specified.</param>
	/// <param name="c">Character to use.</param>
	public void DrawArc(Point pos, int radius, int color, int arc = 360, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		DrawArc(pos, radius, color, BackgroundColor, arc, c);
	}

	/// <summary> Draws an Arc. </summary>
	/// <param name="pos">Center of Arc.</param>
	/// <param name="radius">Radius of Arc.</param>
	/// <param name="fgColor">Specified color index.</param>
	/// <param name="bgColor">Specified background color index.</param>
	/// <param name="arc">angle in degrees, 360 if not specified.</param>
	/// <param name="c">Character to use.</param>
	public void DrawArc(Point pos, int radius, int fgColor, int bgColor, int arc = 360, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		for (int a = 0; a < arc; a++)
		{
			int x = (int)(radius * System.Math.Cos((float)a / 57.29577f));
			int y = (int)(radius * System.Math.Sin((float)a / 57.29577f));

			Point v = new Point(pos.X + x, pos.Y + y);
			SetPixel(v, fgColor, bgColor, ConsoleCharacter.Full);
		}
	}

	/// <summary> Draws a filled Arc, calls new method with Background as the bgColor </summary>
	/// <param name="pos">Center of Arc.</param>
	/// <param name="radius">Radius of Arc.</param>
	/// <param name="start">Start angle in degrees.</param>
	/// <param name="arc">End angle in degrees.</param>
	/// <param name="color">Specified color index.</param>
	/// <param name="c">Character to use.</param>
	public void DrawSemiCircle(Point pos, int radius, int start, int arc, int color, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		DrawSemiCircle(pos, radius, start, arc, color, BackgroundColor, c);
	}

	/// <summary> Draws a filled Arc. </summary>
	/// <param name="pos">Center of Arc.</param>
	/// <param name="radius">Radius of Arc.</param>
	/// <param name="start">Start angle in degrees.</param>
	/// <param name="arc">End angle in degrees.</param>
	/// <param name="fgColor">Specified color index.</param>
	/// <param name="bgColor">Specified background color index.</param>
	/// <param name="c">Character to use.</param>
	public void DrawSemiCircle(Point pos, int radius, int start, int arc, int fgColor, int bgColor, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		for (int a = start; a > -arc + start; a--)
		{
			for (int r = 0; r < radius + 1; r++)
			{
				int x = (int)(r * System.Math.Cos((float)a / 57.29577f));
				int y = (int)(r * System.Math.Sin((float)a / 57.29577f));

				Point v = new Point(pos.X + x, pos.Y + y);
				SetPixel(v, fgColor, bgColor, c);
			}
		}
	}

	// Bresenhams Line Algorithm
	// https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm
	/// <summary> Draws a line from start to end. (Bresenhams Line), calls overloaded method with background as bgColor </summary>
	/// <param name="start">Point to draw line from.</param>
	/// <param name="end">Point to end line at.</param>
	/// <param name="color">Color to draw with.</param>
	/// <param name="c">Character to use.</param>
	public void DrawLine(Point start, Point end, int color, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		DrawLine(start, end, color, BackgroundColor, c);
	}

	// Bresenhams Line Algorithm
	// https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm
	/// <summary> Draws a line from start to end. (Bresenhams Line) </summary>
	/// <param name="start">Point to draw line from.</param>
	/// <param name="end">Point to end line at.</param>
	/// <param name="fgColor">Color to draw with.</param>
	/// <param name="bgColor">Color to draw the background with.</param>
	/// <param name="c">Character to use.</param>
	public void DrawLine(Point start, Point end, int fgColor, int bgColor, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		Point delta = end - (Size)start;
		Point da = new(0, 0), db = new(0, 0);
		if (delta.X < 0) da.X = -1; else if (delta.X > 0) da.X = 1;
		if (delta.Y < 0) da.Y = -1; else if (delta.Y > 0) da.Y = 1;
		if (delta.X < 0) db.X = -1; else if (delta.X > 0) db.X = 1;
		int longest = System.Math.Abs(delta.X);
		int shortest = System.Math.Abs(delta.Y);

		if (!(longest > shortest))
		{
			longest = System.Math.Abs(delta.Y);
			shortest = System.Math.Abs(delta.X);
			if (delta.Y < 0) db.Y = -1; else if (delta.Y > 0) db.Y = 1;
			db.X = 0;
		}

		int numerator = longest >> 1;
		Point p = new Point(start.X, start.Y);
		for (int i = 0; i <= longest; i++)
		{
			SetPixel(p, fgColor, bgColor, c);
			numerator += shortest;
			if (!(numerator < longest))
			{
				numerator -= longest;
				p += (Size)da;
			}
			else
			{
				p += (Size)db;
			}
		}
	}

	/// <summary> Draws a Rectangle, calls overloaded method with background as bgColor  </summary>
	/// <param name="pos">Top Left corner of rectangle.</param>
	/// <param name="end">Bottom Right corner of rectangle.</param>
	/// <param name="color">Color to draw with.</param>
	/// <param name="c">Character to use.</param>
	public void DrawRectangle(Point pos, Point end, int color, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		DrawRectangle(pos, end, color, BackgroundColor, c);
	}

	/// <summary> Draws a Rectangle. </summary>
	/// <param name="pos">Top Left corner of rectangle.</param>
	/// <param name="end">Bottom Right corner of rectangle.</param>
	/// <param name="fgColor">Color to draw with.</param>
	/// <param name="bgColor">Color to draw to the background with.</param>
	/// <param name="c">Character to use.</param>
	public void DrawRectangle(Point pos, Point end, int fgColor, int bgColor, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		for (int i = 0; i < end.X - pos.X; i++)
		{
			SetPixel(new Point(pos.X + i, pos.Y), fgColor, bgColor, c);
			SetPixel(new Point(pos.X + i, end.Y), fgColor, bgColor, c);
		}

		for (int i = 0; i < end.Y - pos.Y + 1; i++)
		{
			SetPixel(new Point(pos.X, pos.Y + i), fgColor, bgColor, c);
			SetPixel(new Point(end.X, pos.Y + i), fgColor, bgColor, c);
		}
	}

	/// <summary> Draws a Rectangle and fills it, calls overloaded method with background as bgColor </summary>
	/// <param name="a">Top Left corner of rectangle.</param>
	/// <param name="b">Bottom Right corner of rectangle.</param>
	/// <param name="color">Color to draw with.</param>
	/// <param name="c">Character to use.</param>
	public void DrawRectangleFull(Point a, Point b, int color, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		DrawRectangleFull(a, b, color, BackgroundColor, c);
	}

	/// <summary> Draws a Rectangle and fills it. </summary>
	/// <param name="a">Top Left corner of rectangle.</param>
	/// <param name="b">Bottom Right corner of rectangle.</param>
	/// <param name="fgColor">Color to draw with.</param>
	/// <param name="bgColor">Color to draw the background with.</param>
	/// <param name="c">Character to use.</param>
	public void DrawRectangleFull(Point a, Point b, int fgColor, int bgColor, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		for (int y = a.Y; y < b.Y; y++)
		{
			for (int x = a.X; x < b.X; x++)
			{
				SetPixel(new Point(x, y), fgColor, bgColor, c);
			}
		}
	}

	/// <summary> Draws a grid, calls overloaded method with background as bgColor </summary>
	/// <param name="a">Top Left corner of grid.</param>
	/// <param name="b">Bottom Right corner of grid.</param>
	/// <param name="spacing">the spacing until next line</param>
	/// <param name="color">Color to draw with.</param>
	/// <param name="c">Character to use.</param>
	public void DrawGrid(Point a, Point b, int spacing, int color, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		DrawGrid(a, b, spacing, color, BackgroundColor, c);
	}

	/// <summary> Draws a grid. </summary>
	/// <param name="a">Top Left corner of grid.</param>
	/// <param name="b">Bottom Right corner of grid.</param>
	/// <param name="spacing">the spacing until next line</param>
	/// <param name="fgColor">Color to draw with.</param>
	/// <param name="bgColor">Color to draw the background with.</param>
	/// <param name="c">Character to use.</param>
	public void DrawGrid(Point a, Point b, int spacing, int fgColor, int bgColor, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		for (int y = a.Y; y < b.Y / spacing; y++)
		{
			DrawLine(new Point(a.X, y * spacing), new Point(b.X, y * spacing), fgColor, bgColor, c);
		}
		for (int x = a.X; x < b.X / spacing; x++)
		{
			DrawLine(new Point(x * spacing, a.Y), new Point(x * spacing, b.Y), fgColor, bgColor, c);
		}
	}

	/// <summary> Draws a Triangle, calls overloaded method with background as bgColor </summary>
	/// <param name="a">Point A.</param>
	/// <param name="b">Point B.</param>
	/// <param name="c">Point C.</param>
	/// <param name="color">Color to draw with.</param>
	/// <param name="character">Character to use.</param>
	public void DrawTriangle(Point a, Point b, Point c, int color, ConsoleCharacter character = ConsoleCharacter.Full)
	{
		DrawTriangle(a, b, c, color, BackgroundColor, character);
	}

	/// <summary> Draws a Triangle. </summary>
	/// <param name="a">Point A.</param>
	/// <param name="b">Point B.</param>
	/// <param name="c">Point C.</param>
	/// <param name="fgColor">Color to draw with.</param>
	/// <param name="bgColor">Color to draw to the background with.</param>
	/// <param name="character">Character to use.</param>
	public void DrawTriangle(Point a, Point b, Point c, int fgColor, int bgColor, ConsoleCharacter character = ConsoleCharacter.Full)
	{
		DrawLine(a, b, fgColor, bgColor, character);
		DrawLine(b, c, fgColor, bgColor, character);
		DrawLine(c, a, fgColor, bgColor, character);
	}

	// Bresenhams Triangle Algorithm

	/// <summary> Draws a Triangle and fills it, calls overloaded method with background as bgColor </summary>
	/// <param name="a">Point A.</param>
	/// <param name="b">Point B.</param>
	/// <param name="c">Point C.</param>
	/// <param name="color">Color to draw with.</param>
	/// <param name="character">Character to use.</param>
	public void DrawTriangleFull(Point a, Point b, Point c, int color, ConsoleCharacter character = ConsoleCharacter.Full)
	{
		DrawTriangleFull(a, b, c, color, BackgroundColor, character);
	}

	/// <summary> Draws a Triangle and fills it. </summary>
	/// <param name="a">Point A.</param>
	/// <param name="b">Point B.</param>
	/// <param name="c">Point C.</param>
	/// <param name="fgColor">Color to draw with.</param>
	/// <param name="bgColor">Color to draw to the background with.</param>
	/// <param name="character">Character to use.</param>
	public void DrawTriangleFull(Point a, Point b, Point c, int fgColor, int bgColor, ConsoleCharacter character = ConsoleCharacter.Full)
	{
		Point min = new Point(System.Math.Min(System.Math.Min(a.X, b.X), c.X), System.Math.Min(System.Math.Min(a.Y, b.Y), c.Y));
		Point max = new Point(System.Math.Max(System.Math.Max(a.X, b.X), c.X), System.Math.Max(System.Math.Max(a.Y, b.Y), c.Y));

		Point p = new Point();
		for (p.Y = min.Y; p.Y < max.Y; p.Y++)
		{
			for (p.X = min.X; p.X < max.X; p.X++)
			{
				int w0 = Orient(b, c, p);
				int w1 = Orient(c, a, p);
				int w2 = Orient(a, b, p);

				if (w0 >= 0 && w1 >= 0 && w2 >= 0) SetPixel(p, fgColor, bgColor, character);
			}
		}
	}

	private int Orient(Point a, Point b, Point c)
	{
		return ((b.X - a.X) * (c.Y - a.Y)) - ((b.Y - a.Y) * (c.X - a.X));
	}

	#endregion Primitives

	/// <summary>Checks to see if the console is in focus </summary>
	/// <returns>True if Console is in focus</returns>
	private bool ConsoleFocused()
	{
		return WinAPIWrapper.GetConsoleWindow() == WinAPIWrapper.GetForegroundWindow();
	}

	/// <summary> Checks if specified key is pressed. </summary>
	/// <param name="key">The key to check.</param>
	/// <returns>True if key is pressed</returns>
	public bool IsKeyPressed(ConsoleKey key)
	{
		short s = WinAPIWrapper.GetAsyncKeyState((int)key);
		return (s & 0x8000) > 0 && ConsoleFocused();
	}

	/// <summary> Checks if specified keyCode is pressed. </summary>
	/// <param name="virtualkeyCode">keycode to check</param>
	/// <returns>True if key is pressed</returns>
	public bool IsKeyPressed(int virtualkeyCode)
	{
		short s = WinAPIWrapper.GetAsyncKeyState(virtualkeyCode);
		return (s & 0x8000) > 0 && ConsoleFocused();
	}

	/// <summary> Checks if specified key is pressed down. </summary>
	/// <param name="key">The key to check.</param>
	/// <returns>True if key is down</returns>
	public bool IsKeyDown(ConsoleKey key)
	{
		int s = Convert.ToInt32(WinAPIWrapper.GetAsyncKeyState((int)key));
		return (s == -32767) && ConsoleFocused();
	}

	/// <summary> Checks if specified keyCode is pressed down. </summary>
	/// <param name="virtualkeyCode">keycode to check</param>
	/// <returns>True if key is down</returns>
	public bool IsKeyDown(int virtualkeyCode)
	{
		int s = Convert.ToInt32(WinAPIWrapper.GetAsyncKeyState(virtualkeyCode));
		return (s == -32767) && ConsoleFocused();
	}

	/// <summary> Checks if left mouse button is pressed down. </summary>
	/// <returns>True if left mouse button is down</returns>
	public bool IsMouseLeftDown()
	{
		short s = WinAPIWrapper.GetAsyncKeyState(0x01);
		return (s & 0x8000) > 0 && ConsoleFocused();
	}

	/// <summary> Checks if right mouse button is pressed down. </summary>
	/// <returns>True if right mouse button is down</returns>
	public bool IsMouseRightDown()
	{
		short s = WinAPIWrapper.GetAsyncKeyState(0x02);
		return (s & 0x8000) > 0 && ConsoleFocused();
	}

	/// <summary> Checks if middle mouse button is pressed down. </summary>
	/// <returns>True if middle mouse button is down</returns>
	public bool IsMouseMiddleDown()
	{
		short s = WinAPIWrapper.GetAsyncKeyState(0x04);
		return (s & 0x8000) > 0 && ConsoleFocused();
	}

	/// <summary> Gets the mouse position. </summary>
	/// <returns>The mouse's position in character-space.</returns>
	/// <exception cref="Exception"/>
	public Point GetCursorPosition()
	{
		Rect r = new Rect();
		WinAPIWrapper.GetWindowRect(consoleHandle, ref r);

		if (WinAPIWrapper.GetCursorPos(out POINT p))
		{
			Point point = new Point();
			if (!Borderless)
			{
				p.Y -= 29;
				point = new Point(
					(int)System.Math.Floor(((p.X - r.Left) / (float)FontSize.X) - 0.5f),
					(int)System.Math.Floor(((p.Y - r.Top) / (float)FontSize.Y))
				);
			}
			else
			{
				point = new Point(
					(int)System.Math.Floor(((p.X - r.Left) / (float)FontSize.X)),
					(int)System.Math.Floor(((p.Y - r.Top) / (float)FontSize.Y))
				);
			}
			return new Point(Math.Clamp(point.X, 0, WindowSize.X - 1), Math.Clamp(point.Y, 0, WindowSize.Y - 1));
		}
		throw new Exception();
	}
}
