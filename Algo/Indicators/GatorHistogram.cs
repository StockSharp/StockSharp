#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: GatorHistogram.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;

	using Ecng.Serialization;

	/// <summary>
	/// The oscillator histogram <see cref="GatorOscillator"/>.
	/// </summary>
	public class GatorHistogram : BaseIndicator
	{
		private readonly AlligatorLine _line1;
		private readonly AlligatorLine _line2;
		private readonly bool _isNegative;

		internal GatorHistogram(AlligatorLine line1, AlligatorLine line2, bool isNegative)
		{
			_line1 = line1 ?? throw new ArgumentNullException(nameof(line1));
			_line2 = line2 ?? throw new ArgumentNullException(nameof(line2));
			_isNegative = isNegative;
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			if (input.IsFinal)
				IsFormed = true;

			var line1Curr = _line1.GetNullableCurrentValue();
			var line2Curr = _line2.GetNullableCurrentValue();

			if (line1Curr == null || line2Curr == null)
				return new DecimalIndicatorValue(this);

			return new DecimalIndicatorValue(this, (_isNegative ? -1 : 1) * Math.Abs(line1Curr.Value - line2Curr.Value));
		}

		/// <summary>
		/// Create a copy of <see cref="GatorHistogram"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IIndicator Clone()
		{
			return new GatorHistogram((AlligatorLine)_line1.Clone(), (AlligatorLine)_line2.Clone(), _isNegative) { Name = Name };
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			_line1.LoadNotNull(settings, "line1");
			_line2.LoadNotNull(settings, "line2");
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			settings.SetValue("line1", _line1.Save());
			settings.SetValue("line2", _line2.Save());
		}
	}
}