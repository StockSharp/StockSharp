#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.StudioPublic
File: ContentDocumentWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio
{
	using System.Windows;
	using System.Windows.Data;

	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Studio.Core;
	using StockSharp.Xaml.Diagram;

	public partial class ContentDocumentWindow : IContentWindow
	{
		public ContentDocumentWindow()
		{
			InitializeComponent();
		}

	    public string Id { get; set; }

	    public IStudioControl Control
		{
			get { return (IStudioControl)DataContext; }
			set
			{
				DataContext = value;
				Content = value;

				ApplyContext(Tag);
			}
		}

		private void ApplyContext(object context)
		{
			var isStrategy = context is StrategyContainer;

			if (isStrategy)
				this.SetBindings(TitleProperty, context, "Name", BindingMode.OneWay);

			HeaderTemplate = context is Security
				? (DataTemplate)FindResource("IndexSecurityHeaderTemplate")
				: (isStrategy
					? (DataTemplate)FindResource("StrategyHeaderTemplate")
					: (context is CompositionDiagramElement
						? (DataTemplate)FindResource("CompositionHeaderTemplate")
						: (DataTemplate)FindResource("DefaultHeaderTemplate")));
		}
	}
}