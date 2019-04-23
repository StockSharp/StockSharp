namespace StockSharp.Algo.Storages.Binary.Snapshot
{
	using System;

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
	}
}