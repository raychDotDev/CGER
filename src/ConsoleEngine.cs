using System.Text;
using System.Drawing;

namespace CGER;

public class ConsoleEngine
{
	private readonly IntPtr stdInputHandle = WinAPIWrapper.GetStdHandle(-10);
	private readonly IntPtr stdOutputHandle = WinAPIWrapper.GetStdHandle(-11);
	private readonly IntPtr stdErrorHandle = WinAPIWrapper.GetStdHandle(-12);
	private readonly IntPtr consoleHandle = WinAPIWrapper.GetConsoleWindow();

	public Color[] Palette { get; private set; } = Palettes.Default;

	public Point FontSize { get; private set; }

	public Point WindowSize { get; private set; }

	private Glyph[,] GlyphBuffer { get; set; }
	private int BackgroundColor { get; set; }
	private ConsoleBuffer ConsoleBuffer { get; set; }
	private bool Borderless { get; set; }

	public ConsoleEngine(int width, int height, string title = "Untitiled")
	{
		if (width < 1 || height < 1) throw new ArgumentOutOfRangeException();

		Console.Title = title;

		Console.SetWindowSize(width-1, height-1);
		Console.SetBufferSize(width, height);
		Console.SetWindowSize(width, height);
		
		ConsoleBuffer = new ConsoleBuffer(width, height);

		WindowSize = new Point(width, height);

		GlyphBuffer = new Glyph[width, height];
		for (int y = 0; y < GlyphBuffer.GetLength(1); y++)
			for (int x = 0; x < GlyphBuffer.GetLength(0); x++)
				GlyphBuffer[x, y] = new Glyph();

		SetBackgroundColor(0);

		LockConsoleInput();

		IntPtr handle = WinAPIWrapper.GetConsoleWindow();
		IntPtr sysMenu = WinAPIWrapper.GetSystemMenu(handle, false);

		if (handle != IntPtr.Zero)
		{
			// WinAPIWrapper.DeleteMenu(sysMenu, WinAPIWrapper.SC_CLOSE, 0x0);
			WinAPIWrapper.DeleteMenu(sysMenu, WinAPIWrapper.SC_MINIMIZE, 0x0);
			WinAPIWrapper.DeleteMenu(sysMenu, WinAPIWrapper.SC_MAXIMIZE, 0x0);
			WinAPIWrapper.DeleteMenu(sysMenu, WinAPIWrapper.SC_SIZE, 0x0);
		}

		//delete this maybe...?
		// ConsoleFont.SetFont(stdOutputHandle, (short)fontWidth, (short)fontHeight);
	}

	public void UnlockConsoleInput()
	{
		WinAPIWrapper.GetConsoleMode(stdInputHandle, out var mode);
		WinAPIWrapper.SetConsoleMode(stdInputHandle, mode & 0x0020);
	}

	public void LockConsoleInput()
	{

		WinAPIWrapper.GetConsoleMode(stdInputHandle, out var mode);
		WinAPIWrapper.SetConsoleMode(stdInputHandle, mode & ~0x0020);
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
	
	// var MB_OK = 0x00000000L;
	public void ShowMessageBox(string text, string caption, long type = 0x00000000L)
	{
		WinAPIWrapper.MessageBox(new IntPtr(0), text, caption, type);
	}

	public Glyph? GetGlyph(Point position)
	{
		if (position.X > 0 && position.X < GlyphBuffer.GetLength(0) && position.Y > 0 && position.Y < GlyphBuffer.GetLength(1))
			return GlyphBuffer[position.X, position.Y];
		else return null;
	}


	public void SetPalette(Color[] colors)
	{
		if (colors.Length > 16) throw new ArgumentException("Windows command prompt only support 16 colors.");
		Palette = colors ?? throw new ArgumentNullException();

		for (int i = 0; i < colors.Length; i++)
		{
			ConsoleColorPalette.SetColor(i, colors[i]);
		}
	}

	public void SetBackgroundColor(int color = 0)
	{
		if (color > 16 || color < 0) throw new IndexOutOfRangeException();
		BackgroundColor = color;
	}

	public int GetBackgroundColor()
	{
		return BackgroundColor;
	}

	public void ClearBuffer()
	{
		for (int y = 0; y < GlyphBuffer.GetLength(1); y++)
			for (int x = 0; x < GlyphBuffer.GetLength(0); x++)
				GlyphBuffer[x, y].Clear();
	}

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

	public void SetPixel(Point v, int color, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		SetPixel(v, color, BackgroundColor, (char)c);
	}

	public void SetPixel(Point v, int fgColor, int bgColor, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		SetPixel(v, fgColor, bgColor, (char)c);
	}

	public void DrawFrame(Point pos, Point end, int color)
	{
		DrawFrame(pos, end, color, BackgroundColor);
	}

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

	public void DrawText(Point pos, string text, int color)
	{
		DrawText(pos, text, color, BackgroundColor);
	}

	public void DrawText(Point pos, string text, int fgColor, int bgColor)
	{
		for (int i = 0; i < text.Length; i++)
		{
			SetPixel(new Point(pos.X + i, pos.Y), fgColor, bgColor, text[i]);
		}
	}
	
	public void DrawTextFiglet(Point pos, string text, FigletFont font, int color)
	{
		DrawTextFiglet(pos, text, font, color, BackgroundColor);
	}

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

	public void DrawArc(Point pos, int radius, int color, int arc = 360, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		DrawArc(pos, radius, color, BackgroundColor, arc, c);
	}

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

	public void DrawSemiCircle(Point pos, int radius, int start, int arc, int color, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		DrawSemiCircle(pos, radius, start, arc, color, BackgroundColor, c);
	}

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

	public void DrawLine(Point start, Point end, int color, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		DrawLine(start, end, color, BackgroundColor, c);
	}

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

	public void DrawRectangle(Point pos, Point end, int color, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		DrawRectangle(pos, end, color, BackgroundColor, c);
	}

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

	public void DrawRectangleFull(Point a, Point b, int color, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		DrawRectangleFull(a, b, color, BackgroundColor, c);
	}

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

	public void DrawGrid(Point a, Point b, int spacing, int color, ConsoleCharacter c = ConsoleCharacter.Full)
	{
		DrawGrid(a, b, spacing, color, BackgroundColor, c);
	}

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

	public void DrawTriangle(Point a, Point b, Point c, int color, ConsoleCharacter character = ConsoleCharacter.Full)
	{
		DrawTriangle(a, b, c, color, BackgroundColor, character);
	}

	public void DrawTriangle(Point a, Point b, Point c, int fgColor, int bgColor, ConsoleCharacter character = ConsoleCharacter.Full)
	{
		DrawLine(a, b, fgColor, bgColor, character);
		DrawLine(b, c, fgColor, bgColor, character);
		DrawLine(c, a, fgColor, bgColor, character);
	}

	public void DrawTriangleFull(Point a, Point b, Point c, int color, ConsoleCharacter character = ConsoleCharacter.Full)
	{
		DrawTriangleFull(a, b, c, color, BackgroundColor, character);
	}

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

	private bool ConsoleFocused()
	{
		return WinAPIWrapper.GetConsoleWindow() == WinAPIWrapper.GetForegroundWindow();
	}

	public bool IsKeyPressed(ConsoleKey key)
	{
		short s = WinAPIWrapper.GetAsyncKeyState((int)key);
		return (s & 0x8000) > 0 && ConsoleFocused();
	}

	public bool IsKeyPressed(int virtualkeyCode)
	{
		short s = WinAPIWrapper.GetAsyncKeyState(virtualkeyCode);
		return (s & 0x8000) > 0 && ConsoleFocused();
	}

	public bool IsKeyDown(ConsoleKey key)
	{
		int s = Convert.ToInt32(WinAPIWrapper.GetAsyncKeyState((int)key));
		return (s == -32767) && ConsoleFocused();
	}

	public bool IsKeyDown(int virtualkeyCode)
	{
		int s = Convert.ToInt32(WinAPIWrapper.GetAsyncKeyState(virtualkeyCode));
		return (s == -32767) && ConsoleFocused();
	}

	public bool IsMouseLeftDown()
	{
		short s = WinAPIWrapper.GetAsyncKeyState(0x01);
		return (s & 0x8000) > 0 && ConsoleFocused();
	}

	public bool IsMouseRightDown()
	{
		short s = WinAPIWrapper.GetAsyncKeyState(0x02);
		return (s & 0x8000) > 0 && ConsoleFocused();
	}

	public bool IsMouseMiddleDown()
	{
		short s = WinAPIWrapper.GetAsyncKeyState(0x04);
		return (s & 0x8000) > 0 && ConsoleFocused();
	}

	public Point GetMouseCursorPosition()
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
