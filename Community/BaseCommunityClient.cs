namespace StockSharp.Community
{
	using System;

	/// <summary>
	/// The base client for access to the StockSharp services.
	/// </summary>
	/// <typeparam name="TService">WCF service type.</typeparam>
	public abstract class BaseCommunityClient<TService> : BaseServiceClient<TService>
		where TService : class
	{
		/// <summary>
		/// Initialize <see cref="BaseCommunityClient{TService}"/>.
		/// </summary>
		/// <param name="address">Server address.</param>
		/// <param name="endpointName">The access point name in the configuration file.</param>
		/// <param name="hasCallbacks">Whether the <typeparamref name="TService" /> has events.</param>
		protected BaseCommunityClient(Uri address, string endpointName, bool hasCallbacks = false)
			: base(address, endpointName, hasCallbacks)
		{
		}

		/// <summary>
		/// The session identifier received from <see cref="IAuthenticationService.Login"/>.
		/// </summary>
		protected virtual Guid SessionId
		{
			get { return AuthenticationClient.Instance.SessionId; }
		}

		/// <summary>
		/// The user identifier for <see cref="SessionId"/>.
		/// </summary>
		public long UserId
		{
			get { return AuthenticationClient.Instance.GetId(SessionId); }
		}
	}
}