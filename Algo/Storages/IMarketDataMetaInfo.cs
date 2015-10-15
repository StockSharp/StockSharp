namespace StockSharp.Algo.Storages
{
	using System;
	using System.IO;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Meta-information on data for one day.
	/// </summary>
	public interface IMarketDataMetaInfo
	{
		/// <summary>
		/// Date of day.
		/// </summary>
		DateTime Date { get; }

		/// <summary>
		/// Number of data.
		/// </summary>
		int Count { get; set; }

		/// <summary>
		/// Value <see cref="Security.PriceStep"/> at day <see cref="IMarketDataMetaInfo.Date"/>.
		/// </summary>
		decimal PriceStep { get; set; }

		/// <summary>
		/// Value <see cref="Security.VolumeStep"/> at day <see cref="IMarketDataMetaInfo.Date"/>.
		/// </summary>
		decimal VolumeStep { get; set; }

		/// <summary>
		/// First record time.
		/// </summary>
		DateTime FirstTime { get; set; }

		/// <summary>
		/// Last record time.
		/// </summary>
		DateTime LastTime { get; set; }

		/// <summary>
		/// Last record identifier.
		/// </summary>
		object LastId { get; }

		/// <summary>
		/// To save meta-information parameters to stream.
		/// </summary>
		/// <param name="stream">Data stream.</param>
		void Write(Stream stream);

		/// <summary>
		/// To load meta-information parameters from the stream.
		/// </summary>
		/// <param name="stream">Data stream.</param>
		void Read(Stream stream);
	}

	abstract class MetaInfo : IMarketDataMetaInfo
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

		public abstract object LastId { get; }

		/// <summary>
		/// To save meta-information parameters to stream.
		/// </summary>
		/// <param name="stream">Data stream.</param>
		public abstract void Write(Stream stream);

		/// <summary>
		/// To load meta-information parameters from the stream.
		/// </summary>
		/// <param name="stream">Data stream.</param>
		public abstract void Read(Stream stream);

		//public static TMetaInfo CreateMetaInfo(DateTime date)
		//{
		//	return typeof(TMetaInfo).CreateInstance<TMetaInfo>(date);
		//}
	}
}