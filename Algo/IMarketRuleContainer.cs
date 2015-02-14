namespace StockSharp.Algo
{
	using System;

	using StockSharp.Logging;

	/// <summary>
	/// Интерфейс, описывающий контейнер правил.
	/// </summary>
	public interface IMarketRuleContainer : ILogReceiver
	{
		/// <summary>
		/// Состояние работы.
		/// </summary>
		ProcessStates ProcessState { get; }

		/// <summary>
		/// Активировать правило.
		/// </summary>
		/// <param name="rule">Правило.</param>
		/// <param name="process">Обработчик, возвращающий <see langword="true"/>, если правило закончило свою работу, иначе - false.</param>
		void ActivateRule(IMarketRule rule, Func<bool> process);

		/// <summary>
		/// Приостановлено ли исполнение правил.
		/// </summary>
		/// <remarks>
		/// Приостановка правил происходит через метод <see cref="SuspendRules()"/>.
		/// </remarks>
		bool IsRulesSuspended { get; }

		/// <summary>
		/// Приостановить исполнение правил до следующего восстановления через метод <see cref="ResumeRules"/>.
		/// </summary>
		void SuspendRules();

		/// <summary>
		/// Восстановить исполнение правил, остановленное через метод <see cref="SuspendRules()"/>.
		/// </summary>
		void ResumeRules();

		/// <summary>
		/// Зарегистрированные правила.
		/// </summary>
		IMarketRuleList Rules { get; }
	}
}