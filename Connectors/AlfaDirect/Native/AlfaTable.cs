namespace StockSharp.AlfaDirect.Native
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	using ADLite;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Logging;
	using StockSharp.Messages;

	internal class AlfaTable
	{
		[ObfuscationAttribute(Exclude=true)]
		public enum TableName { papers, queue, fin_info, all_trades, orders, balance, trades, news, trade_places, accounts };

		public string Name { get; private set; }
		public FieldList Fields { get; private set; }

		private readonly HashSet<int> _activeFilter = new HashSet<int>();
		public ICollection<int> FilterPapers { get; private set; }

		private readonly bool _filtered;
		private readonly bool _supportInFilter;
		private readonly bool _supportUpdates;
		private readonly string _globalFilter;

		private readonly string _strFields;

		private readonly AlfaDirectClass _ad;
		private readonly ILogReceiver _logReceiver;

		private bool _isRegistered;

		public AlfaTable(AlfaDirectClass ad, ILogReceiver logReceiver, TableName name, string fields, string uniqueNames = null)
		{
			_ad = ad;
			_logReceiver = logReceiver;
			Name = name.ToString();
			Fields = new FieldList(this, logReceiver, fields, uniqueNames);
			_strFields = Fields.Names.Join(",");

			_supportInFilter = name != TableName.queue;
			_supportUpdates = name != TableName.queue && name != TableName.balance;
			_globalFilter =
				name == TableName.all_trades ? "AT" :
				name == TableName.fin_info ? "FI" :
				name == TableName.queue ? "Q" :
				null;

			_filtered = _globalFilter != null;

			if(_filtered)
				FilterPapers = new HashSet<int>();
		}

		public void ReRegisterTable()
		{
			if (!_filtered)
			{
				if (_isRegistered)
					Log("already registered");
				else
					Subscribe(string.Empty, Enumerable.Empty<int>(), false);

				return;
			}

			UpdateGlobalFilter();

			if (_supportInFilter)
			{
				if (FilterPapers.Count > 0)
					Subscribe("paper_no in ({0})".Put(string.Join(",", FilterPapers)), FilterPapers, true);
				else if (_isRegistered)
					UnRegisterTable();

				return;
			}

			var oldPapers = _activeFilter.Except(FilterPapers).ToList();
			var newPapers = FilterPapers.Except(_activeFilter).ToList();

			foreach (var paperNo in oldPapers)
				Unsubscribe("paper_no = {0}".Put(paperNo), new[] { paperNo }, false);

			foreach (var paperNo in newPapers)
				Subscribe("paper_no = {0}".Put(paperNo), new[] { paperNo }, false);

			_isRegistered = _activeFilter.Count > 0;
		}

		public void UnRegisterTable()
		{
			if (!_isRegistered)
			{
				Log("unregister: not registered");
				return;
			}

			if (!_filtered || _supportInFilter)
				Unsubscribe(string.Empty, _activeFilter, true);
			else
				_activeFilter.ToList().ForEach(paperNo => Unsubscribe("paper_no = {0}".Put(paperNo), new[] { paperNo }, false));

			UpdateGlobalFilter();
		}

		private void Subscribe(string where, IEnumerable<int> paperNumbers, bool replace)
		{
			string message;
			var result = _ad.SubscribeTable(Name, _strFields, where, _supportUpdates ? eSubsctibeOptions.UpdatesOnly : eSubsctibeOptions.Default, out message);
			Log("subscribe {0}: {1}", where, message);
			if (result == tagStateCodes.stcSuccess)
			{
				_isRegistered = true;
				if (replace)
					_activeFilter.Clear();
				_activeFilter.AddRange(paperNumbers.ToList());
			}
			ThrowInError(result, message);
		}

		private void Unsubscribe(string where, ICollection<int> paperNumbers, bool clear)
		{
			string message;
			Log("unsubscribe {0}", !where.IsEmpty() ? where : paperNumbers.Count.To<string>());
			var result = _ad.UnSubscribeTable(Name, where, out message);
			if (result == tagStateCodes.stcSuccess)
			{
				if(!_filtered)
					_isRegistered = false;
				else
				{
					var toremove = paperNumbers.ToList();
					_activeFilter.RemoveRange(toremove);
					FilterPapers.RemoveRange(toremove);
					if (clear)
					{
						FilterPapers.Clear();
						_activeFilter.Clear();
					}

					if (!_activeFilter.Any())
						_isRegistered = false;
				}
			}
			ThrowInError(result, message);
		}

		public string[] GetLocalDbData(IEnumerable<int> papers)
		{
			return !papers.Any() ? ArrayHelper<string>.EmptyArray : GetLocalDbData("paper_no in ({0})".Put(string.Join(",", papers)));
		}

		public string[] GetLocalDbData(string where = null)
		{
			if (where == null)
				where = _filtered && _activeFilter.Any() ? 
					"paper_no in ({0})".Put(string.Join(",", _activeFilter)) : string.Empty;

			var res = _ad.GetLocalDBData(Name, _strFields, where);
			Log("GetLocalDbData {0}: {1}", where, res);
			// сразу после старта терминала АД может вернуть null (баг в терминале)
			return res == null ? ArrayHelper<string>.EmptyArray : res.ToRows();
		}

		private void ThrowInError(tagStateCodes code, string message = null)
		{
			if (code != tagStateCodes.stcSuccess)
			{
				var msg = "{0}: ERROR({1}): {2}".Put(Name, code, !message.IsEmpty() ? message : _ad.LastResultMsg);
				_logReceiver.AddErrorLog(msg);
				throw new AlfaException(code, msg);
			}
		}

		private void UpdateGlobalFilter()
		{
			if (_globalFilter == null)
				return;

			var oldFilter = _ad.GlobalFilter[_globalFilter];
			_ad.GlobalFilter[_globalFilter] = string.Join("|", FilterPapers);

			Log("GlobalFilter[{0}]: {1} => {2}", _globalFilter, oldFilter, _ad.GlobalFilter[_globalFilter]);
		}

		private void Log(string format, params object[] pars)
		{
			_logReceiver.AddDebugLog("{0}: ".Put(Name) + format, pars);
		}
	}

	internal class FieldList
	{
		public string[] Names {get; private set;}
		readonly ILogReceiver _logReceiver;

		public FieldList(AlfaTable table, ILogReceiver logReceiver, string fields, string uniqueIds = null)
		{
			Table = table;
			_logReceiver = logReceiver;

			Names = fields.Split(",").Select(f => f.Trim()).Where(f => !f.IsEmpty()).ToArray();
			var ids = uniqueIds == null ? Enumerable.Empty<string>() : uniqueIds.Split(",").Select(f => f.Trim()).Where(f => !f.IsEmpty());

			var checkIds = new HashSet<string>();
			var allFields = new Dictionary<string, List<Field>>();

			foreach (var info in _definedFields)
			{
				var field = (Field)info.GetValue(this);

				if(checkIds.Contains(field.Id))
					throw new InvalidOperationException("non unique id: {0}".Put(field.Id));
				checkIds.Add(field.Id);

				var list = allFields.TryGetValue(field.Name);
				if (list == null)
					allFields.Add(field.Name, list = new List<Field>());

				list.Add(field);
			}

			for (var i = 0; i < Names.Length; ++i)
			{
				var list = allFields[Names[i]];
				var field = list.Count == 1 ? list.First() : list.First(f => ids.Contains(f.Id));

				field.Init(this, i);
			}

			allFields.Values.SelectMany(l => l).Where(f => f.List == null).ForEach(f => f.Init(this));
		}

		public AlfaTable Table { get; private set; }

		#region fields definitions

		public readonly Field<int> PaperNo                 = new Field<int>("paper_no");
		public readonly Field<int> BasePaperNo             = new Field<int>("base_paper_no");
		public readonly Field<int> LotSize                 = new Field<int>("lot_size");
		public readonly Field<int> Qty                     = new Field<int>("qty");
		public readonly Field<int> BuyQty                  = new Field<int>("buy_qty");
		public readonly Field<int> SellQty                 = new Field<int>("sell_qty");
		public readonly Field<int> BuySQty                 = new Field<int>("buy_sqty");
		public readonly Field<int> SellSQty                = new Field<int>("sell_sqty");
		public readonly Field<int> BuyCount                = new Field<int>("buy_count");
		public readonly Field<int> SellCount               = new Field<int>("sell_count");
		public readonly Field<int> LastQty                 = new Field<int>("last_qty");
		public readonly Field<int> OpenPosQty              = new Field<int>("open_pos_qty");
		public readonly Field<int> Rest                    = new Field<int>("rest");
		public readonly Field<int> Treaty                  = new Field<int>("treaty");
		public readonly Field<int> OrdNo                   = new Field<int>("ord_no");

		public readonly Field<long> TrdNo                  = new Field<long>("trd_no");
		public readonly Field<long> Comments               = new FieldTransactionId("comments");

		public readonly Field<decimal> GoBuy               = new Field<decimal>("go_buy");
		public readonly Field<decimal> GoSell              = new Field<decimal>("go_sell");
		public readonly Field<decimal> PriceStep           = new Field<decimal>("price_step");
		public readonly Field<decimal> PriceStepCost       = new Field<decimal>("price_step_cost");
		public readonly Field<decimal> Strike              = new Field<decimal>("strike");
		public readonly Field<decimal> Price               = new Field<decimal>("price");
		public readonly Field<decimal> OpenPrice           = new Field<decimal>("open_price");
		public readonly Field<decimal> ClosePrice          = new Field<decimal>("close_price");
		public readonly Field<decimal> Sell                = new Field<decimal>("sell");
		public readonly Field<decimal> Buy                 = new Field<decimal>("buy");
		public readonly Field<decimal> MinDeal             = new Field<decimal>("min_deal");
		public readonly Field<decimal> MaxDeal             = new Field<decimal>("max_deal");
		public readonly Field<decimal> Volatility          = new Field<decimal>("volatility");
		public readonly Field<decimal> TheorPrice          = new Field<decimal>("theor_price");
		public readonly Field<decimal> LastPrice           = new Field<decimal>("last_price");
		public readonly Field<decimal> StopPrice           = new Field<decimal>("stop_price");
		public readonly Field<decimal> AvgTrdPrice         = new Field<decimal>("avg_trd_price");
		public readonly Field<decimal> UpdateGrowPrice     = new Field<decimal>("updt_grow_price");
		public readonly Field<decimal> UpdateDownPrice     = new Field<decimal>("updt_down_price");
		public readonly Field<decimal> UpdateNewPrice      = new Field<decimal>("updt_new_price");
		public readonly Field<decimal> TrailingLevel       = new Field<decimal>("trailing_level");
		public readonly Field<decimal> TrailingSlippage    = new Field<decimal>("trailing_slippage");
		public readonly Field<decimal> IncomeRest          = new Field<decimal>("income_rest");
		public readonly Field<decimal> RealRest            = new Field<decimal>("real_rest");
		public readonly Field<decimal> ForwordRest         = new Field<decimal>("forword_rest");
		public readonly Field<decimal> PnL                 = new Field<decimal>("pl");
		public readonly Field<decimal> ProfitVol           = new Field<decimal>("profit_vol");
		public readonly Field<decimal> IncomeVol           = new Field<decimal>("income_vol");
		public readonly Field<decimal> RealVol             = new Field<decimal>("real_vol");
		public readonly Field<decimal> OpenVol             = new Field<decimal>("open_vol");
		public readonly Field<decimal> VarMargin           = new Field<decimal>("var_margin");
		public readonly Field<decimal> BalancePrice        = new Field<decimal>("balance_price");

		public readonly Field<string> NewNo                = new Field<string>("new_no");
		public readonly Field<string> AccCode              = new Field<string>("acc_code");
		public readonly Field<string> PaperCode            = new Field<string>("p_code");
		public readonly Field<string> AnsiName             = new Field<string>("ansi_name");
		public readonly Field<string> PlaceCode            = new Field<string>("place_code");
		public readonly Field<string> PlaceName            = new Field<string>("place_name");
		public readonly Field<string> ExCode               = new Field<string>("ex_code");
		public readonly Field<string> OrderStatus          = new Field<string>("status", "order_status");
		public readonly Field<string> Provider             = new Field<string>("provider");
		public readonly Field<string> Subject              = new Field<string>("subject");
		public readonly Field<string> Body                 = new Field<string>("body");
		public readonly Field<string> Blank                = new Field<string>("blank");

		public readonly Field<DateTime> LastUpdateDate     = new Field<DateTime>("last_update_date");
		public readonly Field<DateTime> LastUpdateTime     = new Field<DateTime>("last_update_time");
		public readonly Field<DateTime> TsTime             = new Field<DateTime>("ts_time");
		public readonly Field<DateTime> DbData             = new Field<DateTime>("db_data");
		public readonly Field<DateTime> MatDate            = new FieldDateTimeWithMax("mat_date");
		public readonly Field<DateTime> ILastUpdate        = new FieldTimestamp("i_last_update");

		public readonly Field<bool> Expired				   = new FieldYesNo("expired");

		public readonly Field<SecurityStates> TradingStatus= new FieldSecurityState("status", "trading_status");

		public readonly Field<CurrencyTypes> CurrCode      = new FieldCurrency("curr_code");

		public readonly Field<Sides> BuySellNum            = new FieldBuySell("b_s", true, "b_s_num");
		public readonly Field<Sides> BuySellStr            = new FieldBuySell("b_s", false, "b_s_str");

		public readonly Field<SecurityTypes?> ATCode       = new FieldSecurityType("at_code");
		#endregion

		static readonly FieldInfo[] _definedFields;
		static FieldList()
		{
			_definedFields = typeof(FieldList).GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public).Where(f => f.FieldType.IsSubclassOf(typeof(Field))).ToArray();
		}

		#region field types

		public class Field
		{
			public string Id { get; private set; }
			public string Name { get; private set; }
			public int Index { get; private set; }
			public FieldList List { get; private set; }

			protected Field(string name, string uniqueName = null)
			{
				Index = -1;
				Id = uniqueName ?? name;
				Name = name;
			}

			public void Init(FieldList list, int index = -1)
			{
				if (List != null)
					throw new InvalidOperationException("already initialized");
				List = list;
				Index = index;
			}
		}

		public class Field<T> : Field
		{
			public Field(string name, string uniqueName = null) : base(name, uniqueName) {}

			public T GetValue(string[] columns, T ifEmpty)
			{
				var str = columns[Index];
				if(str == null || str.Trim() == string.Empty)
					return ifEmpty;
				return GetValue(columns);
			}

			public T GetValue(string[] columns)
			{
				var str = columns[Index];
				try
				{
					return GetValueInternal(str);
				}
				catch(Exception e)
				{
					List._logReceiver.AddErrorLog("error getting value {0}=({1})columns[{2}], count={3}, str={4}, error={5}", Name, typeof(T).Name, Index, columns.Length, str, e);
					return default(T);
				}
			}

			public string GetStrValue(string[] columns, bool trim = true)
			{
				var str = columns[Index];
				return trim ? str.Trim() : str;
			}

			protected virtual T GetValueInternal(string str)
			{
				return str.To<T>();
			}
		}

		class FieldYesNo : Field<bool>
		{
			public FieldYesNo(string name, string uniqueName = null) : base(name, uniqueName) {}

			protected override bool GetValueInternal(string str)
			{
				return str.CompareIgnoreCase("Y");
			}
		}

		class FieldDateTimeWithMax : Field<DateTime>
		{
			static readonly DateTime _maxDate;

			static FieldDateTimeWithMax() { _maxDate = DateTime.Now + TimeSpan.FromDays(365 * 10); }

			public FieldDateTimeWithMax(string name, string uniqueName = null) : base(name, uniqueName) {}

			protected override DateTime GetValueInternal(string str)
			{
				var dt = str.To<DateTime>();
				return dt < _maxDate ? dt : DateTime.MaxValue;
			}
		}

		class FieldTimestamp : Field<DateTime>
		{
			static readonly DateTime _epochStart = new DateTime(1999, 1, 1, 0, 0, 0, 0);

			public FieldTimestamp(string name, string uniqueName = null) : base(name, uniqueName) {}

			protected override DateTime GetValueInternal(string str)
			{
				return _epochStart.AddSeconds(str.To<int>());
			}
		}

		class FieldSecurityState : Field<SecurityStates>
		{
			public FieldSecurityState(string name, string uniqueName = null) : base(name, uniqueName) {}

			protected override SecurityStates GetValueInternal(string str)
			{
				return str.To<int>() == 6 ? SecurityStates.Trading : SecurityStates.Stoped;
			}
		}

		class FieldCurrency : Field<CurrencyTypes>
		{
			public FieldCurrency(string name, string uniqueName = null) : base(name, uniqueName) {}

			protected override CurrencyTypes GetValueInternal(string str)
			{
				return str.CompareIgnoreCase("RUR") ? CurrencyTypes.RUB : str.To<CurrencyTypes>();
			}
		}

		class FieldTransactionId : Field<long>
		{
			public FieldTransactionId(string name, string uniqueName = null) : base(name, uniqueName) {}

			protected override long GetValueInternal(string str)
			{
				long transId;
				return long.TryParse(str, out transId) ? transId : 0L;
			}
		}

		class FieldBuySell : Field<Sides>
		{
			readonly bool _isNumeric;

			public FieldBuySell(string name, bool isNumeric, string uniqueName = null) : base(name, uniqueName)
			{
				_isNumeric = isNumeric;
			}

			protected override Sides GetValueInternal(string str)
			{
				return _isNumeric ?
					str.To<int>() == 1 ? Sides.Sell : Sides.Buy :
					str.CompareIgnoreCase("B") ? Sides.Buy : Sides.Sell;
			}
		}

		class FieldSecurityType : Field<SecurityTypes?>
		{
			public FieldSecurityType(string name, string uniqueName = null) : base(name, uniqueName) {}

			protected override SecurityTypes? GetValueInternal(string str)
			{
				switch (str)
				{
					case "A": // а.о.
					case "P": // а.п.
						return SecurityTypes.Stock;
					case "FC": // расчетный
					case "FD": // поставочный
						return SecurityTypes.Future;
					case "I":
						return SecurityTypes.Index;
					case "OCM": // марж. колл
					case "OPM": // марж. пут
						return SecurityTypes.Option;
				}
				return null;
			}
		}

		#endregion
	}
}
