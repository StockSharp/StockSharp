namespace StockSharp.Xaml
{
	using StockSharp.Logging;

	/// <summary>
	/// Интерфейс к модели дерева источника логов.
	/// </summary>
	public interface ILogSourceNodeModel
	{
		/// <summary>
		/// Обработчик события о добавлении узла дерева источника логов.
		/// </summary>
		/// <param name="node">Узел дерева источника логов.</param>
		void NodeAdded(LogSourceNode node);

		/// <summary>
		/// Обработчик события об удалении узла дерева источника логов.
		/// </summary>
		/// <param name="node">Узел дерева источника логов.</param>
		void NodeRemoved(LogSourceNode node);

		/// <summary>
		/// Cоздать узел дерева источника логов.
		/// </summary>
		/// <param name="source">Источник логов.</param>
		/// <returns>Узел дерева источника логов.</returns>
		LogSourceNode CreateNode(ILogSource source);
	}
}