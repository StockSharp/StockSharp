#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: IMarketDataSerializer.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;

	/// <summary>
	/// The serializer.
	/// </summary>
	public interface IMarketDataSerializer
	{
		/// <summary>
		/// Storage format.
		/// </summary>
		StorageFormats Format { get; }

		/// <summary>
		/// Time precision.
		/// </summary>
		TimeSpan TimePrecision { get; }

		/// <summary>
		/// To create empty meta-information.
		/// </summary>
		/// <param name="date">Date.</param>
		/// <returns>Meta-information on data for one day.</returns>
		IMarketDataMetaInfo CreateMetaInfo(DateTime date);

		/// <summary>
		/// Save data into stream.
		/// </summary>
		/// <param name="stream">Data stream.</param>
		/// <param name="data">Data.</param>
		/// <param name="metaInfo">Meta-information on data for one day.</param>
		void Serialize(Stream stream, IEnumerable data, IMarketDataMetaInfo metaInfo);

		/// <summary>
		/// To load data from the stream.
		/// </summary>
		/// <param name="stream">Data stream.</param>
		/// <param name="metaInfo">Meta-information on data for one day.</param>
		/// <returns>Data.</returns>
		IEnumerable Deserialize(Stream stream, IMarketDataMetaInfo metaInfo);
	}

	/// <summary>
	/// The serializer.
	/// </summary>
	/// <typeparam name="TData">Data type.</typeparam>
	public interface IMarketDataSerializer<TData> : IMarketDataSerializer
	{
		/// <summary>
		/// Save data into stream.
		/// </summary>
		/// <param name="stream">Data stream.</param>
		/// <param name="data">Data.</param>
		/// <param name="metaInfo">Meta-information on data for one day.</param>
		void Serialize(Stream stream, IEnumerable<TData> data, IMarketDataMetaInfo metaInfo);

		/// <summary>
		/// To load data from the stream.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="metaInfo">Meta-information on data for one day.</param>
		/// <returns>Data.</returns>
		new IEnumerable<TData> Deserialize(Stream stream, IMarketDataMetaInfo metaInfo);
	}
}