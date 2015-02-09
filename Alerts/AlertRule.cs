namespace StockSharp.Alerts
{
	using System.Reflection;

	using Ecng.ComponentModel;
	using Ecng.Serialization;

	/// <summary>
	/// Правило.
	/// </summary>
	public class AlertRule
	{
		/// <summary>
		/// Создать <see cref="AlertRule"/>.
		/// </summary>
		public AlertRule()
		{
			
		}

		/// <summary>
		/// Свойство сообщения, с которым будет производиться сравнение со значением <see cref="Value"/>
		/// на основе критерия <see cref="Operator"/>.
		/// </summary>
		[Member]
		public PropertyInfo	Property { get; set; }

		/// <summary>
		/// Критерий сравнения значения <see cref="Value"/>.
		/// </summary>
		public ComparisonOperator Operator { get; set; }

		/// <summary>
		/// Значение для сравнения.
		/// </summary>
		public object Value { get; set; }
	}
}