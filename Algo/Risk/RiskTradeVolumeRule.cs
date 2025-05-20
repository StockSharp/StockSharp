namespace StockSharp.Algo.Risk;

/// <summary>
/// Risk-rule, tracking trade volume.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TradeVolumeKey,
	Description = LocalizedStrings.RiskTradeVolumeKey,
	GroupName = LocalizedStrings.TradesKey)]
public class RiskTradeVolumeRule : RiskRule
{
	private decimal _volume;

	/// <summary>
	/// Trade volume.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VolumeKey,
		Description = LocalizedStrings.TradeVolumeDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public decimal Volume
	{
		get => _volume;
		set
		{
			if (_volume == value)
				return;

			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_volume = value;
			UpdateTitle();
		}
	}

	/// <inheritdoc />
	protected override string GetTitle() => _volume.To<string>();

	/// <inheritdoc />
	public override bool ProcessMessage(Message message)
	{
		if (message.Type != MessageTypes.Execution)
			return false;

		var execMsg = (ExecutionMessage)message;

		if (!execMsg.HasTradeInfo())
			return false;

		return execMsg.TradeVolume >= Volume;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Volume), Volume);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Volume = storage.GetValue<decimal>(nameof(Volume));
	}
}
