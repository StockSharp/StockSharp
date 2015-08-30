namespace StockSharp.IQFeed
{
	using System;
	using System.Globalization;
	using System.Linq;
	using System.Net;
	using System.Net.Sockets;
	using System.Text;
	using System.Threading;

	using Ecng.Common;

	using StockSharp.Logging;

	using StockSharp.Localization;

	/// <summary>
	/// Обертка для работы с IQFeed c помощью протокола TCP/IP.
	/// </summary>
	class IQFeedWrapper
	{
		private readonly ILogReceiver _logReceiver;
		private readonly object _syncDisconnect = new object();
		private Socket _socket;

		/// <summary>
		/// Адрес сервера.
		/// </summary>
		public EndPoint Address { get; private set; }

		/// <summary>
		/// Запущен ли экспорт. Экспорт запускается при отправке IQFeed команды.
		/// </summary>
		private bool IsExportStarted { get; set; }

		/// <summary>
		/// Событие ошибки подключения (например, соединение было разорвано).
		/// </summary>
		public event Action<Exception> ConnectionError;

		/// <summary>
		/// Событие появления новых данных.
		/// </summary>
		public event Action<string> ProcessReply;

		public IQFeedWrapper(ILogReceiver logReceiver, string name, EndPoint address)
		{
			if (logReceiver == null)
				throw new ArgumentNullException("logReceiver");

			if (name.IsEmpty())
				throw new ArgumentNullException("name");

			if (address == null)
				throw new ArgumentNullException("address");

			Address = address;
			_logReceiver = logReceiver;
			Name = name;
		}

		public string Name { get; private set; }

		/// <summary>
		/// Подключиться к серверу.
		/// </summary>
		public void Connect()
		{
			_logReceiver.AddInfoLog("[{0}] Connecting...", Name);

			if (_socket != null)
				throw new InvalidOperationException(LocalizedStrings.Str2152);

			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			try
			{
				_socket.Connect(Address);

				lock (_syncDisconnect)
					IsExportStarted = true;

				ThreadingHelper
					.Thread(() =>
					{
						var buf = new StringBuilder();
						var buffer = new byte[1024];

						try
						{
							while (true)
							{
								if (_socket == null)
									throw new InvalidOperationException(LocalizedStrings.Str2153);

								if (_socket.Poll(500000, SelectMode.SelectRead))
								{
									// http://stackoverflow.com/questions/2661764/how-to-check-if-a-socket-is-connected-disconnected-in-c
									if (_socket.Available == 0)
										throw new InvalidOperationException(LocalizedStrings.Str1611);

									var bytesRecv = _socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);

									buf.Append(Encoding.ASCII.GetString(buffer, 0, bytesRecv));

									var index = buf.LastIndexOf('\n');

									if (index == -1)
										continue;

									var res = buf.ToString(0, index + 1);
									var reply = res.Split("\n").Select(v => v.TrimEnd('\r', '\n'));

									if (index < buf.Length - 1)
										buf.Remove(0, index + 1);
									else
										buf.Clear();

									try
									{
										foreach (var line in reply)
										{
											_logReceiver.AddDebugLog("[{0}] Response: {1}", Name, line);
											ProcessReply.SafeInvoke(line);
										}
									}
									catch (Exception ex)
									{
										_logReceiver.AddErrorLog(ex);
									}
								}

								lock (_syncDisconnect)
								{
									if (IsExportStarted)
										continue;

									_logReceiver.AddInfoLog(LocalizedStrings.Str2154Params, Name);
									Monitor.Pulse(_syncDisconnect);
									break;
								}
							}
						}
						catch (ObjectDisposedException ex)
						{
							ConnectionError.SafeInvoke(new InvalidOperationException(LocalizedStrings.Str2155, ex));
						}
						catch (Exception ex)
						{
							ConnectionError.SafeInvoke(ex);
						}

						try
						{
							_socket.Shutdown(SocketShutdown.Both);
						}
						catch (Exception ex)
						{
							_logReceiver.AddErrorLog(ex);
						}

						_socket.Close();
						_socket = null;

						_logReceiver.AddInfoLog(LocalizedStrings.Str2156Params, Name);
					})
					.Name("IQFeed '{0}' thread".Put(Name))
					.Culture(CultureInfo.InvariantCulture)
					.Start();

				Request("S,SET PROTOCOL,5.1");

				_logReceiver.AddInfoLog("[{0}] Connected", Name);
			}
			catch
			{
				_socket.Close();
				_socket = null;
				throw;
			}
		}

		/// <summary>
		/// Отключиться от сервера.
		/// </summary>
		public void Disconnect()
		{
			_logReceiver.AddInfoLog("[{0}] Disconnecting...", Name);

			lock (_syncDisconnect)
			{
				if (!IsExportStarted)
					return;

				IsExportStarted = false;
				Monitor.Wait(_syncDisconnect);
			}

			_logReceiver.AddInfoLog("Disconnected", Name);
		}

		/// <summary>
		/// Отправить запрос на получение данных.
		/// </summary>
		/// <param name="command">Запрос в формате IQFeed.</param>
		public void Request(string command)
		{
			command += "\r\n";

			_logReceiver.AddInfoLog("[{0}] Request: {1}", Name, command.TrimEnd(Environment.NewLine));

			if (_socket == null)
				throw new InvalidOperationException(LocalizedStrings.Str2153);

			var request = Encoding.ASCII.GetBytes(command);

			var bytesSent = _socket.Send(request, request.Length, SocketFlags.None);

			if (bytesSent != request.Length)
				throw new InvalidOperationException(LocalizedStrings.Str2157Params.Put(command.TrimEnd("\r\n".ToCharArray())));
		}

		/// <summary>
		/// Отправить запрос на получение списка торговых площадок.
		/// </summary>
		public void RequestListedMarkets()
		{
			//Request a list of Listed Markets from the feed. 
			//SLM<CR><LF> 
			//Returns: A list of records. Each record identifies a single listed market in the following format: 
			//[Listed Market ID],[Short Name],[Long Name],<LF>

			Request("SLM");
		}

		/// <summary>
		/// Отправить запрос на получение списка типов инструментов.
		/// </summary>
		public void RequestSecurityTypes()
		{
			//SST<CR><LF> 
			//Request a list of Security Types from the feed. 
			//Returns: A list of records. Each record identifies a single security type in the following format: 
			//[Security Type ID],[Short Name],[Long Name],<LF>
			//After all security types are returned, the list is terminated with a Message in the following format: 
			//!ENDMSG!,<CR><LF>

			Request("SST");
		}

		/// <summary>
		/// Отправить запрос на получение списка инструментов по заданному фильтру.
		/// </summary>
		/// <param name="requestId">Идентификатор запроса.</param>
		/// <param name="searchField">Поле, по которому необходимо искать данные.</param>
		/// <param name="searchText">Строка поиска.</param>
		/// <param name="filterType">Тип фильтра.</param>
		/// <param name="filterValue">Значения фильтра.</param>
		public void RequestSecurities(long requestId, IQFeedSearchField searchField, string searchText, IQFeedFilterType filterType, params string[] filterValue)
		{
			//Symbols By Filter - A symbol search by symbol or description. Can be filtered by providing a list of Listed Markets or Security Types. 
			//SBF,[Field To Search],[Search String],[Filter Type],[Filter Value],[RequestID]<CR><LF> 
			//	[Field To Search] - "s" to search symbols. "d" to search descriptions.
			//	[Search String] - What you want to search for.
			//	[Filter Type] - "e" to search within specific Listed Markets. "t" to search within specific Security Types.
			//	[Filter Value] - A space delimited list of listed markets or security types (based upon the Filter Type parameter).
			//	NOTE: A list of security types or listed marekts can be retrieved dynamicly from the server using the SLM and SST requests below.
			//	[RequestID] - This parameter allows you to specify an identier that will be attached to the returned data inserted as the FIRST field in the output message.

			var request = "SBF,{0},{1},{2},{3},{4}"
				.Put(
					(searchField == IQFeedSearchField.Symbol) ? "s" : "d",
					searchText, (filterType == IQFeedFilterType.Market) ? "e" : "t",
					filterValue.Join(" "), ToIQFeedId(requestId));

			Request(request);
		}

		public void RequestTicks(long requestId, string symbol, long count)
		{
			//HTX,[Symbol],[MaxDatapoints],[DataDirection],[RequestID],[DatapointsPerSend]<CR><LF> 
			//Retrieves up to [MaxDatapoints] number of ticks for the specified [Symbol].
			//	[Symbol] - Required - Max Length 30 characters.
			//	[MaxDatapoints] - Required - The maximum number of datapoints to be retrieved.
			//	[DataDirection] - Optional - '0' (default) for "newest to oldest" or '1' for "oldest to newest".
			//	[RequestID] - Optional - Will be sent back at the start of each line of data returned for this request.
			//	[DatapointsPerSend] - Optional - Specifies the number of datapoints that IQConnect.exe will queue before attempting to send across the socket to your app.

			var request = "HTT,{0},{1},1,{3},".Put(symbol, count, ToIQFeedId(requestId));

			Request(request);
		}

		/// <summary>
		/// Отправить запрос на получение истории сделок по инструменту за указанный период.
		/// </summary>
		/// <param name="requestId">Идентификатор запроса.</param>
		/// <param name="symbol">Код инструмента.</param>
		/// <param name="from">Дата начала периода.</param>
		/// <param name="to">Дата окончания периода.</param>
		public void RequestTicks(long requestId, string symbol, DateTime from, DateTime to)
		{
			//HTT,[Symbol],[BeginDate BeginTime],[EndDate EndTime],[MaxDatapoints],[BeginFilterTime],[EndFilterTime],[DataDirection],[RequestID],[DatapointsPerSend]<CR><LF> 
			//Retrieves tick data between [BeginDate BeginTime] and [EndDate EndTime] for the specified [Symbol].
			//	[Symbol] - Required - Max Length 30 characters.
			//	[BeginDate BeginTime] - Required if [EndDate EndTime] not specified - Format CCYYMMDD HHmmSS - Earliest date/time to receive data for.
			//	[EndDate EndTime] - Required if [BeginDate BeginTime] not specified - Format CCYYMMDD HHmmSS - Most recent date/time to receive data for.
			//	[MaxDatapoints] - Optional - the maximum number of datapoints to be retrieved.
			//	[BeginFilterTime] - Optional - Format HHmmSS - Allows you to specify the earliest time of day (Eastern) for which to receive data.
			//	[EndFilterTime] - Optional - Format HHmmSS - Allows you to specify the latest time of day (Eastern) for which to receive data.
			//	[DataDirection] - Optional - '0' (default) for "newest to oldest" or '1' for "oldest to newest".
			//	[RequestID] - Optional - Will be sent back at the start of each line of data returned for this request.
			//	[DatapointsPerSend] - Optional - Specifies the number of datapoints that IQConnect.exe will queue before attempting to send across the socket to your app.

			var request = "HTT,{0},{1:yyyyMMdd HHmmss},{2:yyyyMMdd HHmmss},,000000,235959,1,{3}"
				.Put(symbol, from, to, ToIQFeedId(requestId));

			Request(request);
		}

		public void RequestCandles(long requestId, string symbol, string intervalType, string arg, long count)
		{
			//HIX,[Symbol],[Interval],[MaxDatapoints],[DataDirection],[RequestID],[DatapointsPerSend],[IntervalType]<CR><LF> 
			//Retrieves [MaxDatapoints] number of Intervals of data for the specified [Symbol].
			//	[Symbol] - Required - Max Length 30 characters.
			//	[Interval] - Required - The interval in seconds.
			//	[MaxDatapoints] - Required - The maximum number of datapoints to be retrieved.
			//	[DataDirection] - Optional - '0' (default) for "newest to oldest" or '1' for "oldest to newest".
			//	[RequestID] - Optional - Will be sent back at the start of each line of data returned for this request.
			//	[DatapointsPerSend] - Optional - Specifies the number of datapoints that IQConnect.exe will queue before attempting to send across the socket to your app.
			//	[IntervalType] - Optional - 's' (default) for time intervals in seconds, 'v' for volume intervals, 't' for tick intervals

			var request = "HIX,{0},{1},{2},1,{3},,{4}".Put(symbol, arg, count, ToIQFeedId(requestId), intervalType);
			
			Request(request);
		}

		/// <summary>
		/// Отправить запрос на получение истории свечек по инструменту за указанный период.
		/// </summary>
		/// <param name="requestId">Идентификатор запроса.</param>
		/// <param name="symbol">Код инструмента.</param>
		/// <param name="intervalType">Тип свечек.</param>
		/// <param name="arg">Параметр свечи.</param>
		/// <param name="from">Дата начала периода.</param>
		/// <param name="to">Дата окончания периода.</param>
		public void RequestCandles(long requestId, string symbol, string intervalType, string arg, DateTime from, DateTime to)
		{
			//HIT,[Symbol],[Interval],[BeginDate BeginTime],[EndDate EndTime],[MaxDatapoints],[BeginFilterTime],[EndFilterTime],[DataDirection],[RequestID],[DatapointsPerSend],[IntervalType]<CR><LF> 
			//Retrieves interval data between [BeginDate BeginTime] and [EndDate EndTime] for the specified [Symbol].
			//	[Symbol] - Required - Max Length 30 characters.
			//	[Interval] - Required - The interval in seconds.
			//	[BeginDate BeginTime] - Required if [EndDate EndTime] not specified - Format CCYYMMDD HHmmSS - Earliest date/time to receive data for.
			//	[EndDate EndTime] - Required if [BeginDate BeginTime] not specified - Format CCYYMMDD HHmmSS - Most recent date/time to receive data for.
			//	[MaxDatapoints] - Optional - the maximum number of datapoints to be retrieved.
			//	[BeginFilterTime] - Optional - Format HHmmSS - Allows you to specify the earliest time of day (Eastern) for which to receive data.
			//	[EndFilterTime] - Optional - Format HHmmSS - Allows you to specify the latest time of day (Eastern) for which to receive data.
			//	[DataDirection] - Optional - '0' (default) for "newest to oldest" or '1' for "oldest to newest".
			//	[RequestID] - Optional - Will be sent back at the start of each line of data returned for this request.
			//	[DatapointsPerSend] - Optional - Specifies the number of datapoints that IQConnect.exe will queue before attempting to send across the socket to your app.
			//	[IntervalType] - Optional - 's' (default) for time intervals in seconds, 'v' for volume intervals, 't' for tick intervals

			var request = "HIT,{0},{1},{2:yyyyMMdd HHmmss},{3:yyyyMMdd HHmmss},,000000,235959,1,{4},,{5}"
				.Put(symbol, arg, from, to, ToIQFeedId(requestId), intervalType);

			Request(request);
		}

		/// <summary>
		/// Отправить запрос на получение истории дневных свечек по инструменту за указанный период.
		/// </summary>
		/// <param name="requestId">Идентификатор запроса.</param>
		/// <param name="symbol">Код инструмента.</param>
		/// <param name="from">Дата начала периода.</param>
		/// <param name="to">Дата окончания периода.</param>
		public void RequestDailyCandles(long requestId, string symbol, DateTime from, DateTime to)
		{
			//HDT,[Symbol],[BeginDate],[EndDate],[MaxDatapoints],[DataDirection],[RequestID],[DatapointsPerSend]<CR><LF> 
			//Retrieves Daily data between [BeginDate] and [EndDate] for the specified [Symbol].
			//	[Symbol] - Required - Max Length 30 characters.
			//	[BeginDate] - Required if [EndDate] not specified - Format CCYYMMDD - Earliest date to receive data for.
			//	[EndDate] - Required if [BeginDate] not specified - Format CCYYMMDD - Most recent date to receive data for.
			//	[MaxDatapoints] - Optional - the maximum number of datapoints to be retrieved.
			//	[DataDirection] - Optional - '0' (default) for "newest to oldest" or '1' for "oldest to newest".
			//	[RequestID] - Optional - Will be sent back at the start of each line of data returned for this request.
			//	[DatapointsPerSend] - Optional - Specifies the number of datapoints that IQConnect.exe will queue before attempting to send across the socket to your app.

			var request = "HDT,{0},{1:yyyyMMdd},{2:yyyyMMdd},,1,{3},"
				.Put(symbol, from, to, ToIQFeedId(requestId));

			Request(request);
		}

		/// <summary>
		/// Отправить запрос на получение истории дневных свечек по инструменту.
		/// </summary>
		/// <param name="requestId">Идентификатор запроса.</param>
		/// <param name="symbol">Код инструмента.</param>
		/// <param name="count">Количесто свечей.</param>
		public void RequestDailyCandles(long requestId, string symbol, long count)
		{
			//HDX,[Symbol],[MaxDatapoints],[DataDirection],[RequestID],[DatapointsPerSend]<CR><LF> 
			//Retrieves up to [MaxDatapoints] number of End-Of-Day Data for the specified [Symbol].
			//	[Symbol] - Required - Max Length 30 characters.
			//	[MaxDatapoints] - Required - The maximum number of datapoints to be retrieved.
			//	[DataDirection] - Optional - '0' (default) for "newest to oldest" or '1' for "oldest to newest".
			//	[RequestID] - Optional - Will be sent back at the start of each line of data returned for this request.
			//	[DatapointsPerSend] - Optional - Specifies the number of datapoints that IQConnect.exe will queue before attempting to send across the socket to your app.

			var request = "HDX,{0},{1},1,{2},".Put(symbol, count, ToIQFeedId(requestId));

			Request(request);
		}

		/// <summary>
		/// Отправить запрос на получение истории недельных свечек по инструменту.
		/// </summary>
		/// <param name="requestId">Идентификатор запроса.</param>
		/// <param name="symbol">Код инструмента.</param>
		/// <param name="count">Количество свечек.</param>
		public void RequestWeeklyCandles(long requestId, string symbol, long count)
		{
			//HWX,[Symbol],[MaxDatapoints],[DataDirection],[RequestID],[DatapointsPerSend]<CR><LF> 
			//Retrieves up to [MaxDatapoints] datapoints of composite weekly datapoints for the specified [Symbol].
			//	[Symbol] - Required - Max Length 30 characters.
			//	[MaxDatapoints] - Required - The maximum number of datapoints to be retrieved.
			//	[DataDirection] - Optional - '0' (default) for "newest to oldest" or '1' for "oldest to newest".
			//	[RequestID] - Optional - Will be sent back at the start of each line of data returned for this request.
			//	[DatapointsPerSend] - Optional - Specifies the number of datapoints that IQConnect.exe will queue before attempting to send across the socket to your app.

			var request = "HWX,{0},{1},1,{2},".Put(symbol, count, ToIQFeedId(requestId));

			Request(request);
		}

		/// <summary>
		/// Отправить запрос на получение истории месячных свечек по инструменту.
		/// </summary>
		/// <param name="requestId">Идентификатор запроса.</param>
		/// <param name="symbol">Код инструмента.</param>
		/// <param name="count">Количество свечек.</param>
		public void RequestMonthlyCandles(long requestId, string symbol, long count)
		{
			//HMX,[Symbol],[MaxDatapoints],[DataDirection],[RequestID],[DatapointsPerSend]<CR><LF> 
			//Retrieves up to [MaxDatapoints] datapoints of composite monthly datapoints for the specified [Symbol].
			//	[Symbol] - Required - Max Length 30 characters.
			//	[MaxDatapoints] - Required - The maximum number of datapoints to be retrieved.
			//	[DataDirection] - Optional - '0' (default) for "newest to oldest" or '1' for "oldest to newest".
			//	[RequestID] - Optional - Will be sent back at the start of each line of data returned for this request.
			//	[DatapointsPerSend] - Optional - Specifies the number of datapoints that IQConnect.exe will queue before attempting to send across the socket to your app.

			var request = "HMX,{0},{1},1,{2},".Put(symbol, count, ToIQFeedId(requestId));

			Request(request);
		}

		public void SubscribeCandles(string symbol, string intervalType, string arg, DateTime beginDate, long requestId)
		{
			//BW,[Symbol],[Interval],[BeginDate BeginTime],[MaxDaysOfDatapoints],[MaxDatapoints],[BeginFilterTime],[EndFilterTime],[RequestID],[Interval Type],[Reserved],[UpdateInterval] 
			//Request a new interval bar watch based on parameters retrieving history based on the same set of parameters
			//	[Symbol] - Required - Symbol to start interval bar watch
			//	[Interval] - Required - The interval in seconds / volume / trades (depending on interval type; defaults to seconds)
			//	[BeginDate BeginTime] - Optional - Format CCYYMMDD HHmmSS - Earliest date/time to receive data for.
			//	[MaxDaysOfDatapoints] - Optional - the maximum number of trading days to be retrieved
			//	[MaxDatapoints] - Optional - the maximum number of datapoints to be retrieved.
			//	[BeginFilterTime] - Optional - Format HHmmSS - Allows you to specify the earliest time of day (Eastern) for which to receive data.
			//	[EndFilterTime] - Optional - Format HHmmSS - Allows you to specify the latest time of day (Eastern) for which to receive data.
			//	[RequestID] - Optional - Will be sent back at the start of each line of data returned for this request.
			//	[IntervalType] - Optional - 's' (default) for time intervals in seconds, 'v' for volume intervals, 't' for tick intervals
			//	[Reserved] - Reserved for future use.
			//	[Update Interval] - Optional - number of seconds before sending out an updated bar (defaults to 0)

			var request = "BW,{0},{1},{2:yyyyMMdd HHmmss},,,,,{3},{4},,"
				.Put(symbol, arg, beginDate, ToIQFeedId(requestId), intervalType);

			Request(request);
		}

		public void UnSubscribeCandles(string symbol, long requestId)
		{
			//BR,[Symbol],[RequestID] 
			//Remove an interval bar watch based on the symbol and option request ID.
			//	[Symbol] - Required - Symbol to remove interval bar watch
			//	[RequestID] - Optional - Request ID associated with the interval watch to remove

			var request = "BR,{0},{1}".Put(symbol, ToIQFeedId(requestId));

			Request(request);
		}

		/// <summary>
		/// Подписаться на получение данных по инструменту.
		/// </summary>
		/// <param name="symbol">Код инструмента.</param>
		public void SubscribeSymbol(string symbol)
		{
			Request("w{0}".Put(symbol));
		}

		/// <summary>
		/// Отписаться от получения данных по инструменту.
		/// </summary>
		/// <param name="symbol">Код инструмента.</param>
		public void UnSubscribeSymbol(string symbol)
		{
			Request("r{0}".Put(symbol));
		}

		/// <summary>
		/// Подписаться на получение новостей.
		/// </summary>
		public void SubscribeNews()
		{
			Request("S,NEWSON");
		}

		/// <summary>
		/// Отписаться на получение новостей.
		/// </summary>
		public void UnSubscribeNews()
		{
			Request("S,NEWSOFF");
		}

		/// <summary>
		/// Отправить запрос на получение описания новости.
		/// </summary>
		public void RequestNewsStory(long requestId, string newsId)
		{
			//NSY,[ID],[XML/Text/Email],[DeliverTo],[RequestID]<CR><LF> 
			//News Story request 
			//	[ID] - Required - The headline/story identifier. Retrieved from the Headline Request above.
			//	[XML/Text/Email] - Optional - 'x' (default) for XML formatted data. 't' for text formatted data. 'e' for email delivery.
			//	[DeliverTo] - Required if [XML/Text/Email] set to email, otherwise ignored. Email address to deliver story to.
			//	[RequestID] - Optional - Will be sent back at the start of each line of data returned for this request.

			var request = "NSY,{0},t,,{1}".Put(newsId, ToIQFeedId(requestId));

			Request(request);
		}

		/// <summary>
		/// Отправить запрос на получение описания новости.
		/// </summary>
		public void RequestNewsHeadlines(long requestId, DateTime date)
		{
			//NHL,[Sources],[Symbols],[XML/Text],[Limit],[Date],[RequestID]<CR><LF> 
			//News Headlines request 
			//	[Sources] - Optional - A colon separated list of news sources (retrieved from the news configuration request).
			//	[Symbols] - Optional - A colon separated list of symbols for which to receive headlines.
			//	[XML/Text] - Optional - 'x' (default) for XML formatted data. 't' for text formatted data.
			//	[Limit] - Optional - The maximum number of headlines to retrieve per source.
			//	[Date] - Optional - The date to retrieve headlines for in the format YYYYMMDD (only works for limited sources)
			//	[RequestID] - Optional - Will be sent back at the start of each line of data returned for this request.

			var request = "NHL,,,t,,{0},{1}".Put(date.ToString("yyyyMMdd"), ToIQFeedId(requestId));

			Request(request);
		}

		/// <summary>
		/// Установить набор полей для получения данных по Level1.
		/// </summary>
		/// <param name="fields">Названия полей.</param>
		public void SetLevel1FieldSet(params string[] fields)
		{
			Request("S,SELECT UPDATE FIELDS,{0}".Put(fields.Join(",")));
		}

		private static string ToIQFeedId(long requestId)
		{
			return "#" + requestId + "#";
		}
	}
}