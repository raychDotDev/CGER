namespace ConsoleGameEngine;

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

public class WinAPIWrapper
{
	[DllImport("user32.dll", SetLastError = true)]
	public static extern short GetAsyncKeyState(Int32 vKey);
	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool GetCursorPos(out POINT vKey);
	[DllImport("user32.dll", SetLastError = true)]
	public static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool GetWindowRect(IntPtr hWnd, ref Rect lpRect);
	[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
	public static extern IntPtr GetDesktopWindow();

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
	[DllImport("user32.dll", SetLastError = true)]
	public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool DrawMenuBar(IntPtr hWnd);
	[DllImport("user32.dll", SetLastError = true)]
	public static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, [In, Out] ref Rect rect, [MarshalAs(UnmanagedType.U4)] int cPoints);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern IntPtr GetStdHandle(int nStdHandle);
	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern IntPtr GetConsoleWindow();


	[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	public static extern SafeFileHandle CreateFile(
		string fileName,
		[MarshalAs(UnmanagedType.U4)] uint fileAccess,
		[MarshalAs(UnmanagedType.U4)] uint fileShare,
		IntPtr securityAttributes,
		[MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
		[MarshalAs(UnmanagedType.U4)] int flags,
	IntPtr template);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool WriteConsoleOutputW(
		SafeFileHandle hConsoleOutput,
		CharInfo[] lpBuffer,
		Coord dwBufferSize,
		Coord dwBufferCoord,
	ref SmallRect lpWriteRegion);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool GetConsoleScreenBufferInfoEx(IntPtr hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFO_EX csbe);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool SetConsoleScreenBufferInfoEx(IntPtr ConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFO_EX csbe);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern Int32 SetCurrentConsoleFontEx(
	IntPtr ConsoleOutput,
	bool MaximumWindow,
	ref CONSOLE_FONT_INFO_EX ConsoleCurrentFontEx);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
}
