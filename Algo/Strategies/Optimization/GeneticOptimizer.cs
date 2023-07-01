using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Compilation;

using GeneticSharp;

using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Logging;

namespace StockSharp.Algo.Strategies.Optimization;

/// <summary>
/// The genetic optimizer of strategies.
/// </summary>
public class GeneticOptimizer : BaseOptimizer
{
	private class StrategyFitness : IFitness
	{
		private readonly GeneticOptimizer _optimizer;
		private readonly Strategy _strategy;
		private readonly Func<Strategy, decimal> _calcFitness;

		public StrategyFitness(GeneticOptimizer optimizer, Strategy strategy, Func<Strategy, decimal> calcFitness)
		{
			_optimizer = optimizer ?? throw new ArgumentNullException(nameof(optimizer));
			_strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
			_calcFitness = calcFitness ?? throw new ArgumentNullException(nameof(calcFitness));
		}

		double IFitness.Evaluate(IChromosome chromosome)
		{
			if (_optimizer._leftIterations is not null)
			{
				lock (_optimizer._leftIterLock)
				{
					_optimizer._leftIterations = _optimizer._leftIterations.Value - 1;

					if (_optimizer._leftIterations < 0)
						return double.MinValue;
				}
			}

			var spc = (StrategyParametersChromosome)chromosome;

			Strategy strategy;

			using(new Scope<CloneHelper.CloneWithoutUI>())
				strategy = _strategy.Clone();

			strategy.Security = _strategy.Security;
			strategy.Portfolio = Portfolio.CreateSimulator();

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

			using var wait = new ManualResetEvent(false);
			
			_optimizer._events.Add(wait);

			try
			{
				var adapterCache = _optimizer.AllocateAdapterCache();
				var storageCache = _optimizer.AllocateStorageCache();

				_optimizer.TryNextRun(
					() => (strategy, parameters),
					adapterCache,
					storageCache,
					() =>
					{
						_optimizer.FreeAdapterCache(adapterCache);
						_optimizer.FreeStorageCache(storageCache);

						if (!_optimizer._events.Contains(wait))
							return;

						try
						{
							wait.Set();
						}
						catch (ObjectDisposedException)
						{
						}
					});

				wait.WaitOne();

				return (double)_calcFitness(strategy);
			}
			finally
			{
				_optimizer._events.Remove(wait);
			}
		}
	}

	private class StrategyParametersChromosome : ChromosomeBase
	{
		private readonly (IStrategyParam param, object from, object to, int precision, object value)[] _parameters;

		public StrategyParametersChromosome((IStrategyParam, object, object, int, object)[] parameters)
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
			var (p, f, t, precision, v) = _parameters[geneIndex];

			if (f is null && t is null && v is null)
				throw new InvalidOperationException($"No values for {p.Name}.");

			object val;

			if (p.Type == typeof(Security))
			{
				val = RandomGen.GetElement((IEnumerable<Security>)v);
			}
			else if (p.Type == typeof(Unit))
			{
				if (f is not null && t is not null)
					val = new Unit(RandomGen.GetDecimal(((Unit)f).Value, ((Unit)f).Value, precision), ((Unit)f).Type);
				else
					val = RandomGen.GetElement((IEnumerable<Unit>)v);
			}
			else if (p.Type == typeof(decimal))
			{
				if (f is not null && t is not null)
					val = RandomGen.GetDecimal(f.To<decimal>(), t.To<decimal>(), precision);
				else
					val = RandomGen.GetElement(((IEnumerable)v).Cast<object>());

				val = val.To(p.Type);
			}
			else if (p.Type.IsPrimitive())
			{
				if (f is not null && t is not null)
					val = RandomGen.GetLong(f.To<long>(), t.To<long>());
				else
					val = RandomGen.GetElement(((IEnumerable)v).Cast<object>());

				val = val.To(p.Type);
			}
			else
				throw new NotSupportedException($"Type {p.Type} not supported.");

			return new((p, val));
		}
	}

	private class MaxIterationsTermination : TerminationBase
	{
		private readonly GeneticOptimizer _optimizer;

		public MaxIterationsTermination(GeneticOptimizer optimizer)
        {
			_optimizer = optimizer ?? throw new ArgumentNullException(nameof(optimizer));
		}

		protected override bool PerformHasReached(IGeneticAlgorithm geneticAlgorithm)
		{
			lock (_optimizer._leftIterLock)
				return _optimizer._leftIterations <= 0;
		}
	}

	private readonly SynchronizedSet<ManualResetEvent> _events = new();
	private GeneticAlgorithm _ga;

	private readonly SyncObject _leftIterLock = new();
	private int? _leftIterations;

	/// <summary>
	/// Initializes a new instance of the <see cref="GeneticOptimizer"/>.
	/// </summary>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
	/// <param name="storageRegistry">Market data storage.</param>
	public GeneticOptimizer(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IStorageRegistry storageRegistry)
		: base(securityProvider, portfolioProvider, storageRegistry.CheckOnNull(nameof(storageRegistry)).ExchangeInfoProvider, storageRegistry, StorageFormats.Binary, storageRegistry.DefaultDrive)
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
	public GeneticOptimizer(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IExchangeInfoProvider exchangeInfoProvider, IStorageRegistry storageRegistry, StorageFormats storageFormat, IMarketDataDrive drive)
		: base(securityProvider, portfolioProvider, exchangeInfoProvider, storageRegistry, storageFormat, drive)
	{
	}

	/// <summary>
	/// <see cref="GeneticSettings"/>
	/// </summary>
	public GeneticSettings Settings { get; } = new();

	private Func<Strategy, decimal> ToFitness(string formula)
	{
		if (formula.IsEmpty())
			throw new ArgumentNullException(nameof(formula));

		if (ServicesRegistry.TryCompiler is null)
			throw new InvalidOperationException($"Service {nameof(ICompiler)} is not initialized.");

		var expression = formula.Compile<decimal>();

		if (!expression.Error.IsEmpty())
			throw new InvalidOperationException(expression.Error);

		var vars = expression.Variables.ToArray();
		var varGetters = new Func<Strategy, decimal>[vars.Length];

		for (var i = 0; i < vars.Length; ++i)
		{
			var par = GeneticSettings.FormulaVarsItemsSource.ParamFromVarName(vars[i]);
			varGetters[i] = s => s.StatisticManager.Parameters.FirstOrDefault(p => p.Type == par.Type)?.Value.To<decimal?>() ?? throw new ArgumentException($"unable to use '{par.Name}' statistics parameter for fitness calculation");
		}

		return stra =>
		{
			var varValues = new decimal[vars.Length];

			for(var i = 0; i < varValues.Length; ++i)
				varValues[i] = varGetters[i](stra);

			try
			{
				return expression.Calculate(varValues);
			}
			catch (ArithmeticException ex)
			{
				this.AddErrorLog(ex);
				return decimal.MinValue;
			}
		};
	}

	/// <summary>
	/// Start optimization.
	/// </summary>
	/// <param name="strategy">Strategy.</param>
	/// <param name="parameters">Parameters used to generate chromosomes.</param>
	/// <param name="calcFitness">Calc fitness value function. If <see langword="null"/> the value from <see cref="GeneticSettings.Fitness"/> will be used.</param>
	/// <param name="selection"><see cref="ISelection"/>. If <see langword="null"/> the value from <see cref="GeneticSettings.Selection"/> will be used.</param>
	/// <param name="crossover"><see cref="ICrossover"/>. If <see langword="null"/> the value from <see cref="GeneticSettings.Crossover"/> will be used.</param>
	/// <param name="mutation"><see cref="IMutation"/>. If <see langword="null"/> the value from <see cref="GeneticSettings.Mutation"/> will be used.</param>
	[CLSCompliant(false)]
	public void Start(
		Strategy strategy,
		IEnumerable<(IStrategyParam param, object from, object to, int precision, object value)> parameters,
		Func<Strategy, decimal> calcFitness = default,
		ISelection selection = default,
		ICrossover crossover = default,
		IMutation mutation = default
	)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		if (_ga is not null)
			throw new InvalidOperationException("Not stopped.");

		var paramArr = parameters.ToArray();

		var population = new Population(Settings.Population, Settings.PopulationMax, new StrategyParametersChromosome(paramArr));

		calcFitness ??= ToFitness(Settings.Fitness);
		selection ??= Settings.Selection.CreateInstance<ISelection>();
		crossover ??= Settings.Crossover.CreateInstance<ICrossover>();
		mutation ??= Settings.Mutation.CreateInstance<IMutation>();

		if (mutation is SequenceMutationBase && paramArr.Length < 3)
			throw new InvalidOperationException($"Optimization parameters for '{mutation.GetType()}' mutation must be at least 3.");

		_leftIterations = null;

		var terminations = new List<ITermination>();

		if (Settings.GenerationsStagnation > 0)
			terminations.Add(new FitnessStagnationTermination(Settings.GenerationsStagnation));

		if (Settings.GenerationsMax > 0)
			terminations.Add(new GenerationNumberTermination(Settings.GenerationsMax));

		if (EmulationSettings.MaxIterations > 0)
		{
			_leftIterations = EmulationSettings.MaxIterations;
			terminations.Add(new MaxIterationsTermination(this));
		}

		if (terminations.Count == 0)
			throw new InvalidOperationException("No termination set.");

		var termination = terminations.Count == 1
			? terminations[0]
			: new OrTermination(terminations.ToArray());

		_ga = new(population, new StrategyFitness(this, strategy, calcFitness), selection, crossover, mutation)
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

		//_ga.GenerationRan += OnGenerationRan;
		_ga.TerminationReached += OnTerminationReached;

		OnStart();

		Task.Run(_ga.Start);
	}

	/// <inheritdoc />
	protected override int GetProgress()
	{
		var max = Settings.GenerationsMax;
		var ga = _ga;

		return max > 0 && ga is not null ? (int)(ga.GenerationsNumber * 100.0 / max) : -1;
	}

	private void OnTerminationReached(object sender, EventArgs e)
	{
		RaiseStopped();
	}

	//private void OnGenerationRan(object sender, EventArgs e)
	//{
	//}

	/// <inheritdoc />
	public override void Suspend()
	{
		base.Suspend();

		_ga.Stop();
	}

	/// <inheritdoc />
	public override void Resume()
	{
		base.Resume();

		Task.Run(async () =>
		{
			await Task.Yield();
			_ga.Resume();
		});
	}

	/// <inheritdoc />
	public override void Stop()
	{
		base.Stop();

		_ga?.Stop();
		_events.CopyAndClear().ForEach(e =>
		{
			try
			{
				e.Set();
			}
			catch
			{
				// handle can be already disposed
			}
		});

		RaiseStopped();
	}
}
