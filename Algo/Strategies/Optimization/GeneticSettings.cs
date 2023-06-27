using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Ecng.Collections;
using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.Serialization;

using GeneticSharp;

using StockSharp.Localization;
using StockSharp.Algo.Statistics;
using StockSharp.Messages;

namespace StockSharp.Algo.Strategies.Optimization;

/// <summary>
/// Genetic settings.
/// </summary>
public class GeneticSettings : IPersistable
{
	private class ReinsertionItemsSource : ItemsSourceBase<Type>
	{
		public ReinsertionItemsSource()
			: base(ReinsertionService.GetReinsertionTypes())
		{
		}
	}

	private class MutationItemsSource : ItemsSourceBase<Type>
	{
		public MutationItemsSource()
			: base(MutationService.GetMutationTypes())
		{
		}
	}

	private class CrossoverItemsSource : ItemsSourceBase<Type>
	{
		public CrossoverItemsSource()
			: base(CrossoverService.GetCrossoverTypes())
		{
		}
	}

	private class SelectionItemsSource : ItemsSourceBase<Type>
	{
		public SelectionItemsSource()
			: base(SelectionService.GetSelectionTypes())
		{
		}
	}

	/// <summary>
	/// </summary>
	public sealed class FormulaVarsItemsSource : ItemsSourceBase<IStatisticParameter>
	{
		static readonly IStatisticParameter[] _allParams = StatisticManager.GetAllParameters();
		static readonly Dictionary<StatisticParameterTypes, IStatisticParameter> _paramByType;

		static FormulaVarsItemsSource() => _paramByType = _allParams.ToDictionary(v => v.Type);

		/// <summary>
		/// </summary>
		public FormulaVarsItemsSource() : base(_allParams) { }

		/// <summary>
		/// </summary>
		public static string VarNameFromParam(IStatisticParameter p)
		{
			return p.Type switch
			{
				StatisticParameterTypes.NetProfit => "PnL",
				_ => p.Type.ToString(),
			};
		}

		/// <summary>
		/// </summary>
		public static IStatisticParameter ParamFromVarName(string varName)
		{
			return varName.ToLowerInvariant() switch
			{
				"pnl" => _paramByType[StatisticParameterTypes.NetProfit],
				_ => _paramByType.TryGetValue(varName.To<StatisticParameterTypes>()) ?? throw new ArgumentOutOfRangeException($"unknown variable '{varName}'"),
			};
		}

		/// <inheritdoc />
		protected override string GetName(IStatisticParameter value) => VarNameFromParam(value);

		/// <inheritdoc />
		protected override string GetDescription(IStatisticParameter value) => value.Description;
	}

	private string _fitness = nameof(Strategy.PnL);

	/// <summary>
	/// Fitness function formula. For example, 'PnL'.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.FitnessKey,
		Description = LocalizedStrings.FitnessFormulaKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 1)]
	[FormulaEditor(typeof(FormulaVarsItemsSource))]
	public string Fitness
	{
		get => _fitness;
		set => _fitness = value.ThrowIfEmpty(nameof(value));
	}

	private int _population = 8;

	/// <summary>
	/// The initial size of population.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PopulationKey,
		Description = LocalizedStrings.PopulationDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 1)]
	public int Population
	{
		get => _population;
		set => _population = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
	}

	private int _populationMax = 16;

	/// <summary>
	/// The maximum population.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PopulationMaxKey,
		Description = LocalizedStrings.PopulationMaxDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 2)]
	public int PopulationMax
	{
		get => _populationMax;
		set => _populationMax = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
	}

	private int _generationsMax = 20;

	/// <summary>
	/// Maximum number of generations.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.GenerationsKey,
		Description = LocalizedStrings.GenerationsMaxKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 3)]
	public int GenerationsMax
	{
		get => _generationsMax;
		set => _generationsMax = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
	}

	private int _generationsStagnation = 5;

	/// <summary>
	/// The genetic algorithm will be terminate when the best chromosome's fitness has no change in the last generations specified.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StagnationKey,
		Description = LocalizedStrings.StagnationDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 3)]
	public int GenerationsStagnation
	{
		get => _generationsStagnation;
		set => _generationsStagnation = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
	}

	private decimal _mutationProbability = (decimal)GeneticAlgorithm.DefaultMutationProbability;

	/// <summary>
	/// <see cref="GeneticAlgorithm.MutationProbability"/>
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MutationProbabilityKey,
		Description = LocalizedStrings.MutationProbabilityDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 4)]
	public decimal MutationProbability
	{
		get => _mutationProbability;
		set => _mutationProbability = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
	}

	private decimal _crossoverProbability = (decimal)GeneticAlgorithm.DefaultCrossoverProbability;

	/// <summary>
	/// <see cref="GeneticAlgorithm.CrossoverProbability"/>
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CrossoverProbabilityKey,
		Description = LocalizedStrings.CrossoverProbabilityDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 5)]
	public decimal CrossoverProbability
	{
		get => _crossoverProbability;
		set => _crossoverProbability = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
	}

	private Type _reinsertion = typeof(ElitistReinsertion);

	/// <summary>
	/// <see cref="IReinsertion"/>
	/// </summary>
	[ItemsSource(typeof(ReinsertionItemsSource))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ReinsertionKey,
		Description = LocalizedStrings.ReinsertionDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 6)]
	public Type Reinsertion
	{
		get => _reinsertion;
		set => _reinsertion = value ?? throw new ArgumentNullException(nameof(value));
	}

	private Type _mutation = typeof(UniformMutation);

	/// <summary>
	/// <see cref="IMutation"/>
	/// </summary>
	[ItemsSource(typeof(MutationItemsSource))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MutationKey,
		Description = LocalizedStrings.MutationDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 7)]
	public Type Mutation
	{
		get => _mutation;
		set => _mutation = value ?? throw new ArgumentNullException(nameof(value));
	}

	private Type _crossover = typeof(OnePointCrossover);

	/// <summary>
	/// <see cref="ICrossover"/>
	/// </summary>
	[ItemsSource(typeof(CrossoverItemsSource))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CrossoverKey,
		Description = LocalizedStrings.CrossoverDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 8)]
	public Type Crossover
	{
		get => _crossover;
		set => _crossover = value ?? throw new ArgumentNullException(nameof(value));
	}

	private Type _selection = typeof(TournamentSelection);

	/// <summary>
	/// <see cref="ISelection"/>
	/// </summary>
	[ItemsSource(typeof(SelectionItemsSource))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SelectionKey,
		Description = LocalizedStrings.SelectionDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 9)]
	public Type Selection
	{
		get => _selection;
		set => _selection = value ?? throw new ArgumentNullException(nameof(value));
	}

	void IPersistable.Load(SettingsStorage storage)
	{
		Fitness = storage.GetValue(nameof(Fitness), Fitness);
		Population = storage.GetValue(nameof(Population), Population);
		PopulationMax = storage.GetValue(nameof(PopulationMax), PopulationMax);
		GenerationsMax = storage.GetValue(nameof(GenerationsMax), GenerationsMax);
		GenerationsStagnation = storage.GetValue(nameof(GenerationsStagnation), GenerationsStagnation);
		MutationProbability = storage.GetValue(nameof(MutationProbability), MutationProbability);
		CrossoverProbability = storage.GetValue(nameof(CrossoverProbability), CrossoverProbability);
		Reinsertion = storage.GetValue(nameof(Reinsertion), Reinsertion);
		Mutation = storage.GetValue(nameof(Mutation), Mutation);
		Crossover = storage.GetValue(nameof(Crossover), Crossover);
		Selection = storage.GetValue(nameof(Selection), Selection);
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(Fitness), Fitness)
			.Set(nameof(Population), Population)
			.Set(nameof(PopulationMax), PopulationMax)
			.Set(nameof(GenerationsMax), GenerationsMax)
			.Set(nameof(GenerationsStagnation), GenerationsStagnation)
			.Set(nameof(MutationProbability), MutationProbability)
			.Set(nameof(CrossoverProbability), CrossoverProbability)
			.Set(nameof(Reinsertion), Reinsertion)
			.Set(nameof(Mutation), Mutation)
			.Set(nameof(Crossover), Crossover)
			.Set(nameof(Selection), Selection)
		;
	}
}
