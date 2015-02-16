namespace StockSharp.Quik
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Threading;
	using System.Windows.Forms;

	using MoreLinq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Interop;

	using ManagedWinapi.Windows;
	using ManagedWinapi.Windows.Contents;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Вспомогательный класс для управления окнами Quik терминала.
	/// </summary>
	public class QuikTerminal
	{
		private enum DdeExportTypes
		{
			ByDdeTable,
			BySecurity,
			ByCaption,
		}

		private readonly SynchronizedMultiDictionary<DdeExportTypes, object> _activeDdeExport = new SynchronizedMultiDictionary<DdeExportTypes, object>();
		private readonly object _winApiLock = new object();

		private const string _info = "info";
		private const string _infoExe = _info + ".exe";

		private static readonly string[] _loginWndTitles =
		{
			"Идентификация пользователя",
			"Установка сетевого соединения",
            "Установка сетевого соединения (SSL-PRO)",
            "Двухфакторная аутентификация"
		};

		private const int _wmDelay = 5;

		private QuikTerminal(string fileName)
		{
			InitVersion(fileName);
		}

		private QuikTerminal(Process process)
		{
			if (process == null)
				throw new ArgumentNullException("process");

			InitVersion(process.GetFileName());
			AssignProcess(process);
		}

		private void InitVersion(string fileName)
		{
			var versionInfo = FileVersionInfo.GetVersionInfo(fileName);
			FileName = fileName;
			DirectoryName = Path.GetDirectoryName(fileName);
			Version = versionInfo.ProductVersion.To<Version>();
		}

		/// <summary>
		/// Полный путь к директории, где установлен Quik.
		/// </summary>
		public string DirectoryName { get; private set; }

		/// <summary>
		/// Полный путь к исполняемому файлу Quik.
		/// </summary>
		public string FileName { get; private set; }

		/// <summary>
		/// Версия терминала.
		/// </summary>
		public Version Version { get; private set; }

		/// <summary>
		/// Запущен ли терминал.
		/// </summary>
		/// <remarks>
		/// Если терминал не запущен, то <see cref="QuikTerminal"/> был создан лишь по информации об установленной директории.
		/// </remarks>
		public bool IsLaunched
		{
			get { return _systemProcess != null && !_systemProcess.HasExited; }
		}

		/// <summary>
		/// Получить физическое расположение на диске к терминалу <see cref="DefaultTerminal"/>.
		/// </summary>
		/// <returns>Физическое расположение на диске.</returns>
		public static string GetDefaultPath()
		{
			var defProcess = DefaultTerminal;
			return defProcess == null ? null : defProcess.FileName;
		}

		/// <summary>
		/// Информация о первом терминале из <see cref="GetTerminals"/>.
		/// Если ни одного терминала не запущено, то будет возвращено значение null.
		/// </summary>
		public static QuikTerminal DefaultTerminal
		{
			get { return GetTerminals(false).FirstOrDefault(); }
		}

		/// <summary>
		/// Получить список всех доступных адресов серверов Quik.
		/// </summary>
		public IEnumerable<IPEndPoint> Addresses
		{
			get
			{
				lock (_winApiLock)
				{
					var wnd = LoginWindow;

					if (wnd != null)
					{
						return GetLoginWindowAddresses(wnd);
					}
					else
					{
						CloseAllConnectionWindows();

						var menu = MainWindow.GetMenu();
						menu.Items.First(i => i.Text == "&Связь").Items.First(i => i.Text == "&Доступные соединения").Click();
						var connectionsWnd = WaitForOpen(() => ConnectionWindows, "Открытие окна соединений");

						var allConnections = connectionsWnd.AllChildWindows.First(e => e.DialogID == 12306).ToListView();

						var addresses = new IPEndPoint[allConnections.Count];

						for (var i = 0; i < addresses.Length; i++)
							addresses[i] = (allConnections[i, 1].Title + ":" + allConnections[i, 2].Title).To<IPEndPoint>();

						CloseConnectionWindow(connectionsWnd);
						return addresses;
					}
				}
			}
		}

		private static IEnumerable<IPEndPoint> GetLoginWindowAddresses(SystemWindow wnd)
		{
			var combo = wnd.AllChildWindows.First(e => e.DialogID == 10103).ToComboBox();

			var addresses = new IPEndPoint[combo.Count];

			for (var i = 0; i < addresses.Length; i++)
			{
				var item = combo[i];
				item = item.Substring(item.IndexOf('[') + 1).Replace("]", string.Empty);
				addresses[i] = item.To<IPEndPoint>();
			}

			return addresses;
		}

		/// <summary>
		/// Получить список запущенных терминалов Quik.
		/// </summary>
		/// <param name="throwOnError">Бросать ли исключение при получении недостаточной информации о процессе Quik. Если значение установлено в false, то
		/// терминал с такой информацией исключается из результирующего списка. По умолчанию значение включено.</param>
		/// <returns>Список запущенных терминалов.</returns>
		public static IEnumerable<QuikTerminal> GetTerminals(bool throwOnError = true)
		{
			return QuikProcesses.Select(p =>
			{
				try
				{
					return new QuikTerminal(p);
				}
				catch (Win32Exception)
				{
					return null;
				}
				catch
				{
					if (throwOnError)
						throw;
					else
						return null;
				}
			}).Where(p => p != null).ToArray();
		}

		private static IEnumerable<Process> QuikProcesses
		{
			get
			{
				var processes = (IEnumerable<Process>)Process.GetProcessesByName(_info);

				if (processes.Count() > 1)
				{
					var currentUser = Environment.UserName;

					// http://stocksharp.com/forum/default.aspx?g=posts&t=442
					processes = processes.OrderBy((p1, p2) =>
					{
						var owner1 = p1.GetOwner();
						var owner2 = p2.GetOwner();

						if (owner1 == owner2)
							return 0;
						else if (owner1 == currentUser)
							return -1;
						else if (owner2 == currentUser)
							return 1;
						return 0;
					});
				}

				return processes;
			}
		}

		internal void AssignProcess()
		{
			var process = QuikProcesses.FirstOrDefault(p => p.GetFileName().CompareIgnoreCase(FileName));

			if (process == null)
				throw new InvalidOperationException(LocalizedStrings.Str1810Params.Put(FileName));

			AssignProcess(process);
		}

		private void AssignProcess(Process process)
		{
			if (process == null)
				throw new ArgumentNullException("process");

			SystemProcess = process;
			MainWindow = QuikWindows.FirstOrDefault(IsQuikMainWindow);

			if (MainWindow == null)
			{
				var wnd = QuikWindows.FirstOrDefault();

				if (wnd != null)
					wnd = GetQuikMainWindow(wnd.Parent);
				else
				{
					wnd = new SystemWindow(process.MainWindowHandle);

					if (!IsQuikMainWindow(wnd))
						throw new ArgumentException(LocalizedStrings.Str1811, "process");
				}

				MainWindow = wnd;
			}

			_statusBar = MainWindow.AllChildWindows.SingleOrDefault(window => window.ClassName.CompareIgnoreCase("msctls_statusbar32"));

			if (_statusBar == null)
				throw new InvalidOperationException(LocalizedStrings.Str1812);
		}

		/// <summary>
		/// Получить запущенный терминал Quik по указанному пути.
		/// </summary>
		/// <param name="path">Путь, где установлен Quik.</param>
		/// <returns>Информация о терминале.</returns>
		public static QuikTerminal Get(string path)
		{
			if (path.IsEmpty())
				throw new ArgumentNullException("path");

			var dir = GetDirectory(path).TrimEnd('\\');

			var terminal = GetTerminals().FirstOrDefault(p => p.DirectoryName.TrimEnd('\\').CompareIgnoreCase(dir));

			if (terminal == null)
			{
				path = GetFile(path);

				if (File.Exists(path))
					terminal = new QuikTerminal(path);
				else
					throw new ArgumentException(LocalizedStrings.Str1813Params.Put(path), "path");
			}

			return terminal;
		}

		/// <summary>
		/// Запустить Quik терминал, установленный по указанному пути <see cref="FileName"/>.
		/// </summary>
		/// <remarks>
		/// Метод заканчивает работу при появлении окна с логином. Если окно не появилось в течении 6 минут, то будет выброшего исключение.
		/// </remarks>
		public void Launch()
		{
			if (IsLaunched)
				throw new InvalidOperationException(LocalizedStrings.Str1814);

			var processStartInfo = new ProcessStartInfo { FileName = FileName };

			if (System.Environment.OSVersion.Version.Major >= 6)  // Windows Vista or higher
			{
				processStartInfo.Verb = "runas";
			}

			//processStartInfo.Arguments = string.Empty;
			//processStartInfo.WindowStyle = ProcessWindowStyle.Normal;
			processStartInfo.UseShellExecute = true;
			processStartInfo.WorkingDirectory = DirectoryName;

			var process = Process.Start(processStartInfo);

			WaitForOpen(() => GetLoginWindows(GetQuikWindows(process)), "Запуск терминала", 360);
			
			AssignProcess(process);
		}

		/// <summary>
		/// Подключить Quik к серверу торгов с использованием сертификата.
		/// </summary>
		/// <param name="certPath">Путь к сертификату.</param>
		/// <param name="password">Пароль. Если значение не задано, то пароль не используется при подключении.</param>
		/// <param name="address">Адрес сервера Quik. Если значение равно null, то используется адрес по умолчанию.</param>
		public void LoginWithCertificate(string certPath, string password = null, IPEndPoint address = null)
		{
			if (certPath.IsEmpty())
				throw new ArgumentNullException("certPath");

			lock (_winApiLock)
			{
				//Ввод пути к сертификату и выбор адреса.
				var wnd = LoginWindow;

				if (wnd == null)
				{
					var connect = MainWindow.GetMenu().Items[0].Items[0];

					if (connect.IsEnabled)
					{
						connect.Click();

						wnd = WaitForOpen(() => LoginWindows, "Открытие окна подключения");
					}
					else
						return;
				}

				var certPathCtrl = wnd.AllChildWindows.First(w => w.DialogID == 0x2779);

				certPathCtrl.SetText(certPath);

				if (address != null)
				{
					var addresses = GetLoginWindowAddresses(wnd).ToArray();

					var index = addresses.IndexOf(address);

					if (index == -1)
						throw new ArgumentOutOfRangeException("address", address, LocalizedStrings.Str1815);

					var combo = wnd.AllChildWindows.First(e => e.DialogID == 0x2777);

					addresses.ForEach(a => combo.PressKeyButton(VirtualKeys.Up));

					for (var i = 0; i < index; i++)
						combo.PressKeyButton(VirtualKeys.Down);
				}

				CloseOk(wnd);

				if (!password.IsEmpty())
				{
					//Ввод пароля.
					WaitForClose(() => LoginWindows, "Закрытие окна подключения");

					wnd = LoginWindow ?? WaitForOpen(() => LoginWindows, "Открытие окна подключения");

					var passwordCtrl = wnd.AllChildWindows.First(w => w.DialogID == 10011);
					passwordCtrl.SetText(password);

					CloseOk(wnd);
				}
			}
		}

		/// <summary>
		/// Подключить Quik к серверу торгов.
		/// </summary>
		/// <param name="login">Логин.</param>
		/// <param name="password">Пароль.</param>
		/// <param name="address">Адрес сервера Quik. Если значение равно null, то используется адрес по умолчанию.</param>
		public void Login(string login, string password, IPEndPoint address = null)
		{
			if (login.IsEmpty())
				throw new ArgumentNullException("login");

			if (password == null)
				throw new ArgumentNullException("password");

			lock (_winApiLock)
			{
				var wnd = LoginWindow;

				if (wnd == null)
				{
					var connect = MainWindow.GetMenu().Items[0].Items[0];

					if (connect.IsEnabled)
					{
						connect.Click();

						wnd = WaitForOpen(() => LoginWindows, "Открытие окна подключения");
					}
					else
						return;
				}

				var loginCtrl = wnd.AllChildWindows.First(w => w.DialogID == 0x2775);
				var passwordCtrl = wnd.AllChildWindows.First(w => w.DialogID == 0x2776);

				loginCtrl.SetText(login);
				passwordCtrl.SetText(password);

				if (address != null)
				{
					var addresses = GetLoginWindowAddresses(wnd).ToArray();

					var index = addresses.IndexOf(address);

					if (index == -1)
						throw new ArgumentOutOfRangeException("address", address, LocalizedStrings.Str1815);

					var combo = wnd.AllChildWindows.First(e => e.DialogID == 10103);

					addresses.ForEach(a => combo.PressKeyButton(VirtualKeys.Up));

					for (var i = 0; i < index; i++)
						combo.PressKeyButton(VirtualKeys.Down);
				}

				CloseOk(wnd);	
			}
		}

		/// <summary>
		/// Отключить Quik от сервера торгов.
		/// </summary>
		public void Logout()
		{
			lock (_winApiLock)
			{
				var loginWindow = LoginWindow;

				if (loginWindow != null)
					return;

				var connect = MainWindow.GetMenu().Items[0].Items[1];

				if (connect.IsEnabled)
					connect.Click();
			}
		}

		/// <summary>
		/// Выключить Quik.
		/// </summary>
		public void Exit()
		{
			CloseAllLoginWindows();
			MainWindow.PostMessage(WM.CLOSE, IntPtr.Zero, IntPtr.Zero);

			SystemProcess = null;
			MainWindow = null;
		}

		private Process _systemProcess;

		/// <summary>
		/// Системная информация о процессе Quik.
		/// </summary>
		public Process SystemProcess
		{
			get
			{
				ThrowIfNotLaunched();
				return _systemProcess;
			}
			private set { _systemProcess = value; }
		}

		private SystemWindow _mainWindow;

		/// <summary>
		/// Системная информация о главном окне Quik.
		/// </summary>
		[CLSCompliant(false)]
		public SystemWindow MainWindow
		{
			get
			{
				ThrowIfNotLaunched();
				return _mainWindow;
			}
			private set { _mainWindow = value; }
		}

		private SystemWindow _statusBar;

		private SystemWindow StatusBar
		{
			get
			{
				ThrowIfNotLaunched();
				return _statusBar;
			}
		}

		/// <summary>
		/// Подключен ли терминал к торгам.
		/// </summary>
		public bool IsConnected
		{
			get { return !StatusBar.GetText().IsEmpty(); }
		}

		private QuikSessionHolder _sessionHolder;

		internal QuikSessionHolder SessionHolder
		{
			private get
			{
				if (_sessionHolder == null)
					throw new InvalidOperationException(LocalizedStrings.Str1816);

				return _sessionHolder;
			}
			set
			{
				_sessionHolder = value;

				if (_sessionHolder == null)
					_allTables = null;
				else
				{
					_allTables = new[]
					{
						value.SecuritiesTable,
						value.TradesTable,
						value.OrdersTable,
						value.StopOrdersTable,
						value.MyTradesTable,
						value.EquityPortfoliosTable,
						value.DerivativePortfoliosTable,
						value.EquityPositionsTable,
						value.DerivativePositionsTable,
						value.SecuritiesChangeTable,
						value.CurrencyPortfoliosTable
					};
				}
			}
		}

		private DdeTable[] _allTables;

		private DdeTable[] AllTables
		{
			get
			{
				if (_allTables == null)
					throw new InvalidOperationException(LocalizedStrings.Str1817);

				return _allTables;
			}
		}

		private IEnumerable<DdeTable> FilteredAllTables
		{
			get
			{
				var except = new List<DdeTable>();

				if (!SessionHolder.UseCurrencyPortfolios)
					except.Add(SessionHolder.CurrencyPortfoliosTable);

				if (!SessionHolder.UseSecuritiesChange)
					except.Add(SessionHolder.SecuritiesChangeTable);

				return except.Count == 0 ? AllTables : AllTables.Except(except);
			}
		}

		private SecurityIdGenerator _securityIdGenerator = new SecurityIdGenerator();

		/// <summary>
		/// Генератор идентификаторов инструментов <see cref="Security.Id"/>.
		/// </summary>
		public SecurityIdGenerator SecurityIdGenerator
		{
			get { return _securityIdGenerator; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_securityIdGenerator = value;
			}
		}

		#region ServerTime

		/// <summary>
		/// Получить время биржи из Quik окна.
		/// Свойство возвращает null, если соединение с биржей потеряно.
		/// </summary>
		public DateTime? ServerTime
		{
			get
			{
				// http://groups.google.ru/group/stocksharp/browse_thread/thread/cead5a007d9dce0e#
				var srvTimeString = StatusBar.GetText();

				if (!srvTimeString.IsEmpty())
				{
					// Время сервера: 16:53:49; записей: 110; текущая: 623558
					var startIndex = srvTimeString.IndexOf(":", StringComparison.Ordinal);
					var endIndex = srvTimeString.IndexOf(";", StringComparison.Ordinal);

					// http://stocksharp.com/forum/default.aspx?g=posts&m=4541#post4541
					if (startIndex != -1 && endIndex != -1)
					{
						startIndex += 2;

						var serverTime = srvTimeString.Substring(startIndex, endIndex - startIndex).To<DateTime>();

						// http://groups.google.ru/group/stocksharp/msg/3685e1c26c35e249
						var delta = TimeZoneInfo.Local.BaseUtcOffset - Exchange.Moex.TimeZoneInfo.BaseUtcOffset;

						var deltaDate = serverTime.Add(delta).Date;

						if (serverTime.Date < deltaDate)
							serverTime = serverTime.AddDays(-1);
						else if (serverTime.Date > deltaDate)
							serverTime = serverTime.AddDays(1);


						return serverTime;
					}
				}
				
				return null;
			}
		}

		#endregion

		#region DdeWindowCaption

		private string _ddeWindowCaption = "Вывод через DDE сервер";

		/// <summary>
		/// Заголовок окна экспорта через DDE в Quik-е. В русской версии окно имеет заголовок "Вывод через DDE сервер", что является значением по-умолчанию.
		/// </summary>
		public string DdeWindowCaption
		{
			get { return _ddeWindowCaption; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException("value");

				_ddeWindowCaption = value;
			}
		}

		#endregion

		#region EditWindowCaption

		private string _editWindowCaption = "Редактирование";

		/// <summary>
		/// Заголовок окна редактирования таблицы в Quik-е. В русской версии окно имеет заголовок "Редактирование таблицы", что является значение по-умолчанию.
		/// </summary>
		public string EditWindowCaption
		{
			get { return _editWindowCaption; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException("value");

				_editWindowCaption = value;
			}
		}

		#endregion

		#region AccountWindowCaption

		private string _accountWindowCaption = "Выбор активных счетов и задание их очередности";

		/// <summary>
		/// Заголовок окна редактирования таблицы в Quik-е. В русской версии окно имеет заголовок "Выбор активных счетов и задание их очередности", что является значение по-умолчанию.
		/// </summary>
		public string AccountWindowCaption
		{
			get { return _accountWindowCaption; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException("value");

				_accountWindowCaption = value;
			}
		}

		#endregion

		internal bool IsExportStarted
		{
			get { return _activeDdeExport.Count > 0; }
		}

		/// <summary>
		/// Включить возможность настраивать в Quik фильтр таблицы Все Сделки через методы <see cref="RegisterTrades"/> и <see cref="UnRegisterTrades"/>.
		/// </summary>
		public bool EnableFiltering { get; set; }

		/// <summary>
		/// Получить список счетов из окна управления счетами.
		/// </summary>
		/// <returns>Список счетов.</returns>
		public IEnumerable<string> GetAccounts()
		{
			lock (_winApiLock)
			{
				CloseAllAccountWindows();

				var menu = MainWindow.GetMenu();
				menu.Items.First(i => i.Text == "Торговл&я").Items.First(i => i.Text.Contains("&Настройка счетов")).Click();
				var accountWnd = WaitForOpen(() => AccountWindows, "Открытие окна счетов");

				var allAccounts = accountWnd.AllChildWindows.First(e => e.DialogID == 12001).ToListBox();
				var selectedAccounts = accountWnd.AllChildWindows.First(e => e.DialogID == 12002).ToListBox();

				var accounts = allAccounts.GetListBoxItems().Concat(selectedAccounts.GetListBoxItems()).ToArray();
				CloseAccountWindow(accountWnd);
				return accounts;
			}
		}

		#region IsDdeStarted

		/// <summary>
		/// Запущен ли экспорт DDE для переданой таблицы.
		/// </summary>
		/// <param name="table">Таблица, для которой необходимо узнать, запущен ли экспорт.</param>
		/// <returns><see langword="true"/>, если экспорт запущен, иначе, <see langword="false"/>.</returns>
		public bool IsDdeStarted(DdeTable table)
		{
			if (table == null)
				throw new ArgumentNullException("table");

			return _activeDdeExport.Contains(DdeExportTypes.ByDdeTable, table);
		}

		/// <summary>
		/// Запущен ли экспорт данных через DDE для произвольной таблицы, зарегистрированной в <see cref="QuikTrader.CustomTables"/>.
		/// </summary>
		/// <param name="table">Описание DDE экспорта произвольной таблицы.</param>
		/// <returns><see langword="true"/>, если экспорт запущен, иначе, <see langword="false"/>.</returns>
		public bool IsDdeStarted(DdeCustomTable table)
		{
			if (table == null)
				throw new ArgumentNullException("table");

			return IsDdeStarted(table.TableName);
		}

		/// <summary>
		/// Запущен ли экспорт DDE для таблицы с указанным заголовком.
		/// </summary>
		/// <param name="caption">Название таблицы, для которой необходимо узнать, запущен ли экспорт.</param>
		/// <returns><see langword="true"/>, если экспорт запущен, иначе, <see langword="false"/>.</returns>
		public bool IsDdeStarted(string caption)
		{
			if (caption.IsEmpty())
				throw new ArgumentNullException("caption");

			return _activeDdeExport.Contains(DdeExportTypes.ByCaption, caption);
		}

		/// <summary>
		/// Запущен ли экспорт DDE для стакана котировок по переданному инструменту.
		/// </summary>
		/// <param name="security">Инструмент, для которой необходимо узнать, запущен ли экспорт стакана.</param>
		/// <returns><see langword="true"/>, если экспорт запущен, иначе, <see langword="false"/>.</returns>
		public bool IsDdeStarted(Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			return _activeDdeExport.Contains(DdeExportTypes.BySecurity, security);
		}

		#endregion

		#region StartDde StopDde

		internal void StartDde()
		{
			StartDde(FilteredAllTables);
		}

		internal void StartDde(IEnumerable<DdeTable> ddeTables)
		{
			ThrowIfQuotesTable(ddeTables);

			if (ddeTables.IsEmpty())
				throw new ArgumentOutOfRangeException("ddeTables", LocalizedStrings.Str1818);

			foreach (var table in OrderBy(ddeTables, true))
				StartDde(table.Caption);
		}

		internal void StopDde()
		{
			StopDde(FilteredAllTables);
		}

		internal void StopDde(IEnumerable<DdeTable> ddeTables)
		{
			ThrowIfQuotesTable(ddeTables);

			if (ddeTables.IsEmpty())
				throw new ArgumentOutOfRangeException("ddeTables", LocalizedStrings.Str1818);

			foreach (var table in OrderBy(ddeTables, false))
				StopDde(table.Caption);
		}

		private void ThrowIfQuotesTable(IEnumerable<DdeTable> ddeTables)
		{
			if (ddeTables == null)
				throw new ArgumentNullException("ddeTables");

			if (ddeTables.Any(t => t == SessionHolder.QuotesTable))
				throw new ArgumentException(LocalizedStrings.Str1819, "ddeTables");
		}

		private string GetSecurityId(SecurityId security)
		{
			return SecurityIdGenerator.GenerateId(security.SecurityCode, security.BoardCode);
		}

		internal void StartDde(SecurityId security)
		{
			lock (_winApiLock)
			{
				CloseAllDdeWindows();

				var quotesWindow = GetQuotesWindow(security);

				quotesWindow.Book = SessionHolder.QuotesTable.Caption;
				quotesWindow.Sheet = GetSecurityId(security);

				StartDde(quotesWindow, new DdeSettings());
			}

			_activeDdeExport.Add(DdeExportTypes.BySecurity, security);
		}

		internal void StopDde(SecurityId security)
		{
			lock (_winApiLock)
			{
				CloseAllDdeWindows();

				if (IsQuotesOpened(security))
					StopDde(GetQuotesWindow(security));
			}

			_activeDdeExport.Remove(DdeExportTypes.BySecurity, security);
		}

		internal void StartDde(DdeCustomTable customTable)
		{
			if (customTable == null)
				throw new ArgumentNullException("customTable");

			StartDde(customTable.DdeSettings);
		}

		internal void StopDde(DdeCustomTable customTable)
		{
			if (customTable == null)
				throw new ArgumentNullException("customTable");

			StopDde(customTable.TableName);
		}

		internal void StartDde(string caption)
		{
			StartDde(new DdeSettings { TableName = caption });
		}

		internal void StopDde(string caption)
		{
			lock (_winApiLock)
			{
				CloseAllDdeWindows();
				var window = GetTableWindow(caption);
				var ddeWindow = OpenDdeWindow(window);
				StopDde(ddeWindow);
			}

			var table = GetWellKnownTable(caption);

			if (table != null)
				_activeDdeExport.Remove(DdeExportTypes.ByDdeTable, table);
			else
				_activeDdeExport.Remove(DdeExportTypes.ByCaption, caption);
		}

		private void StartDde(DdeSettings ddeSettings)
		{
			if (ddeSettings == null)
				throw new ArgumentNullException("ddeSettings");

			lock (_winApiLock)
			{
				CloseAllDdeWindows();

				var window = GetTableWindow(ddeSettings.TableName);
				var ddeWindow = OpenDdeWindow(window);

				ddeWindow.Book = ddeSettings.TableName;
				ddeWindow.Sheet = string.Empty;

				StartDde(ddeWindow, ddeSettings);
			}

			var table = GetWellKnownTable(ddeSettings.TableName);

			if (table != null)
				_activeDdeExport.Add(DdeExportTypes.ByDdeTable, table);
			else
				_activeDdeExport.Add(DdeExportTypes.ByCaption, ddeSettings.TableName);
		}

		private DdeTable GetWellKnownTable(string caption)
		{
			if (caption.IsEmpty())
				throw new ArgumentNullException("caption");

			return AllTables.FirstOrDefault(t => t.Caption == caption);
		}

		#endregion

		/// <summary>
		/// Перезапустить экспорт DDE по всем таблицам, запущенных в данный момент.
		/// </summary>
		internal void ReStartDde()
		{
			_activeDdeExport.SyncDo(d =>
			{
				if (d.Count > 0)
				{
					foreach (var pair in d.Reverse().ToArray())
					{
						var values = pair.Value.ToArray();

						switch (pair.Key)
						{
							case DdeExportTypes.ByDdeTable:
								StopDde(values.Cast<DdeTable>());
								StartDde(values.Cast<DdeTable>());
								break;
							case DdeExportTypes.BySecurity:
								values.Cast<SecurityId>().ForEach(StopDde);
								values.Cast<SecurityId>().ForEach(StartDde);
								break;
							case DdeExportTypes.ByCaption:
								values.Cast<string>().ForEach(StopDde);
								values.Cast<string>().ForEach(StartDde);
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}
					}
				}
				else
					StartDde();
			});
		}

		/// <summary>
		/// Проверить, открыто ли окно таблицы.
		/// </summary>
		/// <param name="table">Таблица, для которой необходимо проверить наличие открытого окна.</param>
		/// <returns><see langword="true"/>, если окно открыто, иначе, <see langword="false"/>.</returns>
		public bool IsTableOpened(DdeTable table)
		{
			if (table == null)
				throw new ArgumentNullException("table");

			ThrowIfQuotesTable(new[] { table });

			return IsTableOpened(table.Caption);
		}

		/// <summary>
		/// Проверить, открыто ли окно по названию таблицы.
		/// </summary>
		/// <param name="caption">Название таблица, для которой необходимо проверить наличие открытого окна.</param>
		/// <returns><see langword="true"/>, если окно открыто, иначе, <see langword="false"/>.</returns>
		public bool IsTableOpened(string caption)
		{
			if (caption.IsEmpty())
				throw new ArgumentNullException("caption");

			lock (_winApiLock)
				return GetTableWindow(caption, false) != null;
		}

		private SystemWindow MdiWindow
		{
			get { return MainWindow.AllChildWindows.First(w => w.ClassName.CompareIgnoreCase("MDIClient")); }
		}

		private IEnumerable<SystemWindow> QuotesWindows
		{
			get
			{
				return MdiWindow
					.FilterDescendantWindows(false, child => child.ClassName.CompareIgnoreCase("InfoPriceTable"))
					.ToArray();
			}
		}

		/// <summary>
		/// Получить список сообщений из Окна сообщений Квика.
		/// </summary>
		/// <returns>Список сообщений.</returns>
		public IEnumerable<string> GetMessages()
		{
			try
			{
				var reBarWindow = MainWindow.AllChildWindows.First(w => w.ClassName.CompareIgnoreCase("ReBarWindow32"));
				foreach (var wnd in reBarWindow.AllChildWindows)
				{
					if (!wnd.ClassName.CompareIgnoreCase("ToolbarWindow32")) continue;

					foreach (var combobox32 in wnd.AllChildWindows)
					{
						if (!combobox32.ClassName.CompareIgnoreCase("ComboBoxEx32")) continue;

						var listContent = combobox32.Content as ListContent;
						if (listContent == null) break;

						//Reverse для FIFO - ранние сообщения должны быть с меньшим индексом в массиве
						return listContent.GetListContentItems().Reverse();
					}
				}
			}
			catch
			{
				throw new InvalidOperationException(LocalizedStrings.Str1820);
			}

			return null;
		}

		/// <summary>
		/// Начать расчёт заново по открытой таблице.
		/// </summary>
		/// <param name="table">Описание DDE экспорта произвольной таблицы, для которой необходимо начать заново расчёт.</param>
		public void StartTableCalculation(DdeCustomTable table)
		{
			if (table == null)
				throw new ArgumentNullException("table");

			StartTableCalculation(table.TableName);
		}

		/// <summary>
		/// Начать расчёт заново по открытой таблице.
		/// </summary>
		/// <param name="caption">Название таблицы, для которой необходимо начать заново расчёт.</param>
		public void StartTableCalculation(string caption)
		{
			if (caption == null)
				throw new ArgumentNullException("caption");

			var window = GetTableWindow(caption);
			
			const int menuItemId = 323;
			window.PostMessage(WM.COMMAND, menuItemId, 0);
		}

		/// <summary>
		/// Проверить, открыто ли окно стакана для переданного инструмента.
		/// </summary>
		/// <param name="security">Идентификатор нструмента, для которого необходимо проверить наличие окна стакана.</param>
		/// <returns><see langword="true"/>, если окно открыто, иначе, <see langword="false"/>.</returns>
		public bool IsQuotesOpened(SecurityId security)
		{
			return IsTableOpened(GetSecurityId(security));
		}

		/// <summary>
		/// Проверить, содержит ли таблица <see cref="QuikSessionHolder.SecuritiesTable"/> указанный инструмент.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <returns><see langword="true"/>, если стакан можно открыть, иначе, <see langword="false"/>.</returns>
		public bool SecuritiesTableContains(SecurityId securityId)
		{
			var securityCode = securityId.SecurityCode;
			var securityClass = SessionHolder.GetSecurityClass(securityId);

			var text = GetSecuritiesTableData();

			if (text.IsEmpty())
				return false;

			return text
				.Split(Environment.NewLine)
				.Any(l => IsRequiredSecurity(securityCode, securityClass, l.Split('\t')));
		}

		private string GetSecuritiesTableData()
		{
			lock (_winApiLock)
			{
				var table = SessionHolder.SecuritiesTable;

				var window = SessionHolder.IsDde
					? GetTableWindow(table.Caption)
					: GetTableWindowByClass(table.ClassName);

				if (window == null)
					return null;

				const int menuItemId = 304;
				window.SendMessage(WM.COMMAND, menuItemId, 0);

				//http://stocksharp.com/forum/yaf_postst1139_RegisterQuotes-otkryvaiet-stakan-drughogho-instrumienta.aspx
				const int sortMenuItemId = 850;
				window.SendMessage(WM.COMMAND, sortMenuItemId, 0);

				return ThreadingHelper.InvokeAsSTA(() => Clipboard.GetText());
			}
		}

		private bool IsRequiredSecurity(string securityCode, string securityClass, IList<string> parts)
		{
			if (!SessionHolder.IsDde)
				return parts.Any(i => i.CompareIgnoreCase(securityCode)) && parts.Any(i => i.CompareIgnoreCase(securityClass));

			var table = SessionHolder.SecuritiesTable;

			var secCode = parts[table.Columns.IndexOf(DdeSecurityColumns.Code) + 1];
			var secClass = parts[table.Columns.IndexOf(DdeSecurityColumns.Class) + 1];

			return secCode.CompareIgnoreCase(securityCode) && secClass.CompareIgnoreCase(securityClass);
		}

		/// <summary>
		/// Открыть окно стакана для переданного инструмента.
		/// </summary>
		/// <param name="securityId">Идентификатор нструмента, для которого необходимо открыть окно стакана.</param>
		public void OpenQuotes(SecurityId securityId)
		{
			var securityClass = SessionHolder.GetSecurityClass(securityId);

			lock (_winApiLock)
			{
				CloseAllEditWindows();

				var text = GetSecuritiesTableData();

				var table = SessionHolder.SecuritiesTable;

				var window = SessionHolder.IsDde
					? GetTableWindow(table.Caption)
					: GetTableWindowByClass(table.ClassName);

				window = window.FilterDescendantWindows(false, w => w.ClassName.CompareIgnoreCase("MultiList")).First();
				
				//window.SendMessage(WM.MOUSEACTIVATE, MainWindow.HWnd, WinApi.MakeWParam(1, (int)WM.LBUTTONDOWN));
				var coords = WinApi.MakeParam(20, 20);
				window.SendMessage(WM.LBUTTONDOWN, 0x0001, coords);
				//window.SendMessage(WM.KILLFOCUS, MainWindow.HWnd, 0);
				//window.SendMessage(WM.SETFOCUS, window.Parent.HWnd, 0);
				window.SendMessage(WM.LBUTTONUP, 0, coords);

				var securityIdStr = GetSecurityId(securityId);
				var lines = text.Split(Environment.NewLine);
				var index = -1;

				for (var i = 0; i < lines.Length; i++)
				{
					window.PressKeyButton(VirtualKeys.Up);

					if (index != -1)
						continue;

					if (IsRequiredSecurity(securityId.SecurityCode, securityClass, lines[i].Split('\t')))
						index = i;
				}

				if (index == -1)
					throw new ArgumentException(LocalizedStrings.Str1821Params.Put(securityIdStr, table.Caption), "securityId");

				for (var i = 0; i < (index - 1); i++)
					window.PressKeyButton(VirtualKeys.Down);

				var prevTables = QuotesWindows;

				window.SendMessage(WM.KEYDOWN, (int)VirtualKeys.Return, 0);
				window.SendMessage(WM.CHAR, (int)VirtualKeys.Return, 1);

				// при Lua подключении стакан может иметь любое название и настройки.
				if (!SessionHolder.IsDde)
					return;

				var currentTables = QuotesWindows;

				var newQuoteWindow = currentTables.FirstOrDefault(w => !prevTables.Contains(w));

				if (newQuoteWindow == null)
					throw new ArgumentException(LocalizedStrings.Str1822Params.Put(securityIdStr), "securityId");

				var editWindow = OpenEditWindow(newQuoteWindow);
				editWindow.AllChildWindows.First(e => e.DialogID == 0x3072).SetText(securityIdStr);
				CloseOk(editWindow);

				// http://stocksharp.com/forum/yaf_postst1123_Exception-pri-piervom-vyzovie-Trader-RegisterQuotes.aspx
				WaitFor(() => newQuoteWindow.GetText() != securityIdStr, "Заголовок стакана {0}".Put(securityIdStr));
			}
		}

		/// <summary>
		/// Открыть таблицу Quik.
		/// </summary>
		/// <param name="table">Таблица, для которой необходимо открыть Quik таблицу.</param>
		public void OpenTable(DdeTable table)
		{
			if (table == null)
				throw new ArgumentNullException("table");

			ThrowIfQuotesTable(new[] { table });
			throw new NotImplementedException();
		}

		/// <summary>
		/// Получить окно стакана по переданному инструменту.
		/// </summary>
		/// <param name="security">Идентификатор нструмента, для которого необходимо получить окно стакана.</param>
		/// <returns>Окно стакана.</returns>
		private DdeWindow GetQuotesWindow(SecurityId security)
		{
			return OpenDdeWindow(GetTableWindow(GetSecurityId(security)));
		}

		/// <summary>
		/// Остановить все активные DDE потоки.
		/// </summary>
		public void StopActiveDdeExport()
		{
			foreach (var pair in _activeDdeExport.SyncGet(d => d.Reverse().ToArray()))
			{
				var values = pair.Value.ToArray();

				switch (pair.Key)
				{
					case DdeExportTypes.ByDdeTable:
						StopDde(values.Cast<DdeTable>());
						break;
					case DdeExportTypes.BySecurity:
						values.Cast<SecurityId>().ForEach(StopDde);
						break;
					case DdeExportTypes.ByCaption:
						values.Cast<string>().ForEach(StopDde);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		/// <summary>
		/// Получить все ошибки настроек таблиц в Quik (несоответствие названий, отсутствие необходимых колонок и т.д.).
		/// </summary>
		/// <param name="tables">Таблицы, для которых необходимо найти ошибки.
		/// Если не передается ни одна из таблиц, то проверятся будут все таблицы (инструменты, сделки, заявки, мои сделки и т.д.), в том числе
		/// и окна стаканов.</param>
		/// <returns>Ошибки настроек.</returns>
		public IEnumerable<DdeSettingsResult> GetTableSettings(params DdeTable[] tables)
		{
			return SessionHolder.IsDde ? GetDdeTableSettings(tables) : GetLuaTableSettings(tables);
		}

		private IEnumerable<DdeSettingsResult> GetLuaTableSettings(params DdeTable[] tables)
		{
			if (tables == null)
				throw new ArgumentNullException("tables");

			if (tables.Length == 0)
				tables = FilteredAllTables.ToArray();

			lock (_winApiLock)
			{
				var results = new List<DdeSettingsResult>();

				CloseAllEditWindows();

				foreach (var table in tables.Where(t => t != SessionHolder.QuotesTable))
				{
					var window = GetTableWindowByClass(table.ClassName, false);

					if (window == null)
						results.Add(new DdeSettingsResult(table, new InvalidOperationException(LocalizedStrings.Str1823.Put(table.Caption)), true));
				}

				return results;
			}
		}

		private IEnumerable<DdeSettingsResult> GetDdeTableSettings(params DdeTable[] tables)
		{
			if (tables == null)
				throw new ArgumentNullException("tables");

			if (tables.Length == 0)
				tables = FilteredAllTables.Concat(SessionHolder.QuotesTable).ToArray();

			lock (_winApiLock)
			{
				var results = new List<DdeSettingsResult>();

				CloseAllEditWindows();

				foreach (var table in tables)
				{
					if (table == SessionHolder.QuotesTable)
					{
						foreach (var quotesWindow in QuotesWindows)
						{
							results.AddRange(GetTableSettings(table, quotesWindow));

							var title = quotesWindow.Title;
							var firstIndex = title.IndexOf(SecurityIdGenerator.Delimiter, StringComparison.Ordinal);

							if (firstIndex == -1 || firstIndex != title.LastIndexOf(SecurityIdGenerator.Delimiter, StringComparison.Ordinal))
								results.Add(new DdeSettingsResult(table, new InvalidOperationException(LocalizedStrings.Str1824Params.Put(title)), true));
						}
					}
					else
					{
						results.AddRange(GetTableSettings(table, GetTableWindow(table.Caption)));
					}
				}

				return results;
			}
		}

		private IEnumerable<DdeSettingsResult> GetTableSettings(DdeTable table, SystemWindow window)
		{
			if (table == null)
				throw new ArgumentNullException("table");

			if (window == null)
				throw new ArgumentNullException("window");

			var editWnd = OpenEditWindow(window);

			var results = new List<DdeSettingsResult>();

			try
			{
				if (table == SessionHolder.SecuritiesTable)
				{
					var ctrl = editWnd.AllChildWindows.First(e => e.DialogID == 10442);
					if (ctrl.CheckState == CheckState.Checked)
					{
						ctrl.CheckState = CheckState.Unchecked;
						ctrl.Parent.Command(ctrl);
						//Thread.Sleep(_wmDelay);
					}
				}

				int columnsCtrlId;

				if (table == SessionHolder.SecuritiesTable)
					columnsCtrlId = 10411;
				else if (table == SessionHolder.SecuritiesChangeTable)
					columnsCtrlId = 10511;
				else if (table == SessionHolder.OrdersTable)
					columnsCtrlId = 11806;
				else if (table == SessionHolder.StopOrdersTable)
					columnsCtrlId = 31257;
				else if (table == SessionHolder.TradesTable)
					columnsCtrlId = 30303;
				else if (table == SessionHolder.MyTradesTable)
					columnsCtrlId = 11709;
				else if (table == SessionHolder.EquityPortfoliosTable)
					columnsCtrlId = 17903;
				else if (table == SessionHolder.DerivativePortfoliosTable)
					columnsCtrlId = 30857;
				else if (table == SessionHolder.EquityPositionsTable)
					columnsCtrlId = 12707;
				else if (table == SessionHolder.DerivativePositionsTable)
					columnsCtrlId = 30909;
				else if (table == SessionHolder.QuotesTable)
					columnsCtrlId = 12405;
				else
					throw new InvalidOperationException(LocalizedStrings.Str1825);

				var columnsCtrl = editWnd.AllChildWindows.First(e => e.DialogID == columnsCtrlId);
				var content = (ListContent)columnsCtrl.Content;

				if (content.Count < table.Columns.Count)
					throw new InvalidOperationException(LocalizedStrings.Str1826Params.Put(table.Caption, table.Columns.Count));

				for (var i = 0; i < table.Columns.Count; i++)
				{
					var expectedColumn = table.Columns[i];
					var columnTitle = content[i];

					if (columnTitle == expectedColumn.Name)
						continue;

					// http://stocksharp.com/forum/default.aspx?g=posts&t=447
					if (expectedColumn == DdeSecurityColumns.Status && columnTitle.CompareIgnoreCase("статус торговли инструментом"))
						continue;

					var realColumn = table.Columns[columnTitle];

					var nonCritical = realColumn != null && realColumn.DataType == expectedColumn.DataType;

					var message = nonCritical
					              	? LocalizedStrings.Str1827Params
					              	: LocalizedStrings.Str1828Params;

					var tableCaption = table.Caption;

					if (table == SessionHolder.QuotesTable)
						tableCaption = window.Title;

					results.Add(new DdeSettingsResult(table, new InvalidOperationException(
											message.Put(tableCaption, i, expectedColumn.Name, columnTitle)), nonCritical));
				}
			}
			catch (Exception ex)
			{
				results.Add(new DdeSettingsResult(table, ex, true));
			}

			if (editWnd != null)
				CloseEditWindow(editWnd, false);

			return results;
		}

		private SystemWindow GetTableWindow(string caption, bool throwException = true)
		{
			if (caption.IsEmpty())
				throw new ArgumentNullException("caption");

			foreach (var window in MdiWindow.AllChildWindows)
			{
				if (window.Title.CompareIgnoreCase(caption))
					return window;
			}

			if (throwException)
				throw new ArgumentException(LocalizedStrings.Str1829Params.Put(caption), "caption");
			else
				return null;
		}

		private SystemWindow GetTableWindowByClass(string className, bool throwException = true)
		{
			if (className.IsEmpty())
				throw new ArgumentNullException("className");

			var window = MdiWindow
				.FilterDescendantWindows(false, w => w.ClassName.CompareIgnoreCase(className))
				.FirstOrDefault();

			if (window == null && throwException)
				throw new ArgumentException(LocalizedStrings.Str1830Params.Put(className), "className");

			return window;
		}

		private void ThrowIfNotLaunched()
		{
			if (!IsLaunched)
				throw new InvalidOperationException(LocalizedStrings.Str1831);
		}

		private static SystemWindow GetQuikMainWindow(SystemWindow wnd)
		{
			if (wnd == null)
				throw new ArgumentNullException("wnd");

			if (wnd.HWnd == IntPtr.Zero)
				throw new ArgumentException(LocalizedStrings.Str1832, "wnd");

			return IsQuikMainWindow(wnd) ? wnd : GetQuikMainWindow(wnd.Parent);
		}

		private static bool IsQuikMainWindow(SystemWindow window)
		{
			if (window == null)
				throw new ArgumentNullException("window");

			return window.ClassName == "InfoClass";
		}

		private static IEnumerable<SystemWindow> GetQuikWindows(Process process)
		{
			if (process == null)
				throw new ArgumentException("process");

			//http://stocksharp.com/forum/yaf_postsm9060_Podkliuchieniie-k-Quik-i-zapusk-DDE.aspx#post9060
			return SystemWindow.FilterToplevelWindows(wnd => wnd.GetProcessId() == process.Id);
		}

		private static IEnumerable<SystemWindow> GetLoginWindows(IEnumerable<SystemWindow> quikWindows)
		{
			return quikWindows.Where(q => _loginWndTitles.Any(t => q.Title.ContainsIgnoreCase(t))).ToArray();
		}

		private IEnumerable<SystemWindow> QuikWindows
		{
			get { return GetQuikWindows(SystemProcess); }
		}

		private SystemWindow LoginWindow
		{
			get { return LoginWindows.FirstOrDefault(); }
		}

		private IEnumerable<SystemWindow> LoginWindows
		{
			get { return GetLoginWindows(QuikWindows); }
		}

		private IEnumerable<SystemWindow> DialogWindows
		{
			get
			{
				return QuikWindows.Where(wnd =>
				{
					try
					{
						return wnd.ClassName == "#32770";
					}
					// когда окно уже закрыто
					// http://stocksharp.com/forum/default.aspx?g=posts&m=4541#post4541 
					catch (Win32Exception)
					{
						return false;
					}
				});
			}
		}

		private IEnumerable<SystemWindow> DdeWindows
		{
			get { return DialogWindows.Where(q => q.Title.ContainsIgnoreCase(DdeWindowCaption)).ToArray(); }
		}

		private IEnumerable<SystemWindow> EditWindows
		{
			get { return DialogWindows.Where(q => q.Title.ContainsIgnoreCase(EditWindowCaption)).ToArray(); }
		}

		private IEnumerable<SystemWindow> AccountWindows
		{
			get { return DialogWindows.Where(q => q.Title.ContainsIgnoreCase(AccountWindowCaption)).ToArray(); }
		}

		private IEnumerable<SystemWindow> ConnectionWindows
		{
			get { return DialogWindows.Where(q => q.Title.ContainsIgnoreCase("Просмотр доступных соединений")).ToArray(); }
		}

		private IEnumerable<SystemWindow> TradeFilterWindows
		{
			get { return DialogWindows.Where(q => q.Title.ContainsIgnoreCase("фильтр ценных бумаг")).ToArray(); }
		}

		private void StartDde(DdeWindow ddeWindow, DdeSettings ddeSettings)
		{
			if (ddeWindow == null)
				throw new ArgumentNullException("ddeWindow");

			if (ddeSettings == null)
				throw new ArgumentNullException("ddeSettings");

			ddeWindow.DdeServer = SessionHolder.DdeServer;

			ddeWindow.Row = 1;
			ddeWindow.Column = 1;
			ddeWindow.StartFromRow = 1;

			ddeWindow.OutAfterCreate = false;
			ddeWindow.OutAfterCtrlShiftL = false;
			ddeWindow.RowsCaption = ddeSettings.RowsCaption;
			ddeWindow.ColumnsCaption = ddeSettings.ColumnsCaption;
			ddeWindow.FormalValues = ddeSettings.FormalValues;
			ddeWindow.EmptyCells = ddeSettings.EmptyCells;

			StopDdeOutput(ddeWindow);

			WaitFor(() =>
			{
				ddeWindow.Window.Command(ddeWindow.BeginOutBtn);
				return ddeWindow.BeginOutBtn.Enabled;
			}, "Запуск DDE вывода");

			CloseDde(ddeWindow);
		}

		private static void StopDdeOutput(DdeWindow ddeWindow)
		{
			if (ddeWindow == null)
				throw new ArgumentNullException("ddeWindow");

			if (!ddeWindow.BeginOutBtn.Enabled)
			{
                WaitFor(() =>
                {
                    ddeWindow.Window.Command(ddeWindow.StopOutBtn);
                    return !ddeWindow.BeginOutBtn.Enabled;
                }, "Остановка DDE вывода");
			}
		}

		private void StopDde(DdeWindow ddeWindow)
		{
			StopDdeOutput(ddeWindow);
			CloseDde(ddeWindow);
		}

		private void CloseDde(DdeWindow ddeWindow)
		{
			if (ddeWindow == null)
				throw new ArgumentNullException("ddeWindow");

			ddeWindow.Window.Command(ddeWindow.CloseBtn);
			WaitForClose(() => DdeWindows, "Закрытие DDE окна");
		}

		private IEnumerable<DdeTable> OrderBy(IEnumerable<DdeTable> ddeTables, bool isAscending)
		{
			if (ddeTables == null)
				throw new ArgumentNullException("ddeTables");

			return ddeTables.Select(t =>
			{
				var index = AllTables.IndexOf(t);

				if (index == -1)
					throw new InvalidOperationException(LocalizedStrings.Str1833Params.Put(t.Caption));

				return new { Index = index, Info = t };
			}).OrderBy(p => isAscending ? p.Index : 0 - p.Index).Select(p => p.Info);
		}

		/// <summary>
		/// Открыть окно DDE экспорта.
		/// </summary>
		/// <param name="window">Окно Quik, для которого необходимо открыть DDE окно.</param>
		/// <returns>Окно DDE экспорта.</returns>
		private DdeWindow OpenDdeWindow(SystemWindow window)
		{
			if (window == null)
				throw new ArgumentNullException("window");

			var menu = window.Parent.Parent.HWnd.GetMenu();

			var menuItemId = menu.GetSubMenu(1).GetMenuItemID(4);

			if (menuItemId == 0)
				menuItemId = menu.GetSubMenu(2).GetMenuItemID(4);

			window.PostMessage(WM.COMMAND, (int)menuItemId, 0);
			var systemWnd = WaitForOpen(() => DdeWindows, "Открытие DDE окна");
			systemWnd.VisibilityFlag = true;
			return new DdeWindow(systemWnd);
		}

		private void CloseAllDdeWindows()
		{
			DdeWindows.ForEach(wnd => CloseDde(new DdeWindow(wnd)));
		}

		private void CloseAllLoginWindows()
		{
			LoginWindows.ForEach(CloseLoginWindow);
		}

		private void CloseLoginWindow(SystemWindow loginWindow)
		{
			if (loginWindow == null)
				throw new ArgumentNullException("loginWindow");

			CloseCancel(loginWindow);
			WaitForClose(() => LoginWindows, "Закрытие окна подключений");
		}

		private void CloseAllEditWindows()
		{
			EditWindows.ForEach(wnd => CloseEditWindow(wnd, false));
		}

		private void CloseEditWindow(SystemWindow editWindow, bool isOk)
		{
			if (editWindow == null)
				throw new ArgumentNullException("editWindow");

			if (isOk)
				CloseOk(editWindow);
			else
				CloseCancel(editWindow);

			WaitForClose(() => EditWindows, "Закрытие окна настроек");
		}

		private SystemWindow OpenEditWindow(DdeTable table)
		{
			if (table == null)
				throw new ArgumentNullException("table");

			var window = GetTableWindow(table.Caption);
			return OpenEditWindow(window);
		}

		private SystemWindow OpenEditWindow(SystemWindow quoteWindow)
		{
			const int menuItemId = 303;
			quoteWindow.PostMessage(WM.COMMAND, menuItemId, 0);
			return WaitForOpen(() => EditWindows, "Открытие окна настроек");
		}

		private void CloseAllAccountWindows()
		{
			AccountWindows.ForEach(CloseAccountWindow);
		}

		private void CloseAccountWindow(SystemWindow accountWindow)
		{
			if (accountWindow == null)
				throw new ArgumentNullException("accountWindow");

			CloseCancel(accountWindow);
			WaitForClose(() => AccountWindows, "Закрытие окна счетов");
		}

		private void CloseAllConnectionWindows()
		{
			ConnectionWindows.ForEach(CloseConnectionWindow);
		}

		private void CloseConnectionWindow(SystemWindow connectionWindow)
		{
			if (connectionWindow == null)
				throw new ArgumentNullException("connectionWindow");

			CloseCancel(connectionWindow);
			WaitForClose(() => ConnectionWindows, "Закрытие окна соединений");
		}

		/// <summary>
		/// Добавить новый инструмент в фильтр таблицы Инструменты.
		/// </summary>
		/// <remarks>
		/// Данный метод работать только при включенном режиме <see cref="EnableFiltering"/>.
		/// </remarks>
		/// <param name="securityName">Название инструмента, по которому необходимо получать обновления.</param>
		public void RegisterSecurity(string securityName)
		{
			if (!EnableFiltering)
				return;

			lock (_winApiLock)
			{
				CloseAllEditWindows();

				var editWindow = OpenEditWindow(SessionHolder.SecuritiesTable);
				var securityTypesWnd = editWindow.AllChildWindows.First(e => e.DialogID == 10402);
				var securityTypesCtrl = securityTypesWnd.ToListBox();
				var addCtrl = editWindow.AllChildWindows.First(e => e.DialogID == 10404);

				var itemsCount = securityTypesCtrl.Count;
				var index = 0;
				var founded = false;

				while (index < itemsCount && !founded)
				{
					securityTypesCtrl.SelectMultiListBoxItem(index, true);
					securityTypesWnd.PostMessage(WM.KEYDOWN, (int) VirtualKeys.Space, 1);
					WaitFor(() => itemsCount == securityTypesWnd.ToListBox().Count, "Раскрытие класса инструментов");

					var newItemsCount = securityTypesCtrl.Count;
					// Очищаем выделение класса инструментов, чтоб не добавлять весь класс
					securityTypesCtrl.SelectMultiListBoxItem(index, false);
					for (var i = index + 1; i < newItemsCount; i++)
					{
						if (securityTypesCtrl[i].CompareIgnoreCase(securityName))
						{
							securityTypesCtrl.SelectMultiListBoxItem(i, true);
							editWindow.Command(addCtrl);
							founded = true;
							break;
						}
					}

					if (!founded)
					{
						securityTypesCtrl.SelectMultiListBoxItem(index, true);
						securityTypesWnd.PostMessage(WM.KEYDOWN, (int)VirtualKeys.Space, 1);
						WaitFor(() => newItemsCount == securityTypesWnd.ToListBox().Count, "Сокрытие класса инструментов");
					}

					index++;
				}

				CloseEditWindow(editWindow, founded);

				if (founded && IsDdeStarted(SessionHolder.SecuritiesTable))
				{
					WaitAndCloseDdeWindow();
				}
			}
		}

		/// <summary>
		/// Убрать инструмент из фильтра таблицы Инструменты.
		/// </summary>
		/// <remarks>
		/// Данный метод работать только при включенном режиме <see cref="EnableFiltering"/>.
		/// </remarks>
		/// <param name="securityName">Название инструмента, по которому необходимо прекратить получать обновления.</param>
		public void UnRegisterSecurity(string securityName)
		{
			if (!EnableFiltering)
				return;

			lock (_winApiLock)
			{
				CloseAllEditWindows();

				var editWindow = OpenEditWindow(SessionHolder.SecuritiesTable);

				var selectedSecuritiesCtrl = editWindow.AllChildWindows.First(e => e.DialogID == 10403).ToListBox();
				var removeCtrl = editWindow.AllChildWindows.First(e => e.DialogID == 10405);

				var index = -1;
				var itemsCount = selectedSecuritiesCtrl.Count;

				// Должен оставаться хотя бы 1 инструмент
				if (itemsCount == 1)
				{
					CloseEditWindow(editWindow, false);
					return;
				}

				for (var i = 0; i < itemsCount; i++)
				{
					if (selectedSecuritiesCtrl[i].ContainsIgnoreCase(securityName))
					{
						index = i;
						break;
					}
				}

				var founded = index != -1;
				if (founded)
				{
					selectedSecuritiesCtrl.SelectMultiListBoxItem(index, true);
					editWindow.Command(removeCtrl);
				}

				CloseEditWindow(editWindow, founded);

				if (founded && IsDdeStarted(SessionHolder.SecuritiesTable))
				{
					WaitAndCloseDdeWindow();
				}
			}
		}

		/// <summary>
		/// Добавить новый инструмент в фильтр таблицы Все Сделки.
		/// </summary>
		/// <remarks>
		/// Данный метод работать только при включенном режиме <see cref="EnableFiltering"/>.
		/// </remarks>
		/// <param name="securityName">Название инструмента, по которому необходимо получать тиковые сделки.</param>
		public void RegisterTrades(string securityName)
		{
			FilterSecurityTrades(securityName, 15501, 15503);
		}

		/// <summary>
		/// Убрать инструмент из фильтра таблицы Все Сделки.
		/// </summary>
		/// <remarks>
		/// Данный метод работать только при включенном режиме <see cref="EnableFiltering"/>.
		/// </remarks>
		/// <param name="securityName">Название инструмента, по которому необходимо прекратить получать тиковые сделки.</param>
		public void UnRegisterTrades(string securityName)
		{
			FilterSecurityTrades(securityName, 15502, 15504);
		}

		private void FilterSecurityTrades(string securityName, int securitiesListId, int buttonId)
		{
			if (!EnableFiltering)
				return;

			lock (_winApiLock)
			{
				CloseAllEditWindows();

				var editWindow = OpenEditWindow(SessionHolder.TradesTable);

				var securityTypesCtrl = editWindow.AllChildWindows.First(e => e.DialogID == 9000).ToListBox();
			    var itemsCount = securityTypesCtrl.Count;
			    var index = 0;
			    var founded = false;
			    
                while (index < itemsCount && !founded)
			    {
                    securityTypesCtrl.SelectListBoxItem(index);

                    var moreCtrl = editWindow.AllChildWindows.First(e => e.DialogID == 9002);
                    editWindow.PostMessage(WM.COMMAND, moreCtrl.DialogID, 0);

                    var moreWindow = WaitForOpen(() => TradeFilterWindows, "Фильтр инструментов");

                    var securitiesList = moreWindow.AllChildWindows.First(e => e.DialogID == securitiesListId).ToListBox();
                    var button = moreWindow.AllChildWindows.First(e => e.DialogID == buttonId);

					var secName = "{0} [{1}]".Put(securityName, securityTypesCtrl[index]);

                    var itemIndex = -1;

                    for (var i = 0; i < securitiesList.Count; i++)
                    {
						if (securitiesList[i].CompareIgnoreCase(secName))
                        {
                            itemIndex = i;
                            break;
                        }
                    }

                    founded = itemIndex != -1;

                    if (founded)
                    {
                        securitiesList.SelectMultiListBoxItem(itemIndex, true);
                        moreWindow.Command(button);
                        CloseOk(moreWindow);
                    }
                    else
                        CloseCancel(moreWindow);

                    WaitForClose(() => TradeFilterWindows, "Фильтр инструментов");
			        index++;
			    }

				CloseEditWindow(editWindow, founded);

				if (founded && IsDdeStarted(SessionHolder.TradesTable))
				{
					WaitAndCloseDdeWindow();
				}
			}
		}

		private void WaitAndCloseDdeWindow()
		{
			var wnd = WaitForOpen(() => DdeWindows, "Открытие DDE окна");
			var ddeWindow = new DdeWindow(wnd);
			WaitFor(() =>
			{
				ddeWindow.Window.Command(ddeWindow.BeginOutBtn);
				return ddeWindow.BeginOutBtn.Enabled;
			}, "Запуск DDE вывода");
			CloseDde(ddeWindow);
		}

		private static string GetDirectory(string value)
		{
			if (Path.GetExtension(value) != string.Empty)
				value = Path.GetDirectoryName(value);

			return value;
		}

		private static string GetFile(string value)
		{
			if (Path.GetExtension(value) == string.Empty)
				value = Path.Combine(value, _infoExe);

			return value;
		}

		private static SystemWindow WaitForOpen(Func<IEnumerable<SystemWindow>> getWindows, string action, int interval = 60)
		{
			if (getWindows == null)
				throw new ArgumentNullException("getWindows");

			SystemWindow window = null;
			WaitFor(() => (window = getWindows().FirstOrDefault()) == null, action, interval);
			return window;
		}

		private static void WaitForClose(Func<IEnumerable<SystemWindow>> getWindows, string action)
		{
			if (getWindows == null)
				throw new ArgumentNullException("getWindows");

			WaitFor(() => !getWindows().IsEmpty(), action);
		}

		private static void WaitFor(Func<bool> condition, string action, int interval = 60)
		{
			if (condition == null)
				throw new ArgumentNullException("condition");

			var now = DateTime.Now;

			while (condition())
			{
				Thread.Sleep(_wmDelay);

				if ((DateTime.Now - now) > TimeSpan.FromSeconds(interval))
					throw new TimeoutException(LocalizedStrings.Str1834Params.Put(action));
			}
		}

		private static void CloseOk(SystemWindow window)
		{
			CloseWindow(window, 1);
		}

		private static void CloseCancel(SystemWindow window)
		{
			CloseWindow(window, 2);
		}

		private static void CloseWindow(SystemWindow window, int id)
		{
			if (window == null)
				throw new ArgumentNullException("window");

			var btn = window.AllChildWindows.First(w => w.DialogID == id);
			window.Command(btn);
		}
	}
}
