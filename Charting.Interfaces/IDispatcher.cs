namespace StockSharp.Charting
{
	using System;

	/// <summary>
	/// Threads dispatcher.
	/// </summary>
	public interface IDispatcher
	{
		/// <summary>
		/// Call action in dispatcher thread.
		/// </summary>
		/// <param name="action">Action.</param>
		void Invoke(Action action);

		/// <summary>
		/// Call action in dispatcher thread.
		/// </summary>
		/// <param name="action">Action.</param>
		void InvokeAsync(Action action);
	}
}