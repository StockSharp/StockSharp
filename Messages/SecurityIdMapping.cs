namespace StockSharp.Messages
{
	using System.Collections.Generic;

	/// <summary>
	/// Security identifier mapping.
	/// </summary>
	public struct SecurityIdMapping
	{
		/// <summary>
		/// StockSharp format.
		/// </summary>
		public SecurityId StockSharpId { get; set; }

		/// <summary>
		/// Adapter format.
		/// </summary>
		public SecurityId AdapterId { get; set; }

		/// <summary>
		/// Cast <see cref="KeyValuePair{T1,T2}"/> object to the type <see cref="SecurityIdMapping"/>.
		/// </summary>
		/// <param name="pair"><see cref="KeyValuePair{T1,T2}"/> value.</param>
		/// <returns><see cref="SecurityIdMapping"/> value.</returns>
		public static implicit operator SecurityIdMapping(KeyValuePair<SecurityId, SecurityId> pair)
		{
			return new SecurityIdMapping
			{
				StockSharpId = pair.Key,
				AdapterId = pair.Value
			};
		}

		/// <summary>
		/// Cast object from <see cref="SecurityIdMapping"/> to <see cref="KeyValuePair{T1,T2}"/>.
		/// </summary>
		/// <param name="mapping"><see cref="SecurityIdMapping"/> value.</param>
		/// <returns><see cref="KeyValuePair{T1,T2}"/> value.</returns>
		public static explicit operator KeyValuePair<SecurityId, SecurityId>(SecurityIdMapping mapping)
		{
			return new KeyValuePair<SecurityId, SecurityId>(mapping.StockSharpId, mapping.AdapterId);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"{StockSharpId}<->{AdapterId}";
		}
	}
}