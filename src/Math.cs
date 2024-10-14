namespace CGER;

public static class Math
{
	static public int Clamp(int a, int min, int max)
	{
		a = (a > max) ? max : a;
		a = (a < min) ? min : a;

		return a;
	}

	public static readonly float Rad2Deg = 180f / System.MathF.PI;
	public static readonly float Deg2Rad = System.MathF.PI / 180f;

}
