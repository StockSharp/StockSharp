#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Oanda.Native.Oanda
File: OandaStreamingClient.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Oanda.Native
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Web;

	using Newtonsoft.Json;

	using StockSharp.Oanda.Native.Communications;
	using StockSharp.Oanda.Native.DataTypes;

	class OandaStreamingClient : Disposable
	{
		private class StreamingWorker<TData, TResponse>
		{
			private enum States
			{
				Starting,
				Started,
				Stopping,
				Stopped,
			}

			private readonly OandaStreamingClient _parent;
			private readonly string _methodName;
			private readonly Action<QueryString, TData[]> _fillQuery;
			private readonly Action<TResponse> _newLine;
			private readonly CachedSynchronizedSet<TData> _data = new CachedSynchronizedSet<TData>();
			private int _dataVersion;
			private States _currState = States.Stopped;
			private WebResponse _response;

			public StreamingWorker(OandaStreamingClient parent, string methodName, Action<QueryString, TData[]> fillQuery, Action<TResponse> newLine)
			{
				if (parent == null)
					throw new ArgumentNullException(nameof(parent));

				if (methodName.IsEmpty())
					throw new ArgumentNullException(nameof(methodName));

				if (fillQuery == null)
					throw new ArgumentNullException(nameof(fillQuery));

				if (newLine == null)
					throw new ArgumentNullException(nameof(newLine));

				_parent = parent;
				_methodName = methodName;
				_fillQuery = fillQuery;
				_newLine = newLine;
			}

			public void Add(TData data)
			{
				lock (_data.SyncRoot)
				{
					if (!_data.TryAdd(data))
						return;

					_dataVersion++;

					switch (_currState)
					{
						case States.Starting:
							return;
						case States.Started:
							//_currState = States.Starting;
							_response.Close();
							return;
						case States.Stopping:
							_currState = States.Starting;
							return;
						case States.Stopped:
							_currState = States.Starting;
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				ThreadingHelper.Thread(() =>
				{
					var errorCount = 0;
					const int maxErrorCount = 10;

					while (!_parent.IsDisposed)
					{
						var dataVersion = 0;

						try
						{
							var url = new Url(_parent._streamingUrl + "/v1/" + _methodName);

							TData[] cachedData;

							lock (_data.SyncRoot)
							{
								switch (_currState)
								{
									case States.Starting:
										lock (_data.SyncRoot)
										{
											cachedData = _data.Cache;
											dataVersion = _dataVersion;	
										}
										break;
									case States.Started:
									case States.Stopped:
										throw new InvalidOperationException();
									case States.Stopping:
										_currState = States.Stopped;
										return;
									default:
										throw new ArgumentOutOfRangeException();
								}
							}

							_fillQuery(url.QueryString, cachedData);

							var request = WebRequest.Create(url);

							// for non-sandbox requests
							if (_parent._token != null)
								request.Headers.Add("Authorization", "Bearer " + _parent._token.To<string>());

							request.Headers.Add("X-Accept-Datetime-Format", "UNIX");

							using (var response = request.GetResponse())
							{
								lock (_data.SyncRoot)
								{
									switch (_currState)
									{
										case States.Starting:
											// new items may be added or removed
											if (dataVersion < _dataVersion)
												continue;

											_currState = States.Started;
											_response = response;
											break;
										case States.Started:
										case States.Stopped:
											throw new InvalidOperationException();
										case States.Stopping:
											continue;
										default:
											throw new ArgumentOutOfRangeException();
									}
								}

								using (var reader = new StreamReader(response.GetResponseStream()))
								{
									string line;

									var lineErrorCount = 0;
									const int maxLineErrorCount = 100;

									while (!_parent.IsDisposed && (line = reader.ReadLine()) != null)
									{
										try
										{
											_newLine(JsonConvert.DeserializeObject<TResponse>(line));
											lineErrorCount = 0;
										}
										catch (Exception ex)
										{
											_parent.NewError.SafeInvoke(ex);

											if (++lineErrorCount >= maxLineErrorCount)
											{
												//this.AddErrorLog("Max error {0} limit reached.", maxLineErrorCount);
												break;
											}
										}
									}
								}
							}
						}
						catch (Exception ex)
						{
							bool needLog;

							lock (_data.SyncRoot)
							{
								needLog = dataVersion == _dataVersion;
								_currState = _data.Count > 0 ? States.Starting : States.Stopping;
								_response = null;
							}

							if (needLog)
							{
								_parent.NewError.SafeInvoke(ex);

								if (++errorCount >= maxErrorCount)
								{
									//this.AddErrorLog("Max error {0} limit reached.", maxErrorCount);
									break;
								}
							}
							else
								errorCount = 0;
						}
						finally
						{
							lock (_data.SyncRoot)
								_response = null;
						}
					}
				})
				.Name("Oanda " + _methodName)
				.Launch();
			}

			public void Remove(TData data)
			{
				lock (_data.SyncRoot)
				{
					if (!_data.Remove(data))
						return;

					_dataVersion++;

					switch (_currState)
					{
						case States.Starting:
							if (_data.Count == 0)
								_currState = States.Stopping;

							break;
						case States.Started:
							//if (_data.Count == 0)
							//	_currState = States.Stopping;

							_response.Close();
							break;
						case States.Stopping:
							return;
						case States.Stopped:
							throw new InvalidOperationException();
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}

			public void Stop()
			{
				lock (_data.SyncRoot)
				{
					_data.Clear();
					_dataVersion++;

					switch (_currState)
					{
						case States.Starting:
							_currState = States.Stopping;
							break;
						case States.Started:
							//_currState = States.Stopping;
							_response.Close();
							break;
						case States.Stopping:
						case States.Stopped:
							return;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}
		}

		private readonly string _streamingUrl;

		private readonly StreamingWorker<string, StreamingPriceResponse> _pricesWorker;
		private readonly StreamingWorker<int, StreamingEventResponse> _eventsWorker;
		private readonly SecureString _token;

		public OandaStreamingClient(OandaServers server, SecureString token, Func<string, int> getAccountId)
		{
			if (getAccountId == null)
				throw new ArgumentNullException(nameof(getAccountId));

			switch (server)
			{
				case OandaServers.Sandbox:
					if (token != null)
						throw new ArgumentException("token");

					_streamingUrl = "http://stream-sandbox.oanda.com";
					break;
				case OandaServers.Practice:
					if (token == null)
						throw new ArgumentNullException(nameof(token));

					_streamingUrl = "https://stream-fxpractice.oanda.com";
					break;
				case OandaServers.Real:
					if (token == null)
						throw new ArgumentNullException(nameof(token));

					_streamingUrl = "https://stream-fxtrade.oanda.com";
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(server));
			}

			_token = token;

			_pricesWorker = new StreamingWorker<string, StreamingPriceResponse>(this, "prices",
				(qs, instruments) =>
					qs
						.Append("accountId", getAccountId(null))
						.Append("instruments", instruments.Join(",")),
				price =>
				{
					if (price.Tick == null)
						return;

					NewPrice.SafeInvoke(price.Tick);
				});

			_eventsWorker = new StreamingWorker<int, StreamingEventResponse>(this, "events",
				(qs, accounts) =>
					qs.Append("accountIds", accounts.Select(a => a.To<string>()).Join(",")),
				price =>
				{
					if (price.Transaction == null)
						return;

					NewTransaction.SafeInvoke(price.Transaction);
				});
		}

		public event Action<Exception> NewError;
		public event Action<Price> NewPrice;
		public event Action<Transaction> NewTransaction;

		public void SubscribePricesStreaming(int accountId, string instrument)
		{
			_pricesWorker.Add(instrument);
		}

		public void UnSubscribePricesStreaming(string instrument)
		{
			_pricesWorker.Remove(instrument);
		}

		public void SubscribeEventsStreaming(int accountId)
		{
			_eventsWorker.Add(accountId);
		}

		public void UnSubscribeEventsStreaming(int accountId)
		{
			_eventsWorker.Remove(accountId);
		}

		protected override void DisposeManaged()
		{
			_eventsWorker.Stop();
			_pricesWorker.Stop();

			base.DisposeManaged();
		}
	}
}