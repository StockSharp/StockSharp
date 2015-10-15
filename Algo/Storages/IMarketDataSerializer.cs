namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;

	using Ecng.Collections;

	/// <summary>
	/// The serializer.
	/// </summary>
	public interface IMarketDataSerializer
	{
		/// <summary>
		/// To create empty meta-information.
		/// </summary>
		/// <param name="date">Date.</param>
		/// <returns>Meta-information on data for one day.</returns>
		IMarketDataMetaInfo CreateMetaInfo(DateTime date);

		/// <summary>
		/// Cast data into stream.
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
		IEnumerableEx Deserialize(Stream stream, IMarketDataMetaInfo metaInfo);
	}

	/// <summary>
	/// The serializer.
	/// </summary>
	/// <typeparam name="TData">Data type.</typeparam>
	public interface IMarketDataSerializer<TData> : IMarketDataSerializer
	{
		/// <summary>
		/// Cast data into stream.
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
		new IEnumerableEx<TData> Deserialize(Stream stream, IMarketDataMetaInfo metaInfo);
	}
}