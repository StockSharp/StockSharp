namespace StockSharp.Algo.Indicators
{
	using System;

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
		/// Is input.
		/// </summary>
		public bool IsInput { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="IndicatorValueAttribute"/>.
		/// </summary>
		/// <param name="type">Value type.</param>
		/// <param name="isInput">Is input.</param>
		protected IndicatorValueAttribute(Type type, bool isInput)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			if (!typeof(IIndicatorValue).IsAssignableFrom(type))
				throw new ArgumentException(nameof(type));

			Type = type;
			IsInput = isInput;
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
			: base(type, true)
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
			: base(type, false)
		{
		}
	}
}