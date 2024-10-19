namespace StockSharp.Algo.Storages.Binary.Snapshot;

static class Sizes
{
	public const int S100 = 100;
	public const int S200 = 200;

	public const int S32 = 32;
	public const int S256 = 256;

	public static string VerifySize(this string value, int size)
	{
		if (value?.Length > size)
			throw new ArgumentOutOfRangeException(nameof(value), value);

		return value;
	}

	public static bool ToBool(this byte value) => value == 1;
	public static byte ToByte(this bool value) => (byte)(value ? 1 : 0);

	public static T ToEnum<T>(this byte value) where T : struct
		=> ((int)value).To<T>();
	public static byte ToByte<T>(this T value) where T : struct
		=> (byte)value.To<int>();
}