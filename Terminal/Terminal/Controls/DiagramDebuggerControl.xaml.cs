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

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

using Ecng.Common;
using Ecng.Xaml;

using StockSharp.Xaml.Diagram;
using StockSharp.Terminal.Logics;

namespace StockSharp.Terminal.Controls
{
	public partial class DiagramDebuggerControl
	{
		public static readonly DependencyProperty StrategyProperty = DependencyProperty.Register("Strategy", typeof(EmulationDiagramStrategy), typeof(DiagramDebuggerControl),
			new PropertyMetadata(null, OnStrategyPropertyChanged));

		private static void OnStrategyPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			((DiagramDebuggerControl)sender).OnStrategyPropertyChanged((EmulationDiagramStrategy)args.NewValue);
		}

		public EmulationDiagramStrategy Strategy
		{
			get { return (EmulationDiagramStrategy)GetValue(StrategyProperty); }
			set { SetValue(StrategyProperty, value); }
		}

		public DiagramDebugger Debugger { get; private set; }

		public ICommand AddBreakpointCommand { get; private set; }

		public ICommand RemoveBreakpointCommand { get; private set; }

		public ICommand StepNextCommand { get; private set; }

		public ICommand StepToOutParamCommand { get; private set; }

		public ICommand StepIntoCommand { get; private set; }

		public ICommand StepOutCommand { get; private set; }

		public ICommand ContinueCommand { get; private set; }

		public event Action Changed;

		public DiagramDebuggerControl()
		{
			InitializeCommands();
            InitializeComponent();
		}

		private void InitializeCommands()
		{
			AddBreakpointCommand = new DelegateCommand(
				obj =>
				{
					Debugger.AddBreak(DiagramEditor.SelectedElement);
					Changed.SafeInvoke();
				},
				obj => Debugger != null && DiagramEditor.SelectedElement != null && !Debugger.IsBreak(DiagramEditor.SelectedElement));

			RemoveBreakpointCommand = new DelegateCommand(
				obj =>
				{
					Debugger.RemoveBreak(DiagramEditor.SelectedElement);
					Changed.SafeInvoke();
				},
				obj => Debugger != null && DiagramEditor.SelectedElement != null && Debugger.IsBreak(DiagramEditor.SelectedElement));

			StepNextCommand = new DelegateCommand(
				obj => Debugger.StepNext(),
				obj => Debugger != null && Debugger.IsWaiting);

			StepToOutParamCommand = new DelegateCommand(
				obj => Debugger.StepOut(),
				obj => Debugger != null && Debugger.IsWaitingOnInput);

			StepIntoCommand = new DelegateCommand(
				obj => Debugger.StepInto(),
				obj => Debugger != null && Debugger.IsWaitingOnInput && Debugger.CanStepInto);

			StepOutCommand = new DelegateCommand(
				obj => Debugger.StepOut(),
				obj => Debugger != null && Debugger.CanStepOut);

			ContinueCommand = new DelegateCommand(
				obj => Debugger.Continue(),
				obj => Debugger != null && Debugger.IsWaiting);
		}

		private void OnDiagramEditorSelectionChanged(DiagramElement element)
		{
			ShowElementProperties(element);
		}

		private void OnStrategyPropertyChanged(EmulationDiagramStrategy strategy)
		{
			if (strategy != null)
			{
				strategy.PropertyChanged += OnStrategyPropertyChanged;

				var composition = strategy.Composition;

				Debugger = new DiagramDebugger(composition)
				{
					IsEnabled = true
				};
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
			Changed.SafeInvoke();
		}

		private void OnDebuggerBreak(DiagramElement element)
		{
			this.GuiAsync(() =>
			{
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
				PropertyGridControl.IsReadOnly = false;
			}
		}
	}
}
