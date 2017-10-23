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

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Welles Wilder Directional Movement Index.
	/// </summary>
	[DisplayName("DX")]
	[DescriptionLoc(LocalizedStrings.Str762Key)]
	public class DirectionalIndex : BaseComplexIndicator
	{
		private sealed class DxValue : ComplexIndicatorValue
		{
			private decimal _value;

			public DxValue(IIndicator indicator)
				: base(indicator)
			{
			}

			public override IIndicatorValue SetValue<T>(IIndicator indicator, T value)
			{
				IsEmpty = false;
				_value = value.To<decimal>();
				return new DecimalIndicatorValue(indicator, _value);
			}

			public override T GetValue<T>()
			{
				return _value.To<T>();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DirectionalIndex"/>.
		/// </summary>
		public DirectionalIndex()
		{
			InnerIndicators.Add(Plus = new DiPlus());
			InnerIndicators.Add(Minus = new DiMinus());
		}

		/// <summary>
		/// Period length.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str736Key)]
		[DescriptionLoc(LocalizedStrings.Str737Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public virtual int Length
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
		[DisplayName("DI+")]
		[Description("DI+.")]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public DiPlus Plus { get; }

		/// <summary>
		/// DI-.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayName("DI-")]
		[Description("DI-.")]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public DiMinus Minus { get; }

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
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

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			Length = settings.GetValue<int>(nameof(Length));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue(nameof(Length), Length);
		}
	}
}