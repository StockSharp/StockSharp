#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Quik.QuikPublic
File: QuikTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Quik
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Localization;
	using Ecng.ComponentModel;

	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Quik;
	using StockSharp.Quik.Xaml;
	using StockSharp.Localization;
	using StockSharp.Quik.Lua;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[TargetPlatform(Languages.Russian)]
	[Doc("http://stocksharp.com/doc/html/54a1e95e-ea39-4322-9613-e74859a3a596.htm")]
	[TaskCategory(TaskCategories.Russia | TaskCategories.RealTime |
		TaskCategories.Level1 | TaskCategories.MarketDepth | TaskCategories.Stock |
		TaskCategories.Transactions | TaskCategories.Free | TaskCategories.Ticks)]
	class QuikTask : ConnectorHydraTask<IMessageAdapter>
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
				public string PropertyName { get; }

				/// <summary>
				/// Значения свойства от которого зависит видимость 
				/// (через запятую, если несколько), при котором свойство, к
				/// которому применен атрибут, будет видимо. 
				/// </summary>
				public object ShowOn { get; }

				/// <summary>
				/// Конструктор  
				/// </summary>
				/// <param name="propName">Название свойства, от которого будет зависить видимость</param>
				/// <param name="showOn">Значения свойства, через запятую, если несколько, при котором свойство, к
				/// которому применен атрибут, будет видимо.</param>
				public DynamicPropertyFilterAttribute(string propName, object showOn)
				{
					if (propName.IsEmpty())
						throw new ArgumentNullException(nameof(propName));

					PropertyName = propName;
					ShowOn = showOn;
				}
			}

			private const string _ddeCategory = "DDE";
			private const string _luaCategory = "LUA";

			public QuikSettings(HydraTaskSettings settings)
				: base(settings)
			{
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

			[CategoryLoc(_ddeCategory)]
			[DisplayNameLoc(LocalizedStrings.OverrideKey)]
			[DescriptionLoc(LocalizedStrings.OverrideDllKey)]
			[PropertyOrder(5)]
			public bool OverrideDll
			{
				get { return (bool)ExtensionInfo["OverrideDll"]; }
				set { ExtensionInfo["OverrideDll"] = value; }
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

		private QuikSettings _settings;

		public override HydraTaskSettings Settings => _settings;

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new QuikSettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.Path = QuikTerminal.GetDefaultPath() ?? string.Empty;
			_settings.DdeServer = "hydra";
			_settings.IsDownloadSecurityChangesHistory = false;
			_settings.IsDde = false;
			_settings.ExtendedColumns = new List<string>();
			_settings.ExtendedColumnsHistory = new List<string>();
			_settings.OverrideDll = true;
			_settings.LuaAddress = QuikTrader.DefaultLuaAddress;
			_settings.LuaLogin = "quik";
			_settings.LuaPassword = new SecureString();
		}

		protected override IMessageAdapter GetAdapter(IdGenerator generator)
		{
			if (_settings.IsDde)
			{
				var adapter = new QuikDdeAdapter(generator)
				{
					//Path = _settings.Path,
					DdeServer = _settings.DdeServer,
					//OverrideDll = _settings.OverrideDll
				};

				adapter.Tables = new[] { adapter.SecuritiesTable, adapter.TradesTable, adapter.OrdersTable, adapter.StopOrdersTable, adapter.MyTradesTable };

				if (_settings.IsDownloadSecurityChangesHistory)
					adapter.Tables = adapter.Tables.Concat(new[] { adapter.SecuritiesChangeTable });

				//Добавление выбранных колонок в экспорт
				if (!_settings.IsDownloadSecurityChangesHistory)
				{
					adapter
						.SecuritiesTable
						.Columns
						.AddRange(DdeSecurityColumnsEditor.GetColumns(_settings.ExtendedColumns));
				}
				else
				{
					adapter
						.SecuritiesChangeTable
						.Columns
						.AddRange(DdeSecurityChangesColumnsEditor.GetColumns(_settings.ExtendedColumnsHistory));
				}

				return adapter;
			}
			else
			{
				return new LuaFixMarketDataMessageAdapter(generator)
				{
					Address = _settings.LuaAddress,
					Login = _settings.LuaLogin,
					Password = _settings.LuaPassword
				};
			}
		}
	}
}