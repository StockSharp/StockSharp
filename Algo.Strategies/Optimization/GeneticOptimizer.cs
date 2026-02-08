namespace StockSharp.Algo.Strategies.Optimization;

using GeneticSharp;

/// <summary>
/// The genetic optimizer of strategies.
/// </summary>
public class GeneticOptimizer : BaseOptimizer
{
	private class StrategyFitness : IAsyncFitness, IFitness
	{
		private readonly GeneticOptimizer _optimizer;
		private readonly Strategy _strategy;
		private readonly Func<Strategy, decimal> _calcFitness;
		private readonly DateTime _startTime;
		private readonly DateTime _stopTime;
		private readonly SynchronizedDictionary<DynamicTuple, decimal> _cache = [];

		public StrategyFitness(GeneticOptimizer optimizer, Strategy strategy, Func<Strategy, decimal> calcFitness, DateTime startTime, DateTime stopTime)
		{
			_optimizer = optimizer ?? throw new ArgumentNullException(nameof(optimizer));
			_strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
			_calcFitness = calcFitness ?? throw new ArgumentNullException(nameof(calcFitness));
			_startTime = startTime;
			_stopTime = stopTime;

			if (_strategy.Security is null)
				throw new ArgumentException(LocalizedStrings.SecurityNotSpecified, nameof(strategy));

			if (_strategy.Portfolio is null)
				throw new ArgumentException(LocalizedStrings.PortfolioNotSpecified, nameof(strategy));
		}

		double IFitness.Evaluate(IChromosome chromosome)
			=> throw new NotSupportedException();

		async Task<double> IAsyncFitness.EvaluateAsync(IChromosome chromosome, CancellationToken cancellationToken)
		{
			var spc = (StrategyParametersChromosome)chromosome;

			Strategy strategy;

			using (new Scope<StrategyContext>(new() { ExcludeUI = true }))
				strategy = _strategy.Clone();

			strategy.Security = _strategy.Security;

			var genes = spc.GetGenes();
			var parameters = new IStrategyParam[genes.Length];

			for (var i = 0; i < genes.Length; i++)
			{
				var gene = genes[i];
				var (param, value) = ((IStrategyParam, object))gene.Value;
				var realParam = strategy.Parameters[param.Id];
				realParam.Value = value;
				parameters[i] = realParam;
			}

			var key = new DynamicTuple([.. parameters.Select(p => p.Value)]);

			if (_cache.TryGetValue(key, out var fitVal))
				return (double)fitVal;

			// Check iteration limit
			if (!_optimizer.TryConsumeIteration())
				return double.MinValue;

			var adapterCache = _optimizer.AllocateAdapterCache();
			var storageCache = _optimizer.AllocateStorageCache();

			try
			{
				await _optimizer.TryNextRunAsync(_startTime, _stopTime,
					pfProvider =>
					{
						strategy.Portfolio = pfProvider.LookupByPortfolioName((_strategy.Portfolio?.Name).IsEmpty(Extensions.SimulatorPortfolioName));

						return (strategy, parameters);
					},
					adapterCache,
					storageCache,
					cancellationToken);

				fitVal = _calcFitness(strategy);

				_cache[key] = fitVal;

				return (double)fitVal;
			}
			finally
			{
				_optimizer.FreeAdapterCache(adapterCache);
				_optimizer.FreeStorageCache(storageCache);
			}
		}
	}

	private class StrategyParametersChromosome : ChromosomeBase
	{
		private readonly (IStrategyParam param, Func<object> getValue)[] _parameters;

		public StrategyParametersChromosome((IStrategyParam, Func<object>)[] parameters)
			: base(parameters.CheckOnNull(nameof(parameters)).Length)
		{
			_parameters = parameters;

			for (var i = 0; i < Length; i++)
			{
				ReplaceGene(i, GenerateGene(i));
			}
		}

		public override IChromosome CreateNew()
			=> new StrategyParametersChromosome(_parameters);

		public override Gene GenerateGene(int geneIndex)
		{
			var (p, g) = _parameters[geneIndex];

			return new((p, g()));
		}
	}

	private class MaxIterationsTermination(GeneticOptimizer optimizer) : TerminationBase
	{
		private readonly GeneticOptimizer _optimizer = optimizer ?? throw new ArgumentNullException(nameof(optimizer));

		protected override bool PerformHasReached(IGeneticAlgorithm geneticAlgorithm)
			=> !_optimizer.HasRemainingIterations();
	}

	private GeneticAlgorithm _ga;

	private readonly Lock _iterLock = new();
	private int? _maxIterations;
	private int _consumedIterations;

	private readonly IFitnessFormulaProvider _formulaProvider;

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneticOptimizer"/>.
	/// </summary>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
	/// <param name="storageRegistry">Market data storage.</param>
	/// <param name="fileSystem">File system.</param>
	public GeneticOptimizer(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IStorageRegistry storageRegistry, IFileSystem fileSystem)
		: this(securityProvider, portfolioProvider, storageRegistry.CheckOnNull(nameof(storageRegistry)).ExchangeInfoProvider, storageRegistry, StorageFormats.Binary, storageRegistry.DefaultDrive, new FitnessFormulaProvider(fileSystem))
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneticOptimizer"/>.
	/// </summary>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	/// <param name="storageRegistry">Market data storage.</param>
	/// <param name="storageFormat">The format of market data. <see cref="StorageFormats.Binary"/> is used by default.</param>
	/// <param name="drive">The storage which is used by default. By default, <see cref="IStorageRegistry.DefaultDrive"/> is used.</param>
	/// <param name="fileSystem">File system.</param>
	public GeneticOptimizer(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IExchangeInfoProvider exchangeInfoProvider, IStorageRegistry storageRegistry, StorageFormats storageFormat, IMarketDataDrive drive, IFileSystem fileSystem)
		: this(securityProvider, portfolioProvider, exchangeInfoProvider, storageRegistry, storageFormat, drive, new FitnessFormulaProvider(fileSystem))
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneticOptimizer"/>.
	/// </summary>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	/// <param name="storageRegistry">Market data storage.</param>
	/// <param name="storageFormat">The format of market data. <see cref="StorageFormats.Binary"/> is used by default.</param>
	/// <param name="drive">The storage which is used by default. By default, <see cref="IStorageRegistry.DefaultDrive"/> is used.</param>
	/// <param name="formulaProvider">Fitness formula provider.</param>
	public GeneticOptimizer(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IExchangeInfoProvider exchangeInfoProvider, IStorageRegistry storageRegistry, StorageFormats storageFormat, IMarketDataDrive drive, IFitnessFormulaProvider formulaProvider)
		: base(securityProvider, portfolioProvider, exchangeInfoProvider, storageRegistry, storageFormat, drive)
	{
		_formulaProvider = formulaProvider ?? throw new ArgumentNullException(nameof(formulaProvider));
	}

	/// <summary>
	/// <see cref="GeneticSettings"/>
	/// </summary>
	public GeneticSettings Settings { get; } = new();

	/// <summary>
	/// Try to consume one iteration from the limit.
	/// </summary>
	/// <returns>True if iteration was consumed, false if limit reached.</returns>
	private bool TryConsumeIteration()
	{
		using (_iterLock.EnterScope())
		{
			if (_maxIterations is null)
				return true;

			if (_consumedIterations >= _maxIterations.Value)
				return false;

			_consumedIterations++;
			return true;
		}
	}

	/// <summary>
	/// Check if there are remaining iterations.
	/// </summary>
	private bool HasRemainingIterations()
	{
		using (_iterLock.EnterScope())
		{
			if (_maxIterations is null)
				return true;

			return _consumedIterations < _maxIterations.Value;
		}
	}

	private GeneticAlgorithm SetupGA(
		DateTime startTime, DateTime stopTime,
		Strategy strategy,
		IEnumerable<(IStrategyParam param, object from, object to, object step, IEnumerable values)> parameters,
		Func<Strategy, decimal> calcFitness,
		ISelection selection,
		ICrossover crossover,
		IMutation mutation)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		if (_ga is not null)
			throw new InvalidOperationException("Not stopped.");

		var paramArr = parameters.Select(t =>
		{
			var param = t.param;
			var values = t.values?.Cast<object>().ToArray();

			if (values?.Length > 0)
				return (param, () => RandomGen.GetElement(values));

			var from = t.from ?? throw new ArgumentException(LocalizedStrings.ParamDoesntContain.Put(param.Id, LocalizedStrings.From));
			var to = t.to ?? throw new ArgumentException(LocalizedStrings.ParamDoesntContain.Put(param.Id, LocalizedStrings.Until));
			var step = t.step;

			Func<object> getValue;

			var type = param.Type;
			type = type.GetUnderlyingType() ?? type;

			if (step is null && type != typeof(bool))
				throw new ArgumentException(LocalizedStrings.ParamDoesntContain.Put(param.Id, LocalizedStrings.Step));

			if (type == typeof(Unit))
			{
				var fu = (Unit)from;
				var tu = (Unit)to;
				var su = (Unit)step;

				if (su.Value == 0)
					throw new ArgumentException(LocalizedStrings.ChangeStepCannotBeZero);
				else if (su.Value < 0)
				{
					(fu, tu) = (tu, fu);
					su = new(su.Value.Abs(), su.Type);
				}

				var scale = su.Value.GetDecimalInfo().EffectiveScale;

				getValue = () => new Unit(RandomGen.GetDecimal(fu.Value, tu.Value, scale).Round(su.Value, null), fu.Type);
			}
			else if (type == typeof(decimal))
			{
				var fd = (decimal)from;
				var td = (decimal)to;
				var sd = (decimal)step;

				if (sd == 0)
					throw new ArgumentException(LocalizedStrings.ChangeStepCannotBeZero);
				else if (sd < 0)
				{
					(fd, td) = (td, fd);
					sd = sd.Abs();
				}

				var scale = sd.GetDecimalInfo().EffectiveScale;

				getValue = () => RandomGen.GetDecimal(fd, td, scale).Round(sd, null);
			}
			else if (type == typeof(bool))
			{
				getValue = () => RandomGen.GetBool();
			}
			else if (type.IsPrimitive() || type == typeof(TimeSpan))
			{
				if (type.IsNumeric() && !type.IsNumericInteger())
				{
					var fd = (decimal)from;
					var td = (decimal)to;
					var sd = step.To<decimal>();

					if (sd == 0)
						throw new ArgumentException(LocalizedStrings.ChangeStepCannotBeZero);
					else if (sd < 0)
					{
						(fd, td) = (td, fd);
						sd = sd.Abs();
					}

					var scale = sd.GetDecimalInfo().EffectiveScale;

					getValue = () =>
					{
						var d = RandomGen.GetDecimal(fd, td, scale);

						if (sd != 1)
							d = (d / sd) * sd;

						return d.To(type);
					};
				}
				else
				{
					var fl = from.To<long>();
					var tl = to.To<long>();
					var sl = step.To<long>();

					if (sl == 0)
						throw new ArgumentException(LocalizedStrings.ChangeStepCannotBeZero);
					else if (sl < 0)
					{
						(fl, tl) = (tl, fl);
						sl = sl.Abs();
					}

					getValue = () =>
					{
						var l = RandomGen.GetLong(fl, tl);

						if (sl != 1)
							l = (l / sl) * sl;

						return l.To(type);
					};
				}
			}
			else
				throw new NotSupportedException(LocalizedStrings.TypeNotSupported.Put(type));

			return (param, getValue);
		}).ToArray();

		var population = new Population(Settings.Population, Settings.PopulationMax, new StrategyParametersChromosome(paramArr));

		calcFitness ??= _formulaProvider.Compile(Settings.Fitness);
		selection ??= Settings.Selection.CreateInstance<ISelection>();
		crossover ??= Settings.Crossover.CreateInstance<ICrossover>();
		mutation ??= Settings.Mutation.CreateInstance<IMutation>();

		if (mutation is SequenceMutationBase && paramArr.Length < mutation.MinChromosomeLength)
			throw new InvalidOperationException($"Optimization parameters for '{mutation.GetType()}' mutation must be at least {mutation.MinChromosomeLength}.");

		// Setup iteration limit
		using (_iterLock.EnterScope())
		{
			_maxIterations = EmulationSettings.MaxIterations > 0 ? EmulationSettings.MaxIterations : null;
			_consumedIterations = 0;
		}

		var terminations = new List<ITermination>();

		if (Settings.GenerationsStagnation > 0)
			terminations.Add(new FitnessStagnationTermination(Settings.GenerationsStagnation));

		if (Settings.GenerationsMax > 0)
			terminations.Add(new GenerationNumberTermination(Settings.GenerationsMax));

		if (_maxIterations is not null)
			terminations.Add(new MaxIterationsTermination(this));

		if (terminations.Count == 0)
			throw new InvalidOperationException("No termination set.");

		var termination = terminations.Count == 1
			? terminations[0]
			: new OrTermination([.. terminations]);

		_ga = new(population, new StrategyFitness(this, strategy, calcFitness, startTime, stopTime), selection, crossover, mutation)
		{
			TaskExecutor = new ParallelTaskExecutor
			{
				MinThreads = 1,
				MaxThreads = EmulationSettings.BatchSize,
			},

			Termination = termination,

			MutationProbability = (float)Settings.MutationProbability,
			CrossoverProbability = (float)Settings.CrossoverProbability,

			Reinsertion = Settings.Reinsertion.CreateInstance<IReinsertion>(),
		};

		return _ga;
	}

	/// <summary>
	/// Run optimization and yield completed iterations as they finish.
	/// </summary>
	/// <param name="startTime">Date in history for starting the paper trading.</param>
	/// <param name="stopTime">Date in history to stop the paper trading (date is included).</param>
	/// <param name="strategy">Strategy.</param>
	/// <param name="parameters">Parameters used to generate chromosomes.</param>
	/// <param name="calcFitness">Calc fitness value function. If <see langword="null"/> the value from <see cref="GeneticSettings.Fitness"/> will be used.</param>
	/// <param name="selection"><see cref="ISelection"/>. If <see langword="null"/> the value from <see cref="GeneticSettings.Selection"/> will be used.</param>
	/// <param name="crossover"><see cref="ICrossover"/>. If <see langword="null"/> the value from <see cref="GeneticSettings.Crossover"/> will be used.</param>
	/// <param name="mutation"><see cref="IMutation"/>. If <see langword="null"/> the value from <see cref="GeneticSettings.Mutation"/> will be used.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Async enumerable of completed (strategy, parameters) pairs.</returns>
	[CLSCompliant(false)]
	public async IAsyncEnumerable<(Strategy Strategy, IStrategyParam[] Parameters)> RunAsync(
		DateTime startTime,
		DateTime stopTime,
		Strategy strategy,
		IEnumerable<(IStrategyParam param, object from, object to, object step, IEnumerable values)> parameters,
		Func<Strategy, decimal> calcFitness = default,
		ISelection selection = default,
		ICrossover crossover = default,
		IMutation mutation = default,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var ga = SetupGA(startTime, stopTime, strategy, parameters, calcFitness, selection, crossover, mutation);

		var estimatedIterations = Settings.Population * Math.Max(1, Settings.GenerationsMax);
		var maxIters = EmulationSettings.MaxIterations;
		if (maxIters > 0 && estimatedIterations > maxIters)
			estimatedIterations = maxIters;

		InitializeRunAsync(estimatedIterations, cancellationToken);

		// Stop GA when cancelled
		cancellationToken.Register(() =>
		{
			try { ga.Stop(); } catch (InvalidOperationException) { }
		});

		_ = Task.Run(() =>
		{
			try
			{
				ga.Start();
			}
			catch (Exception ex) when (cancellationToken.IsCancellationRequested)
			{
				this.AddWarningLog("GA cancelled: {0}", ex.Message);
			}
			finally
			{
				CompleteChannel();
				_ga = null;
			}
		}, CancellationToken.None);

		await foreach (var result in ReadResultsAsync(cancellationToken))
		{
			yield return result;
		}
	}
}
