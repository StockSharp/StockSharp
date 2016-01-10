#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDiagram.SampleDiagramPublic
File: SolutionExplorerControl.xaml.cs
Created: 2015, 12, 9, 6:53 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Designer
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using Hardcodet.Wpf.GenericTreeView;

	using MoreLinq;

	using StockSharp.Xaml.Diagram;

	public partial class SolutionExplorerControl
	{
		#region Compositions

		public static readonly DependencyProperty CompositionsProperty = DependencyProperty.Register("Compositions", typeof(INotifyList<DiagramElement>),
			typeof(SolutionExplorerControl), new PropertyMetadata(null, CompositionsPropertyChanged));

		private static void CompositionsPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((SolutionExplorerControl)sender).CompositionsPropertyChanged((INotifyList<DiagramElement>)args.NewValue);
		}

		public INotifyList<DiagramElement> Compositions
		{
			get { return (INotifyList<DiagramElement>)GetValue(CompositionsProperty); }
			set { SetValue(CompositionsProperty, value); }
		}

		#endregion

		#region Strategies

		public static readonly DependencyProperty StrategiesProperty = DependencyProperty.Register("Strategies", typeof(INotifyList<DiagramElement>),
			typeof(SolutionExplorerControl), new PropertyMetadata(null, StrategiesPropertyChanged));

		private static void StrategiesPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((SolutionExplorerControl)sender).StrategiesPropertyChanged((INotifyList<DiagramElement>)args.NewValue);
		}

		public INotifyList<DiagramElement> Strategies
		{
			get { return (INotifyList<DiagramElement>)GetValue(StrategiesProperty); }
			set { SetValue(StrategiesProperty, value); }
		}

		#endregion

		#region SelectedItem

		public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register("SelectedItem", typeof(CompositionItem),
			typeof(SolutionExplorerControl), new PropertyMetadata(null));

		public CompositionItem SelectedItem
		{
			get { return (CompositionItem)GetValue(SelectedItemProperty); }
			set { SetValue(SelectedItemProperty, value); }
		}

		#endregion

		#region NodeContextMenu

		public static readonly DependencyProperty NodeContextMenuProperty = DependencyProperty.Register("NodeContextMenu", typeof(ContextMenu),
			typeof(SolutionExplorerControl), new PropertyMetadata(null));

		public ContextMenu NodeContextMenu
		{
			get { return (ContextMenu)GetValue(NodeContextMenuProperty); }
			set { SetValue(NodeContextMenuProperty, value); }
		}

		#endregion

		private readonly SolutionExplorerItem _compositionsItem;
		private readonly SolutionExplorerItem _strategiesItem;

		public event Action<CompositionItem> Open;

		public SolutionExplorerControl()
		{
			_compositionsItem = new SolutionExplorerItem(CompositionType.Composition);
			_strategiesItem = new SolutionExplorerItem(CompositionType.Strategy);


			InitializeComponent();
			
			ExplorerTree.Items = new List<SolutionExplorerItem>
			{
				_compositionsItem,
				_strategiesItem,
			};
		}

		private void CompositionsPropertyChanged(INotifyList<DiagramElement> elements)
		{
			_compositionsItem.SetSource(elements);
		}

		private void StrategiesPropertyChanged(INotifyList<DiagramElement> elements)
		{
			_strategiesItem.SetSource(elements);
		}

		private void ExplorerTree_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var item = ExplorerTree.SelectedItem;

			if (item?.Element == null)
				return;

			Open.SafeInvoke(item.Element);
        }

		private void ExplorerTree_OnSelectedItemChanged(object sender, RoutedTreeItemEventArgs<SolutionExplorerItem> e)
		{
			SelectedItem = ExplorerTree.SelectedItem?.Parent == null ? null : ExplorerTree.SelectedItem.Element;
		}
	}

	public class SolutionExplorerItem : NotifiableObject
	{
		private INotifyList<DiagramElement> _source;
		private string _name;

		public Guid Id { get; }

		public string TextId { get; }

		public string Name
		{
			get { return _name; }
			set
			{
				_name = value;
				NotifyChanged("Name");
			}
		}

		public CompositionType Type { get; }

		public SolutionExplorerItem Parent { get; }

		public CompositionItem Element { get; }

		public ObservableCollection<SolutionExplorerItem> ChildItems { get; }

		private SolutionExplorerItem(Guid id, string name, SolutionExplorerItem parent)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			Id = id;
			TextId = id.ToString();
			Name = name;
			Parent = parent;
			ChildItems = new ObservableCollection<SolutionExplorerItem>();
		}

		public SolutionExplorerItem(CompositionType type)
			: this(Guid.NewGuid(), type.GetDisplayName(), null)
		{
			Type = type;
		}

		public SolutionExplorerItem(CompositionDiagramElement element, SolutionExplorerItem parent)
			: this(element.TypeId, element.Name, parent)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			if (parent == null)
				throw new ArgumentNullException(nameof(parent));

			Type = parent.Type;

			Element = new CompositionItem(parent.Type, element);
			Element.Element.PropertyChanged += OnElementPropertyChanged;
		}

		private void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Name")
				Name = Element.Element.Name;
		}

		public void SetSource(INotifyList<DiagramElement> elements)
		{
			if (_source != null)
			{
				_source.Added -= OnSourceAdded;
				_source.Removed -= OnSourceRemoved;
				_source.Cleared -= OnSourceCleared;
			}

			ChildItems.Clear();

			_source = elements;

			if (_source == null)
				return;

			_source.Added += OnSourceAdded;
			_source.Removed += OnSourceRemoved;
			_source.Cleared += OnSourceCleared;

			_source.ForEach(OnSourceAdded);
		}

		private void OnSourceAdded(DiagramElement element)
		{
			var composition = element as CompositionDiagramElement;

			if (composition == null)
				return;

			ChildItems.Add(new SolutionExplorerItem(composition, this));
		}

		private void OnSourceRemoved(DiagramElement element)
		{
			var composition = element as CompositionDiagramElement;

			if (composition == null)
				return;

			ChildItems.RemoveWhere(i => i.Id == element.TypeId);
		}

		private void OnSourceCleared()
		{
			ChildItems.Clear();
        }
	}

	public class SolutionExplorerTree : TreeViewBase<SolutionExplorerItem>
	{
		public override string GetItemKey(SolutionExplorerItem item)
		{
			return item.TextId;
		}

		public override ICollection<SolutionExplorerItem> GetChildItems(SolutionExplorerItem item)
		{
			return item.ChildItems;
		}

		public override SolutionExplorerItem GetParentItem(SolutionExplorerItem item)
		{
			return item.Parent;
		}
	}
}
