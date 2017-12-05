#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: IMarketDataMetaInfo.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
		object LastId { get; set; }

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

		/// <summary>
		/// Is override all data.
		/// </summary>
		bool IsOverride { get; }
	}

	abstract class MetaInfo : IMarketDataMetaInfo
	{
		protected MetaInfo(DateTime date)
		{
			Date = date;
		}

		public DateTime Date { get; }
		public int Count { get; set; }

		public decimal PriceStep { get; set; }
		public decimal VolumeStep { get; set; }

		//public decimal FirstPriceStep { get; set; }
		public decimal LastPriceStep { get; set; }

		public DateTime FirstTime { get; set; }
		public DateTime LastTime { get; set; }

		public abstract object LastId { get; set; }

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

		/// <summary>
		/// Is override all data.
		/// </summary>
		public virtual bool IsOverride => false;

		//public static TMetaInfo CreateMetaInfo(DateTime date)
		//{
		//	return typeof(TMetaInfo).CreateInstance<TMetaInfo>(date);
		//}
	}
}