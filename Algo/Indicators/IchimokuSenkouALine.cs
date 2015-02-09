namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using StockSharp.Algo.Candles;

	/// <summary>
	/// Линия Senkou Span A.
	/// </summary>
	public class IchimokuSenkouALine : LengthIndicator<decimal>
	{
		/// <summary>
		/// Создать <see cref="IchimokuSenkouALine"/>.
		/// </summary>
		/// <param name="tenkan">Линия Tenkan.</param>
		/// <param name="kijun">Линия Kijun.</param>
		public IchimokuSenkouALine(IchimokuLine tenkan, IchimokuLine kijun)
			: base(typeof(Candle))
		{
			if (tenkan == null)
				throw new ArgumentNullException("tenkan");

			if (kijun == null)
				throw new ArgumentNullException("kijun");

			Tenkan = tenkan;
			Kijun = kijun;
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		/// 
		public override bool IsFormed
		{
			get { return Buffer.Count >= Kijun.Length; }
		}

		/// <summary>
		/// Линия Tenkan.
		/// </summary>
		[Browsable(false)]
		public IchimokuLine Tenkan { get; private set; }

		/// <summary>
		/// Линия Kijun.
		/// </summary>
		[Browsable(false)]
		public IchimokuLine Kijun { get; private set; }

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			decimal? result = null;

			if (Tenkan.IsFormed && Kijun.IsFormed)
			{
				if (input.IsFinal)
					Buffer.Add((Tenkan.GetCurrentValue() + Kijun.GetCurrentValue())/2);

				if (IsFormed)
					result = Buffer[0];

				if (Buffer.Count > Kijun.Length && input.IsFinal)
				{
					Buffer.RemoveAt(0);
				}
			}

			return result == null ? new DecimalIndicatorValue(this) : new DecimalIndicatorValue(this, result.Value);
		}
	}
}