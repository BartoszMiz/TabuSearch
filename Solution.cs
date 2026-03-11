using System.Collections;
using System.Text;

namespace MiniProject1;

public class Solution(int size) : IEnumerable<bool>
{
	private readonly bool[] _data = new bool[size];
	public int Length => _data.Length;

	public bool this[int index]
	{
		get => _data[index];
		set => _data[index] = value;
	}
	
	public IEnumerator<bool> GetEnumerator()
	{
		return _data.AsEnumerable().GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _data.GetEnumerator();
	}

	public Solution Copy()
	{
		var copy = new Solution(Length);
		Array.Copy(this._data, copy._data, Length);
		return copy;
	}

	public string ToString(Item[] items)
	{
		var sb = new StringBuilder();
		var chosenItems = this.Zip(items).Where(x => x.First).Select(x => x.Second).ToArray();
		foreach (var item in chosenItems)
		{
			sb.Append(item);
			sb.Append(' ');
		}

		sb.Append($"Weight: {chosenItems.Sum(item => item.Weight)} Value: {chosenItems.Sum(item => item.Value)}");

		return sb.ToString();
	}

	public override string ToString()
	{
		var sb = new StringBuilder();
		ulong number = 0;
		foreach (var choice in _data)
		{
			number |= choice ? 1ul : 0;
			number <<= 1;
		}

		sb.Append(number);
		sb.Append('\t');
		sb.Append('[');
		
		foreach (var choice in _data)
		{
			sb.Append(choice ? 1 : 0);
		}
		
		sb.Append(']');

		return sb.ToString();
	}

	public override int GetHashCode()
	{
		var hash = new HashCode();
		foreach (var choice in _data)
		{
			hash.Add(choice);
		}

		return hash.ToHashCode();
	}

	public override bool Equals(object? obj)
	{
		if (obj is Solution sol)
		{
			return _data.SequenceEqual(sol._data);
		}

		return false;
	}
}