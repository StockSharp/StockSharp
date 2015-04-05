namespace StockSharp.Xaml
{
	using System;

	using Ecng.Common;
	using Ecng.Xaml;

	/// <summary>
	/// Узел дерева источника логов.
	/// </summary>
	public class LogSourceNode : Disposable
	{
		/// <summary>
		/// Уникальный ключ.
		/// </summary>
		public Guid Key { get; private set; }

		internal readonly string KeyStr;

		/// <summary>
		/// Отображаемое имя.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Создать <see cref="LogSourceNode"/>.
		/// </summary>
		/// <param name="key">Уникальный ключ.</param>
		/// <param name="name">Отображаемое имя.</param>
		/// <param name="parentNode">Родительский узел.</param>
		public LogSourceNode(Guid key, string name, LogSourceNode parentNode)
		{
			//if (key.IsEmpty())
			//	throw new ArgumentNullException("key");

			if (name.IsEmpty())
				throw new ArgumentNullException("name");

			Key = key;
			Name = name;
			ParentNode = parentNode;

			KeyStr = Key.ToString();
			ChildNodes = new ThreadSafeObservableCollection<LogSourceNode>(new ObservableCollectionEx<LogSourceNode>());
		}

		/// <summary>
		/// Родительский узел.
		/// </summary>
		public LogSourceNode ParentNode { get; private set; }

		/// <summary>
		/// Дочерние узлы.
		/// </summary>
		public ThreadSafeObservableCollection<LogSourceNode> ChildNodes { get; private set; }

		/// <summary>
		/// Получить строковое представление узла.
		/// </summary>
		/// <returns>Строковое представление узла.</returns>
		public override string ToString()
		{
			return Name;
		}
	}
}