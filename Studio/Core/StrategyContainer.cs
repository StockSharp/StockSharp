#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.CorePublic
File: StrategyContainer.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Core
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using Ecng.Collections;
    using Ecng.Common;
    using Ecng.ComponentModel;
    using Ecng.Configuration;
    using Ecng.Serialization;

    using MoreLinq;

    using StockSharp.Algo;
    using StockSharp.Algo.Strategies;
    using StockSharp.BusinessEntities;
    using StockSharp.Logging;
    using StockSharp.Messages;
    using StockSharp.Xaml.PropertyGrid;

    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.Str3169Key)]
	[DescriptionLoc(LocalizedStrings.Str3170Key)]
	public class StrategyContainer : Strategy, IStrategyContainer, INotifyPropertiesChanged
	{
		private const int _defaultHistoryDaysCount = 30;

		private readonly HashSet<string> _additionalProperties = new HashSet<string>
		{
			"HistoryDaysCount"
		};
		private readonly HashSet<string> _skippedParameters = new HashSet<string>
		{
			"Id", "Name"
		};

		private SettingsStorage _storage;
		private bool _needRestart;

		public StrategyInfo StrategyInfo { get; set; }

		public override string Name
		{
			get { return Strategy != null ? Strategy.Name : base.Name; }
			set
			{
				base.Name = value;

				if (Strategy != null)
					Strategy.Name = value;
			}
		}

		private Strategy _strategy;
		private MarketDataSettings _marketDataSettings;
		private SessionType _sessionType;

		[Browsable(false)]
		public Strategy Strategy
		{
			get { return _strategy; }
			set
			{
				if (_strategy != null)
				{
					ChildStrategies.Remove(_strategy);
					_strategy.Log -= OnLog;
					_strategy.PropertyChanged -= OnPropertyChanged;
					_strategy.ProcessStateChanged -= OnProcessStateChanged;

					//new ChartClearAreasCommand().Process(Strategy);

					StrategyRemoved.SafeInvoke(_strategy);
				}

				_strategy = value;

				if (_strategy == null)
					return;

				if (_storage != null)
					_strategy.Id = _storage.GetValue<Guid>("StrategyContainerId");

				var storage = new SettingsStorage
				{
					{ "Settings", _storage },
					{ "Statistics", GetStatistics() },
					{ "Orders", GetActiveOrders() },
					{ "Positions", PositionManager.Positions.ToArray() },
				};

				_strategy.NameGenerator.AutoGenerateStrategyName = false;
				_strategy.Connector = Connector;
				_strategy.SafeLoadState(storage);
				
				ChildStrategies.Add(_strategy);
				_strategy.Parent = null;

				_strategy.Log += OnLog;
				_strategy.PropertyChanged += OnPropertyChanged;
				_strategy.ProcessStateChanged += OnProcessStateChanged;
				_strategy.ParametersChanged += OnParametersChanged;

				_strategy.UnrealizedPnLInterval = UnrealizedPnLInterval;

				StrategyAssigned.SafeInvoke(_strategy);

				RaiseParametersChanged("Name");
				RaisePropertiesChanged();
			}
		}

		public event Action<Strategy> StrategyRemoved;
		public event Action<Strategy> StrategyAssigned;

		public override Security Security
		{
			get { return base.Security; }
			set
			{
				base.Security = value;

				if (Strategy != null)
					Strategy.Security = value;
			}
		}

		public override Portfolio Portfolio
		{
			get { return base.Portfolio; }
			set
			{
				base.Portfolio = value;

				if (Strategy != null)
					Strategy.Portfolio = value;
			}
		}

		public override IConnector Connector
		{
			get { return base.Connector; }
			set
			{
				base.Connector = value;

				if (Strategy != null)
					Strategy.Connector = value;
			}
		}

		public override ProcessStates ProcessState
		{
			get
			{
				return Strategy == null ? ProcessStates.Stopped : Strategy.ProcessState;
			}
		}

		public override TimeSpan UnrealizedPnLInterval
		{
			get { return base.UnrealizedPnLInterval; }
			set
			{
				base.UnrealizedPnLInterval = value;

				if (Strategy != null)
					Strategy.UnrealizedPnLInterval = value;
			}
		}

		public bool NeedRestart
		{
			get { return _needRestart; }
			set
			{
				_needRestart = value;
				this.Notify("NeedRestart");
			}
		}

		public SessionType SessionType
		{
			get { return _sessionType; }
			set
			{
				_sessionType = value;

				if (_sessionType == SessionType.Emulation)
					NameGenerator.Pattern += LocalizedStrings.Str3171;
				else
					NameGenerator.Pattern.TrimEnd(LocalizedStrings.Str3171);
			}
		}

		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str3172Key)]
		[DescriptionLoc(LocalizedStrings.Str3173Key)]
		[PropertyOrder(0)]
		public int HistoryDaysCount { get; set; }

		public MarketDataSettings MarketDataSettings
		{
			get { return _marketDataSettings; }
			set
			{
				_marketDataSettings = value;
				RaiseParametersChanged("MarketDataSettings");
			}
		}

		public StrategyContainer()
		{
			HistoryDaysCount = _defaultHistoryDaysCount;
		}

		private IDictionary<Order, IEnumerable<MyTrade>> GetActiveOrders()
		{
			return Orders
				.Filter(OrderStates.Active)
				.ToDictionary(o => o, o => MyTrades.Where(t => t.Order == o));
		}

		private SettingsStorage GetStatistics()
		{
			var statistics = new SettingsStorage();

			foreach (var parameter in StatisticManager.Parameters)
				statistics.SetValue(parameter.Name, parameter.Save());

			return statistics;
		}

		private void OnLog(LogMessage message)
		{
			if(message.Source == Strategy)
				RaiseLog(message);
		}

		protected override void RaiseLog(LogMessage message)
		{
			if (message.Source == this && Strategy != null)
				return;

			base.RaiseLog(message);
		}

		private void OnProcessStateChanged(Strategy strategy)
		{
			RaiseProcessStateChanged(strategy);

			if (strategy == Strategy)
				this.Notify("ProcessState");
		}

		private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "Security":
					Security = Strategy.Security;
					break;

				case "Portfolio":
					Portfolio = Strategy.Portfolio;
					break;

				case "Name":
					RaiseParametersChanged("Name");
					break;
			}
		}

		private void OnParametersChanged()
		{
			RaiseParametersChanged("Strategy");
		}

		public override void Load(SettingsStorage storage)
		{
			_storage = storage;

			var strategyInfoId = storage.GetValue<long?>("StrategyInfoId");

			if (Strategy == null && strategyInfoId != null)
			{
				StrategyInfo = ConfigManager
					.GetService<IStudioEntityRegistry>()
					.Strategies.ReadById(strategyInfoId);

				this.InitStrategy();
			}

			if (Strategy != null)
			{
				Strategy.Id = storage.GetValue<Guid>("StrategyContainerId");
				Strategy.Load(storage);
			}

			var marketDataSettings = storage.GetValue<string>("MarketDataSettings");
			if (marketDataSettings != null)
			{
				var id = marketDataSettings.To<Guid>();
				var settings = ConfigManager.GetService<MarketDataSettingsCache>().Settings.FirstOrDefault(s => s.Id == id);

				if (settings != null)
					MarketDataSettings = settings;
			}

			HistoryDaysCount = storage.GetValue("HistoryDaysCount", _defaultHistoryDaysCount);
		}

		public override void Save(SettingsStorage storage)
		{
			if (Strategy != null)
			{
				storage.SetValue("StrategyContainerId", Strategy.Id);
				storage.SetValue("StrategyInfoId", StrategyInfo.Id);

				if (Strategy.Security != null)
					storage.SetValue("security", _strategy.Security.Id);

				if (Strategy.Portfolio != null)
					storage.SetValue("portfolio", _strategy.Portfolio.Name);

				Strategy.Save(storage);
			}
			else if (_storage != null)
			{
				_storage.ForEach(pair => storage.SetValue(pair.Key, pair.Value));
			}

			if (MarketDataSettings != null)
				storage.SetValue("MarketDataSettings", MarketDataSettings.Id.To<string>());

			storage.SetValue("HistoryDaysCount", HistoryDaysCount);
		}

		public override void Start()
		{
			if (Strategy == null)
			{
				this.AddErrorLog(LocalizedStrings.Str3174);
				return;
			}

			Strategy.Environment.Clear();
			Strategy.Environment.AddRange(Environment);

			var parameters = Parameters.ToDictionary(p => p.Name);
			foreach (var strategyParam in Strategy.Parameters)
			{
				if (_skippedParameters.Contains(strategyParam.Name))
					continue;

				var tmp = parameters.TryGetValue(strategyParam.Name);

				if (tmp != null)
					strategyParam.Value = tmp.Value;
			}

			Strategy.Start();
		}

		public override void Stop()
		{
			if (Strategy == null)
			{
				this.AddErrorLog(LocalizedStrings.Str3174);
				return;
			}

			Strategy.Stop();

			if (!NeedRestart)
				return;

			this.InitStrategy();
			NeedRestart = false;
		}

		public override Strategy Clone()
		{
			var strategy = (StrategyContainer)base.Clone();

			strategy.StrategyInfo = StrategyInfo;
			strategy.MarketDataSettings = MarketDataSettings;
			strategy.SessionType = SessionType;

			if (Strategy != null)
			{
				strategy.Strategy = Strategy.Clone();
				strategy.Strategy.Id = Guid.NewGuid();
			}

			return strategy;
		}

		public override void AttachOrder(Order order, IEnumerable<MyTrade> myTrades)
		{
			Strategy.AttachOrder(order, myTrades);
		}

		protected override void AssignOrderStrategyId(Order order)
		{
			order.UserOrderId = this.GetStrategyId().To<string>();
		}

		#region Implementation of ICustomTypeDescriptor

		public String GetClassName()
		{
			return TypeDescriptor.GetClassName(Strategy, true);
		}

		public AttributeCollection GetAttributes()
		{
			return TypeDescriptor.GetAttributes(Strategy, true);
		}

		public String GetComponentName()
		{
			return TypeDescriptor.GetComponentName(Strategy, true);
		}

		public TypeConverter GetConverter()
		{
			return TypeDescriptor.GetConverter(Strategy ?? this, true);
		}

		public EventDescriptor GetDefaultEvent()
		{
			return TypeDescriptor.GetDefaultEvent(Strategy, true);
		}

		public PropertyDescriptor GetDefaultProperty()
		{
			return TypeDescriptor.GetDefaultProperty(Strategy, true);
		}

		public object GetEditor(Type editorBaseType)
		{
			return TypeDescriptor.GetEditor(Strategy, editorBaseType, true);
		}

		public EventDescriptorCollection GetEvents(Attribute[] attributes)
		{
			return TypeDescriptor.GetEvents(Strategy, attributes, true);
		}

		public PropertyDescriptorCollection GetProperties()
		{
			var props = TypeDescriptor
				.GetProperties(Strategy)
				.OfType<PropertyDescriptor>()
				.Where(NeedShowProperty);

			var additionalProps = TypeDescriptor
				.GetProperties(this, true)
				.OfType<PropertyDescriptor>()
				.Where(pd => _additionalProperties.Contains(pd.Name));

			return new PropertyDescriptorCollection(props.Concat(additionalProps).ToArray());
		}

		public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
		{
			return TypeDescriptor.GetProperties(Strategy, attributes);
		}

		public EventDescriptorCollection GetEvents()
		{
			return TypeDescriptor.GetEvents(Strategy, true);
		}

		public object GetPropertyOwner(PropertyDescriptor pd)
		{
			return _additionalProperties.Contains(pd.Name) ? this : Strategy;
		}

		private bool NeedShowProperty(PropertyDescriptor pd)
		{
			if (pd.Category == LocalizedStrings.Str436 || pd.Category == LocalizedStrings.Str1559 || pd.Category == LocalizedStrings.Str3050)
				return false;

			return true;
		}

		#endregion

		#region INotifyPropertiesChanged

		public event Action PropertiesChanged;

		protected virtual void RaisePropertiesChanged()
		{
			PropertiesChanged.SafeInvoke();
		}

		#endregion
	}
}