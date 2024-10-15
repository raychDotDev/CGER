using System.Runtime.InteropServices;

namespace CGER;

class ConsoleFont
{
	internal static int SetFontSize(IntPtr handler, short sizeX, short sizeY)
	{
		if (handler == new IntPtr(-1))
		{
			return Marshal.GetLastWin32Error();
		}

		CONSOLE_FONT_INFO_EX cfi = new CONSOLE_FONT_INFO_EX();
		cfi.cbSize = (uint)Marshal.SizeOf(cfi);
		cfi.nFont = 0;

		cfi.dwFontSize.X = sizeX;
		cfi.dwFontSize.Y = sizeY;

		// sätter font till Terminal (Raster)
		// if (sizeX < 4 || sizeY < 4) 
			// cfi.FaceName = "Terminal";
		// else cfi.FaceName = "Terminal";

		WinAPIWrapper.SetCurrentConsoleFontEx(handler, false, ref cfi);
		return 0;
	}
}
