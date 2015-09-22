namespace StockSharp.Community
{
	using System;
	using System.ServiceModel;

	/// <summary>
	/// The interface describing the authorization service.
	/// </summary>
	[ServiceContract(Namespace = "http://stocksharp.com/services/authenticationservice.svc")]
	public interface IAuthenticationService
	{
		/// <summary>
		/// To log in.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		/// <returns>Session ID.</returns>
		[OperationContract]
		Guid Login(string login, string password);

		/// <summary>
		/// Logout.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		[OperationContract]
		void Logout(Guid sessionId);

		/// <summary>
		/// Get a user id.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <returns>User id.</returns>
		[OperationContract]
		long GetId(Guid sessionId);
	}
}