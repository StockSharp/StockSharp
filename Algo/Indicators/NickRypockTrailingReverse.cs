#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: NickRypockTrailingReverse.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// NickRypockTrailingReverse (Nick Rypock Trailing reverse).
	/// </summary>
	[DisplayName("NRTR")]
	[Description("Nick Rypock Trailing reverse.")]
	public class NickRypockTrailingReverse : LengthIndicator<decimal>
	{
		private bool _isInitialized;

		private decimal _k;
		private decimal _reverse;
		private decimal _price;
		private decimal _highPrice;
		private decimal _lowPrice;
		private int _newTrend;

		/// <summary>
		/// The trend direction.
		/// </summary>
		private int _trend;

		private decimal _multiple;

		/// <summary>
		/// Multiplication factor.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str806Key)]
		[DescriptionLoc(LocalizedStrings.Str807Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Multiple
		{
			get => _multiple * 1000;
			set
			{
				var tmpValue = value;

				if (tmpValue <= 1)
					tmpValue = 1;

				_multiple = tmpValue / 1000;

				Reset();
			}
		}

		//private int _roundDigits;

		///// <summary>
		///// Округление до знака после запятой.
		///// </summary>
		//[DisplayName("Округление после запятой")]
		//[Description("Округление до знака после запятой.")]
		//[Category("Основное")]
		//public int RoundDigits
		//{
		//	get { return _roundDigits; }
		//	set
		//	{
		//		_roundDigits = value;

		//		if (_roundDigits < 0)
		//			_roundDigits = 0;

		//		Reset();
		//	}
		//}

		/// <summary>
		/// Initializes a new instance of the <see cref="NickRypockTrailingReverse"/>.
		/// </summary>
		public NickRypockTrailingReverse()
		{
			Multiple = 100;
			Length = 50;
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			if (_isInitialized == false)
			{
				_k = input.GetValue<decimal>();
				_highPrice = input.GetValue<decimal>();
				_lowPrice = input.GetValue<decimal>();

				_isInitialized = true;
			}

			_price = input.GetValue<decimal>();

			_k = (_k + (_price - _k) / Length) * _multiple;

			_newTrend = 0;

			if (_trend >= 0)
			{
				if (_price > _highPrice)
					_highPrice = _price;

				_reverse = _highPrice - _k;

				if (_price <= _reverse)
				{
					_newTrend = -1;
					_lowPrice = _price;
					_reverse = _lowPrice + _k;
				}
				else
				{
					_newTrend = +1;
				}
			}

			if (_trend <= 0)
			{
				if (_price < _lowPrice)
					_lowPrice = _price;

				_reverse = _lowPrice + _k;

				if (_price >= _reverse)
				{
					_newTrend = +1;
					_highPrice = _price;
					_reverse = _highPrice - _k;
				}
				else
				{
					_newTrend = -1;
				}
			}

			if (_newTrend != 0)
				_trend = _newTrend;

			var newValue = _reverse;

			// если буффер стал достаточно большим (стал больше длины)
			if (IsFormed)
			{
				// удаляем хвостовое значение
				Buffer.RemoveAt(0);
			}

			Buffer.Add(newValue);

			// значение NickRypockTrailingReverse
			return new DecimalIndicatorValue(this, newValue);
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			_isInitialized = false;

			_k = 0;
			_reverse = 0;
			_price = 0;
			_highPrice = 0;
			_lowPrice = 0;
			_trend = 0;
			_newTrend = 0;
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			Multiple = settings.GetValue<decimal>(nameof(Multiple));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			settings.SetValue(nameof(Multiple), Multiple);
		}
	}
}