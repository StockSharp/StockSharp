namespace StockSharp.Messages
{
	using System;

	/// <summary>
	/// Shows the message adapter is stub.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class MessageAdapterStubAttribute : Attribute
	{
		/// <summary>
		/// Package id.
		/// </summary>
		public string PackageId { get; set; }
	}
}