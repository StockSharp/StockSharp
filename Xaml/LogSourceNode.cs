#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: LogSourceNode.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;

	using Ecng.Common;
	using Ecng.Xaml;

	/// <summary>
	/// The logs source tree node.
	/// </summary>
	public class LogSourceNode : Disposable
	{
		/// <summary>
		/// The unique key.
		/// </summary>
		public Guid Key { get; }

		internal readonly string KeyStr;

		/// <summary>
		/// The display name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LogSourceNode"/>.
		/// </summary>
		/// <param name="key">The unique key.</param>
		/// <param name="name">The display name.</param>
		/// <param name="parentNode">The parent node.</param>
		public LogSourceNode(Guid key, string name, LogSourceNode parentNode)
		{
			//if (key.IsEmpty())
			//	throw new ArgumentNullException("key");

			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			Key = key;
			Name = name;
			ParentNode = parentNode;

			KeyStr = Key.ToString();
			ChildNodes = new ThreadSafeObservableCollection<LogSourceNode>(new ObservableCollectionEx<LogSourceNode>());
		}

		/// <summary>
		/// The parent node.
		/// </summary>
		public LogSourceNode ParentNode { get; private set; }

		/// <summary>
		/// Child nodes.
		/// </summary>
		public ThreadSafeObservableCollection<LogSourceNode> ChildNodes { get; private set; }

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return Name;
		}
	}
}