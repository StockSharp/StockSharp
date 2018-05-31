#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: BaseServiceClient.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	using System;
	using System.ServiceModel;

	using Ecng.Common;
	using Ecng.Net;

	/// <summary>
	/// The base client for access to the WCF services.
	/// </summary>
	/// <typeparam name="TService">WCF service type.</typeparam>
	public abstract class BaseServiceClient<TService> : Disposable
		where TService : class
	{
		private readonly string _endpointName;
		private readonly bool _hasCallbacks;
		private TService _service;
		private ChannelFactory<TService> _factory;

		/// <summary>
		/// Initialize <see cref="BaseCommunityClient{TService}"/>.
		/// </summary>
		/// <param name="address">Server address.</param>
		/// <param name="endpointName">The access point name in the configuration file.</param>
		/// <param name="hasCallbacks">Whether the <typeparamref name="TService" /> has events.</param>
		protected BaseServiceClient(Uri address, string endpointName, bool hasCallbacks = false)
		{
			if (address == null)
				throw new ArgumentNullException(nameof(address));

			Address = address;
			_endpointName = endpointName;
			_hasCallbacks = hasCallbacks;
		}

		/// <summary>
		/// Server address.
		/// </summary>
		public Uri Address { get; }

		/// <summary>
		/// Whether the connection has been established.
		/// </summary>
		public bool IsConnected { get; private set; }

		/// <summary>
		/// Create WCF channel.
		/// </summary>
		/// <returns>WCF channel.</returns>
		protected virtual ChannelFactory<TService> CreateChannel()
		{
			return new ChannelFactory<TService>(new WSHttpBinding
			{
				Security =
				{
					Mode = SecurityMode.Transport,
					//Message =
					//{
					//	ClientCredentialType = MessageCredentialType.Certificate,
					//	AlgorithmSuite = SecurityAlgorithmSuite.Default
					//}
				},
				UseDefaultWebProxy = true
			}, new EndpointAddress(Address));
		}

		/// <summary>
		/// To connect. The connection is established automatically when the method <see cref="Invoke"/> or <see cref="Invoke{TService}"/> is called.
		/// </summary>
		public virtual void Connect()
		{
			_factory = ChannelHelper.Create(_endpointName, CreateChannel);

			if (_hasCallbacks)
				_service = _factory.CreateChannel();

			OnConnect();

			IsConnected = true;
		}

		/// <summary>
		/// Connect.
		/// </summary>
		protected virtual void OnConnect()
		{
		}

		/// <summary>
		/// To call the service <typeparamref name="TService" /> method.
		/// </summary>
		/// <typeparam name="TResult">The result type returning the service method.</typeparam>
		/// <param name="handler">The handler in which the method is called.</param>
		/// <returns>The result returning the service method.</returns>
		protected virtual TResult Invoke<TResult>(Func<TService, TResult> handler)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			if (!IsConnected)
				Connect();

			return _hasCallbacks ? handler(_service) : _factory.Invoke(handler);
		}

		/// <summary>
		/// To call the service <typeparamref name="TService" /> method.
		/// </summary>
		/// <param name="handler">The handler in which the method is called.</param>
		protected void Invoke(Action<TService> handler)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			Invoke<object>(srv =>
			{
				handler(srv);
				return null;
			});
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			if (IsConnected)
			{
				((IDisposable)_factory).Dispose();

				_factory = null;
				_service = null;

				IsConnected = false;
			}

			base.DisposeManaged();
		}
	}
}