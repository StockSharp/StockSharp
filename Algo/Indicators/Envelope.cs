#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: Envelope.cs
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
	/// Envelope.
	/// </summary>
	[DisplayName("Envelope")]
	[Description("Envelope.")]
	public class Envelope : BaseComplexIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Envelope"/>.
		/// </summary>
		public Envelope()
			: this(new SimpleMovingAverage())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Envelope"/>.
		/// </summary>
		/// <param name="ma">Middle line.</param>
		public Envelope(LengthIndicator<decimal> ma)
		{
			InnerIndicators.Add(Middle = ma);
			InnerIndicators.Add(Upper = ma.TypedClone());
			InnerIndicators.Add(Lower = ma.TypedClone());

			Upper.Name = "Upper";
			Lower.Name = "Lower";
		}

		/// <summary>
		/// Middle line.
		/// </summary>
		[Browsable(false)]
		public LengthIndicator<decimal> Middle { get; }

		/// <summary>
		/// Upper line.
		/// </summary>
		[Browsable(false)]
		public LengthIndicator<decimal> Upper { get; }

		/// <summary>
		/// Lower line.
		/// </summary>
		[Browsable(false)]
		public LengthIndicator<decimal> Lower { get; }

		/// <summary>
		/// Period length. By default equal to 1.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str778Key)]
		[DescriptionLoc(LocalizedStrings.Str779Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public virtual int Length
		{
			get => Middle.Length;
			set
			{
				Middle.Length = Upper.Length = Lower.Length = value;
				Reset();
			}
		}

		private decimal _shift = 0.01m;

		/// <summary>
		/// The shift width. Specified as percentage from 0 to 1. The default equals to 0.01.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str783Key)]
		[DescriptionLoc(LocalizedStrings.Str784Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Shift
		{
			get => _shift;
			set
			{
				if (value < 0)
					throw new ArgumentNullException(nameof(value));

				_shift = value;
				Reset();
			}
		}

		/// <inheritdoc />
		public override bool IsFormed => Middle.IsFormed;

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var value = (ComplexIndicatorValue)base.OnProcess(input);

			var upper = value.InnerValues[Upper];
			value.InnerValues[Upper] = upper.SetValue(Upper, upper.GetValue<decimal>() * (1 + Shift));

			var lower = value.InnerValues[Lower];
			value.InnerValues[Lower] = lower.SetValue(Lower, lower.GetValue<decimal>() * (1 - Shift));

			return value;
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);
			Shift = storage.GetValue<decimal>(nameof(Shift));
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);
			storage.SetValue(nameof(Shift), Shift);
		}
	}
}