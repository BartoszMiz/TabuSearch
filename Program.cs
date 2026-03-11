using System.Text;
using Gurobi;
using MiniProject1;

Random rng = new(42);
if (args.Length == 0)
{
	PrintUsage();
	return -1;
}
string command = args[0].Trim().ToLower();
switch (command)
{
	case "tune":
		Tune();
		return -1;
	case "solve":
		try
		{
			int iterationCount = int.Parse(args[1]);
			int neighborCount = int.Parse(args[2]);
			int tabuTimeout = int.Parse(args[3]);
			Solve(iterationCount, neighborCount, tabuTimeout);
			return 0;
		}
		catch (Exception)
		{
			Console.Error.WriteLine("Failed to parse the arguments!");
			PrintUsage();
			return -1;
		}
	default:
		Console.Error.WriteLine($"Unknown command: {command}");
		return -1;
}

void PrintUsage()
{
	Console.Error.WriteLine($"Usage: {Environment.ProcessPath} <command> [options]");
	Console.Error.WriteLine(
		"""
		Available commands:
			tune - runs tuning and outputs best parameters to STDOUT; takes no options
			solve - attempts to solve a list of predetermined Knapsack problems
				Options: <iteration count> <neighbor count> <tabu timeout>
				Example: solve 100 5 3
		""");
}

void Solve(int iterationCount, int neighborCount, int tabuTimeout)
{
	GRBEnv gurobiEnv = new();
	gurobiEnv.Set("OutputFlag", "0");
	gurobiEnv.Start();
	
	Instance[] instances =
	[
		new()
		{
			Items = [new Item(2, 3), new Item(3, 7), new Item(5, 8), new Item(7, 6), new Item(3, 5)],
			Capacity = 10
		}
	];

	foreach (Instance instance in instances)
	{
		TabuSearch search = new(instance.Items, instance.Capacity, iterationCount, neighborCount, tabuTimeout);
		Solution solution = search.Execute();

		GRBModel model = new(gurobiEnv);
		GRBVar[] decision = model.AddVars(instance.Items.Length, GRB.BINARY);
		GRBLinExpr constraintEq = new();
		foreach ((Item item, GRBVar variable) in instance.Items.Zip(decision))
		{
			constraintEq.AddTerm(item.Weight, variable);
		}

		model.AddConstr(constraintEq, GRB.LESS_EQUAL, instance.Capacity, string.Empty);

		GRBLinExpr objectiveEq = new();
		foreach ((Item item, GRBVar variable) in instance.Items.Zip(decision))
		{
			objectiveEq.AddTerm(item.Value, variable);
		}
		
		model.SetObjective(objectiveEq, GRB.MAXIMIZE);
		model.Optimize();
		Solution solverSolution = new(instance.Items.Length);
		for (int i = 0; i < instance.Items.Length; i++)
			solverSolution[i] = Math.Abs(decision[i].X - 1) < float.Epsilon;
		
		Console.WriteLine($"Capacity: {instance.Capacity}");
		Console.WriteLine($"Solution from search: {solution.ToString(instance.Items)}");
		Console.WriteLine($"Solution from solver: {solverSolution.ToString(instance.Items)}");
	}
}

void Tune()
{
	int[] iterationCounts = [50, 100, 200, 500, 1_000, 2_500];
	int[] neighborCounts = [1, 3, 5, 6, 7, 8, 10, 15];
	int[] tabuTimeouts = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

	int defaultIterationCount = iterationCounts[iterationCounts.Length / 2];
	int defaultNeighborsCount = neighborCounts[neighborCounts.Length / 2];
	const int instanceCount = 100;
	const int repetitions = 10;
	Span<int> bestIterationCounts = stackalloc int[repetitions];
	Span<int> bestNeighborsCounts = stackalloc int[repetitions];
	Span<int> bestTabuTimeouts = stackalloc int[repetitions];

	for (int i = 0; i < repetitions; i++)
	{
		List<(int parameter, double evaluation)> results = [];
		Instance[] instances = GenerateInstances(instanceCount);

		foreach (int tabuTimeout in tabuTimeouts)
		{
			double average = EvaluateCombination(defaultIterationCount, defaultNeighborsCount, tabuTimeout, instances);
			results.Add((tabuTimeout, average));
		}

		int bestTabuTimeout = results.MaxBy(x => x.evaluation).parameter;
		results = [];

		foreach (int neighborCount in neighborCounts)
		{
			double average = EvaluateCombination(defaultIterationCount, neighborCount, bestTabuTimeout, instances);
			results.Add((neighborCount, average));
		}

		int bestNeighborsCount = results.MaxBy(x => x.evaluation).parameter;
		results = [];

		foreach (int iterationCount in iterationCounts)
		{
			double average = EvaluateCombination(iterationCount, bestNeighborsCount, bestTabuTimeout, instances);
			results.Add((iterationCount, average));
		}

		int bestIterationCount = results.MaxBy(x => x.evaluation).parameter;

		bestIterationCounts[i] = bestIterationCount;
		bestNeighborsCounts[i] = bestNeighborsCount;
		bestTabuTimeouts[i] = bestTabuTimeout;
	}

	Console.WriteLine("== Fine tuned parameters ===");
	Console.WriteLine($"Tabu timeout: {bestTabuTimeouts.ToPrettyString()}");
	Console.WriteLine($"Neighbors count: {bestNeighborsCounts.ToPrettyString()}");
	Console.WriteLine($"Iterations count: {bestIterationCounts.ToPrettyString()}");
	Console.WriteLine("=== ===");
}

double EvaluateCombination(int iterationCount, int neighborCount, int tabuTimeout, Instance[] instances)
{
	Console.WriteLine("=== Evaluating ===");
	Console.WriteLine($"Tabu timeout: {tabuTimeout}");
	Console.WriteLine($"Neighbors count: {neighborCount}");
	Console.WriteLine($"Iterations count: {iterationCount}");
	
	int[] evaluations = new int[instances.Length];
	int completed = 0;
#if false
	for (int i = 0; i < instanceCount; i++)
	{
		evaluations[i] = RunCombination(iterationCount, neighborCount, tabuTimeout);
		completed++;
		
		if (completed % (instanceCount / 50) == 0)
			Console.Write('*');
	}
#else
	object @lock = new();
	Parallel.For(0, instances.Length, i =>
	{
		evaluations[i] = RunCombination(iterationCount, neighborCount, tabuTimeout, instances[i]);
		lock (@lock)
		{
			completed++;
			if (completed % (instances.Length / 50) == 0)
				Console.Write('*');
		}
	});
#endif

	Console.WriteLine();
	return evaluations.Average();
}

int RunCombination(int iterationCount, int neighborCount, int tabuTimeout, Instance instance)
{
	// Item[] items = GenerateRandomItems(200);
	// int capacity = rng.Next(1_000, 2_000);

	// Console.WriteLine("=== Items available ===");
	// foreach (var item in items)
	// {
	// 	Console.WriteLine(item);
	// }
	// Console.WriteLine("=== ===");

	TabuSearch tabuSearch = new(instance.Items, instance.Capacity, iterationCount, neighborCount, tabuTimeout);
	Solution solution = tabuSearch.Execute();
	// Console.WriteLine(solution.ToString(items));

	return tabuSearch.Evaluate(solution);
}

Instance[] GenerateInstances(int instanceCount)
{
	Item[] GenerateRandomItems(int count)
	{
		// return Enumerable.Range(1, count)
		// 	.Select(_ =>
		// 		new Item(rng.Next(1, 10), rng.Next(10, 100))
		// 	)
		// 	.ToArray();

		Item[] items = new Item[count];
		for (int i = 0; i < count; i++)
		{
			items[i] = new Item(rng.Next(1, 10), rng.Next(10, 100));
		}

		return items;
	}

	return Enumerable.Range(0, instanceCount).Select(_ => new Instance
		{
			Capacity = rng.Next(1_000, 2_000),
			Items = GenerateRandomItems(200)
		})
		.ToArray();
}

struct Instance
{
	public Item[] Items { get; init; }
	public int Capacity { get; init; }
}

public static class SpanExtensions
{
	public static string ToPrettyString<T>(this Span<T> span)
	{
		StringBuilder sb = new();
		sb.Append('[');
		if (span.Length > 0)
		{
			sb.Append(span[0]);
		}

		for (int i = 1; i < span.Length; i++)
		{
			sb.Append(", ");
			sb.Append(span[i]);
		}

		sb.Append(']');

		return sb.ToString();
	}
}
