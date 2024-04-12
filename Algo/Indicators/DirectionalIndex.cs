#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: DirectionalIndex.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.ComponentModel;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Welles Wilder Directional Movement Index.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/dmi.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DMIKey,
		Description = LocalizedStrings.WellesWilderDirectionalMovementIndexKey)]
	[Doc("topics/api/indicators/list_of_indicators/dmi.html")]
	public class DirectionalIndex : BaseComplexIndicator
	{
		private class DxValue : ComplexIndicatorValue
		{
			private decimal _value;

			public DxValue(IComplexIndicator indicator)
				: base(indicator)
			{
			}

			public override IIndicatorValue SetValue<T>(IIndicator indicator, T value)
			{
				IsEmpty = false;
				_value = value.To<decimal>();
				return new DecimalIndicatorValue(indicator, _value);
			}

			public override T GetValue<T>(Level1Fields? field)
			{
				return _value.To<T>();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DirectionalIndex"/>.
		/// </summary>
		public DirectionalIndex()
		{
			AddInner(Plus = new());
			AddInner(Minus = new());
		}

		/// <inheritdoc />
		public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

		/// <summary>
		/// Period length.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PeriodKey,
			Description = LocalizedStrings.IndicatorPeriodKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public int Length
		{
			get => Plus.Length;
			set
			{
				Plus.Length = Minus.Length = value;
				Reset();
			}
		}

		/// <summary>
		/// DI+.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.DiPlusKey,
			Description = LocalizedStrings.DiPlusLineKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public DiPlus Plus { get; }

		/// <summary>
		/// DI-.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.DiMinusKey,
			Description = LocalizedStrings.DiMinusLineKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public DiMinus Minus { get; }

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var value = new DxValue(this) { IsFinal = input.IsFinal };

			var plusValue = Plus.Process(input);
			var minusValue = Minus.Process(input);

			value.InnerValues.Add(Plus, plusValue);
			value.InnerValues.Add(Minus, minusValue);

			if (plusValue.IsEmpty || minusValue.IsEmpty)
				return value;

			var plus = plusValue.GetValue<decimal>();
			var minus = minusValue.GetValue<decimal>();

			var diSum = plus + minus;
			var diDiff = Math.Abs(plus - minus);

			value.InnerValues.Add(this, value.SetValue(this, diSum != 0m ? (100 * diDiff / diSum) : 0m));

			return value;
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);
			Length = storage.GetValue<int>(nameof(Length));
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);
			storage.SetValue(nameof(Length), Length);
		}

		/// <inheritdoc />
		public override string ToString() => base.ToString() + " " + Length;
	}
}