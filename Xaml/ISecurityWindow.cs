namespace StockSharp.Xaml
{
	using System;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Интерфейс, описывающий окно создания или редактирование торгового инструмента.
	/// </summary>
	public interface ISecurityWindow
	{
		/// <summary>
		/// Обработчик, проверяющий доступность введенного идентификатора для <see cref="Security"/>.
		/// </summary>
		Func<string, string> ValidateId { get; set; }

		/// <summary>
		/// Инструмент.
		/// </summary>
		Security Security { get; set; }
	}
}