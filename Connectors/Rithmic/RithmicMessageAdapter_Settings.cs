#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Rithmic.Rithmic
File: RithmicMessageAdapter_Settings.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Rithmic
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Security;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Server types.
	/// </summary>
	public enum RithmicServers
	{
		/// <summary>
		/// Test.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str3461Key)]
		Sim,

		///// <summary>
		///// Тестовый (аггр).
		///// </summary>
		//[EnumDisplayName("Тестовый (аггр)")]
		//SimAggr,

		/// <summary>
		/// On live data.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str3462Key)]
		Paper,

		/// <summary>
		/// Real.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str3463Key)]
		Real,
	}

	[Icon("Rithmic_logo.png")]
	[Doc("http://stocksharp.com/doc/html/777d9208-1146-4d54-b5ae-0315b4186522.htm")]
	[DisplayName("Rithmic")]
	[CategoryLoc(LocalizedStrings.AmericaKey)]
	[DescriptionLoc(LocalizedStrings.Str1770Key, "Rithmic")]
	[CategoryOrderLoc(LocalizedStrings.GeneralKey, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str174Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 2)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 3)]
	partial class RithmicMessageAdapter : INotifyPropertyChanged
	{
		/// <summary>
		/// Login.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.LoginKey)]
		[DescriptionLoc(LocalizedStrings.LoginKey, true)]
		[PropertyOrder(0)]
		public string UserName { get; set; }

		/// <summary>
		/// Password.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.PasswordKey)]
		[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
		[PropertyOrder(1)]
		public SecureString Password { get; set; }

		/// <summary>
		/// Path to certificate file, necessary yo connect to Rithmic system.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str3465Key)]
		[DescriptionLoc(LocalizedStrings.Str3466Key)]
		[PropertyOrder(2)]
		[Editor(typeof(FileBrowserEditor), typeof(FileBrowserEditor))]
		public string CertFile { get; set; }

		/// <summary>
		/// Additional login. Used when transaction sending is carried out to a separate server.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str3467Key)]
		[DescriptionLoc(LocalizedStrings.Str3468Key)]
		[PropertyOrder(3)]
		public string TransactionalUserName { get; set; }

		/// <summary>
		/// Additional password. Used when transaction sending is carried out to a separate server.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str3469Key)]
		[DescriptionLoc(LocalizedStrings.Str3470Key)]
		[PropertyOrder(4)]
		public SecureString TransactionalPassword { get; set; }

		/// <summary>
		/// Path to lg file.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str3471Key)]
		[DescriptionLoc(LocalizedStrings.Str3472Key)]
		[PropertyOrder(5)]
		[Editor(typeof(FileBrowserEditor), typeof(FileBrowserEditor))]
		public string LogFileName { get; set; }

		private RithmicServers _server;

		/// <summary>
		/// Server type.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3473Key)]
		[DescriptionLoc(LocalizedStrings.Str3474Key)]
		[PropertyOrder(0)]
		public RithmicServers Server
		{
			get { return _server; }
			set
			{
				_server = value;

				switch (value)
				{
					case RithmicServers.Sim:
					{
						AdminConnectionPoint = "dd_admin_sslc";
						MarketDataConnectionPoint = "login_agent_sim_uatc";
						TransactionConnectionPoint = "login_agent_uatc";
						PositionConnectionPoint = "login_agent_pnl_simc";
						HistoricalConnectionPoint = "login_agent_history_simc";
						DomainServerAddress = "rituz01000.01.rithmic.com:65000~rituz01000.01.rithmic.net:65000~rituz01000.01.theomne.net:65000~rituz01000.01.theomne.com:65000";
						DomainName = "rithmic_uat_01_dmz_domain";
						LicenseServerAddress = "rituz01000.01.rithmic.com:56000~rituz01000.01.rithmic.net:56000~rituz01000.01.theomne.net:56000~rituz01000.01.theomne.com:56000";
						LocalBrokerAddress = "rituz01000.01.rithmic.com:64100";
						LoggerAddress = "rituz01000.01.rithmic.com:45454~rituz01000.01.rithmic.net:45454~rituz01000.01.theomne.com:45454~rituz01000.01.theomne.net:45454";

						break;
					}
					//case RithmicServers.SimAggr:
					//{
					//	AdminConnectionPoint = "dd_admin_sslc";
					//	MarketDataConnectionPoint = "login_agent_sim_uatc";
					//	TransactionConnectionPoint = "login_agent_uatc";
					//	PositionConnectionPoint = "login_agent_pnl_simc";
					//	HistoricalConnectionPoint = "login_agent_history_simc";
					//	DomainServerAddress = "rituz01000.01.rithmic.com:65000~rituz01000.01.rithmic.net:65000~rituz01000.01.theomne.net:65000~rituz01000.01.theomne.com:65000";
					//	DomainName = "rithmic_uat_01_dmz_domain";
					//	LicenseServerAddress = "rituz01000.01.rithmic.com:56000~rituz01000.01.rithmic.net:56000~rituz01000.01.theomne.net:56000~rituz01000.01.theomne.com:56000";
					//	LocalBrokerAddress = "rituz01000.01.rithmic.com:64100";
					//	LoggerAddress = "rituz01000.01.rithmic.com:45454~rituz01000.01.rithmic.net:45454~rituz01000.01.theomne.com:45454~rituz01000.01.theomne.net:45454";

					//	break;
					//}
					case RithmicServers.Paper:
					{
						AdminConnectionPoint = "dd_admin_sslc";
						MarketDataConnectionPoint = "login_agent_tp_paperc";
						TransactionConnectionPoint = "login_agent_op_paperc";
						PositionConnectionPoint = "login_agent_pnl_paperc";
						HistoricalConnectionPoint = "login_agent_history_paperc";
						DomainServerAddress = "ritpa11120.11.rithmic.com:65000~ritpa11120.11.rithmic.net:65000~ritpa11120.11.theomne.net:65000~ritpa11120.11.theomne.com:65000";
						DomainName = "rithmic_paper_prod_domain";
						LicenseServerAddress = "ritpa11120.11.rithmic.com:56000~ritpa11120.11.rithmic.net:56000~ritpa11120.11.theomne.net:56000~ritpa11120.11.theomne.com:56000";
						LocalBrokerAddress = "ritpa11120.11.rithmic.com:64100";
						LoggerAddress = "ritpa11120.11.rithmic.com:45454~ritpa11120.11.rithmic.net:45454~ritpa11120.11.theomne.net:45454~ritpa11120.11.theomne.com:45454";
						
						break;
					}
					case RithmicServers.Real:
					{
						throw new NotImplementedException();
						break;
					}
					default:
						throw new ArgumentOutOfRangeException(nameof(value));
				}
			}
		}

		private string _adminConnectionPoint;

		/// <summary>
		/// Connection point for administrative functions (initialization/deinitialization).
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3475Key)]
		[DescriptionLoc(LocalizedStrings.Str3476Key)]
		[PropertyOrder(1)]
		public string AdminConnectionPoint
		{
			get { return _adminConnectionPoint; }
			set
			{
				_adminConnectionPoint = value;
				RaisePropertyChanged("AdminConnectionPoint");
			}
		}

		private string _marketDataConnectionPoint;

		/// <summary>
		/// Connection point to market data.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3477Key)]
		[DescriptionLoc(LocalizedStrings.Str3478Key)]
		[PropertyOrder(2)]
		public string MarketDataConnectionPoint
		{
			get { return _marketDataConnectionPoint; }
			set
			{
				_marketDataConnectionPoint = value;
				RaisePropertyChanged("MarketDataConnectionPoint");
			}
		}

		private string _transactionConnectionPoint;

		/// <summary>
		/// Connection point to the transactions execution system.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3479Key)]
		[DescriptionLoc(LocalizedStrings.Str3480Key)]
		[PropertyOrder(3)]
		public string TransactionConnectionPoint
		{
			get { return _transactionConnectionPoint; }
			set
			{
				_transactionConnectionPoint = value;
				RaisePropertyChanged("TransactionConnectionPoint");
			}
		}

		private string _positionConnectionPoint;

		/// <summary>
		/// Connection point for access to portfolios and positions information.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3481Key)]
		[DescriptionLoc(LocalizedStrings.Str3482Key)]
		[PropertyOrder(4)]
		public string PositionConnectionPoint
		{
			get { return _positionConnectionPoint; }
			set
			{
				_positionConnectionPoint = value;
				RaisePropertyChanged("PositionConnectionPoint");
			}
		}

		private string _historicalConnectionPoint;

		/// <summary>
		/// Connection point for access to history data.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3483Key)]
		[DescriptionLoc(LocalizedStrings.Str3484Key)]
		[PropertyOrder(5)]
		public string HistoricalConnectionPoint
		{
			get { return _historicalConnectionPoint; }
			set
			{
				_historicalConnectionPoint = value;
				RaisePropertyChanged("HistoricalConnectionPoint");
			}
		}

		private string _domainServerAddress;

		/// <summary>
		/// Domain address.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3485Key)]
		[DescriptionLoc(LocalizedStrings.Str3486Key)]
		[PropertyOrder(6)]
		public string DomainServerAddress
		{
			get { return _domainServerAddress; }
			set
			{
				_domainServerAddress = value;
				RaisePropertyChanged("DomainServerAddress");
			}
		}

		private string _domainName;

		/// <summary>
		/// Domain name.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3487Key)]
		[DescriptionLoc(LocalizedStrings.Str3488Key)]
		[PropertyOrder(7)]
		public string DomainName
		{
			get { return _domainName; }
			set
			{
				_domainName = value;
				RaisePropertyChanged("DomainName");
			}
		}

		private string _licenseServerAddress;

		/// <summary>
		/// Licenses server address.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3489Key)]
		[DescriptionLoc(LocalizedStrings.Str3490Key)]
		[PropertyOrder(8)]
		public string LicenseServerAddress
		{
			get { return _licenseServerAddress; }
			set
			{
				_licenseServerAddress = value;
				RaisePropertyChanged("LicenseServerAddress");
			}
		}

		private string _localBrokerAddress;

		/// <summary>
		/// Broker address.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.BrokerKey)]
		[DescriptionLoc(LocalizedStrings.Str3491Key)]
		[PropertyOrder(9)]
		public string LocalBrokerAddress
		{
			get { return _localBrokerAddress; }
			set
			{
				_localBrokerAddress = value;
				RaisePropertyChanged("LocalBrokerAddress");
			}
		}

		private string _loggerAddress;

		/// <summary>
		/// Logger address.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3492Key)]
		[DescriptionLoc(LocalizedStrings.Str3493Key)]
		[PropertyOrder(10)]
		public string LoggerAddress
		{
			get { return _loggerAddress; }
			set
			{
				_loggerAddress = value;
				RaisePropertyChanged("LoggerAddress");
			}
		}

		/// <summary>
		/// Проверить введенные параметры на валидность.
		/// </summary>
		[Browsable(false)]
		public override bool IsValid => !UserName.IsEmpty() && !Password.IsEmpty();

		private static readonly HashSet<TimeSpan> _timeFrames = new HashSet<TimeSpan>(new[]
		{
			TimeSpan.FromMinutes(1),
			TimeSpan.FromHours(1),
			TimeSpan.FromDays(1)
		});

		/// <summary>
		/// Available time frames.
		/// </summary>
		[Browsable(false)]
		public static IEnumerable<TimeSpan> TimeFrames => _timeFrames;

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			UserName = storage.GetValue<string>(nameof(UserName));
			Password = storage.GetValue<SecureString>(nameof(Password));
			TransactionalUserName = storage.GetValue<string>(nameof(TransactionalUserName));
			TransactionalPassword = storage.GetValue<SecureString>(nameof(TransactionalPassword));
			AdminConnectionPoint = storage.GetValue<string>(nameof(AdminConnectionPoint));
			MarketDataConnectionPoint = storage.GetValue<string>(nameof(MarketDataConnectionPoint));
			TransactionConnectionPoint = storage.GetValue<string>(nameof(TransactionConnectionPoint));
			PositionConnectionPoint = storage.GetValue<string>(nameof(PositionConnectionPoint));
			HistoricalConnectionPoint = storage.GetValue<string>(nameof(HistoricalConnectionPoint));
			CertFile = storage.GetValue<string>(nameof(CertFile));
			DomainServerAddress = storage.GetValue<string>(nameof(DomainServerAddress));
			DomainName = storage.GetValue<string>(nameof(DomainName));
			LicenseServerAddress = storage.GetValue<string>(nameof(LicenseServerAddress));
			LocalBrokerAddress = storage.GetValue<string>(nameof(LocalBrokerAddress));
			LoggerAddress = storage.GetValue<string>(nameof(LoggerAddress));
			LogFileName = storage.GetValue<string>(nameof(LogFileName));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(UserName), UserName);
			storage.SetValue(nameof(Password), Password);
			storage.SetValue(nameof(TransactionalUserName), TransactionalUserName);
			storage.SetValue(nameof(TransactionalPassword), TransactionalPassword);
			storage.SetValue(nameof(AdminConnectionPoint), AdminConnectionPoint);
			storage.SetValue(nameof(MarketDataConnectionPoint), MarketDataConnectionPoint);
			storage.SetValue(nameof(TransactionConnectionPoint), TransactionConnectionPoint);
			storage.SetValue(nameof(PositionConnectionPoint), PositionConnectionPoint);
			storage.SetValue(nameof(HistoricalConnectionPoint), HistoricalConnectionPoint);
			storage.SetValue(nameof(CertFile), CertFile);
			storage.SetValue(nameof(DomainServerAddress), DomainServerAddress);
			storage.SetValue(nameof(DomainName), DomainName);
			storage.SetValue(nameof(LicenseServerAddress), LicenseServerAddress);
			storage.SetValue(nameof(LocalBrokerAddress), LocalBrokerAddress);
			storage.SetValue(nameof(LoggerAddress), LoggerAddress);
			storage.SetValue(nameof(LogFileName), LogFileName);
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return LocalizedStrings.Str3334Params
				.Put(UserName, TransactionConnectionPoint, MarketDataConnectionPoint, HistoricalConnectionPoint);
		}

		private PropertyChangedEventHandler _propertyChanged;

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add { _propertyChanged += value; }
			remove { _propertyChanged -= value; }
		}

		private void RaisePropertyChanged(string propertyName)
		{
			_propertyChanged.SafeInvoke(this, propertyName);
		}
	}
}