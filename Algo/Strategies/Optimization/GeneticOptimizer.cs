namespace StockSharp.Algo.Strategies.Optimization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Collections;
using Ecng.Common;

using GeneticSharp;

using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

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
			var spc = (StrategyParametersChromosome)chromosome;
			var strategy = _strategy.Clone();

			var genes = spc.GetGenes();
			var parameters = new IStrategyParam[genes.Length];

			for (var i = 0; i < genes.Length; i++)
			{
				var gene = genes[i];
				var (param, value) = ((IStrategyParam, object))gene.Value;
				_strategy.Parameters[param.Id].Value = value;
				parameters[i] = param;
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

						wait.Set();
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
		private readonly (IStrategyParam param, object from, object to, int precision)[] _parameters;

		public StrategyParametersChromosome((IStrategyParam param, object from, object to, int precision)[] parameters)
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
			var (p, f, t, precision) = _parameters[geneIndex];

			object v;

			if (p.Type == typeof(Security))
			{
				v = RandomGen.GetElement((IEnumerable<Security>)f);
			}
			else if (p.Type == typeof(Unit))
			{
				var fu = (Unit)f;
				var tu = (Unit)f;

				v = new Unit(RandomGen.GetDecimal(fu.Value, tu.Value, precision), fu.Type);
			}
			else if (p.Type == typeof(decimal))
			{
				v = RandomGen.GetDecimal(f.To<decimal>(), t.To<decimal>(), precision).To(p.Type);
			}
			else if (p.Type.IsPrimitive())
			{
				v = RandomGen.GetLong(f.To<long>(), t.To<long>()).To(p.Type);
			}
			else
				throw new NotSupportedException($"Type {p.Type} not supported.");

			return new((p, v));
		}
	}

	private readonly SynchronizedSet<ManualResetEvent> _events = new();
	private GeneticAlgorithm _ga;

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

	private static Func<Strategy, decimal> ToFitness(string formula)
	{
		if (formula.IsEmpty())
			throw new ArgumentNullException(nameof(formula));

		// TODO
		return s => s.PnL;
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
		IEnumerable<(IStrategyParam param, object from, object to, int precision)> parameters,
		Func<Strategy, decimal> calcFitness = default,
		ISelection selection = default,
		ICrossover crossover = default,
		IMutation mutation = default
	)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		if (calcFitness is null)
			throw new ArgumentNullException(nameof(calcFitness));

		if (_ga is not null)
			throw new InvalidOperationException("Not stopped.");

		var population = new Population(Settings.Population, Settings.PopulationMax, new StrategyParametersChromosome(parameters.ToArray()));

		calcFitness ??= ToFitness(Settings.Fitness);
		selection ??= Settings.Selection.CreateInstance<ISelection>();
		crossover ??= Settings.Crossover.CreateInstance<ICrossover>();
		mutation ??= Settings.Mutation.CreateInstance<IMutation>();

		var iterCount = EmulationSettings.MaxIterations.Max(1);

		_ga = new(population, new StrategyFitness(this, strategy, calcFitness), selection, crossover, mutation)
		{
			TaskExecutor = new ParallelTaskExecutor
			{
				MinThreads = 1,
				MaxThreads = EmulationSettings.BatchSize,
			},

			Termination = new OrTermination(
				new FitnessStagnationTermination(Settings.StagnationGenerations),
				new GenerationNumberTermination(iterCount)
			),

			MutationProbability = (float)Settings.MutationProbability,
			CrossoverProbability = (float)Settings.CrossoverProbability,

			Reinsertion = Settings.Reinsertion.CreateInstance<IReinsertion>(),
		};

		//_ga.GenerationRan += OnGenerationRan;
		_ga.TerminationReached += OnTerminationReached;

		OnStart(iterCount * EmulationSettings.BatchSize);

		Task.Run(async () =>
		{
			await Task.Yield();
			_ga.Start();
		});
	}

	private void RaiseStop()
	{
		State = ChannelStates.Stopped;
	}

	private void OnTerminationReached(object sender, EventArgs e)
	{
		if (State != ChannelStates.Stopping)
			State = ChannelStates.Stopping;

		RaiseStop();
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

		_ga.Stop();
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

		RaiseStop();
	}
}