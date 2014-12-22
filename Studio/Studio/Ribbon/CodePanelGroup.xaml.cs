namespace StockSharp.Studio.Ribbon
{
	using System.Windows;
	using System.Windows.Input;

	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;

	public partial class CodePanelGroup
	{
		public readonly static RoutedCommand EditReferencesCommand = new RoutedCommand();

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
