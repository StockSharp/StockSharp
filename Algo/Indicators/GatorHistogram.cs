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
			if (line1 == null)
				throw new ArgumentNullException(nameof(line1));

			if (line2 == null)
				throw new ArgumentNullException(nameof(line2));

			_line1 = line1;
			_line2 = line2;
			_isNegative = isNegative;
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			if (input.IsFinal)
				IsFormed = true;

			return new DecimalIndicatorValue(this, (_isNegative ? -1 : 1) * Math.Abs(_line1.GetCurrentValue() - _line2.GetCurrentValue()));
		}

		/// <summary>
		/// Create a copy of <see cref="GatorHistogram"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IIndicator Clone()
		{
			return new GatorHistogram((AlligatorLine)_line1.Clone(), (AlligatorLine)_line2.Clone(), _isNegative) { Name = Name };
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			_line1.LoadNotNull(settings, "line1");
			_line2.LoadNotNull(settings, "line2");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			settings.SetValue("line1", _line1.Save());
			settings.SetValue("line2", _line2.Save());
		}
	}
}