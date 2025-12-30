namespace StockSharp.BusinessEntities;

/// <summary>
/// The custom provider.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
public interface ICustomProvider<T>
{
	/// <summary>
	/// All items.
	/// </summary>
	IEnumerable<T> All { get; }

	/// <summary>
	/// Add.
	/// </summary>
	/// <param name="item">Type of <typeparamref name="T"/>.</param>
	void Add(T item);

	/// <summary>
	/// Remove.
	/// </summary>
	/// <param name="item">Type of <typeparamref name="T"/>.</param>
	void Remove(T item);
}