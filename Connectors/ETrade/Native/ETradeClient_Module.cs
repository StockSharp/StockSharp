#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.ETrade.Native.ETrade
File: ETradeClient_Module.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.ETrade.Native
{
	using System;
	using System.Threading;
	using System.Collections.Generic;
	using Timer = System.Timers.Timer;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Logging;

	using StockSharp.Localization;

	partial class ETradeClient
	{
		private abstract class ETradeModule
		{
			//private const double _requestsLimitMultiplier = 1d;

			private double _requestsPerSecond;
			//private DateTime _nextIntervalStartsAt;
			private DateTime _lastLimitsUpdateTime;
			//private readonly TimeSpan _updateLimitsInterval = TimeSpan.FromMinutes(5);

			private readonly string _name;
			protected ETradeClient Client { get; private set; }

			protected bool IsStarted { get; private set; }
			protected bool IsStopping { get; private set; }

			private readonly SynchronizedQueue<ETradeRequest> _userRequests = new SynchronizedQueue<ETradeRequest>();
			private readonly AutoResetEvent _wakeupEvt = new AutoResetEvent(false);
			private readonly ManualResetEvent _doneEvt = new ManualResetEvent(false);

			private readonly Timer _wakeupTimer = new Timer();

			private readonly List<DateTime> _recentRequests = new List<DateTime>();

			protected ETradeRequest CurrentAutoRequest { get; private set; }

			protected ETradeModule(string name, ETradeClient client)
			{
				_name = name;
				Client = client;
				Client.Connection.ConnectionStateChanged += HandleClientState;

				_wakeupTimer.AutoReset = false;
				_wakeupTimer.Enabled = false;
				_wakeupTimer.Elapsed += (sender, args) => _wakeupEvt.Set();
			}

			public void HandleClientState()
			{
				if (Client.IsConnected)
				{
					Start();
					Wakeup();
				}
				else
				{
					Stop();
				}
			}

			protected virtual void Start()
			{
				if (IsStarted) return;

				IsStarted = true;
				IsStopping = false;
				_doneEvt.Reset();

				_lastLimitsUpdateTime = DateTime.MinValue;
				//_nextIntervalStartsAt = DateTime.MinValue;
				_requestsPerSecond = 0;

				CurrentAutoRequest = null;

				_recentRequests.Clear();

				UpdateLimitsIfNecessary();

				ThreadingHelper
					.Thread(() =>
					{
						try
							{ ModuleThreadFunc(); }
						catch (Exception e)
							{ Client.RaiseError(new ETradeException(LocalizedStrings.Str3362Params.Put(_name), e)); }
						finally
							{ _doneEvt.Set(); }
					})
					.Background(true)
					.Name("etrade_" + _name)
					.Launch();
			}

			internal void Stop()
			{
				if (!IsStarted) return;

				_wakeupTimer.Stop();
				IsStopping = true;
				_wakeupEvt.Set();
				_doneEvt.WaitOne();
				IsStarted = IsStopping = false;
			}

			protected abstract ETradeRequest GetNextAutoRequest();

			public void ExecuteUserRequest<TResp>(ETradeRequest<TResp> request, Action<ETradeResponse<TResp>> responseHandler)
			{
				request.ResponseHandler = responseHandler;
				_userRequests.Enqueue(request);
				_wakeupEvt.Set();
			}

			private void ModuleThreadFunc()
			{
				Client.AddDebugLog("Module '{0}' has started.", _name);
				CurrentAutoRequest = GetNextAutoRequest();

				while (!IsStopping)
				{
					_wakeupEvt.WaitOne();

					while (!IsStopping)
					{
						var now = DateTime.UtcNow;
						var removeBefore = now - TimeSpan.FromSeconds(.999d);
						_recentRequests.RemoveAll(dt => dt < removeBefore);
						var canDoRequestNow = _recentRequests.Count + 1 <= _requestsPerSecond;

						if(CurrentAutoRequest == null)
							CurrentAutoRequest = GetNextAutoRequest();

						var request = !_userRequests.IsEmpty() ? _userRequests.Peek() : CurrentAutoRequest;

						if(request == null) break;

						if (request.IsRequestRateLimited)
						{
							if (!canDoRequestNow)
							{
								StartWakeupTimer(_recentRequests.Count > 0 ? 1 - (now - _recentRequests[0]).TotalSeconds : 1);
								break;
							}

							_recentRequests.Add(now);
						}

						Client.AddDebugLog("{0}: executing request: {1}", _name, request.ToString());

						var response = request.ExecuteNextPart(Client);

						if (response.Exception is ETradeUnauthorizedException)
						{
							Client.AddWarningLog("{0}: Response returned Unauthorized exception.", _name);
							IsStopping = true;
							Client.Connection.ReconnectAsync();
							break;
						}

						ProcessResponse(response);

						if (request.IsDone)
						{
							if(request == CurrentAutoRequest)
								CurrentAutoRequest = GetNextAutoRequest();
							else
								_userRequests.Dequeue();

							if(response.Exception == null)
								UpdateLimitsIfNecessary();
						}
					}
				}
			}

			protected virtual void ProcessResponse(ETradeResponse response)
			{
				response.Process();
			}

			private void StartWakeupTimer(double seconds)
			{
				_wakeupTimer.Stop();
				_wakeupTimer.Interval = seconds * 1000;
				_wakeupTimer.Start();
			}

			public void Wakeup()
			{
				StartWakeupTimer(0.01);
			}

			private void UpdateLimitsIfNecessary()
			{
				if (_lastLimitsUpdateTime != DateTime.MinValue)
					return;

				_lastLimitsUpdateTime = DateTime.UtcNow;
				//_nextIntervalStartsAt = DateTime.MaxValue;
				_requestsPerSecond = 2;

				#region "limits" api was removed in latest version of ETradeApi
//				var now = DateTime.UtcNow;
//				if (now - _lastLimitsUpdateTime < _updateLimitsInterval && now < _nextIntervalStartsAt)
//					return;
//
//				_lastLimitsUpdateTime = now;
//
//				ExecuteUserRequest(new ETradeRateLimitsRequest(_name), response =>
//				{
//					if (response.Exception != null)
//					{
//						Client.RaiseError(new ETradeException("Запрос RateLimits вернул ошибку", response.Exception));
//						_nextIntervalStartsAt = now + _updateLimitsInterval;
//						_requestsPerSecond = 1;
//						Client.AddWarningLog("{0}: Failed to update rate limits. next interval: {1}, requests per second: {2:F4}", 
//											_name, _nextIntervalStartsAt, _requestsPerSecond);
//						return;
//					}
//
//					_lastLimitsUpdateTime = DateTime.UtcNow;
//					_nextIntervalStartsAt = ETradeUtil.ETradeTimestampToUTC(response.Data.resetTimeEpochSeconds);
//					// .2 because ETrade allows 2 requests per second for limit of 7000 requests per hour, or 4 requests per second for 14000/hour
//					_requestsPerSecond = .2 + (_requestsLimitMultiplier * response.Data.requestsLimit) / response.Data.limitIntervalInSeconds;
//
//					if (_nextIntervalStartsAt > _lastLimitsUpdateTime)
//					{
//						var actualRequestsPerSecond = .2 + (_requestsLimitMultiplier * response.Data.requestsRemaining) / (_nextIntervalStartsAt - _lastLimitsUpdateTime).TotalSeconds;
//						if(actualRequestsPerSecond < _requestsPerSecond)
//							_requestsPerSecond = actualRequestsPerSecond;
//					} 
//					else if (_lastLimitsUpdateTime - _nextIntervalStartsAt > TimeSpan.FromDays(1)) // workaround for Sandbox mode, which returns year 2010.
//					{
//						_nextIntervalStartsAt = now + _updateLimitsInterval;
//					}
//
//					if(_requestsPerSecond < 1) _requestsPerSecond = 1;
//
//					Client.AddDebugLog("{0}: Rate limits updated. next interval: {1}, requests per second: {2:F4}", 
//										_name, _nextIntervalStartsAt, _requestsPerSecond);
//				});
				#endregion
			}
		}
	}
}
