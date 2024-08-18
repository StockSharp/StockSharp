namespace StockSharp.Algo.Indicators;

using System.ComponentModel.DataAnnotations;

using Ecng.Serialization;

using StockSharp.Messages;
using StockSharp.Localization;
using StockSharp.Charting;

/// <summary>
/// The indicator, built on the market data basis.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.Level1Key,
	Description = LocalizedStrings.Level1IndicatorKey)]
[IndicatorIn(typeof(SingleIndicatorValue<Level1ChangeMessage>))]
[ChartIgnore]
public class Level1Indicator : BaseIndicator
{
	/// <summary>
	/// Level one market-data field, which is used as an indicator value.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.FieldKey,
		Description = LocalizedStrings.Level1FieldKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public Level1Fields Field { get; set; } = Level1Fields.ClosePrice;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var value = input.GetValue<decimal?>(Field);

		if (!IsFormed && value != null && input.IsFinal)
			IsFormed = true;

		return value is decimal d
			? new DecimalIndicatorValue(this, d)
			: new DecimalIndicatorValue(this);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);
		Field = storage.GetValue<Level1Fields>(nameof(Field));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage.SetValue(nameof(Field), Field);
	}
}
