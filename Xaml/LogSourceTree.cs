#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: LogSourceTree.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.Collections.ObjectModel;
	using System.Collections.Generic;

	using Hardcodet.Wpf.GenericTreeView;

	using StockSharp.Localization;

	/// <summary>
	/// The visual logs sources tree.
	/// </summary>
	[CLSCompliant(false)]
	public class LogSourceTree : TreeViewBase<LogSourceNode>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LogSourceTree"/>.
		/// </summary>
		public LogSourceTree()
		{
			base.ObserveChildItems = true;

			_coreRootNode = new LogSourceNode(Guid.NewGuid(), LocalizedStrings.Str1559, null);
			_strategyRootNode = new LogSourceNode(Guid.NewGuid(), LocalizedStrings.Str1355, _coreRootNode);

			base.Items = new ObservableCollection<LogSourceNode> { _coreRootNode };

			_coreRootNode.ChildNodes.Add(_strategyRootNode);
		}

		private readonly LogSourceNode _strategyRootNode;

		/// <summary>
		/// The root node of the strategies tree.
		/// </summary>
		public LogSourceNode StrategyRootNode => _strategyRootNode;

		private readonly LogSourceNode _coreRootNode;

		/// <summary>
		/// The root node of the logger.
		/// </summary>
		public LogSourceNode CoreRootNode => _coreRootNode;

		/// <summary>
		/// Generates a unique identifier for a given item that is represented as a node of the tree.
		/// </summary>
		/// <param name="item">An item which is represented by a tree node.</param>
		/// <returns>A unique key that represents the item.</returns>
		public override string GetItemKey(LogSourceNode item)
		{
			return item.KeyStr;
		}

		/// <summary>
		/// Gets all child items of a given parent item. The tree needs this method to properly traverse the
		/// logic tree of a given item.<br/>
		/// Important: If you plan to have the tree automatically update itself if nested content is being changed, you
		/// the <see cref="P:Hardcodet.Wpf.GenericTreeView.TreeViewBase`1.ObserveChildItems"/> property must be
		/// true, and the collection that is being returned needs to implement the <see cref="T:System.Collections.Specialized.INotifyCollectionChanged"/>
		/// interface (e.g. by returning an collection of type <see cref="T:System.Collections.ObjectModel.ObservableCollection`1"/>.
		/// </summary>
		/// <param name="parent">A currently processed item that is being represented as a node of the tree.</param>
		/// <returns>
		/// All child items to be represented by the tree. The returned collection needs to implement
		/// <see cref="T:System.Collections.Specialized.INotifyCollectionChanged"/> if the
		/// <see cref="P:Hardcodet.Wpf.GenericTreeView.TreeViewBase`1.ObserveChildItems"/> feature is supposed to work.
		/// </returns>
		/// <remarks>
		/// If this is an expensive operation, you should override <see cref="M:Hardcodet.Wpf.GenericTreeView.TreeViewBase`1.HasChildItems(`0)"/>
		/// which invokes this method by default.
		/// </remarks>
		public override ICollection<LogSourceNode> GetChildItems(LogSourceNode parent)
		{
			return parent.ChildNodes.Items;
		}

		/// <summary>
		/// Gets the parent of a given item, if available. If the item is a top-level element, this method is supposed to return a null reference.
		/// </summary>
		/// <param name="item">The currently processed item.</param>
		/// <returns>The parent of the item, if available.</returns>
		public override LogSourceNode GetParentItem(LogSourceNode item)
		{
			return item.ParentNode;
		}
	}
}