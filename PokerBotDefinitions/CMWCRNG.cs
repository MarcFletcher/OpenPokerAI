using System;

namespace PokerBot.Definitions
{
  /// <summary>
  /// A modern random number generator based on G. Marsaglia: 
  /// Seeds for Random Number Generators, Communications of the
  /// ACM 46, 5 (May 2003) 90-93; and a posting by Marsaglia to 
  /// comp.lang.c on 2003-04-03.
  /// </summary>
  public class CMWCRandom : Random
  {
    private uint[] Q = new uint[4096];
    private uint[] StoredQ = new uint[4096];

    private uint c = 123, i = 4095;

    private uint Cmwc()
    {
      ulong t, a = 18782UL;
      uint x, r = 0xfffffffe;

      i = (i + 1) & 4095;
      t = a * Q[i] + c;
      c = (uint)(t >> 32);
      x = (uint)(t + c);
      if (x < c)
      {
        x++;
        c++;
      }

      return Q[i] = r - x;
    }


    /// <summary>
    /// Get a new random System.Double value
    /// </summary>
    /// <returns>The random double</returns>
    public override double NextDouble()
    {
      return Cmwc() / 4294967296.0;
    }


    /// <summary>
    /// Get a new random System.Double value
    /// </summary>
    /// <returns>The random double</returns>
    protected override double Sample()
    {
      return NextDouble();
    }


    /// <summary>
    /// Get a new random System.Int32 value
    /// </summary>
    /// <returns>The random int</returns>
    public override int Next()
    {
      return (int)Cmwc();
    }


    /// <summary>
    /// Get a random non-negative integer less than a given upper bound
    /// </summary>
    /// <exception cref="ArgumentException">If max is negative</exception>
    /// <param name="max">The upper bound (exclusive)</param>
    /// <returns></returns>
    public override int Next(int max)
    {
      if (max < 0)
        throw new ArgumentException("max must be non-negative");

      return (int)(Cmwc() / 4294967296.0 * max);
    }


    /// <summary>
    /// Get a random integer between two given bounds
    /// </summary>
    /// <exception cref="ArgumentException">If max is less than min</exception>
    /// <param name="min">The lower bound (inclusive)</param>
    /// <param name="max">The upper bound (exclusive)</param>
    /// <returns></returns>
    public override int Next(int min, int max)
    {
      if (min > max)
        throw new ArgumentException("min must be less than or equal to max");

      return min + (int)(Cmwc() / 4294967296.0 * (max - min));
    }

    /// <summary>
    /// Fill a array of byte with random bytes
    /// </summary>
    /// <param name="buffer">The array to fill</param>
    public override void NextBytes(byte[] buffer)
    {
      for (int i = 0, length = buffer.Length; i < length; i++)
        buffer[i] = (byte)Cmwc();
    }


    /// <summary>
    /// Create a random number generator seed by system time.
    /// </summary>
    public CMWCRandom()
        : this(DateTime.Now.Ticks)
    {
    }


    /// <summary>
    /// Create a random number generator with a given seed
    /// </summary>
    /// <exception cref="ArgumentException">If seed is zero</exception>
    /// <param name="seed">The seed</param>
    public CMWCRandom(long seed)
    {
      if (seed == 0)
        throw new ArgumentException("Seed must be non-zero");

      for (int i = 0; i < 4096; i++)
      {
        seed = (~seed) + (seed << 21); // seed = (seed << 21) - seed - 1;
        seed = seed ^ (seed >> 24);
        seed = (seed + (seed << 3)) + (seed << 8); // seed * 265
        seed = seed ^ (seed >> 14);
        seed = (seed + (seed << 2)) + (seed << 4); // seed * 21
        seed = seed ^ (seed >> 28);
        seed = seed + (seed << 31);
        Q[i] = (uint)(seed % uint.MaxValue);
      }

      Q[4095] = (uint)(seed ^ (seed >> 32));

      Array.Copy(Q, StoredQ, 4096);
    }

    /// <summary>
    /// Create a random number generator with a specified internal start state.
    /// </summary>
    /// <exception cref="ArgumentException">If Q is not of length exactly 16</exception>
    /// <param name="Q">The start state. Must be a collection of random bits given by an array of exactly 16 uints.</param>
    public CMWCRandom(uint[] Q)
    {
      if (Q.Length != 4096)
        throw new ArgumentException("Q must have length 4096, was " + Q.Length);

      Array.Copy(Q, this.Q, 4096);
      Array.Copy(Q, StoredQ, 4096);
    }

    public void ReSeed(long seed)
    {
      Array.Copy(StoredQ, Q, 4096);
      c = (uint)(seed % 18782L);
      i = 4095;
    }
  }
}

