namespace StockSharp.Algo.Export
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Algo.Indicators;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Indicator value with time.
	/// </summary>
	public class IndicatorValue : IServerTimeMessage
	{
		/// <summary>
		/// Security.
		/// </summary>
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Value time.
		/// </summary>
		public DateTimeOffset Time { get; set; }

		private IIndicatorValue _value;

		/// <summary>
		/// Value.
		/// </summary>
		public IIndicatorValue Value
		{
			get => _value;
			set
			{
				_value = value;

				if (value == null)
					ValuesAsDecimal = null;
				else
				{
					var values = new List<decimal?>(); 
					FillValues(Value, values);
					ValuesAsDecimal = values;
				}
			}
		}

		/// <summary>
		/// Converted to <see cref="decimal"/> type value.
		/// </summary>
		[Obsolete("Use Value1 property.")]
		public decimal? ValueAsDecimal => Value1;

		/// <summary>
		/// Converted to <see cref="decimal"/> type value.
		/// </summary>
		public decimal? Value1 => ValueAt(0);

		/// <summary>
		/// Converted to <see cref="decimal"/> type value.
		/// </summary>
		public decimal? Value2 => ValueAt(1);

		/// <summary>
		/// Converted to <see cref="decimal"/> type value.
		/// </summary>
		public decimal? Value3 => ValueAt(2);

		/// <summary>
		/// Converted to <see cref="decimal"/> type value.
		/// </summary>
		public decimal? Value4 => ValueAt(3);

		private decimal? ValueAt(int index)
		{
			return ValuesAsDecimal?.ElementAtOrDefault(index);
		}

		/// <summary>
		/// Converted to <see cref="decimal"/> type values.
		/// </summary>
		public IEnumerable<decimal?> ValuesAsDecimal { get; private set; }

		private static void FillValues(IIndicatorValue value, ICollection<decimal?> values)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (values == null)
				throw new ArgumentNullException(nameof(values));

			if (value is DecimalIndicatorValue || value is CandleIndicatorValue || value is ShiftedIndicatorValue)
			{
				values.Add(value.IsEmpty ? (decimal?)null : value.GetValue<decimal>());
			}
			else if (value is ComplexIndicatorValue complexValue)
			{
				foreach (var innerIndicator in ((IComplexIndicator)value.Indicator).InnerIndicators)
				{
					var innerValue = complexValue.InnerValues.TryGetValue(innerIndicator);

					if (innerValue == null)
						values.Add(null);
					else
						FillValues(innerValue, values);
				}
			}
			else
				throw new ArgumentOutOfRangeException(nameof(value), value.GetType(), LocalizedStrings.Str1655);
		}

		DateTimeOffset IServerTimeMessage.ServerTime
		{
			get => Time;
			set => Time = value;
		}

		DateTimeOffset IMessage.LocalTime
		{
			get => Time;
			set => Time = value;
		}
		
		MessageTypes IMessage.Type => throw new NotSupportedException();
		IMessageAdapter IMessage.Adapter { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
		MessageBackModes IMessage.BackMode { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
		object ICloneable.Clone() => throw new NotSupportedException();
	}
}