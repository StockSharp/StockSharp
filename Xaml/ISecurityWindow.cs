namespace StockSharp.Xaml
{
	using System;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface that describes a window for trading instrument creating or editing.
	/// </summary>
	public interface ISecurityWindow
	{
		/// <summary>
		/// The handler checking the entered identifier availability for <see cref="ISecurityWindow.Security"/>.
		/// </summary>
		Func<string, string> ValidateId { get; set; }

		/// <summary>
		/// Security.
		/// </summary>
		Security Security { get; set; }
	}
}