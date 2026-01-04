namespace StockSharp.Algo.Strategies.Optimization;

using Ecng.Compilation;

/// <summary>
/// Default implementation of <see cref="IFitnessFormulaProvider"/> that compiles C# expressions.
/// </summary>
/// <param name="fileSystem">File system for compilation.</param>
public class FitnessFormulaProvider(IFileSystem fileSystem) : IFitnessFormulaProvider
{
	private readonly IFileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
	private readonly AssemblyLoadContextTracker _context = new();

	/// <inheritdoc />
	public Func<Strategy, decimal> Compile(string formula)
	{
		if (formula.IsEmpty())
			throw new ArgumentNullException(nameof(formula));

		if (CodeExtensions.TryGetCSharpCompiler() is null)
			throw new InvalidOperationException(LocalizedStrings.ServiceNotRegistered.Put(nameof(ICompiler)));

		var expression = formula.Compile<decimal>(_fileSystem, _context);

		if (!expression.Error.IsEmpty())
			throw new InvalidOperationException(expression.Error);

		var vars = expression.Variables.ToArray();
		var varGetters = new Func<Strategy, decimal>[vars.Length];

		for (var i = 0; i < vars.Length; ++i)
		{
			var par = GeneticSettings.FormulaVarsItemsSource.ParamFromVarName(vars[i]);
			varGetters[i] = s => s.StatisticManager.Parameters.FirstOrDefault(p => p.Type == par.Type)?.Value.To<decimal?>()
				?? throw new ArgumentException($"unable to use '{par.Name}' statistics parameter for fitness calculation");
		}

		return strategy =>
		{
			var varValues = new decimal[vars.Length];

			for (var i = 0; i < varValues.Length; ++i)
				varValues[i] = varGetters[i](strategy);

			return expression.Calculate(varValues);
		};
	}
}
