namespace StockSharp.Algo.Import;

/// <summary>
/// Messages importer from text file in CSV format into storage.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CsvImporter"/> that writes to the provided single storage.
/// </remarks>
/// <param name="dataType">Data type info.</param>
/// <param name="fields">Importing fields.</param>
/// <param name="securityStorage">Securities meta info storage.</param>
/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
/// <param name="getStorage">Function to get <see cref="IMarketDataStorage"/> by <see cref="SecurityId"/>.</param>
public class CsvImporter(DataType dataType, IEnumerable<FieldMapping> fields, ISecurityStorage securityStorage, IExchangeInfoProvider exchangeInfoProvider, Func<SecurityId, IMarketDataStorage> getStorage) : CsvParser(dataType, fields)
{
	private readonly ISecurityStorage _securityStorage = securityStorage ?? throw new ArgumentNullException(nameof(securityStorage));
	private readonly IExchangeInfoProvider _exchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));
	private readonly Func<SecurityId, IMarketDataStorage> _getStorage = getStorage ?? throw new ArgumentNullException(nameof(getStorage));

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
	/// <param name="stream">The file stream.</param>
	/// <param name="updateProgress">Progress notification.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Count and last time.</returns>
	public async ValueTask<(int count, DateTimeOffset? lastTime)> Import(Stream stream, Action<int> updateProgress, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(stream);
		ArgumentNullException.ThrowIfNull(updateProgress);

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

		//LogInfo(LocalizedStrings.ImportOfType.Put(fileName, DataType.MessageType.Name));

		var canProgress = stream.CanSeek;
		var len = canProgress ? stream.Length : -1;
		var prevPercent = 0;

		var isSecurityRequired = DataType.IsSecurityRequired;

		await foreach (var msg in Parse(stream, cancellationToken).WithEnforcedCancellation(cancellationToken))
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

			if (!canProgress)
				continue;

			var percent = (int)Math.Round(((double)stream.Position / len) * 100);

			if (percent <= prevPercent)
				continue;

			prevPercent = percent;
			updateProgress(prevPercent);
		}

		if (buffer.Count > 0)
			Flush();

		if (canProgress)
		{
			if (prevPercent < 100)
				updateProgress(100);
		}

		return (count, lastTime);
	}

	private void SaveSecurity(SecurityId securityId)
	{
		var security = _securityStorage.LookupById(securityId);

		if (security != null)
			return;

		security = new()
		{
			Id = securityId.ToStringId(),
			Code = securityId.SecurityCode,
			Board = _exchangeInfoProvider.GetOrCreateBoard(securityId.BoardCode),
		};

		_securityStorage.Save(security, false);
		LogInfo(LocalizedStrings.CreatingSec.Put(securityId));
	}

	private void FlushBuffer(List<Message> buffer)
	{
		if (buffer.Count == 0)
			return;

		var secIdMsgs = buffer.Cast<ISecurityIdMessage>().ToArray();

		var secIds = secIdMsgs.Select(s => s.SecurityId).Where(id => !id.IsAllSecurity() && !id.IsSpecial).Distinct().ToSet();
		secIds.AddRange(buffer.OfType<INullableSecurityIdMessage>().Select(s => s.SecurityId).OfType<SecurityId>().Where(secId => !secId.IsAllSecurity() && !secId.IsSpecial));

		foreach (var secId in secIds)
			SaveSecurity(secId);

		foreach (var secGroup in secIdMsgs.GroupBy(m => m.SecurityId))
		{
			var storage = _getStorage(secGroup.Key);
			storage.Save(secGroup.Cast<IServerTimeMessage>().OrderBy(m => m.ServerTime).Cast<Message>());
		}

		buffer.Clear();
	}
}