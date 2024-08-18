namespace StockSharp.Charting;

/// <summary>
/// Attribute to ignore entity in the chart.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ChartIgnoreAttribute : Attribute
{
}