#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: LinearRegression.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// The full class of linear regression, calculates LinearReg, LinearRegSlope, RSquared and StandardError at the same time.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LinearRegressionKey,
		Description = LocalizedStrings.LinearRegressionDescKey)]
	[Browsable(false)]
	public class LinearRegression : BaseComplexIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LinearRegression"/>.
		/// </summary>
		public LinearRegression()
			: this(new(), new(), new(), new())
		{
			Length = 11;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LinearRegression"/>.
		/// </summary>
		/// <param name="linearReg">Linear regression.</param>
		/// <param name="rSquared">Regression R-squared.</param>
		/// <param name="regSlope">Coefficient with independent variable, slope of a straight line.</param>
		/// <param name="standardError">Standard error.</param>
		public LinearRegression(LinearReg linearReg, LinearRegRSquared rSquared, LinearRegSlope regSlope, StandardError standardError)
			: base(linearReg, rSquared, regSlope, standardError)
		{
			LinearReg = linearReg;
			RSquared = rSquared;
			LinearRegSlope = regSlope;
			StandardError = standardError;

			Mode = ComplexIndicatorModes.Parallel;
		}

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
			get => LinearReg.Length;
			set
			{
				LinearReg.Length = RSquared.Length = LinearRegSlope.Length = StandardError.Length = value;
				Reset();
			}
		}

		/// <summary>
		/// Linear regression.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.LinearRegressionKey,
			Description = LocalizedStrings.LinearRegressionKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public LinearReg LinearReg { get; }

		/// <summary>
		/// Regression R-squared.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.RSquaredKey,
			Description = LocalizedStrings.RSquaredKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public LinearRegRSquared RSquared { get; }

		/// <summary>
		/// Standard error.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.StandardErrorKey,
			Description = LocalizedStrings.StandardErrorLinearRegKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public StandardError StandardError { get; }

		/// <summary>
		/// Coefficient with independent variable, slope of a straight line.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.LRSKey,
			Description = LocalizedStrings.LinearRegSlopeKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public LinearRegSlope LinearRegSlope { get; }

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