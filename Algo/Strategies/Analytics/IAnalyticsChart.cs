namespace StockSharp.Algo.Strategies.Analytics
{
	using System;

	/// <summary>
	/// The interface for work with a chart.
	/// </summary>
	public interface IAnalyticsChart
	{
		/// <summary>
		/// Append new values.
		/// </summary>
		/// <param name="x">X value.</param>
		/// <param name="y">Y value.</param>
		/// <param name="z">Z value.</param>
		void Append(DateTime x, decimal y, decimal z);

		/// <summary>
		/// Append new values.
		/// </summary>
		/// <param name="x">X value.</param>
		/// <param name="y">Y value.</param>
		void Append(decimal x, decimal y);
		
		/// <summary>
		/// Update values.
		/// </summary>
		/// <param name="x">X value.</param>
		/// <param name="y">Y value.</param>
		/// <param name="z">Z value.</param>
		void Update(DateTime x, decimal y, decimal z);

		/// <summary>
		/// Append new values.
		/// </summary>
		/// <param name="x">X value.</param>
		/// <param name="y">Y value.</param>
		void Update(decimal x, decimal y);
	}
}