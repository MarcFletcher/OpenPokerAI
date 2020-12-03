using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.AI.InfoProviders;

namespace PokerBot.AI
{
  public struct RequestedInfoKey
  {
    ulong a, b, c, d;

    public RequestedInfoKey(bool allTrue = false)
    {
      d = c = b = a = 0;

      if (allTrue)
      {
        foreach (var obj in Enum.GetValues(typeof(InfoType)))
        {
          int bitNumber = (int)obj;

          if (bitNumber < 64)
          {
            a |= (1UL << bitNumber);
          }
          else if (bitNumber < 128)
          {
            bitNumber -= 64;

            b |= (1UL << bitNumber);
          }
          else if (bitNumber < 192)
          {
            bitNumber -= 128;

            c |= (1UL << bitNumber);
          }
          else
          {
            bitNumber -= 192;

            d |= (1UL << bitNumber);
          }
        }
      }
      else
      {
        d = c = b = a = 0;
      }
    }

    public void SetInfoTypeRequired(InfoType infoType, bool required = true)
    {
      int bitNumber = (int)infoType;

      if (bitNumber < 64)
      {
        if (required)
          a |= (1UL << bitNumber);
        else
          a &= ~(1UL << bitNumber);
      }
      else if (bitNumber < 128)
      {
        bitNumber -= 64;

        if (required)
          b |= (1UL << bitNumber);
        else
          b &= ~(1UL << bitNumber);
      }
      else if (bitNumber < 192)
      {
        bitNumber -= 128;

        if (required)
          c |= (1UL << bitNumber);
        else
          c &= ~(1UL << bitNumber);
      }
      else
      {
        bitNumber -= 192;

        if (required)
          d |= (1UL << bitNumber);
        else
          d &= ~(1UL << bitNumber);
      }
    }

    public bool IsInfoTypeRequired(InfoType infoType)
    {
      int bitNumber = (int)infoType;

      if (bitNumber < 64)
      {
        return (a & (1UL << bitNumber)) != 0;
      }
      else if (bitNumber < 128)
      {
        bitNumber -= 64;

        return (b & (1UL << bitNumber)) != 0;
      }
      else if (bitNumber < 192)
      {
        bitNumber -= 128;

        return (c & (1UL << bitNumber)) != 0;
      }
      else
      {
        bitNumber -= 192;

        return (d & (1UL << bitNumber)) != 0;
      }
    }
  }

}
