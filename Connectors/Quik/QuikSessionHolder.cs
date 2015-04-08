namespace StockSharp.Quik
{
	using System;
	using System.ComponentModel;
	using System.Net;
	using System.Security;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Localization;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Fix;
	using StockSharp.Messages;
	using StockSharp.Quik.Xaml;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Контейнер для сессии.
	/// </summary>
	[DisplayName("Quik")]
	[CategoryLoc(LocalizedStrings.Str1769Key)]
	[DescriptionLoc(LocalizedStrings.Str1770Key)]
	[CategoryOrderLoc(LocalizedStrings.Str1771Key, 0)]
	[CategoryOrder(_luaCategory, 1)]
	[CategoryOrder(_ddeCategory, 2)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 3)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 4)]
	public class QuikSessionHolder : FixSessionHolder
	{
		private const string _ddeCategory = "DDE";
		private const string _luaCategory = "LUA";

		internal event Action IsLuaChanged;
		private bool _isDde;

		/// <summary>
		/// Использовать для старое подключение DDE + Trans2Quik. По-умолчанию выключено.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1771Key)]
		[DisplayName("DDE")]
		[DescriptionLoc(LocalizedStrings.Str1772Key)]
		[PropertyOrder(0)]
		public bool IsDde
		{
			get { return _isDde; }
			set
			{
				if (_isDde == value)
					return;

				_isDde = value;
				IsLuaChanged.SafeInvoke();
			}
		}

		private string _path;

		/// <summary>
		/// Путь к директории, где установлен Quik (или путь к файлу info.exe).
		/// По-умолчанию равно <see cref="QuikTerminal.GetDefaultPath"/>.
		/// </summary>
		[Category(_ddeCategory)]
		[DisplayNameLoc(LocalizedStrings.Str1773Key)]
		[DescriptionLoc(LocalizedStrings.Str1774Key)]
		[PropertyOrder(0)]
		[Editor(typeof(FolderBrowserEditor), typeof(FolderBrowserEditor))]
		public string Path
		{
			get { return _path; }
			set
			{
				if (Path == value)
					return;

				Terminal = null;

				_path = value;
			}
		}

		private FixSession _transactionSession = new FixSession
		{
			
		};

		/// <summary>
		/// Транзакционная сессия.
		/// </summary>
		[Category(_luaCategory)]
		[DisplayNameLoc(LocalizedStrings.TransactionsKey)]
		[DescriptionLoc(LocalizedStrings.TransactionalSessionKey)]
		[PropertyOrder(0)]
		public override FixSession TransactionSession
		{
			get { return _transactionSession; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_transactionSession = value;
			}
		}

		private FixSession _marketDataSession = new FixSession
		{
			
		};

		/// <summary>
		/// Маркет-дата сессия.
		/// </summary>
		[Category(_luaCategory)]
		[DisplayNameLoc(LocalizedStrings.MarketDataKey)]
		[DescriptionLoc(LocalizedStrings.MarketDataSessionKey)]
		[PropertyOrder(1)]
		public override FixSession MarketDataSession
		{
			get { return _marketDataSession; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_marketDataSession = value;
			}
		}

		

		

		/// <summary>
		/// Создать <see cref="QuikSessionHolder"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public QuikSessionHolder(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			Path = QuikTerminal.GetDefaultPath();

			IsTransactionEnabled = true;
			IsMarketDataEnabled = true;
		}

		

		

		internal event Action TerminalChanged;


		/// <summary>
		/// Вспомогательный класс для управления терминалом Quik.
		/// </summary>
		[Browsable(false)]
		public QuikTerminal Terminal
		{
			
		}

		/// <summary>
		/// Получить строковое представление контейнера.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return IsDde
				? LocalizedStrings.Str1808Params.Put(Path)
				: base.ToString();
		}
	}
}