#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.StudioPublic
File: OptimizatorContent.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo.Strategies;
	using StockSharp.Logging;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Studio.Services;
	using StockSharp.Localization;
	using StockSharp.Xaml.Actipro;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	public partial class OptimizatorContent : IStudioControl, IStudioCommandScope
	{
		private class ParameterInfo : NotifiableObject, IPersistable
		{
			private decimal _from;
			private decimal _to;
			private decimal _step;
			private bool _isEnabled;

			[DisplayNameLoc(LocalizedStrings.Str3570Key)]
			[ReadOnly(true)]
			[PropertyOrder(1)]
			public string Name { get; set; }

			[Browsable(false)]
			public Type Type { get; set; }

			[DisplayNameLoc(LocalizedStrings.Str3148Key)]
			[PropertyOrder(2)]
			public bool IsEnabled
			{
				get { return _isEnabled; }
				set
				{
					_isEnabled = value;
					NotifyChanged("IsEnabled");
				}
			}

			[DisplayNameLoc(LocalizedStrings.Str343Key)]
			[PropertyOrder(3)]
			public decimal From
			{
				get { return _from; }
				set
				{
					_from = value;
					NotifyChanged("From");
				}
			}

			[DisplayNameLoc(LocalizedStrings.Str345Key)]
			[PropertyOrder(4)]
			public decimal To
			{
				get { return _to; }
				set
				{
					_to = value;
					NotifyChanged("To");
				}
			}

			[DisplayNameLoc(LocalizedStrings.Str812Key)]
			[PropertyOrder(5)]
			public decimal Step
			{
				get { return _step; }
				set
				{
					_step = value;
					NotifyChanged("Step");
				}
			}

			[Browsable(false)]
			public decimal Value { get; set; }

			public void Check()
			{
				if (!IsEnabled)
					return;

				if (Step == 0)
					throw new ArgumentException(LocalizedStrings.Str3571);

				if (To == From)
					throw new ArgumentException(LocalizedStrings.Str3572Params.Put(From, To));

				if (Step > 0 && To < From)
					throw new ArgumentException(LocalizedStrings.Str3573Params.Put(Step, From, To));

				if (Step < 0 && To > From)
					throw new ArgumentException(LocalizedStrings.Str3574Params.Put(Step, From, To));
			}

			public void Load(SettingsStorage storage)
			{
				Name = storage.GetValue<string>("Name");
				Type = storage.GetValue<Type>("Type");
				IsEnabled = storage.GetValue<bool>("IsEnabled");
				From = storage.GetValue<decimal>("From");
				To = storage.GetValue<decimal>("To");
				Step = storage.GetValue<decimal>("Step");
			}

			public void Save(SettingsStorage storage)
			{
				storage.SetValue("Name", Name);
				storage.SetValue("Type", Type.GetTypeName(false));
				storage.SetValue("IsEnabled", IsEnabled);
				storage.SetValue("From", From);
				storage.SetValue("To", To);
				storage.SetValue("Step", Step);
			}
		}

		public static readonly RoutedCommand OpenStrategyCommand = new RoutedCommand();
		public static readonly RoutedCommand SetParametersCommand = new RoutedCommand();

		private readonly ObservableCollection<ParameterInfo> _parameters = new ObservableCollection<ParameterInfo>();
		private readonly Dictionary<Strategy, StrategyContainer> _strategies = new Dictionary<Strategy, StrategyContainer>();
		private readonly List<StrategyContainer> _selectedStrategies = new List<StrategyContainer>();

		private StrategyContainer _strategy;
		private string _strategyName;

		public StrategyContainer Strategy
		{
			get { return _strategy; }
			set
			{
				if (_strategy == value)
					return;

				if (_strategy != null)
					_strategy.StrategyAssigned -= OnStrategyAssigned;

				_strategy = value;

				if (_strategy == null)
					return;

				_strategyName = _strategy.Name.Replace(LocalizedStrings.Str3177 + " ", LocalizedStrings.Str3576);

				EmulationService = new EmulationService(Strategy);
				EmulationService.ProgressChanged += EmulationServiceOnProgressChanged;

				_strategy.StrategyAssigned += OnStrategyAssigned;

				if (_strategy.Strategy != null)
					OnStrategyAssigned(_strategy.Strategy);
			}
		}

		public EmulationService EmulationService { get; private set; }

		private StrategyContainer SelectedStrategy
		{
			get
			{
				var strategy = ResultsPanel.SelectedStrategy;
				return strategy == null ? null : _strategies.TryGetValue(strategy);
			}
		}

		public IEnumerable<StrategyContainer> SelectedStrategies => _selectedStrategies;

		public OptimizatorContent()
		{
			InitializeComponent();

			ParametersGrid.ItemsSource = _parameters;

			var isInitialization = false;

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();

			cmdSvc.Register<StartStrategyCommand>(this, true,
				cmd =>
				{
					var error = Strategy.CheckCanStart(false);
					if (error != null)
					{
						new MessageBoxBuilder()
							.Owner(this)
							.Caption(LocalizedStrings.Str3577)
							.Text(error)
							.Warning()
							.Show();

						return;
					}

					try
					{
						_parameters.ForEach(p => p.Check());
					}
					catch (Exception excp)
					{
						new MessageBoxBuilder()
							.Owner(this)
							.Caption(LocalizedStrings.Str3577)
							.Text(excp.Message)
							.Warning()
							.Show();

						return;
					}

					isInitialization = true;

					_strategies.Clear();
					ResultsPanel.Clear();

					Task.Factory.StartNew(() =>
					{
						EmulationService.Strategies = CreateStrategies();
						EmulationService.StartEmulation();

						isInitialization = false;
					});
				},
				cmd => EmulationService != null && EmulationService.CanStart && !isInitialization);

			cmdSvc.Register<StopStrategyCommand>(this, true, 
				cmd => EmulationService.StopEmulation(),
				cmd => EmulationService != null && EmulationService.CanStop);

			ResultsPanel.AddContextMenuItem(new Separator());
			ResultsPanel.AddContextMenuItem(new MenuItem { Header = LocalizedStrings.Str3578, Command = OpenStrategyCommand, CommandTarget = this });
			ResultsPanel.AddContextMenuItem(new MenuItem { Header = LocalizedStrings.Str3579, Command = SetParametersCommand, CommandTarget = this });
		}

		#region IStudioControl

		void IPersistable.Load(SettingsStorage storage)
		{
			var layout = storage.GetValue<string>("Layout");
			if (layout != null)
				DockSite.LoadLayout(layout);

			var parameters = storage.GetValue<IEnumerable<SettingsStorage>>("Parameters");
			if (parameters != null)
			{
				_parameters.Clear();
				_parameters.AddRange(parameters.Select(p => p.Load<ParameterInfo>()));
			}

			if (Strategy != null && Strategy.Strategy != null)
				OnStrategyAssigned(Strategy.Strategy);
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.SetValue("Layout", DockSite.SaveLayout());
			storage.SetValue("Parameters", _parameters.Select(p => p.Save()).ToArray()); 
		}

		void IDisposable.Dispose()
		{
		}

		public string Title => LocalizedStrings.Str3580;

		public Uri Icon => null;

		#endregion

		private IEnumerable<Strategy> CreateStrategies()
		{
			EmulationService.EmulationConnector.AddInfoLog(LocalizedStrings.Str3581);

			var parameters = _parameters.Where(p => p.IsEnabled).ToArray();

			parameters.ForEach(pv => pv.Value = pv.From);

			do
			{
				var strategy = CreateStrategy(parameters);
				strategy.Name = _strategyName + LocalizedStrings.Str3582 + _strategies.Count;

				_strategies.Add(strategy.Strategy, strategy);
				ResultsPanel.AddStrategies(new[] { strategy.Strategy });

				yield return strategy;
			}
			while (GetNext(parameters, parameters.Length - 1));

			EmulationService.EmulationConnector.AddInfoLog(LocalizedStrings.Str3583);
		}

		private void OnStrategyAssigned(Strategy strategy)
		{
			var oldParameters = _parameters
				.CopyAndClear()
				.ToDictionary(p => p.Name);

			foreach (var strategyParam in strategy.Parameters)
			{
				var type = strategyParam.Value.GetType();

				if (!type.IsNumeric() || type.IsEnum() || ResultsPanel.ExcludeParameters.Contains(strategyParam.Name))
					continue;

				var parameter = oldParameters.TryGetValue(strategyParam.Name);

				if (parameter == null)
				{
					parameter = new ParameterInfo
					{
						Name = strategyParam.Name,
						Type = strategyParam.Value.GetType(),
						IsEnabled = true,
						From = GetOptimizeValue(strategyParam.OptimizeFrom, 1m),
						To = GetOptimizeValue(strategyParam.OptimizeTo, 10m),
						Step = GetOptimizeValue(strategyParam.OptimizeStep, 1m)
					};

					parameter.PropertyChanged += ParameterPropertyChanged;
				}

				_parameters.Add(parameter);
			}

			ParametersGrid.SelectedItem = _parameters.FirstOrDefault();

			new ControlChangedCommand(this).Process(this);
		}

		private static decimal GetOptimizeValue(object value, decimal defaultValue)
		{
			var val = value.To<decimal?>();

			if (val == null || val == 0)
				return defaultValue;

			return val.Value;
		}

		private void ParameterPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "IsEnabled")
			{
				var parameter = (ParameterInfo)sender;

				ResultsPanel.SetColumnVisibility(parameter.Name, parameter.IsEnabled ? Visibility.Visible : Visibility.Collapsed);
			}

			new ControlChangedCommand(this).Process(this);
		}

		private void EmulationServiceOnProgressChanged(StrategyContainer strategy, int value)
		{
			ResultsPanel.UpdateProgress(strategy.Strategy, value);
		}

		private static bool GetNext(IList<ParameterInfo> parameters, int n)
		{
			while (true)
			{
				if (n < 0)
					return false;

				var parameter = parameters[n];

				if (parameter.Value >= parameter.To)
				{
					if (n == 0)
						return false;

					parameter.Value = parameter.From;
					n--;
					continue;
				}

				parameter.Value += parameter.Step;
				return true;
			}
		}

		private int GetIterationCount()
		{
			return _parameters
				.Where(p => p.Step > 0)
				.Aggregate(1, (c, p) => c * (int)((p.To - p.From) / p.Step));
		}

		private StrategyContainer CreateStrategy(IEnumerable<ParameterInfo> parameters)
		{
			var s = (StrategyContainer)Strategy.Clone();

			foreach (var pv in parameters)
			{
				s.Strategy.Parameters.First(p => p.Name == pv.Name).Value = pv.Value.To(pv.Type);
			}

			return s;
		}

		private void OpenStrategy(StrategyContainer selectedStrategy)
		{
			var strategy = (StrategyContainer)selectedStrategy.Clone();

			strategy.Strategy.Id = selectedStrategy.Strategy.Id;
			strategy.Portfolio = Strategy.Portfolio;
			strategy.SessionType = SessionType.Battle;

			strategy.Reseted += () =>
			{
				var settings = EmulationService.EmulationSettings;
				new StartStrategyCommand(strategy, settings.StartTime, settings.StopTime, null, true).Process(this);
			};

			new OpenStrategyCommand(strategy, Properties.Resources.EmulationStrategyContent).Process(strategy.StrategyInfo);
		}

		private void ExecutedOpenStrategyCommand(object sender, ExecutedRoutedEventArgs e)
		{
			OpenStrategy(SelectedStrategy);
		}

		private void CanExecuteOpenStrategyCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedStrategy != null;
		}

		private void ExecutedSetParametersCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var strategy = (StrategyContainer)SelectedStrategy.Clone();

			strategy.StrategyInfo = Strategy.StrategyInfo;
			strategy.SessionType = SessionType.Battle;
			strategy.Portfolio = Strategy.Portfolio;

			new AddStrategyCommand(Strategy.StrategyInfo, strategy, SessionType.Battle).SyncProcess(this);
		}

		private void CanExecuteSetParametersCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedStrategy != null;
		}

		private void ResultsPanel_OnStrategyDoubleClick(Strategy strategy)
		{
			OpenStrategy(SelectedStrategy);
		}

		private void ResultsPanel_OnSelectionChanged()
		{
			var selection = ResultsPanel
				.SelectedStrategies
				.Select(s => _strategies.TryGetValue(s))
				.Where(s => s != null)
				.ToArray();

			var toRemove = _selectedStrategies.Except(selection).ToArray();
			var toAdd = selection.Except(_selectedStrategies).ToArray();

			toAdd.ForEach(s => _selectedStrategies.Add((s)));
			toRemove.ForEach(s => _selectedStrategies.Remove((s)));
		}
	}
}