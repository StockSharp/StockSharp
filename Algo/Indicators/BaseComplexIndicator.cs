#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: BaseComplexIndicator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Serialization;

	using MoreLinq;

	/// <summary>
	/// Embedded indicators processing modes.
	/// </summary>
	public enum ComplexIndicatorModes
	{
		/// <summary>
		/// In-series. The result of the previous indicator execution is passed to the next one,.
		/// </summary>
		Sequence,

		/// <summary>
		/// In parallel. Results of indicators execution for not depend on each other.
		/// </summary>
		Parallel,
	}

	/// <summary>
	/// The base indicator, built in form of several indicators combination.
	/// </summary>
	public abstract class BaseComplexIndicator : BaseIndicator, IComplexIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BaseComplexIndicator"/>.
		/// </summary>
		/// <param name="innerIndicators">Embedded indicators.</param>
		protected BaseComplexIndicator(params IIndicator[] innerIndicators)
		{
			if (innerIndicators == null)
				throw new ArgumentNullException(nameof(innerIndicators));

			if (innerIndicators.Any(i => i == null))
				throw new ArgumentException(nameof(innerIndicators));

			InnerIndicators = new List<IIndicator>(innerIndicators);

			Mode = ComplexIndicatorModes.Parallel;
		}

		/// <summary>
		/// Embedded indicators processing mode. The default equals to <see cref="ComplexIndicatorModes.Parallel"/>.
		/// </summary>
		[Browsable(false)]
		public ComplexIndicatorModes Mode { get; protected set; }

		/// <summary>
		/// Embedded indicators.
		/// </summary>
		[Browsable(false)]
		protected IList<IIndicator> InnerIndicators { get; }

		IEnumerable<IIndicator> IComplexIndicator.InnerIndicators => InnerIndicators;

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed
		{
			get { return InnerIndicators.All(i => i.IsFormed); }
		}

		/// <summary>
		/// Result values type.
		/// </summary>
		public override Type ResultType { get; } = typeof(ComplexIndicatorValue);

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var value = new ComplexIndicatorValue(this);

			foreach (var indicator in InnerIndicators)
			{
				var result = indicator.Process(input);

				value.InnerValues.Add(indicator, result);

				if (Mode == ComplexIndicatorModes.Sequence)
				{
					if (!indicator.IsFormed)
					{
						break;
					}

					input = result;
				}
			}

			return value;
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			base.Reset();
			InnerIndicators.ForEach(i => i.Reset());
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			var index = 0;

			foreach (var indicator in InnerIndicators)
			{
				var innerSettings = new SettingsStorage();
				indicator.Save(innerSettings);
				settings.SetValue(indicator.Name + index, innerSettings);
				index++;
			}
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			var index = 0;

			foreach (var indicator in InnerIndicators)
			{
				indicator.Load(settings.GetValue<SettingsStorage>(indicator.Name + index));
				index++;
			}
		}
	}
}
