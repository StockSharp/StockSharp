namespace StockSharp.Messages;

partial class Extensions
{
	/// <summary>
	/// Create a copy of the adapter.
	/// </summary>
	/// <param name="adapter"><see cref="IMessageAdapter"/></param>
	/// <returns>Copy.</returns>
	[Obsolete("Blocking sync-over-async wrapper. Use CloneAsync instead.")]
	public static IMessageAdapter Clone(this IMessageAdapter adapter)
	{
		if (adapter is null)
			throw new ArgumentNullException(nameof(adapter));

		return AsyncHelper.Run(() => adapter.CloneAsync(default));
	}
}
