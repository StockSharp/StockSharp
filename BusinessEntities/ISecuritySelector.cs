using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StockSharp.BusinessEntities;

/// <summary>
/// Allows user to select one or more securities from the provider.
/// </summary>
public interface ISecuritySelector
{
	/// <summary>
	/// Select securities.
	/// </summary>
	ValueTask<IEnumerable<Security>> SelectSecurities(bool allowMultiple, CancellationToken token);
}
