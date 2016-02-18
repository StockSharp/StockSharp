#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDiagram.SampleDiagramPublic
File: DiagramDebuggerControl.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Designer
{
	using System;
	using System.ComponentModel;
	using System.Windows;
	using System.Windows.Input;
	using System.Windows.Media;

	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Strategies;
	using StockSharp.Designer.Layout;
	using StockSharp.Xaml.Diagram;

	public partial class DiagramDebuggerControl : IPersistable
	{
		private readonly LayoutManager _layoutManager;

		private bool _isDefaultLayout = true;
		private bool _isLoaded;

		#region Strategy

		public static readonly DependencyProperty StrategyProperty = DependencyProperty.Register("Strategy", typeof(DiagramStrategy), typeof(DiagramDebuggerControl),
			new PropertyMetadata(null, OnStrategyPropertyChanged));

		private static void OnStrategyPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((DiagramDebuggerControl)sender).OnStrategyPropertyChanged((DiagramStrategy)args.NewValue);
		}

		public DiagramStrategy Strategy
		{
			get { return (DiagramStrategy)GetValue(StrategyProperty); }
			set { SetValue(StrategyProperty, value); }
		}

		#endregion

		public DiagramDebugger Debugger { get; private set; }

		public ICommand AddBreakpointCommand { get; private set; }

		public ICommand RemoveBreakpointCommand { get; private set; }

		public ICommand StepNextCommand { get; private set; }

		public ICommand StepIntoCommand { get; private set; }

		public ICommand StepOutCommand { get; private set; }

		public ICommand ContinueCommand { get; private set; }

		public event Action Changed;

		public DiagramDebuggerControl()
		{
			InitializeCommands();
            InitializeComponent();
			
			_layoutManager = new LayoutManager(DockingManager);
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);

			if (_isLoaded)
				return;

			_isLoaded = true;

			if (!_isDefaultLayout)
				return;

			//PropertyGridControlAnchorable.ToggleAutoHide();
		}

		private void InitializeCommands()
		{
			AddBreakpointCommand = new DelegateCommand(
				obj =>
				{
					Debugger.AddBreak(DiagramEditor.SelectedElement.SelectedSocket);
					Changed.SafeInvoke();
				},
				obj => SafeCheckDebugger((d, s) => !d.IsBreak(s)));

			RemoveBreakpointCommand = new DelegateCommand(
				obj =>
				{
					Debugger.RemoveBreak(DiagramEditor.SelectedElement.SelectedSocket);
					Changed.SafeInvoke();
				},
				obj => SafeCheckDebugger((d, s) => d.IsBreak(s)));

			StepNextCommand = new DelegateCommand(
				obj => Debugger.StepNext(),
				obj => Debugger != null && Debugger.IsWaiting);

			StepIntoCommand = new DelegateCommand(
				obj => Debugger.StepInto(DiagramEditor?.SelectedElement as CompositionDiagramElement),
				obj => (Debugger != null && Debugger.IsWaitingOnInput && Debugger.CanStepInto) || DiagramEditor?.SelectedElement is CompositionDiagramElement);

			StepOutCommand = new DelegateCommand(
				obj => Debugger.StepOut(DiagramEditor.Composition),
				obj => Debugger != null && Debugger.CanStepOut);

			ContinueCommand = new DelegateCommand(
				obj => Debugger.Continue(),
				obj => Debugger != null && Debugger.IsWaiting);
		}

		private void OnDiagramEditorSelectionChanged(DiagramElement element)
		{
			ShowElementProperties(element);
		}

		private void OnDiagramEditorElementDoubleClicked(DiagramElement element)
		{
			var composition = element as CompositionDiagramElement;

			if (composition == null)
				return;

			Debugger.StepInto(composition);
		}

		private void OnStrategyPropertyChanged(DiagramStrategy strategy)
		{
			if (strategy != null)
			{
				strategy.PropertyChanged += OnStrategyPropertyChanged;
				strategy.ProcessStateChanged += OnStrategyProcessStateChanged;

				var composition = strategy.Composition;

				Debugger = new DiagramDebugger(composition);
				Debugger.Break += OnDebuggerBreak;
				Debugger.CompositionChanged += OnDebuggerCompositionChanged;

				NoStrategyLabel.Visibility = Visibility.Hidden;
				DiagramEditor.Composition = composition;

				ShowElementProperties(null);
			}
			else
			{
				Debugger = null;

				NoStrategyLabel.Visibility = Visibility.Visible;
				DiagramEditor.Composition = new CompositionDiagramElement { Name = string.Empty };
			}

			DiagramEditor.Composition.IsModifiable = false;
		}

		private void OnStrategyPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			//Changed.SafeInvoke();
		}

		private void OnStrategyProcessStateChanged(Strategy strategy)
		{
			this.GuiAsync(() =>
			{
				if (PropertyGridControl.SelectedObject == strategy)
					PropertyGridControl.IsReadOnly = strategy.ProcessState != ProcessStates.Stopped;
			});
		}

		private void OnDebuggerBreak(DiagramSocket socket)
		{
			this.GuiAsync(() =>
			{
				var element = socket.Parent;

				DiagramEditor.SelectedElement = element;
				ShowElementProperties(element);
			});
		}

		private void OnDebuggerCompositionChanged(CompositionDiagramElement element)
		{
			this.GuiAsync(() => DiagramEditor.Composition = element);
		}

		private void ShowElementProperties(DiagramElement element)
		{
			if (element != null)
			{
				if (PropertyGridControl.SelectedObject == element)
					PropertyGridControl.SelectedObject = null;

				PropertyGridControl.SelectedObject = new DiagramElementParameters(element);
				PropertyGridControl.IsReadOnly = true;
			}
			else
			{
				PropertyGridControl.SelectedObject = Strategy;
				PropertyGridControl.IsReadOnly = Strategy.ProcessState != ProcessStates.Stopped;
			}
		}

		private bool SafeCheckDebugger(Func<DiagramDebugger, DiagramSocket, bool> func)
		{
			return Debugger != null && 
				DiagramEditor.SelectedElement != null &&
				DiagramEditor.SelectedElement.SelectedSocket != null && 
				func(Debugger, DiagramEditor.SelectedElement.SelectedSocket);
		}

		#region IPersistable

		public void Load(SettingsStorage storage)
		{
			Debugger.Load(storage);

			var layout = storage.GetValue<string>("Layout");

			if (!layout.IsEmpty())
			{
				_isDefaultLayout = false;
				_layoutManager.LoadLayout(layout);
			}

			var diagramEditor = storage.GetValue<SettingsStorage>("DiagramEditor");

			if (diagramEditor != null)
				DiagramEditor.Load(diagramEditor);
		}

		public void Save(SettingsStorage storage)
		{
			Debugger.Save(storage);

			storage.SetValue("Layout", _layoutManager.SaveLayout());
			storage.SetValue("DiagramEditor", DiagramEditor.Save());
		}

		#endregion
	}
}
