namespace StockSharp.Community
{
	using System;

	/// <summary>
	/// Клиент для доступа к <see cref="IDocService"/>.
	/// </summary>
	public class DocClient : BaseCommunityClient<IDocService>
	{
		/// <summary>
		/// Создать <see cref="DocClient"/>.
		/// </summary>
		public DocClient()
			: this(new Uri("http://stocksharp.com/services/docservice.svc"))
		{
		}

		/// <summary>
		/// Создать <see cref="DocClient"/>.
		/// </summary>
		/// <param name="address">Адрес сервера.</param>
		public DocClient(Uri address)
			: base(address, "doc", true)
		{
		}

		/// <summary>
		/// Загрузить описание новой версии.
		/// </summary>
		/// <param name="product">Тип продукта.</param>
		/// <param name="version">Номер новой версии.</param>
		/// <param name="description">Описание новой версии.</param>
		public void PostNewVersion(Products product, string version, string description)
		{
			Invoke(f => f.PostNewVersion(SessionId, product, version, description));
		}
	}
}