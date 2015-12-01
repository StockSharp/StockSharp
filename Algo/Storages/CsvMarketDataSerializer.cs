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

	using SmartFormat;
	using SmartFormat.Core.Formatting;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The serializer in the CSV format.
	/// </summary>
	/// <typeparam name="TData">Data type.</typeparam>
	public class CsvMarketDataSerializer<TData> : IMarketDataSerializer<TData>
	{
		class CsvMetaInfo : MetaInfo
		{
			//private readonly Encoding _encoding;
			private readonly Func<string[], object> _toId;

			public CsvMetaInfo(DateTime date, /*Encoding encoding,*/ Func<string[], object> toId)
				: base(date)
			{
				//_encoding = encoding;
				_toId = toId;
			}

			//public override CsvMetaInfo Clone()
			//{
			//	return new CsvMetaInfo(Date, _encoding, _toId)
			//	{
			//		Count = Count,
			//		FirstTime = FirstTime,
			//		LastTime = LastTime,
			//		PriceStep = PriceStep,
			//		VolumeStep = VolumeStep,
			//	};
			//}

			private object _lastId;

			public override object LastId => _lastId;

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
						FirstTime = ParseTime(firstLine.Split(';'), Date).UtcDateTime;
						LastTime = ParseTime(lastLine.Split(';'), Date).UtcDateTime;

						if (_toId != null)
							_lastId = _toId(lastLine.Split(';'));
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
			private readonly object _candleArg;
			private readonly MemberProxy[] _members;

			public CsvReader(Stream stream, Encoding encoding, SecurityId securityId, DateTime date, ExecutionTypes? executionType, object candleArg, MemberProxy[] members)
			{
				if (stream == null)
					throw new ArgumentNullException(nameof(stream));

				_enumerator = stream.EnumerateLines(encoding).GetEnumerator();
				_stream = stream;
				_securityId = securityId;
				_date = date.ChangeKind(DateTimeKind.Utc);
				_executionType = executionType;
				_candleArg = candleArg;
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

					var time = ParseTime(parts, _date);

					if (_isLevel1)
					{
						var l1 = item.To<Level1ChangeMessage>();

						l1.ServerTime = time;
						l1.SecurityId = _securityId;

						for (var i = _skipColumns; i < parts.Length; i++)
						{
							var part = parts[i];

							if (part.IsEmpty())
								continue;

							var field = _level1Fields[i - _skipColumns];
							object value;

							switch (field)
							{
								case Level1Fields.BestAskTime:
								case Level1Fields.BestBidTime:
								case Level1Fields.LastTradeTime:
									value = part.To<DateTimeOffset>();
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

						for (var i = _skipColumns; i < parts.Length; i++)
						{
							if (parts[i].IsEmpty())
								continue;

							var member = _members[i - _skipColumns];

							if (_isNews && i == 7)
								member.SetValue(item, new SecurityId { SecurityCode = parts[i] });
							else
								member.SetValue(item, parts[i].To(member.ReturnType));
						}

						if (_setSecurityId != null)
							_setSecurityId.SetValue(item, _securityId);

						if (_executionType != null)
							_setExecutionType.SetValue(item, _executionType.Value);

						if (_candleArg != null)
							_setCandleArg.SetValue(item, _candleArg);
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
		private static readonly MemberProxy _setCandleArg;
		private static readonly FastInvoker<VoidType, VoidType, TData> _ctor;
		private const string _timeFormat = "HHmmssfff";
		private static readonly SynchronizedDictionary<Tuple<Type, ExecutionTypes?>, MemberProxy[]> _info = new SynchronizedDictionary<Tuple<Type, ExecutionTypes?>, MemberProxy[]>();
		private static readonly bool _isLevel1 = typeof(TData) == typeof(Level1ChangeMessage);
		private static readonly bool _isNews = typeof(TData) == typeof(NewsMessage);
		private static readonly bool _isQuotes = typeof(TData) == typeof(QuoteChangeMessage);
		private static readonly Level1Fields[] _level1Fields = _isLevel1 ? Enumerator.GetValues<Level1Fields>().Where(l1 => l1 != Level1Fields.ExtensionInfo && l1 != Level1Fields.BestAsk && l1 != Level1Fields.BestBid && l1 != Level1Fields.LastTrade).OrderBy(l1 => (int)l1).ToArray() : null;
		private static readonly MemberProxy _dateMember;
		private static readonly UTF8Encoding _utf = new UTF8Encoding(false);
		private const int _skipColumns = 2;
		// ReSharper restore StaticFieldInGenericType

		static CsvMarketDataSerializer()
		{
			var isCandles = typeof(TData).IsCandleMessage();

			if (typeof(TData) == typeof(ExecutionMessage) || isCandles)
				_setSecurityId = MemberProxy.Create(typeof(TData), "SecurityId");

			if (typeof(TData) == typeof(ExecutionMessage))
				_setExecutionType = MemberProxy.Create(typeof(TData), "ExecutionType");

			if (isCandles)
				_setCandleArg = MemberProxy.Create(typeof(TData), "Arg");

			_ctor = FastInvoker<VoidType, VoidType, TData>.Create(typeof(TData).GetMember<ConstructorInfo>());

			_dateMember = MemberProxy.Create(typeof(TData), isCandles ? "OpenTime" : "ServerTime");
		}

		private readonly Encoding _encoding;
		private readonly ExecutionTypes? _executionType;
		private readonly object _candleArg;
		private readonly string _format;
		private readonly MemberProxy[] _members;
		private readonly Func<string[], object> _toId;
		private FormatCache _templateCache;

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvMarketDataSerializer{T}"/>.
		/// </summary>
		/// <param name="encoding">Encoding.</param>
		public CsvMarketDataSerializer(Encoding encoding = null)
			: this(default(SecurityId), null, encoding)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvMarketDataSerializer{T}"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="executionType">The type of execution.</param>
		/// <param name="candleArg">Candle arg.</param>
		/// <param name="encoding">Encoding.</param>
		public CsvMarketDataSerializer(SecurityId securityId, ExecutionTypes? executionType = null, object candleArg = null, Encoding encoding = null)
		{
			if (securityId.IsDefault() && !_isNews)
				throw new ArgumentNullException(nameof(securityId));

			SecurityId = securityId;
			_executionType = executionType;
			_candleArg = candleArg;
			_encoding = encoding ?? _utf;

			if (_isQuotes)
				return;

			_format = GetFormat(executionType).Replace(":{0}", ".UtcDateTime:" + _timeFormat);

			if (_isLevel1)
				return;

			const string timeFormat = ":" + _timeFormat;

			_members = _info.SafeAdd(Tuple.Create(typeof(TData), executionType), key =>
				_format
					.Split(';')
					.Skip(_skipColumns)
					.Select(s =>
						MemberProxy.Create(typeof(TData),
							s.Substring(1, s.Length - 2).Replace(timeFormat, string.Empty)))
					.Concat(_isNews ? new[] { MemberProxy.Create(typeof(TData), "SecurityId") } : Enumerable.Empty<MemberProxy>())
					.ToArray());

			if (typeof(TData) == typeof(ExecutionMessage))
			{
				switch (executionType)
				{
					case ExecutionTypes.Tick:
					case ExecutionTypes.OrderLog:
						_toId = lines => lines[_skipColumns].To<long>();
						break;
				}
			}
		}

		/// <summary>
		/// Security ID.
		/// </summary>
		public SecurityId SecurityId { get; }

		private static string GetFormat(ExecutionTypes? executionType)
		{
			if (typeof(TData) == typeof(ExecutionMessage))
			{
				switch (executionType)
				{
					case ExecutionTypes.Tick:
						return "{ServerTime:{0}};{ServerTime:zzz};{TradeId};{TradePrice};{Volume};{OriginSide};{OpenInterest};{IsSystem}";
					case ExecutionTypes.OrderLog:
						return "{ServerTime:{0}};{ServerTime:zzz};{TransactionId};{OrderId};{OrderPrice};{Volume};{Side};{OrderState};{TimeInForce};{TradeId};{TradePrice};{PortfolioName};{IsSystem}";
					case null:
						throw new ArgumentNullException(nameof(executionType));
					default:
						throw new ArgumentOutOfRangeException(nameof(executionType));
				}
			}
			
			if (typeof(TData) == typeof(TimeQuoteChange))
				return "{ServerTime:{0}};{ServerTime:zzz};{Price};{Volume};{Side}";

			if (typeof(TData) == typeof(Level1ChangeMessage))
			{
				var fields = _level1Fields.Select(s =>
				{
					//string time = null;

					//switch (s)
					//{
					//	case Level1Fields.BestAskTime:
					//	case Level1Fields.BestBidTime:
					//	case Level1Fields.LastTradeTime:
					//		time = ".UtcDateTime:" + _timeFormat;
					//		break;
					//}

					return "{" + s + "}";
				}).Join(";");

				return "{ServerTime:{0}};{ServerTime:zzz};" + "{{Changes:{0}}}".Put(fields);
			}

			if (typeof(TData).IsCandleMessage())
				return "{OpenTime:{0}};{OpenTime:zzz};{OpenPrice};{HighPrice};{LowPrice};{ClosePrice};{TotalVolume}";

			if (typeof(TData) == typeof(NewsMessage))
			{
				// NewsMessage.Story do not supported
				return "{ServerTime:{0}};{ServerTime:zzz};{Headline};{Source};{Url};{Id};{BoardCode}";
			}

			throw new InvalidOperationException(LocalizedStrings.Str888Params.Put(typeof(TData).Name));
		}

		/// <summary>
		/// To create empty meta-information.
		/// </summary>
		/// <param name="date">Date.</param>
		/// <returns>Meta-information on data for one day.</returns>
		public virtual IMarketDataMetaInfo CreateMetaInfo(DateTime date)
		{
			return new CsvMetaInfo(date, _toId);
		}

		void IMarketDataSerializer.Serialize(Stream stream, IEnumerable data, IMarketDataMetaInfo metaInfo)
		{
			Serialize(stream, data.Cast<TData>(), metaInfo);
		}

		IEnumerableEx IMarketDataSerializer.Deserialize(Stream stream, IMarketDataMetaInfo metaInfo)
		{
			return Deserialize(stream, metaInfo);
		}

		/// <summary>
		/// Cast data into stream.
		/// </summary>
		/// <param name="stream">Data stream.</param>
		/// <param name="data">Data.</param>
		/// <param name="metaInfo">Meta-information on data for one day.</param>
		public virtual void Serialize(Stream stream, IEnumerable<TData> data, IMarketDataMetaInfo metaInfo)
		{
			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				var writer = new StreamWriter(stream, _encoding);

				var appendLine = metaInfo.Count > 0;

				foreach (var item in data)
				{
					if (appendLine)
						writer.WriteLine();
					else
						appendLine = true;

					//writer.Write(_format.PutEx(item));
					writer.Write(Smart.Default.FormatWithCache(ref _templateCache, _format, item));

					var news = item as NewsMessage;
					if (news == null)
						continue;

					writer.Write(";");

					writer.Write(
						news.SecurityId == null
							? null
							: news.SecurityId.Value.SecurityCode);
				}

				writer.Flush();
			});
		}

		/// <summary>
		/// To load data from the stream.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="metaInfo">Meta-information on data for one day.</param>
		/// <returns>Data.</returns>
		public virtual IEnumerableEx<TData> Deserialize(Stream stream, IMarketDataMetaInfo metaInfo)
		{
			// TODO (переделать в будущем)
			var copy = new MemoryStream();
			stream.CopyTo(copy);
			copy.Position = 0;

			stream.Dispose();

			return new SimpleEnumerable<TData>(() =>
				new CsvReader(copy, _encoding, SecurityId, metaInfo.Date.Date, _executionType, _candleArg, _members))
				.ToEx(metaInfo.Count);
		}

		private static DateTimeOffset ParseTime(string[] parts, DateTime date)
		{
			return (date + parts[0].ToDateTime(_timeFormat).TimeOfDay).ToDateTimeOffset(TimeSpan.Parse(parts[1].Replace("+", string.Empty)));
		}
	}
}