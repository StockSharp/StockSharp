namespace StockSharp.Messages;

/// <summary>
/// Security identifier mapping.
/// </summary>
[DataContract]
[Serializable]
public class SecurityIdMapping : IAsyncPersistable
{
	/// <summary>
	/// StockSharp format.
	/// </summary>
	[DataMember]
	public SecurityId StockSharpId { get; set; }

	/// <summary>
	/// Adapter format.
	/// </summary>
	[DataMember]
	public SecurityId AdapterId { get; set; }

	/// <summary>
	/// Cast <see cref="KeyValuePair{T1,T2}"/> object to the type <see cref="SecurityIdMapping"/>.
	/// </summary>
	/// <param name="pair"><see cref="KeyValuePair{T1,T2}"/> value.</param>
	/// <returns><see cref="SecurityIdMapping"/> value.</returns>
	public static implicit operator SecurityIdMapping(KeyValuePair<SecurityId, SecurityId> pair)
	{
		return new SecurityIdMapping
		{
			StockSharpId = pair.Key,
			AdapterId = pair.Value
		};
	}

	/// <summary>
	/// Cast object from <see cref="SecurityIdMapping"/> to <see cref="KeyValuePair{T1,T2}"/>.
	/// </summary>
	/// <param name="mapping"><see cref="SecurityIdMapping"/> value.</param>
	/// <returns><see cref="KeyValuePair{T1,T2}"/> value.</returns>
	public static explicit operator KeyValuePair<SecurityId, SecurityId>(SecurityIdMapping mapping)
	{
		if (mapping is null)
			throw new ArgumentNullException(nameof(mapping));

		return new KeyValuePair<SecurityId, SecurityId>(mapping.StockSharpId, mapping.AdapterId);
	}

	/// <summary>
	/// Create a copy of <see cref="SecurityIdMapping"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public SecurityIdMapping Clone() => (SecurityIdMapping)MemberwiseClone();

	/// <inheritdoc />
	public override string ToString()
	{
		return $"{StockSharpId}<->{AdapterId}";
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		StockSharpId = await storage.GetValue<SettingsStorage>(nameof(StockSharpId)).LoadAsync<SecurityId>(cancellationToken);
		AdapterId = await storage.GetValue<SettingsStorage>(nameof(AdapterId)).LoadAsync<SecurityId>(cancellationToken);
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		storage.SetValue(nameof(StockSharpId), await StockSharpId.SaveAsync(cancellationToken));
		storage.SetValue(nameof(AdapterId), await AdapterId.SaveAsync(cancellationToken));
	}
}