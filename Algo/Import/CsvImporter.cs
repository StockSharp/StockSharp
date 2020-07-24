namespace StockSharp.Algo.Import
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Messages importer from text file in CSV format into storage.
	/// </summary>
	public class CsvImporter : CsvParser
	{
		private readonly ISecurityStorage _securityStorage;
		private readonly IExchangeInfoProvider _exchangeInfoProvider;
		private readonly IMarketDataDrive _drive;
		private readonly StorageFormats _storageFormat;

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvImporter"/>.
		/// </summary>
		/// <param name="dataType">Data type info.</param>
		/// <param name="fields">Importing fields.</param>
		/// <param name="securityStorage">Securities meta info storage.</param>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="storageFormat">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		public CsvImporter(DataType dataType, IEnumerable<FieldMapping> fields, ISecurityStorage securityStorage, IExchangeInfoProvider exchangeInfoProvider, IMarketDataDrive drive, StorageFormats storageFormat)
			: base(dataType, fields)
		{
			_securityStorage = securityStorage ?? throw new ArgumentNullException(nameof(securityStorage));
			_exchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));
			_drive = drive;
			_storageFormat = storageFormat;
		}

		/// <summary>
		/// Update duplicate securities if they already exists.
		/// </summary>
		public bool UpdateDuplicateSecurities { get; set; }

		/// <summary>
		/// Security updated event.
		/// </summary>
		public event Action<Security, bool> SecurityUpdated;

		/// <summary>
		/// Import from CSV file.
		/// </summary>
		/// <param name="fileName">File name.</param>
		/// <param name="updateProgress">Progress notification.</param>
		/// <param name="isCancelled">The processor, returning process interruption sign.</param>
		/// <returns>Count and last time.</returns>
		public (int, DateTimeOffset?) Import(string fileName, Action<int> updateProgress, Func<bool> isCancelled)
		{
			var count = 0;
			var lastTime = default(DateTimeOffset?);

			var buffer = new List<Message>();

			void Flush()
			{
				count += buffer.Count;

				if (buffer.LastOrDefault() is IServerTimeMessage timeMsg)
					lastTime = timeMsg.ServerTime;

				FlushBuffer(buffer);
			}

			this.AddInfoLog(LocalizedStrings.Str2870Params.Put(fileName, DataType.MessageType.Name));

			try
			{
				var len = new FileInfo(fileName).Length;
				var prevPercent = 0;
				var lineIndex = 0;

				foreach (var msg in Parse(fileName, isCancelled))
				{
					if (msg is SecurityMappingMessage)
						continue;

					if (!(msg is SecurityMessage secMsg))
					{
						buffer.Add(msg);

						if (buffer.Count > 1000)
							Flush();
					}
					else
					{
						var security = _securityStorage.LookupById(secMsg.SecurityId);
						var isNew = true;

						if (security != null)
						{
							if (!UpdateDuplicateSecurities)
							{
								this.AddErrorLog(LocalizedStrings.Str1453.Put(secMsg.SecurityId));
								continue;
							}

							isNew = false;
							security.ApplyChanges(secMsg, _exchangeInfoProvider, UpdateDuplicateSecurities);
						}
						else
							security = secMsg.ToSecurity(_exchangeInfoProvider);

						_securityStorage.Save(security, UpdateDuplicateSecurities);

						ExtendedInfoStorageItem?.Add(secMsg.SecurityId, secMsg.ExtensionInfo);

						SecurityUpdated?.Invoke(security, isNew);
					}

					var percent = (int)(((double)lineIndex / len) * 100 - 1).Round();

					lineIndex++;

					if (percent <= prevPercent)
						continue;

					prevPercent = percent;
					updateProgress?.Invoke(prevPercent);
				}
			}
			catch (Exception ex)
			{
				ex.LogError();
			}

			if (buffer.Count > 0)
				Flush();

			return (count, lastTime);
		}

		private SecurityId TryInitSecurity(SecurityId securityId)
		{
			var security = _securityStorage.LookupById(securityId);

			if (security != null)
				return securityId;

			security = new Security
			{
				Id = securityId.ToStringId(),
				Code = securityId.SecurityCode,
				Board = _exchangeInfoProvider.GetOrCreateBoard(securityId.BoardCode),
			};

			_securityStorage.Save(security, false);
			this.AddInfoLog(LocalizedStrings.Str2871Params.Put(securityId));

			return securityId;
		}

		private void FlushBuffer(List<Message> buffer)
		{
			var registry = ServicesRegistry.StorageRegistry;

			if (DataType.MessageType == typeof(NewsMessage))
			{
				registry.GetNewsMessageStorage(_drive, _storageFormat).Save(buffer);
			}
			else
			{
				foreach (var typeGroup in buffer.GroupBy(i => i.GetType()))
				{
					var dataType = typeGroup.Key;

					foreach (var secGroup in typeGroup.GroupBy(g => ((ISecurityIdMessage)g).SecurityId))
					{
						var secId = TryInitSecurity(secGroup.Key);

						if (dataType.IsCandleMessage())
						{
							var timeFrame = DataType.Arg as TimeSpan?;
							var candles = secGroup.Cast<CandleMessage>().ToArray();

							foreach (var candle in candles)
							{
								if (candle.CloseTime < candle.OpenTime)
								{
									// close time doesn't exist in importing file
									candle.CloseTime = default;
								}
								else if (candle.CloseTime > candle.OpenTime)
								{
									// date component can be missed for open time
									if (candle.OpenTime.Date.IsDefault())
									{
										candle.OpenTime = candle.CloseTime;

										if (timeFrame != null)
											candle.OpenTime -= timeFrame.Value;
									}
									else if (candle.OpenTime.TimeOfDay.IsDefault())
									{
										candle.OpenTime = candle.CloseTime;

										if (timeFrame != null)
											candle.OpenTime -= timeFrame.Value;
									}
								}
							}

							registry
								.GetCandleMessageStorage(dataType, secId, DataType.Arg, _drive, _storageFormat)
								.Save(candles.OrderBy(c => c.OpenTime));
						}
						else if (dataType == typeof(TimeQuoteChange))
						{
							var storage = registry.GetQuoteMessageStorage(secId, _drive, _storageFormat);
							storage.Save(secGroup.Cast<QuoteChangeMessage>().OrderBy(md => md.ServerTime));
						}
						else
						{
							var storage = registry.GetStorage(secId, dataType, DataType.Arg, _drive, _storageFormat);

							if (dataType == typeof(ExecutionMessage))
								((IMarketDataStorage<ExecutionMessage>)storage).Save(secGroup.Cast<ExecutionMessage>().OrderBy(m => m.ServerTime));
							else if (dataType == typeof(Level1ChangeMessage))
								((IMarketDataStorage<Level1ChangeMessage>)storage).Save(secGroup.Cast<Level1ChangeMessage>().OrderBy(m => m.ServerTime));
							else
								throw new NotSupportedException(LocalizedStrings.Str2872Params.Put(dataType.Name));
						}
					}
				}
			}

			buffer.Clear();
		}
	}
}