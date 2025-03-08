namespace StockSharp.Algo.Import;

/// <summary>
/// Messages importer from text file in CSV format into storage.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CsvImporter"/>.
/// </remarks>
/// <param name="dataType">Data type info.</param>
/// <param name="fields">Importing fields.</param>
/// <param name="securityStorage">Securities meta info storage.</param>
/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
/// <param name="storageFormat">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
public class CsvImporter(DataType dataType, IEnumerable<FieldMapping> fields, ISecurityStorage securityStorage, IExchangeInfoProvider exchangeInfoProvider, IMarketDataDrive drive, StorageFormats storageFormat) : CsvParser(dataType, fields)
{
	private readonly ISecurityStorage _securityStorage = securityStorage ?? throw new ArgumentNullException(nameof(securityStorage));
	private readonly IExchangeInfoProvider _exchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));

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

		LogInfo(LocalizedStrings.ImportOfType.Put(fileName, DataType.MessageType.Name));

		try
		{
			var len = new FileInfo(fileName).Length;
			var prevPercent = 0;
			var lineIndex = 0;

			var isSecurityRequired = DataType.IsSecurityRequired;

			foreach (var msg in Parse(fileName, isCancelled))
			{
				if (msg is SecurityMappingMessage)
					continue;

				if (isSecurityRequired && msg is ISecurityIdMessage secIdMsg && secIdMsg.SecurityId == default)
					throw new InvalidOperationException($"{LocalizedStrings.EmptySecId}: {msg}");

				if (msg is not SecurityMessage secMsg)
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
							LogError(LocalizedStrings.HasDuplicates.Put(secMsg.SecurityId));
							continue;
						}

						isNew = false;
						security.ApplyChanges(secMsg, _exchangeInfoProvider, UpdateDuplicateSecurities);
					}
					else
						security = secMsg.ToSecurity(_exchangeInfoProvider);

					_securityStorage.Save(security, UpdateDuplicateSecurities);

					//ExtendedInfoStorageItem?.Add(secMsg.SecurityId, secMsg.ExtensionInfo);

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
		LogInfo(LocalizedStrings.CreatingSec.Put(securityId));

		return securityId;
	}

	private void FlushBuffer(List<Message> buffer)
	{
		var registry = ServicesRegistry.StorageRegistry;

		if (DataType.MessageType == typeof(NewsMessage))
		{
			registry.GetNewsMessageStorage(drive, storageFormat).Save(buffer);
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
								if (candle.OpenTime.Date == default)
								{
									candle.OpenTime = candle.CloseTime;

									if (timeFrame != null)
										candle.OpenTime -= timeFrame.Value;
								}
								else if (candle.OpenTime.TimeOfDay == default)
								{
									candle.OpenTime = candle.CloseTime;

									if (timeFrame != null)
										candle.OpenTime -= timeFrame.Value;
								}
							}
						}

						registry
							.GetCandleMessageStorage(dataType, secId, DataType.Arg, drive, storageFormat)
							.Save(candles.OrderBy(c => c.OpenTime));
					}
					else if (dataType == typeof(TimeQuoteChange))
					{
						var storage = registry.GetQuoteMessageStorage(secId, drive, storageFormat);
						storage.Save(secGroup.Cast<QuoteChangeMessage>().OrderBy(md => md.ServerTime));
					}
					else
					{
						var storage = registry.GetStorage(secId, dataType, DataType.Arg, drive, storageFormat);

						if (dataType == typeof(ExecutionMessage))
							((IMarketDataStorage<ExecutionMessage>)storage).Save(secGroup.Cast<ExecutionMessage>().OrderBy(m => m.ServerTime));
						else if (dataType == typeof(Level1ChangeMessage))
							((IMarketDataStorage<Level1ChangeMessage>)storage).Save(secGroup.Cast<Level1ChangeMessage>().OrderBy(m => m.ServerTime));
						else
							throw new NotSupportedException(LocalizedStrings.UnsupportedType.Put(dataType.Name));
					}
				}
			}
		}

		buffer.Clear();
	}
}