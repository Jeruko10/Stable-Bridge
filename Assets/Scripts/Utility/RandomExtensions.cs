using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RandomExtensions
{
	/// <summary>
	/// Returns a uniformly distributed random element from the specified read-only list.
	/// </summary>
	/// <param name="random">The <see cref="Random"/> instance used to generate the selection.</param>
	/// <param name="list">The non-empty collection from which an element will be selected.</param>
	/// <typeparam name="T">The type of elements contained in the collection.</typeparam>
	/// <returns>A randomly selected element from <paramref name="list"/>.</returns>
	public static T Choose<T>(this System.Random random, IReadOnlyList<T> list)
	{
		Debug.Assert(list.Count > 0, $"Cannot select an element from an empty list");
		return list[random.Next(list.Count)];
	}

	/// <summary>
	/// Returns a specified number of unique elements randomly selected from the given read-only list.
	/// </summary>
	/// <param name="random">The <see cref="Random"/> instance used to generate the selections.</param>
	/// <param name="list">The collection from which elements will be selected.</param>
	/// <param name="amount">The number of unique elements to retrieve.</param>
	/// <typeparam name="T">The type of elements contained in the collection.</typeparam>
	/// <returns>
	/// An <see cref="IEnumerable{T}"/> containing <paramref name="amount"/> distinct elements randomly chosen from <paramref name="list"/>.
	/// </returns>
	public static IEnumerable<T> Choose<T>(this System.Random random, IReadOnlyList<T> list, int amount)
	{
		Debug.Assert(list.Count < amount, $"Not enough elements in collection. Collection: {list.Count}, amount: {amount}");
		HashSet<int> indices = new();
		while (indices.Count < amount){
			indices.Add(random.Next(list.Count));
		}
		return indices.Select(i=>list[i]);
	}

	/// <summary>
	/// Returns a random sign value.
	/// </summary>
	/// <param name="random">The <see cref="Random"/> instance used to generate the value.</param>
	/// <param name="includeZero">If true, zero may also be returned; otherwise only -1 or 1 are possible results.</param>
	/// <returns>-1 or 1 by default; optionally 0 if <paramref name="includeZero"/> is true.</returns>
	public static int RandomSign(this System.Random random, bool includeZero = false)
	{
		if (includeZero) return random.Next(-1, 2);
		return random.Next(0,2)*2 -1;
	}

	/// <summary>
	/// Generates a random angle in radians within the range [0, 2π).
	/// </summary>
	/// <param name="random">The <see cref="Random"/> instance used to generate the value.</param>
	/// <returns>A random radian value spanning a full rotation.</returns>
    public static float RandomRadian(this System.Random random) => (float)random.NextDouble() * UnityEngine.Mathf.PI * 2f;

	/// <summary>
	/// Generates a random 3D Euler rotation where each axis is independently randomized over a full 2π range.
	/// </summary>
	/// <param name="random">The <see cref="Random"/> instance used to generate the rotation.</param>
	/// <returns>A <see cref="Vector3"/> representing random Euler angles in radians.</returns>
    public static Vector3 RandomEulerRotation(this System.Random random) => new(random.RandomRadian(), random.RandomRadian(), random.RandomRadian());
    
	/// <summary>
	/// Generates a random floating-point value in the range [-1, 1].
	/// </summary>
	/// <param name="random">The <see cref="Random"/> instance used to generate the value.</param>
	/// <returns>A random float between -1 and 1.</returns>
    public static float RandomAxis(this System.Random random) => (float)(random.NextDouble() * 2.0 - 1.0);
    
	/// <summary>
	/// Generates a normalized random direction vector in 2D space.
	/// </summary>
	/// <param name="random">The <see cref="Random"/> instance used to generate the direction.</param>
	/// <returns>A unit-length <see cref="Vector2"/> pointing in a random direction.</returns>
    public static Vector2 RandomDirection2D(this System.Random random)
    {
        return new Vector2(
            random.RandomAxis(),
            random.RandomAxis()
		).normalized;
    }

	/// <summary>
	/// Generates a normalized random direction vector in 3D space.
	/// </summary>
	/// <param name="random">The <see cref="Random"/> instance used to generate the direction.</param>
	/// <returns>A unit-length <see cref="Vector3"/> pointing in a random direction.</returns>
    public static Vector3 RandomDirection3D(this System.Random random)
    {
        return new Vector3(
            random.RandomAxis(),
            random.RandomAxis(),
            random.RandomAxis()
        ).normalized;
    }
}