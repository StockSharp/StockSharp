namespace StockSharp.Algo.Statistics
{
	using System;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Интерфейс, описывающий параметр статистики, рассчитывающийся на основе позиции.
	/// </summary>
	public interface IPositionStatisticParameter
	{
		/// <summary>
		/// Добавить в параметр новое значение позиции.
		/// </summary>
		/// <param name="marketTime">Биржевое время.</param>
		/// <param name="position">Новое значение позиции.</param>
		void Add(DateTimeOffset marketTime, decimal position);
	}

	/// <summary>
	/// Максимальный размер длинной позиции.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str970Key)]
	[DescriptionLoc(LocalizedStrings.Str971Key)]
	[CategoryLoc(LocalizedStrings.Str972Key)]
	public class MaxLongPositionParameter : BaseStatisticParameter<decimal>, IPositionStatisticParameter
	{
		/// <summary>
		/// Добавить в параметр новое значение позиции.
		/// </summary>
		/// <param name="marketTime">Биржевое время.</param>
		/// <param name="position">Новое значение позиции.</param>
		public void Add(DateTimeOffset marketTime, decimal position)
		{
			if (position > 0)
				Value = position.Max(Value);
		}
	}

	/// <summary>
	/// Максимальный размер короткой позиции.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str973Key)]
	[DescriptionLoc(LocalizedStrings.Str974Key)]
	[CategoryLoc(LocalizedStrings.Str972Key)]
	public class MaxShortPositionParameter : BaseStatisticParameter<decimal>, IPositionStatisticParameter
	{
		/// <summary>
		/// Добавить в параметр новое значение позиции.
		/// </summary>
		/// <param name="marketTime">Биржевое время.</param>
		/// <param name="position">Новое значение позиции.</param>
		public void Add(DateTimeOffset marketTime, decimal position)
		{
			if (position < 0)
				Value = position.Abs().Max(Value);
		}
	}
}