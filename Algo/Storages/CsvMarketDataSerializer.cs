namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Text;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection;
	using Ecng.Reflection.Path;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	class CsvMarketDataSerializer<TData> : IMarketDataSerializer<TData>
	{
		class CsvMetaInfo : MetaInfo<CsvMetaInfo>
		{
			private readonly Encoding _encoding;

			public CsvMetaInfo(DateTime date, Encoding encoding)
				: base(date)
			{
				_encoding = encoding;
			}

			public override CsvMetaInfo Clone()
			{
				return new CsvMetaInfo(Date, _encoding)
				{
					Count = Count,
					FirstTime = FirstTime,
					LastTime = LastTime,
					PriceStep = PriceStep,
					VolumeStep = VolumeStep,
				};
			}

			public override void Write(Stream stream)
			{
			}

			public override void Read(Stream stream)
			{
				CultureInfo.InvariantCulture.DoInCulture(() =>
				{
					var count = 0;
					string firstLine = null;
					string lastLine = null;

					foreach (var line in stream.EnumerateLines())
					{
						if (firstLine == null)
							firstLine = line;

						lastLine = line;
						count++;
					}

					Count = count;

					if (firstLine != null)
					{
						FirstTime = ParseTime(firstLine.Split(';')[0], Date).UtcDateTime;
						LastTime = ParseTime(lastLine.Split(';')[0], Date).UtcDateTime;
					}

					stream.Position = 0;
				});
			}
		}

		private class CsvReader : SimpleEnumerator<TData>
		{
			private readonly IEnumerator<string> _enumerator;
			private readonly Stream _stream;
			private readonly SecurityId _securityId;
			private readonly DateTime _date;
			private readonly ExecutionTypes? _executionType;
			private readonly MemberProxy[] _members;

			public CsvReader(Stream stream, Encoding encoding, SecurityId securityId, DateTime date, ExecutionTypes? executionType, MemberProxy[] members)
			{
				if (stream == null)
					throw new ArgumentNullException("stream");

				_enumerator = stream.EnumerateLines(encoding).GetEnumerator();
				_stream = stream;
				_securityId = securityId;
				_date = date.ChangeKind(DateTimeKind.Utc);
				_executionType = executionType;
				_members = members;
			}

			public override bool MoveNext()
			{
				var retVal = _enumerator.MoveNext();

				if (!retVal)
					return false;

				var line = _enumerator.Current;

				Current = CultureInfo.InvariantCulture.DoInCulture(() =>
				{
					var parts = line.Split(';');

					var item = _ctor.Ctor(null);

					var time = ParseTime(parts[0], _date);

					if (_isLevel1)
					{
						var l1 = item.To<Level1ChangeMessage>();

						l1.ServerTime = time;

						for (var i = 1; i < parts.Length; i++)
						{
							var part = parts[i];

							if (part.IsEmpty())
								continue;

							var field = _level1Fields[i - 1];
							object value;

							switch (field)
							{
								case Level1Fields.BestAskTime:
								case Level1Fields.BestBidTime:
								case Level1Fields.LastTradeTime:
									value = ParseTime(part, _date);
									break;
								case Level1Fields.AsksCount:
								case Level1Fields.BidsCount:
								case Level1Fields.TradesCount:
									value = part.To<int>();
									break;
								case Level1Fields.LastTradeId:
									value = part.To<long>();
									break;
								case Level1Fields.LastTradeOrigin:
									value = part.To<Sides>();
									break;
								case Level1Fields.LastTradeUpDown:
								case Level1Fields.IsSystem:
									value = part.To<bool>();
									break;
								case Level1Fields.State:
									value = part.To<SecurityStates>();
									break;
								default:
									value = part.To<decimal>();
									break;
							}

							l1.Changes.Add(field, value);
						}
					}
					else
					{
						_dateMember.SetValue(item, time);

						for (var i = 1; i < parts.Length; i++)
						{
							if (parts[i].IsEmpty())
								continue;

							_members[i].SetValue(item, parts[i].To(_members[i].ReturnType));
						}

						if (_setSecurityId != null)
							_setSecurityId.SetValue(item, _securityId);

						if (_executionType != null)
							_setExecutionType.SetValue(item, _executionType.Value);
	
					}
					
					return item;
				});

				return true;
			}

			public override void Reset()
			{
				_enumerator.Reset();
				base.Reset();
			}

			protected override void DisposeManaged()
			{
				_enumerator.Dispose();
				_stream.Dispose();

				base.DisposeManaged();
			}
		}

		// ReSharper disable StaticFieldInGenericType
		private static readonly MemberProxy _setSecurityId;
		private static readonly MemberProxy _setExecutionType;
		private static readonly FastInvoker<VoidType, VoidType, TData> _ctor;
		private const string _timeFormat = "HHmmssffffff";
		private static readonly SynchronizedDictionary<Tuple<Type, ExecutionTypes?>, MemberProxy[]> _info = new SynchronizedDictionary<Tuple<Type, ExecutionTypes?>, MemberProxy[]>();
		private static readonly bool _isLevel1 = typeof(TData) == typeof(Level1ChangeMessage);
		private static readonly Level1Fields[] _level1Fields = _isLevel1 ? Enumerator.GetValues<Level1Fields>().Where(l1 => l1 != Level1Fields.ExtensionInfo && l1 != Level1Fields.BestAsk && l1 != Level1Fields.BestBid && l1 != Level1Fields.LastTrade).OrderBy(l1 => (int)l1).ToArray() : null;
		private static readonly MemberProxy _dateMember;
		// ReSharper restore StaticFieldInGenericType

		static CsvMarketDataSerializer()
		{
			if (typeof(TData) == typeof(ExecutionMessage) || typeof(TData).IsSubclassOf(typeof(CandleMessage)))
				_setSecurityId = MemberProxy.Create(typeof(TData), "SecurityId");

			if (typeof(TData) == typeof(ExecutionMessage))
				_setExecutionType = MemberProxy.Create(typeof(TData), "ExecutionType");

			_ctor = FastInvoker<VoidType, VoidType, TData>.Create(typeof(TData).GetMember<ConstructorInfo>());

			_dateMember = MemberProxy.Create(typeof(TData),
				typeof(TData).IsSubclassOf(typeof(CandleMessage)) ? "OpenTime" : "ServerTime");
		}

		private readonly Encoding _encoding;
		private readonly ExecutionTypes? _executionType;
		private readonly string _format;
		private readonly MemberProxy[] _members;

		public CsvMarketDataSerializer(Encoding encoding = null)
			: this(default(SecurityId), null, encoding)
		{
		}

		public CsvMarketDataSerializer(SecurityId securityId, ExecutionTypes? executionType, Encoding encoding = null)
		{
			if (securityId.IsDefault() && typeof(TData) != typeof(NewsMessage))
				throw new ArgumentNullException("securityId");

			SecurityId = securityId;
			_executionType = executionType;
			_encoding = encoding ?? Encoding.UTF8;

			if (typeof(TData) == typeof(QuoteChangeMessage))
				return;

			_format = GetFormat(executionType).Replace(":{0}", ".UtcDateTime:" + _timeFormat);

			if (_isLevel1)
				return;

			const string timeFormat = ":" + _timeFormat;

			_members = _info.SafeAdd(Tuple.Create(typeof(TData), executionType), key =>
				_format
					.Split(';')
					.Select(s =>
						MemberProxy.Create(typeof(TData),
							s.Substring(1, s.Length - 2).Replace(timeFormat, string.Empty)))
					.ToArray());
		}

		public SecurityId SecurityId { get; private set; }

		private static string GetFormat(ExecutionTypes? executionType)
		{
			if (typeof(TData) == typeof(ExecutionMessage))
			{
				switch (executionType)
				{
					case ExecutionTypes.Tick:
						return "{ServerTime:{0}};{TradeId};{TradePrice};{Volume};{OriginSide};{OpenInterest};{IsSystem}";
					case ExecutionTypes.OrderLog:
						return "{ServerTime:{0}};{IsSystem};{OrderId};{Price};{Volume};{Side};{OrderState};{TimeInForce};{TradeId};{TradePrice};{PortfolioName}";
					case null:
						throw new ArgumentNullException("executionType");
					default:
						throw new ArgumentOutOfRangeException("executionType");
				}
			}
			
			if (typeof(TData) == typeof(TimeQuoteChange))
				return "{ServerTime:{0}};{Price};{Volume};{Side}";

			if (typeof(TData) == typeof(Level1ChangeMessage))
			{
				var fields = _level1Fields.Select(s =>
				{
					string time = null;

					switch (s)
					{
						case Level1Fields.BestAskTime:
						case Level1Fields.BestBidTime:
						case Level1Fields.LastTradeTime:
							time = ".UtcDateTime:" + _timeFormat;
							break;
					}

					return "{" + s + time + "}";
				}).Join(";");

				return "{ServerTime:{0}};" + "{{Changes:{0}}}".Put(fields);
			}

			if (typeof(TData).IsSubclassOf(typeof(CandleMessage)))
				return "{OpenTime:{0}};{OpenPrice};{HighPrice};{LowPrice};{ClosePrice};{TotalVolume}";

			if (typeof(TData) == typeof(NewsMessage))
			{
				// NewsMessage.Story do not supported
				// TODO ;{SecurityId.Value.SecurityCode}
				return "{ServerTime:{0}};{Headline};{Source};{Url};{Id};{BoardCode}";
			}

			throw new InvalidOperationException(LocalizedStrings.Str888Params.Put(typeof(TData).Name));
		}

		public virtual IMarketDataMetaInfo CreateMetaInfo(DateTime date)
		{
			return new CsvMetaInfo(date, _encoding);
		}

		byte[] IMarketDataSerializer.Serialize(IEnumerable data, IMarketDataMetaInfo metaInfo)
		{
			return Serialize(data.Cast<TData>(), metaInfo);
		}

		IEnumerableEx IMarketDataSerializer.Deserialize(Stream stream, IMarketDataMetaInfo metaInfo)
		{
			return Deserialize(stream, metaInfo);
		}

		public virtual byte[] Serialize(IEnumerable<TData> data, IMarketDataMetaInfo metaInfo)
		{
			return CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				var sb = new StringBuilder();

				var appendLine = metaInfo.Count > 0;

				foreach (var item in data)
				{
					if (appendLine)
						sb.AppendLine();
					else
						appendLine = true;

					sb.Append(_format.PutEx(item));
				}

				return _encoding.GetBytes(sb.ToString());
			});
		}

		public virtual IEnumerableEx<TData> Deserialize(Stream stream, IMarketDataMetaInfo metaInfo)
		{
			// TODO (переделать в будущем)
			var copy = new MemoryStream();
			stream.CopyTo(copy);
			copy.Position = 0;

			stream.Dispose();

			return new SimpleEnumerable<TData>(() =>
				new CsvReader(copy, _encoding, SecurityId, metaInfo.Date.Date, _executionType, _members))
				.ToEx(metaInfo.Count);
		}

		private static DateTimeOffset ParseTime(string str, DateTime date)
		{
			return (date + str.ToDateTime(_timeFormat).TimeOfDay).ChangeKind(DateTimeKind.Utc);
		}
	}
}