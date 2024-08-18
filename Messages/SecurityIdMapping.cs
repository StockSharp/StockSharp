namespace StockSharp.Messages;

/// <summary>
/// Security identifier mapping.
/// </summary>
[DataContract]
[Serializable]
public class SecurityIdMapping : IPersistable
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
		return new KeyValuePair<SecurityId, SecurityId>(mapping.StockSharpId, mapping.AdapterId);
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"{StockSharpId}<->{AdapterId}";
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Load(SettingsStorage storage)
	{
		StockSharpId = storage.GetValue<SettingsStorage>(nameof(StockSharpId)).Load<SecurityId>();
		AdapterId = storage.GetValue<SettingsStorage>(nameof(AdapterId)).Load<SecurityId>();
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(StockSharpId), StockSharpId.Save());
		storage.SetValue(nameof(AdapterId), AdapterId.Save());
	}
}