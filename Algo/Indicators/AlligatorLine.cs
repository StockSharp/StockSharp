#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: AlligatorLine.cs
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
	/// The realization of one of indicator lines Alligator (Jaw, Teeth, and Lips).
	/// </summary>
	[Browsable(false)]
	public class AlligatorLine : LengthIndicator<decimal>
	{
		private readonly MedianPrice _medianPrice;

		private readonly SmoothedMovingAverage _sma;
		//private readonly SimpleMovingAverage _sma;

		/// <summary>
		/// Initializes a new instance of the <see cref="AlligatorLine"/>.
		/// </summary>
		public AlligatorLine()
		{
			_medianPrice = new MedianPrice();
			_sma = new SmoothedMovingAverage();
			//_sma = new SimpleMovingAverage();
		}

		private int _shift;

		/// <summary>
		/// Shift to the future.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str841Key)]
		[DescriptionLoc(LocalizedStrings.Str842Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int Shift
		{
			get => _shift;
			set
			{
				_shift = value;
				Reset();
			}
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			_sma.Length = Length;
			_medianPrice.Reset();

			base.Reset();
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => Buffer.Count > Shift;

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			//если кол-во в буфере больше Shift, то первое значение отдали в прошлый раз, удалим его.
			if (Buffer.Count > Shift)
				Buffer.RemoveAt(0);

			var smaResult = _sma.Process(_medianPrice.Process(input));
			if (_sma.IsFormed & input.IsFinal)
				Buffer.Add(smaResult.GetValue<decimal>());

			return Buffer.Count > Shift
				? new DecimalIndicatorValue(this, Buffer[0])
				: new DecimalIndicatorValue(this);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			Shift = settings.GetValue<int>(nameof(Shift));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue(nameof(Shift), Shift);
		}
	}
}