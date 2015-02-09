namespace StockSharp.Algo.Statistics
{
	using System;

	using Ecng.Serialization;

	using StockSharp.Algo.PnL;
	using StockSharp.Localization;

	/// <summary>
	/// Интерфейс, описывающий параметр статистики, рассчитывающийся на основе сделки.
	/// </summary>
	public interface ITradeStatisticParameter
	{
		/// <summary>
		/// Добавить в параметр информацию о новой сделке.
		/// </summary>
		/// <param name="info">Информация о новой сделке.</param>
		void Add(PnLInfo info);
	}

	/// <summary>
	/// Количество выигранных сделок (прибыль которых больше 0).
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str983Key)]
	[DescriptionLoc(LocalizedStrings.Str984Key)]
	[CategoryLoc(LocalizedStrings.Str985Key)]
	public class WinningTradesParameter : BaseStatisticParameter<int>, ITradeStatisticParameter
	{
		/// <summary>
		/// Добавить в параметр информацию о новой сделке.
		/// </summary>
		/// <param name="info">Информация о новой сделке.</param>
		public void Add(PnLInfo info)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			if (info.PnL <= 0)
				return;

			Value++;
		}
	}

	/// <summary>
	/// Количество проигранных сделок и с нулевой прибылью (прибыль которых меньше равна 0).
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str986Key)]
	[DescriptionLoc(LocalizedStrings.Str987Key)]
	[CategoryLoc(LocalizedStrings.Str985Key)]
	public class LossingTradesParameter : BaseStatisticParameter<int>, ITradeStatisticParameter
	{
		/// <summary>
		/// Добавить в параметр информацию о новой сделке.
		/// </summary>
		/// <param name="info">Информация о новой сделке.</param>
		public void Add(PnLInfo info)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			if (info.ClosedVolume > 0 && info.PnL <= 0)
				Value++;
		}
	}

	/// <summary>
	/// Общее количество сделок.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str988Key)]
	[DescriptionLoc(LocalizedStrings.Str989Key)]
	[CategoryLoc(LocalizedStrings.Str985Key)]
	public class TradeCountParameter : BaseStatisticParameter<int>, ITradeStatisticParameter
	{
		/// <summary>
		/// Добавить в параметр информацию о новой сделке.
		/// </summary>
		/// <param name="info">Информация о новой сделке.</param>
		public void Add(PnLInfo info)
		{
			Value++;
		}
	}

	/// <summary>
	/// Общее количество закрывающих сделок.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str990Key)]
	[DescriptionLoc(LocalizedStrings.Str991Key)]
	[CategoryLoc(LocalizedStrings.Str985Key)]
	public class RoundtripCountParameter : BaseStatisticParameter<int>, ITradeStatisticParameter
	{
		/// <summary>
		/// Добавить в параметр информацию о новой сделке.
		/// </summary>
		/// <param name="info">Информация о новой сделке.</param>
		public void Add(PnLInfo info)
		{
			if (info.ClosedVolume > 0)
				Value++;
		}
	}

	/// <summary>
	/// Средняя величина прибыли сделки.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str992Key)]
	[DescriptionLoc(LocalizedStrings.Str993Key)]
	[CategoryLoc(LocalizedStrings.Str985Key)]
	public class AverageTradeParameter : BaseStatisticParameter<decimal>, ITradeStatisticParameter
	{
		private decimal _sum;
		private int _count;

		/// <summary>
		/// Добавить в параметр информацию о новой сделке.
		/// </summary>
		/// <param name="info">Информация о новой сделке.</param>
		public void Add(PnLInfo info)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			if (info.ClosedVolume == 0)
				return;

			_sum += info.PnL;
			_count++;

			Value = _count > 0 ? _sum / _count : 0;
		}

		/// <summary>
		/// Сохранить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("Sum", _sum);
			storage.SetValue("Count", _count);

			base.Save(storage);
		}

		/// <summary>
		/// Загрузить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			_sum = storage.GetValue<decimal>("Sum");
			_count = storage.GetValue<int>("Count");

			base.Load(storage);
		}
	}

	/// <summary>
	/// Средняя выигрышная сделка.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str994Key)]
	[DescriptionLoc(LocalizedStrings.Str995Key)]
	[CategoryLoc(LocalizedStrings.Str985Key)]
	public class AverageWinTradeParameter : BaseStatisticParameter<decimal>, ITradeStatisticParameter
	{
		private decimal _sum;
		private int _count;

		/// <summary>
		/// Добавить в параметр информацию о новой сделке.
		/// </summary>
		/// <param name="info">Информация о новой сделке.</param>
		public void Add(PnLInfo info)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			if (info.ClosedVolume == 0)
				return;

			if (info.PnL > 0)
			{
				_sum += info.PnL;
				_count++;
			}

			Value = _count > 0 ? _sum / _count : 0;
		}

		/// <summary>
		/// Сохранить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("Sum", _sum);
			storage.SetValue("Count", _count);

			base.Save(storage);
		}

		/// <summary>
		/// Загрузить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			_sum = storage.GetValue<decimal>("Sum");
			_count = storage.GetValue<int>("Count");

			base.Load(storage);
		}
	}

	/// <summary>
	/// Средняя проигрышная сделка.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str996Key)]
	[DescriptionLoc(LocalizedStrings.Str997Key)]
	[CategoryLoc(LocalizedStrings.Str985Key)]
	public class AverageLossTradeParameter : BaseStatisticParameter<decimal>, ITradeStatisticParameter
	{
		private decimal _sum;
		private int _count;

		/// <summary>
		/// Добавить в параметр информацию о новой сделке.
		/// </summary>
		/// <param name="info">Информация о новой сделке.</param>
		public void Add(PnLInfo info)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			if (info.ClosedVolume == 0)
				return;

			if (info.PnL <= 0)
			{
				_sum += info.PnL;
				_count++;
			}

			Value = _count > 0 ? _sum / _count : 0;
		}

		/// <summary>
		/// Сохранить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("Sum", _sum);
			storage.SetValue("Count", _count);

			base.Save(storage);
		}

		/// <summary>
		/// Загрузить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			_sum = storage.GetValue<decimal>("Sum");
			_count = storage.GetValue<int>("Count");

			base.Load(storage);
		}
	}
}