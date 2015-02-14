namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using Ecng.Serialization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Localization;

	/// <summary>
	/// Полный класс линейной регрессии, считает одновременно LinearReg, LinearRegSlope, RSquared и StandardError.
	/// </summary>
	[DisplayName("LinearRegression")]
	[DescriptionLoc(LocalizedStrings.Str735Key)]
	public class LinearRegression : BaseComplexIndicator
	{
		/// <summary>
		/// Создать <see cref="LinearRegression"/>.
		/// </summary>
		public LinearRegression()
			: this(new LinearReg(), new RSquared(), new LinearRegSlope(), new StandardError())
		{
			Length = 11;
		}

		///<summary>
		/// Создать <see cref="LinearRegression"/>.
		///</summary>
		///<param name="linearReg">Линейная регрессия.</param>
		///<param name="rSquared">R-квадрат регрессии.</param>
		///<param name="regSlope">Коэффициент при независимой переменной, угол наклона прямой.</param>
		///<param name="standardError">Стандартная ошибка.</param>
		public LinearRegression(LinearReg linearReg, RSquared rSquared, LinearRegSlope regSlope, StandardError standardError)
			: base(linearReg, rSquared, regSlope, standardError)
		{
			LinearReg = linearReg;
			RSquared = rSquared;
			LinearRegSlope = regSlope;
			StandardError = standardError;

			Mode = ComplexIndicatorModes.Parallel;
		}

		///<summary>
		/// Длина периода.
		///</summary>
		[DisplayNameLoc(LocalizedStrings.Str736Key)]
		[DescriptionLoc(LocalizedStrings.Str737Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public int Length
		{
			get { return LinearReg.Length; }
			set
			{
				LinearReg.Length = RSquared.Length = LinearRegSlope.Length = StandardError.Length = value;
				Reset();
			}
		}

		/// <summary>
		/// Линейная регрессия.
		/// </summary>
		[ExpandableObject]
		[DisplayName("LinearReg")]
		[DescriptionLoc(LocalizedStrings.Str738Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public LinearReg LinearReg { get; private set; }

		/// <summary>
		/// R-квадрат регрессии.
		/// </summary>
		[ExpandableObject]
		[DisplayName("RSquared")]
		[DescriptionLoc(LocalizedStrings.Str739Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public RSquared RSquared { get; private set; }

		/// <summary>
		/// Стандартная ошибка.
		/// </summary>
		[ExpandableObject]
		[DisplayName("StdErr")]
		[DescriptionLoc(LocalizedStrings.Str740Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public StandardError StandardError { get; private set; }

		/// <summary>
		/// Коэффициент при независимой переменной, угол наклона прямой.
		/// </summary>
		[ExpandableObject]
		[DisplayName("LinearRegSlope")]
		[DescriptionLoc(LocalizedStrings.Str741Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public LinearRegSlope LinearRegSlope { get; private set; }

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			Length = settings.GetValue<int>("Length");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue("Length", Length);
		}
	}
}