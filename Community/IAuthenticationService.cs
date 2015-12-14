#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: IAuthenticationService.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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