using System;
using System.Collections.Generic;
using System.Linq;

namespace PokerBot.Definitions
{
  public class ShuffleList
  {
    static Random randomGen = new Random();
    public static IList<T> Shuffle<T>(IList<T> list)
    {
      IList<T> listCopy = list.ToList();
      int n = listCopy.Count;
      while (n > 1)
      {
        n--;
        int k = randomGen.Next(n + 1);
        T value = listCopy[k];
        listCopy[k] = listCopy[n];
        listCopy[n] = value;
      }

      return listCopy;
    }
  }
}
