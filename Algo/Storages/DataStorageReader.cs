namespace StockSharp.Algo.Storages
{
	using System;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	/// <summary>
	/// Интерфейс считывателя данных.
	/// </summary>
	/// <typeparam name="TData">Тип маркет-данных.</typeparam>
	public interface IDataStorageReader<TData> : IDisposable
	{
		/// <summary>
		/// Поток.
		/// </summary>
		Stream Stream { get; }

		/// <summary>
		/// Мета-информация о данных за один день.
		/// </summary>
		IMarketDataMetaInfo MetaInfo { get; }

		/// <summary>
		/// Загрузить данные.
		/// </summary>
		/// <returns>Данные. Если данных не существует, то будет возвращено пустое множество.</returns>
		IEnumerableEx<TData> Load();
	}

	/// <summary>
	/// Считыватель данных.
	/// </summary>
	/// <typeparam name="TData">Тип маркет-данных.</typeparam>
	class DataStorageReader<TData> : Disposable, IDataStorageReader<TData>
	{
		private readonly Stream _stream;
		private readonly IMarketDataMetaInfo _metaInfo;
		private readonly IMarketDataSerializer<TData> _serializer;

		/// <summary>
		/// Поток.
		/// </summary>
		public Stream Stream { get { return _stream; } }

		/// <summary>
		/// Мета-информация о данных за один день.
		/// </summary>
		public IMarketDataMetaInfo MetaInfo { get { return _metaInfo; } }

		/// <summary>
		/// Создать <see cref="DataStorageReader{TData}"/>.
		/// </summary>
		/// <param name="stream">Поток.</param>
		/// <param name="metaInfo">Мета-информация о данных за один день.</param>
		/// <param name="serializer">Сериализатор.</param>
		public DataStorageReader(Stream stream, IMarketDataMetaInfo metaInfo, IMarketDataSerializer<TData> serializer)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			if (serializer == null)
				throw new ArgumentNullException("serializer");

			_stream = stream;
			_metaInfo = metaInfo;
			_serializer = serializer;
		}

		/// <summary>
		/// Загрузить данные.
		/// </summary>
		/// <returns>Данные. Если данных не существует, то будет возвращено пустое множество.</returns>
		public IEnumerableEx<TData> Load()
		{
			_stream.Position = 0;

			return _metaInfo == null 
				? Enumerable.Empty<TData>().ToEx() 
				: _serializer.Deserialize(_stream, _metaInfo);
		}

		/// <summary>
		/// Очистить ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			_stream.Dispose();
			base.DisposeManaged();
		}
	}

	class ConvertableDataStorageReader<TData, TSource> : Disposable, IDataStorageReader<TData>
	{
		private readonly IDataStorageReader<TSource> _reader;
		private readonly Func<IEnumerableEx<TSource>, IEnumerableEx<TData>> _converter;

		public Stream Stream { get { return _reader.Stream; } }

		public IMarketDataMetaInfo MetaInfo { get { return _reader.MetaInfo; } }

		public ConvertableDataStorageReader(IDataStorageReader<TSource> reader, Func<IEnumerableEx<TSource>, IEnumerableEx<TData>> converter)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");

			if (converter == null)
				throw new ArgumentNullException("converter");

			_reader = reader;
			_converter = converter;
		}

		public IEnumerableEx<TData> Load()
		{
			return _converter(_reader.Load());
		}

		protected override void DisposeManaged()
		{
			_reader.Dispose();
			base.DisposeManaged();
		}
	}
}