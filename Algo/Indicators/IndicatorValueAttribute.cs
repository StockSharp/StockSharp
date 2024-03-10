namespace StockSharp.Algo.Indicators
{
	using System;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Attribute, applied to indicator, to provide information about type of values <see cref="IIndicatorValue"/>.
	/// </summary>
	public abstract class IndicatorValueAttribute : Attribute
	{
		/// <summary>
		/// Value type.
		/// </summary>
		public Type Type { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="IndicatorValueAttribute"/>.
		/// </summary>
		/// <param name="type">Value type.</param>
		protected IndicatorValueAttribute(Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			if (!type.Is<IIndicatorValue>())
				throw new ArgumentException(LocalizedStrings.TypeNotImplemented.Put(type.Name, nameof(IIndicatorValue)), nameof(type));

			Type = type;
		}
	}

	/// <summary>
	/// Attribute, applied to indicator, to provide information about type of input values <see cref="IIndicatorValue"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class IndicatorInAttribute : IndicatorValueAttribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IndicatorInAttribute"/>.
		/// </summary>
		/// <param name="type">Values type.</param>
		public IndicatorInAttribute(Type type)
			: base(type)
		{
		}
	}

	/// <summary>
	/// Attribute, applied to indicator, to provide information about type of output values <see cref="IIndicatorValue"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class IndicatorOutAttribute : IndicatorValueAttribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IndicatorOutAttribute"/>.
		/// </summary>
		/// <param name="type">Values type.</param>
		public IndicatorOutAttribute(Type type)
			: base(type)
		{
		}
	}

	/// <summary>
	/// Attribute, applied to indicator that must be hidden from any UI selections.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class IndicatorHiddenAttribute : Attribute
	{
	}
}