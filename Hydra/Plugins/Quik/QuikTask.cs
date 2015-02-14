namespace StockSharp.Hydra.Quik
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Localization;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Quik;
	using StockSharp.Quik.Xaml;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[Category(TaskCategories.Russian)]
	[TaskDisplayName(_sourceName)]
	[TargetPlatform(Languages.Russian)]
	class QuikTask : ConnectorHydraTask<QuikTrader>
	{
		private const string _sourceName = "Quik";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		[CategoryOrder(_ddeCategory, 1)]
		private sealed class QuikSettings : ConnectorHydraTaskSettings, ICustomTypeDescriptor
		{
			/// <summary>
			/// Атрибут для поддержки динамически показываемых свойств
			/// </summary>
			[AttributeUsage(AttributeTargets.Property, Inherited = true)]
			private sealed class DynamicPropertyFilterAttribute : Attribute
			{
				private readonly string _propertyName;

				/// <summary>
				/// Название свойства, от которого будет зависить видимость  
				/// </summary>
				public string PropertyName
				{
					get { return _propertyName; }
				}

				private readonly string _showOn;

				/// <summary>
				/// Значения свойства от которого зависит видимость 
				/// (через запятую, если несколько), при котором свойство, к
				/// которому применен атрибут, будет видимо. 
				/// </summary>
				public string ShowOn
				{
					get { return _showOn; }
				}

				/// <summary>
				/// Конструктор  
				/// </summary>
				/// <param name="propName">Название свойства, от которого будет зависить видимость</param>
				/// <param name="value">Значения свойства, через запятую, если несколько, при котором свойство, к
				/// которому применен атрибут, будет видимо.</param>
				public DynamicPropertyFilterAttribute(string propName, string value)
				{
					_propertyName = propName;
					_showOn = value;
				}
			}

			private const string _ddeCategory = "DDE";

			public QuikSettings(HydraTaskSettings settings)
				: base(settings)
			{
				if (!ExtensionInfo.ContainsKey("ExtendedColumns"))
					ExtendedColumns = new List<string>();

				if (!ExtensionInfo.ContainsKey("ExtendedColumnsHistory"))
					ExtendedColumnsHistory = new List<string>();

				if (!ExtensionInfo.ContainsKey("IsDownloadSecurityChangesHistory"))
					IsDownloadSecurityChangesHistory = false;

				if (!ExtensionInfo.ContainsKey("IsDde"))
					IsDde = false;
			}

			[TaskCategory(_sourceName)]
			[DisplayName("DDE")]
			[DescriptionLoc(LocalizedStrings.Str2803Key)]
			[PropertyOrder(0)]
			public bool IsDde
			{
				get { return (bool)ExtensionInfo["IsDde"]; }
				set { ExtensionInfo["IsDde"] = value; }
			}

			[Category(_ddeCategory)]
			[DisplayNameLoc(LocalizedStrings.Str2804Key)]
			[DescriptionLoc(LocalizedStrings.Str2805Key)]
			[PropertyOrder(0)]
			public string Path
			{
				get { return (string)ExtensionInfo["Path"]; }
				set { ExtensionInfo["Path"] = value; }
			}

			[Category(_ddeCategory)]
			[DisplayNameLoc(LocalizedStrings.Str1779Key)]
			[DescriptionLoc(LocalizedStrings.Str1780Key)]
			[PropertyOrder(1)]
			public string DdeServer
			{
				get { return (string)ExtensionInfo["DdeServer"]; }
				set { ExtensionInfo["DdeServer"] = value; }
			}

			[Category(_ddeCategory)]
			[DisplayNameLoc(LocalizedStrings.Str2806Key)]
			[DescriptionLoc(LocalizedStrings.Str2807Key)]
			[PropertyOrder(2)]
			[Editor(typeof(DdeSecurityColumnsEditor), typeof(DdeSecurityColumnsEditor))]
			[DynamicPropertyFilter("IsDownloadSecurityChangesHistory", "False")]
			public List<string> ExtendedColumns
			{
				get { return (List<string>)ExtensionInfo["ExtendedColumns"]; }
				private set { ExtensionInfo["ExtendedColumns"] = value; }
			}

			[Category(_ddeCategory)]
			[DisplayNameLoc(LocalizedStrings.Str2808Key)]
			[DescriptionLoc(LocalizedStrings.Str2809Key)]
			[PropertyOrder(3)]
			[Editor(typeof(DdeSecurityChangesColumnsEditor), typeof(DdeSecurityChangesColumnsEditor))]
			[DynamicPropertyFilter("IsDownloadSecurityChangesHistory", "True")]
			public List<string> ExtendedColumnsHistory
			{
				get { return (List<string>)ExtensionInfo["ExtendedColumnsHistory"]; }
				private set { ExtensionInfo["ExtendedColumnsHistory"] = value; }
			}

			[Category(_ddeCategory)]
			[DisplayNameLoc(LocalizedStrings.Str2810Key)]
			[DescriptionLoc(LocalizedStrings.Str1786Key)]
			[PropertyOrder(4)]
			[Auxiliary]
			public bool IsDownloadSecurityChangesHistory
			{
				get { return (bool)ExtensionInfo["IsDownloadSecurityChangesHistory"]; }
				set { ExtensionInfo["IsDownloadSecurityChangesHistory"] = value; }
			}

			//--------------------

			private PropertyDescriptorCollection GetFilteredProperties(Attribute[] attributes)
			{
				var pdc = TypeDescriptor.GetProperties(this, attributes, true);
				var finalProps = new PropertyDescriptorCollection(new PropertyDescriptor[0]);

				foreach (PropertyDescriptor pd in pdc)
				{
					var include = false;
					var dynamic = false;

					foreach (Attribute a in pd.Attributes)
					{
						var dpf = a as DynamicPropertyFilterAttribute;
						if (dpf == null)
							continue;

						dynamic = true;

						var temp = pdc[dpf.PropertyName].GetValue(this);

						if (dpf.ShowOn.ContainsIgnoreCase(temp.ToString()))
						{
							include = true;
						}
					}

					if (!dynamic || include)
						finalProps.Add(pd);
				}

				return finalProps;
			}

			#region ICustomTypeDescriptor
			public TypeConverter GetConverter()
			{
				return TypeDescriptor.GetConverter(this, true);
			}

			public EventDescriptorCollection GetEvents(Attribute[] attributes)
			{
				return TypeDescriptor.GetEvents(this, attributes, true);
			}

			EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
			{
				return TypeDescriptor.GetEvents(this, true);
			}

			public string GetComponentName()
			{
				return TypeDescriptor.GetComponentName(this, true);
			}

			public object GetPropertyOwner(PropertyDescriptor pd)
			{
				return this;
			}

			public AttributeCollection GetAttributes()
			{
				return TypeDescriptor.GetAttributes(this, true);
			}

			public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
			{
				return GetFilteredProperties(attributes);
			}

			PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
			{
				return GetFilteredProperties(ArrayHelper<Attribute>.EmptyArray);
			}

			public object GetEditor(Type editorBaseType)
			{
				return TypeDescriptor.GetEditor(this, editorBaseType, true);
			}

			public PropertyDescriptor GetDefaultProperty()
			{
				return TypeDescriptor.GetDefaultProperty(this, true);
			}

			public EventDescriptor GetDefaultEvent()
			{
				return TypeDescriptor.GetDefaultEvent(this, true);
			}

			public string GetClassName()
			{
				return TypeDescriptor.GetClassName(this, true);
			}
			#endregion
		}

		private sealed class QuikMarketDataConnector : MarketDataConnector<QuikTrader>
		{
			private readonly QuikSettings _settings;

			public QuikMarketDataConnector(ISecurityProvider securityProvider, QuikTask task, Func<QuikTrader> createConnector, QuikSettings settings)
				: base(securityProvider, task, createConnector)
			{
				if (settings == null)
					throw new ArgumentNullException("settings");

				_settings = settings;
			}

			protected override void InitializeConnector()
			{
				base.InitializeConnector();
				Connector.NewSecurityChanges += OnNewSecurityChanges;
			}

			protected override void UnInitializeConnector()
			{
				Connector.NewSecurityChanges -= OnNewSecurityChanges;
				base.UnInitializeConnector();
			}

			private void OnNewSecurityChanges(Security security, Level1ChangeMessage message)
			{
				base.AddLevel1Change(security, message); 
			}

			protected override void AddLevel1Change(Security security, Level1ChangeMessage message)
			{
				if (_settings.IsDownloadSecurityChangesHistory)
					return;

				base.AddLevel1Change(security, message);
			}
		}

		private QuikSettings _settings;

		public override Uri Icon
		{
			get { return "quik_logo.png".GetResourceUrl(GetType()); }
		}

		public override string Description
		{
			get { return LocalizedStrings.Str2281Params.Put(_sourceName); }
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override MarketDataConnector<QuikTrader> CreateTrader(HydraTaskSettings settings)
		{
			_settings = new QuikSettings(settings);

			if (settings.IsDefault)
			{
				_settings.Path = QuikTerminal.GetDefaultPath() ?? string.Empty;
				_settings.DdeServer = "hydra";
				_settings.IsDownloadSecurityChangesHistory = false;
				_settings.IsDde = false;
			}

			return new QuikMarketDataConnector(EntityRegistry.Securities, this, CreateHydraQuikTrader, _settings);
		}

		private sealed class HydraQuikTransactionAdapter : MessageAdapter<MessageSessionHolder>
		{
			public HydraQuikTransactionAdapter(MessageSessionHolder sessionHolder)
				: base(MessageAdapterTypes.Transaction, sessionHolder)
			{
			}

			protected override void OnSendInMessage(Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.Connect:
						SendOutMessage(new ConnectMessage());
						break;

					case MessageTypes.Disconnect:
						SendOutMessage(new DisconnectMessage());
						break;

					case MessageTypes.Time: // обработка heartbeat
						break;

					default:
						throw new NotSupportedException(LocalizedStrings.Str2811Params.Put(message.Type));
				}
			}
		}

		private HydraQuikTrader CreateHydraQuikTrader()
		{
			var connector = new HydraQuikTrader
			{
				IsDde = _settings.IsDde,
				Path = _settings.Path,
				DdeServer = _settings.DdeServer,
				IsDownloadSecurityChangesHistory = _settings.IsDownloadSecurityChangesHistory,
			};

			if (_settings.IsDde)
				connector.TransactionAdapter = new HydraQuikTransactionAdapter((MessageSessionHolder)connector.TransactionAdapter.SessionHolder);

			//Добавление выбранных колонок в экспорт
			if (!_settings.IsDownloadSecurityChangesHistory)
			{
				connector
					.SecuritiesTable
					.Columns
					.AddRange(DdeSecurityColumnsEditor.GetColumns(_settings.ExtendedColumns));
			}
			else
			{
				connector
					.SecuritiesChangeTable
					.Columns
					.AddRange(DdeSecurityChangesColumnsEditor.GetColumns(_settings.ExtendedColumnsHistory));				
			}

			return connector;
		}

		/// <summary>
		/// Выполнить задачу.
		/// </summary>
		/// <returns>Минимальный интервал, после окончания которого необходимо снова выполнить задачу.</returns>
		protected override TimeSpan OnProcess()
		{
			// если фильтр по инструментам выключен (выбран инструмент все инструменты)
			if (Connector.Connector.IsDde || this.GetAllSecurity() == null)
				return base.OnProcess();

			this.AddWarningLog(LocalizedStrings.Str2812);
			return TimeSpan.MaxValue;
		}
	}
}