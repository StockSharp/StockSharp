#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: IMarketRuleContainer.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;

	using StockSharp.Logging;

	/// <summary>
	/// The interface, describing the rules container.
	/// </summary>
	public interface IMarketRuleContainer : ILogReceiver
	{
		/// <summary>
		/// The operation state.
		/// </summary>
		ProcessStates ProcessState { get; }

		/// <summary>
		/// To activate the rule.
		/// </summary>
		/// <param name="rule">Rule.</param>
		/// <param name="process">The processor returning <see langword="true" /> if the rule has ended its operation, otherwise - <see langword="false" />.</param>
		void ActivateRule(IMarketRule rule, Func<bool> process);

		/// <summary>
		/// Is rules execution suspended.
		/// </summary>
		/// <remarks>
		/// Rules suspension is performed through the method <see cref="SuspendRules"/>.
		/// </remarks>
		bool IsRulesSuspended { get; }

		/// <summary>
		/// To suspend rules execution until next restoration through the method <see cref="ResumeRules"/>.
		/// </summary>
		void SuspendRules();

		/// <summary>
		/// To restore rules execution, suspended through the method <see cref="SuspendRules"/>.
		/// </summary>
		void ResumeRules();

		/// <summary>
		/// Registered rules.
		/// </summary>
		IMarketRuleList Rules { get; }
	}
}