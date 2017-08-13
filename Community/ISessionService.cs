namespace StockSharp.Community
{
	using System;
	using System.ServiceModel;

	/// <summary>
	/// The interface describing the registration service.
	/// </summary>
	[ServiceContract(Namespace = "http://stocksharp.com/services/sessionservice.svc")]
	public interface ISessionService
	{
		/// <summary>
		/// Create a new activity session.
		/// </summary>
		/// <param name="product">Product.</param>
		/// <param name="sessionId">Session ID (authentication).</param>
		/// <returns>Session ID (activity).</returns>
		[OperationContract]
		long CreateSession(Products product, Guid sessionId);

		/// <summary>
		/// Track the session is alive.
		/// </summary>
		/// <param name="sessionId">Session ID (activity).</param>
		[OperationContract]
		void Ping(long sessionId);

		/// <summary>
		/// Close the session.
		/// </summary>
		/// <param name="sessionId">Session ID (activity).</param>
		[OperationContract]
		void CloseSession(long sessionId);
	}
}