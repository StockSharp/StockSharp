#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: Level1Indicator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The indicator, built on the market data basis.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.SecurityKey)]
	[DescriptionLoc(LocalizedStrings.Str747Key)]
	[IndicatorIn(typeof(SingleIndicatorValue<Level1ChangeMessage>))]
	public class Level1Indicator : BaseIndicator
	{
		/// <summary>
		/// Level one market-data field, which is used as an indicator value.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str748Key)]
		[DescriptionLoc(LocalizedStrings.Str749Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public Level1Fields Field { get; set; }

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var message = input.GetValue<Level1ChangeMessage>();

			var retVal = message.TryGet(Field);

			if (!IsFormed && retVal != null && input.IsFinal)
				IsFormed = true;

			return retVal == null ? new DecimalIndicatorValue(this) : new DecimalIndicatorValue(this, (decimal)retVal);
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
}