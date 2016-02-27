#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Ribbon.StudioPublic
File: CodePanelGroup.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Ribbon
{
	using System.Windows;
	using System.Windows.Input;

	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;

	public partial class CodePanelGroup
	{
		public static readonly RoutedCommand EditReferencesCommand = new RoutedCommand();

		public static readonly DependencyProperty SelectedStrategyInfoProperty = DependencyProperty.Register("SelectedStrategyInfo", typeof(StrategyInfo), typeof(CodePanelGroup));

		public StrategyInfo SelectedStrategyInfo
		{
			get { return (StrategyInfo)GetValue(SelectedStrategyInfoProperty); }
			set { SetValue(SelectedStrategyInfoProperty, value); }
		}

		public CodePanelGroup()
		{
			InitializeComponent();
		}

		private void ExecutedEditReferences(object sender, ExecutedRoutedEventArgs e)
		{
			new EditReferencesCommand().SyncProcess(SelectedStrategyInfo.GetKey());
		}

		private void CanExecuteEditReferences(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedStrategyInfo != null && (SelectedStrategyInfo.Type == StrategyInfoTypes.Analytics || SelectedStrategyInfo.Type == StrategyInfoTypes.SourceCode);
		}
	}
}
