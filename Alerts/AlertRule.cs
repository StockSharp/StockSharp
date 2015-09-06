namespace StockSharp.Alerts
{
	using System.Reflection;

	using Ecng.ComponentModel;
	using Ecng.Serialization;

	/// <summary>
	/// Rule.
	/// </summary>
	public class AlertRule
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AlertRule"/>.
		/// </summary>
		public AlertRule()
		{
			
		}

		/// <summary>
		/// Message property, which will be made a comparison with the value of <see cref="AlertRule.Value"/> based on the criterion <see cref="AlertRule.Operator"/>.
		/// </summary>
		[Member]
		public PropertyInfo	Property { get; set; }

		/// <summary>
		/// The criterion of comparison values <see cref="AlertRule.Value"/>.
		/// </summary>
		public ComparisonOperator Operator { get; set; }

		/// <summary>
		/// Comparison value.
		/// </summary>
		public object Value { get; set; }
	}
}