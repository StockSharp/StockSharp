namespace StockSharp.Algo.Storages
{
	using System;
	using System.IO;

	using Ecng.Common;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Мета-информация о данных за один день.
	/// </summary>
	public interface IMarketDataMetaInfo
	{
		/// <summary>
		/// Дата дня.
		/// </summary>
		DateTime Date { get; }

		/// <summary>
		/// Количество данных.
		/// </summary>
		int Count { get; set; }

		/// <summary>
		/// Значение <see cref="Security.PriceStep"/> в день <see cref="Date"/>.
		/// </summary>
		decimal PriceStep { get; set; }

		/// <summary>
		/// Значение <see cref="Security.VolumeStep"/> в день <see cref="Date"/>.
		/// </summary>
		decimal VolumeStep { get; set; }

		/// <summary>
		/// Время первой записи.
		/// </summary>
		DateTime FirstTime { get; set; }

		/// <summary>
		/// Время последней записи.
		/// </summary>
		DateTime LastTime { get; set; }

		/// <summary>
		/// Сохранить параметры мета-информации в поток.
		/// </summary>
		/// <param name="stream">Поток данных.</param>
		void Write(Stream stream);

		/// <summary>
		/// Загрузить параметры мета-информации из потока.
		/// </summary>
		/// <param name="stream">Поток данных.</param>
		void Read(Stream stream);
	}

	abstract class MetaInfo<TMetaInfo> : Cloneable<TMetaInfo>, IMarketDataMetaInfo
		where TMetaInfo : MetaInfo<TMetaInfo>
	{
		protected MetaInfo(DateTime date)
		{
			Date = date;
		}

		public DateTime Date { get; private set; }
		public int Count { get; set; }

		public decimal PriceStep { get; set; }
		public decimal VolumeStep { get; set; }

		public DateTime FirstTime { get; set; }
		public DateTime LastTime { get; set; }

		/// <summary>
		/// Сохранить параметры мета-информации в поток.
		/// </summary>
		/// <param name="stream">Поток данных.</param>
		public abstract void Write(Stream stream);

		/// <summary>
		/// Загрузить параметры мета-информации из потока.
		/// </summary>
		/// <param name="stream">Поток данных.</param>
		public abstract void Read(Stream stream);

		public static TMetaInfo CreateMetaInfo(DateTime date)
		{
			return typeof(TMetaInfo).CreateInstance<TMetaInfo>(date);
		}
	}
}