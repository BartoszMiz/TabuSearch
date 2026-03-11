namespace MiniProject1;

public class TabuSearch(Item[] items, int capacity, int iterationCount, int neighborCount, int tabuIterationsTimeout)
{
	private readonly Random _rng = Random.Shared;
	private readonly Item[] _items = items;
	private readonly int _capacity = capacity;

	private readonly int _iterationCount = iterationCount;
	private readonly int _neighborCount = neighborCount;
	private readonly int _tabuIterationsTimeout = tabuIterationsTimeout;

	public Solution Execute()
	{
		Solution best = GetRandomSolution();
		int bestFitness = Evaluate(best);
		
		Solution bestCandidate = best;
		
		List<(Solution solution, int timeout)> tabuList =
		[
			(best, _tabuIterationsTimeout)
		];

		for (int iteration = 0; iteration < _iterationCount; iteration++)
		{
			IEnumerable<Solution> neighborhood = GetNeighbors(bestCandidate);
			int bestCandidateFitness = int.MinValue;
			foreach (var candidate in neighborhood)
			{
				int candidateFitness = Evaluate(candidate);
				bool isOnTabuList = false;
				foreach ((Solution solution, int _) in tabuList)
				{
					if (!solution.Equals(candidate)) continue;
					isOnTabuList = true;
					break;
				}
				if (!isOnTabuList && candidateFitness > bestCandidateFitness)
				{
					bestCandidate = candidate;
					bestCandidateFitness = candidateFitness;
				}
			}
			
			// if (bestCandidateFitness == int.MinValue)
			// 	break;

			if (bestCandidateFitness > bestFitness)
			{
				best = bestCandidate;
				bestFitness = bestCandidateFitness;
			}
			
			tabuList.Add((best, _tabuIterationsTimeout));
			tabuList.ForEach(x => x.timeout--);
			tabuList.RemoveAll(x => x.timeout == 0);
			
			// Console.WriteLine($"=== Iteration {iteration} ===");
			// Console.WriteLine(best.ToString(_items));
			// Console.WriteLine("=== ===");
		}

		return best;
	}

	public bool IsValid(Solution solution)
	{
		int sum = 0;
		for (int i = 0; i < solution.Length; i++)
		{
			sum += solution[i] ? _items[i].Weight : 0;
		}
		// int sum = solution.Zip(_items).Where(x => x.First).Sum(x => x.Second.Weight);
		return sum <= _capacity;
	}

	public int Evaluate(Solution solution)
	{
		int sum = 0;
		for (int i = 0; i < solution.Length; i++)
		{
			sum += solution[i] ? _items[i].Value : 0;
		}
		// int sum = solution.Zip(_items).Where(x => x.First).Sum(x => x.Second.Value);
		return sum;
	}

	IEnumerable<Solution> GetNeighbors(Solution original)
	{
		HashSet<Solution> neighbors = [];
		const int maxAttemptCount = 200;
		int attemptCount = 0;
		while (neighbors.Count < _neighborCount && attemptCount++ < maxAttemptCount)
		{
			neighbors.Add(GetNeighbor(original));
		}
		// if (attemptCount >= maxAttemptCount)
		// 	throw new ApplicationException("Could not create enough neighbors!");

		return neighbors.AsEnumerable();
	}

	Solution GetNeighbor(Solution original)
	{
		Solution neighbor = original.Copy();
		while (true)
		{
			int index = _rng.Next(original.Length);
			neighbor[index] = !neighbor[index];
			
			if (IsValid(neighbor))
				return neighbor;

			neighbor[index] = !neighbor[index];
		}
	}

	Solution GetRandomSolution()
	{
		Solution solution = new Solution(_items.Length);
		
		const int maxAttemptCount = 100;
		int attemptCount = 0;
		do
		{
			for (int i = 0; i < _items.Length; i++)
			{
				solution[i] = _rng.NextSingle() < 0.1f;
			}

			if (attemptCount++ < maxAttemptCount)
				return new Solution(_items.Length);
		} 
		while (!IsValid(solution));

		return solution;
	}
}