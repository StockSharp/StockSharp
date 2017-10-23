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

	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// The full class of linear regression, calculates LinearReg, LinearRegSlope, RSquared and StandardError at the same time.
	/// </summary>
	[DisplayName("LinearRegression")]
	[DescriptionLoc(LocalizedStrings.Str735Key)]
	public class LinearRegression : BaseComplexIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LinearRegression"/>.
		/// </summary>
		public LinearRegression()
			: this(new LinearReg(), new RSquared(), new LinearRegSlope(), new StandardError())
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
		public LinearRegression(LinearReg linearReg, RSquared rSquared, LinearRegSlope regSlope, StandardError standardError)
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
		[DisplayNameLoc(LocalizedStrings.Str736Key)]
		[DescriptionLoc(LocalizedStrings.Str737Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
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
		[DisplayName("LinearReg")]
		[DescriptionLoc(LocalizedStrings.Str738Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public LinearReg LinearReg { get; }

		/// <summary>
		/// Regression R-squared.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayName("RSquared")]
		[DescriptionLoc(LocalizedStrings.Str739Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public RSquared RSquared { get; }

		/// <summary>
		/// Standard error.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayName("StdErr")]
		[DescriptionLoc(LocalizedStrings.Str740Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public StandardError StandardError { get; }

		/// <summary>
		/// Coefficient with independent variable, slope of a straight line.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayName("LinearRegSlope")]
		[DescriptionLoc(LocalizedStrings.Str741Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public LinearRegSlope LinearRegSlope { get; }

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