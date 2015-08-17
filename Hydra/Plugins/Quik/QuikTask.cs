namespace StockSharp.Hydra.Quik
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Localization;

	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Quik;
	using StockSharp.Quik.Xaml;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[TargetPlatform(Languages.Russian)]
	[TaskDoc("http://stocksharp.com/doc/html/54a1e95e-ea39-4322-9613-e74859a3a596.htm")]
	[TaskIcon("quik_logo.png")]
	[TaskCategory(TaskCategories.Russia | TaskCategories.RealTime |
		TaskCategories.Level1 | TaskCategories.MarketDepth | TaskCategories.Stock |
		TaskCategories.Transactions | TaskCategories.Free | TaskCategories.Ticks)]
	class QuikTask : ConnectorHydraTask<QuikTrader>
	{
		private const string _sourceName = "Quik";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		[CategoryOrder(_luaCategory, 1)]
		[CategoryOrder(_ddeCategory, 2)]
		private sealed class QuikSettings : ConnectorHydraTaskSettings, ICustomTypeDescriptor
		{
			/// <summary>
			/// Атрибут для поддержки динамически показываемых свойств
			/// </summary>
			[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
			private sealed class DynamicPropertyFilterAttribute : Attribute
			{
				/// <summary>
				/// Название свойства, от которого будет зависить видимость  
				/// </summary>
				public string PropertyName { get; private set; }

				/// <summary>
				/// Значения свойства от которого зависит видимость 
				/// (через запятую, если несколько), при котором свойство, к
				/// которому применен атрибут, будет видимо. 
				/// </summary>
				public object ShowOn { get; private set; }

				/// <summary>
				/// Конструктор  
				/// </summary>
				/// <param name="propName">Название свойства, от которого будет зависить видимость</param>
				/// <param name="showOn">Значения свойства, через запятую, если несколько, при котором свойство, к
				/// которому применен атрибут, будет видимо.</param>
				public DynamicPropertyFilterAttribute(string propName, object showOn)
				{
					if (propName.IsEmpty())
						throw new ArgumentNullException("propName");

					PropertyName = propName;
					ShowOn = showOn;
				}
			}

			private const string _ddeCategory = "DDE";
			private const string _luaCategory = "LUA";

			public QuikSettings(HydraTaskSettings settings)
				: base(settings)
			{
				ExtensionInfo.TryAdd("LuaAddress", QuikTrader.DefaultLuaAddress.To<string>());
				ExtensionInfo.TryAdd("LuaLogin", string.Empty);
				ExtensionInfo.TryAdd("LuaPassword", new SecureString());
			}

			[CategoryLoc(_sourceName)]
			[DisplayName("DDE")]
			[DescriptionLoc(LocalizedStrings.Str2803Key)]
			[PropertyOrder(0)]
			[Auxiliary]
			public bool IsDde
			{
				get { return (bool)ExtensionInfo["IsDde"]; }
				set { ExtensionInfo["IsDde"] = value; }
			}

			[Category(_ddeCategory)]
			[DisplayNameLoc(LocalizedStrings.Str2804Key)]
			[DescriptionLoc(LocalizedStrings.Str2805Key)]
			[PropertyOrder(0)]
			[DynamicPropertyFilter("IsDde", true)]
			public string Path
			{
				get { return (string)ExtensionInfo["Path"]; }
				set { ExtensionInfo["Path"] = value; }
			}

			[Category(_ddeCategory)]
			[DisplayNameLoc(LocalizedStrings.Str1779Key)]
			[DescriptionLoc(LocalizedStrings.Str1780Key)]
			[PropertyOrder(1)]
			[DynamicPropertyFilter("IsDde", true)]
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
			//[DynamicPropertyFilter("IsDownloadSecurityChangesHistory", false)]
			[DynamicPropertyFilter("IsDde", true)]
			public List<string> ExtendedColumns
			{
				get { return (List<string>)ExtensionInfo["ExtendedColumns"]; }
				set { ExtensionInfo["ExtendedColumns"] = value; }
			}

			[Category(_ddeCategory)]
			[DisplayNameLoc(LocalizedStrings.Str2808Key)]
			[DescriptionLoc(LocalizedStrings.Str2809Key)]
			[PropertyOrder(3)]
			[Editor(typeof(DdeSecurityChangesColumnsEditor), typeof(DdeSecurityChangesColumnsEditor))]
			//[DynamicPropertyFilter("IsDownloadSecurityChangesHistory", true)]
			[DynamicPropertyFilter("IsDde", true)]
			public List<string> ExtendedColumnsHistory
			{
				get { return (List<string>)ExtensionInfo["ExtendedColumnsHistory"]; }
				set { ExtensionInfo["ExtendedColumnsHistory"] = value; }
			}

			[Category(_ddeCategory)]
			[DisplayNameLoc(LocalizedStrings.Str2810Key)]
			[DescriptionLoc(LocalizedStrings.Str1786Key)]
			[PropertyOrder(4)]
			[DynamicPropertyFilter("IsDde", true)]
			public bool IsDownloadSecurityChangesHistory
			{
				get { return (bool)ExtensionInfo["IsDownloadSecurityChangesHistory"]; }
				set { ExtensionInfo["IsDownloadSecurityChangesHistory"] = value; }
			}

			[CategoryLoc(_luaCategory)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.AddressKey, true)]
			[PropertyOrder(0)]
			[DynamicPropertyFilter("IsDde", false)]
			public EndPoint LuaAddress
			{
				get { return ExtensionInfo["LuaAddress"].To<EndPoint>(); }
				set { ExtensionInfo["LuaAddress"] = value.To<string>(); }
			}

			[CategoryLoc(_luaCategory)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.LoginKey, true)]
			[PropertyOrder(1)]
			[DynamicPropertyFilter("IsDde", false)]
			public string LuaLogin
			{
				get { return (string)ExtensionInfo["LuaLogin"]; }
				set { ExtensionInfo["LuaLogin"] = value; }
			}

			[CategoryLoc(_luaCategory)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
			[PropertyOrder(2)]
			[DynamicPropertyFilter("IsDde", false)]
			public SecureString LuaPassword
			{
				get { return ExtensionInfo["LuaPassword"].To<SecureString>(); }
				set { ExtensionInfo["LuaPassword"] = value; }
			}

			//--------------------

			private PropertyDescriptorCollection GetFilteredProperties(Attribute[] attributes)
			{
				var pdc = TypeDescriptor.GetProperties(this, attributes, true);
				var finalProps = new PropertyDescriptorCollection(new PropertyDescriptor[0]);

				foreach (PropertyDescriptor pd in pdc)
				{
					var show = true;

					foreach (var attr in pd.Attributes.OfType<DynamicPropertyFilterAttribute>())
					{
						var value = pdc[attr.PropertyName].GetValue(this);

						if (!Equals(attr.ShowOn, value))
						{
							show = false;
						}
					}

					if (show)
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
				return GetFilteredProperties(ArrayHelper.Empty<Attribute>());
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

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override MarketDataConnector<QuikTrader> CreateConnector(HydraTaskSettings settings)
		{
			_settings = new QuikSettings(settings);

			if (settings.IsDefault)
			{
				_settings.Path = QuikTerminal.GetDefaultPath() ?? string.Empty;
				_settings.DdeServer = "hydra";
				_settings.IsDownloadSecurityChangesHistory = false;
				_settings.IsDde = false;
				_settings.ExtendedColumns = new List<string>();
				_settings.ExtendedColumnsHistory = new List<string>();
				_settings.LuaAddress = QuikTrader.DefaultLuaAddress;
				_settings.LuaLogin = "quik";
				_settings.LuaPassword = new SecureString();
			}

			return new QuikMarketDataConnector(EntityRegistry.Securities, this, CreateHydraQuikTrader, _settings);
		}

		private QuikTrader CreateHydraQuikTrader()
		{
			var connector = new QuikTrader
			{
				IsDde = _settings.IsDde,
				Path = _settings.Path,
				DdeServer = _settings.DdeServer,
				LuaFixServerAddress = _settings.LuaAddress,
			};

			if (!_settings.LuaLogin.IsEmpty())
				connector.LuaLogin = _settings.LuaLogin;

			if (!_settings.LuaPassword.IsEmpty())
				connector.LuaPassword = _settings.LuaPassword;

			connector.DdeTables = new[] { connector.SecuritiesTable, connector.TradesTable, connector.OrdersTable, connector.StopOrdersTable, connector.MyTradesTable };

			if (_settings.IsDownloadSecurityChangesHistory)
				connector.DdeTables = connector.DdeTables.Concat(new[] { connector.SecuritiesChangeTable });

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

		///// <summary>
		///// Выполнить задачу.
		///// </summary>
		///// <returns>Минимальный интервал, после окончания которого необходимо снова выполнить задачу.</returns>
		//protected override TimeSpan OnProcess()
		//{
		//	// если фильтр по инструментам выключен (выбран инструмент все инструменты)
		//	if (Connector.Connector.IsDde || this.GetAllSecurity() == null)
		//		return base.OnProcess();

		//	this.AddWarningLog(LocalizedStrings.Str2812);
		//	return TimeSpan.MaxValue;
		//}
	}
}