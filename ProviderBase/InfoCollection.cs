using System;
using System.Collections.Generic;
using System.Linq;

namespace PokerBot.AI.InfoProviders
{
  public class InfoCollection
  {
    object locker = new object();
    bool infoListHardLocked;

    Dictionary<InfoType, InfoPiece> infoList;

    public InfoCollection()
    {
      infoList = new Dictionary<InfoType, InfoPiece>();
      infoListHardLocked = false;
    }

    public void AddInformationType(InfoType infoType, decimal defaultValue)
    {
      if (infoListHardLocked)
        throw new Exception("You are not allowed to make any further changes to the infolist after it has been hardlocked.");

      lock (locker)
      {
        if (infoList.ContainsKey(infoType))
          throw new Exception("Cannot have multiple instances of same info type");

        infoList.Add(infoType, new InfoPiece(infoType, defaultValue, defaultValue));
      }
    }

    /// <summary>
    /// Prevents any items being added or deleted from the infolist.
    /// </summary>
    public void HardLockInfoList()
    {
      infoListHardLocked = true;
    }

    /// <summary>
    /// Adds infopieces to the infolist.
    /// </summary>
    /// <param name="infoPieces"></param>
    public void AddInformationTypes(List<InfoPiece> infoPieces)
    {
      foreach (InfoPiece piece in infoPieces)
        AddInformationType(piece.InformationType, piece.DefaultValue);
    }

    public bool ContainsInfoType(InfoType infoType)
    {
      //if (infoListHardLocked)
      //    throw new Exception("You are not allowed to make any further changes to the infolist after it has been hardlocked.");

      return infoList.ContainsKey(infoType);
    }

    public bool SetInformationValue(InfoType infoType, decimal value)
    {
      if (!infoListHardLocked)
        throw new Exception("You are only allowed to set an information value once the list has been hardlocked.");

      if (infoList.ContainsKey(infoType))
      {
        infoList[infoType].Value = value;
        return true;
      }

      return false;
    }

    public decimal GetInfoValue(InfoType infoType)
    {
      if (!infoListHardLocked)
        throw new Exception("You are only allowed to get an information value once the list has been hardlocked.");

      if (infoList.ContainsKey(infoType))
        return infoList[infoType].Value;

      //If we got here the infoType is not in the list
      throw new Exception("Info type " + infoType.ToString() + " not in information list");
    }

    public decimal[] GetInfoValues(InfoType[] infoTypes)
    {
      decimal[] result = new decimal[infoTypes.Length];

      if (!infoListHardLocked)
        throw new Exception("You are only allowed to get information values once the list has been hardlocked.");

      for (int i = 0; i < infoTypes.Length; i++)
        result[i] = GetInfoValue(infoTypes[i]);

      return result;

    }

    /// <summary>
    /// Returns a new array containing all infostore pieces which are marked as updated.
    /// </summary>
    /// <returns></returns>
    public Dictionary<InfoType, InfoPiece> GetInformationStore()
    {
      if (!infoListHardLocked)
        throw new Exception("You are only allowed to get the info store once the list has been hardlocked.");

      return infoList.Where(e => e.Value.Updated).ToDictionary(e => e.Key, e => e.Value);
    }

    public void SetAllInformationValuesToDefault()
    {
      if (!infoListHardLocked)
        throw new Exception("You are only allowed to set to defaults once the list has been hardlocked.");

      foreach (var infoType in infoList.Values)
        infoType.SetToDefault(true);
    }

    internal void SetInformationValueToDefault(InfoType infoType)
    {
      if (!infoListHardLocked)
        throw new Exception("You are only allowed to set an information value once the list has been hardlocked.");

      if (infoList.ContainsKey(infoType))
        infoList[infoType].SetToDefault(true);
      else
        throw new Exception("AHHHHHHH!!!!!");
    }

    /// <summary>
    /// Returns true or false depending on whether all requiredInfoTypes have been updated
    /// </summary>
    /// <param name="requiredInfoTypes"></param>
    /// <returns></returns>
    public void WaitForInfoPiecesUpdated(List<InfoType> requiredInfoTypes)
    {
      if (requiredInfoTypes == null)
        return;

      if (requiredInfoTypes.Count == 0)
        return;

      if (!infoListHardLocked)
        throw new Exception("You are only allowed to check for updated status once the list has been hardlocked.");

      /*
      var tmp = from
             infoStore in infoList.Values
             join requiredTypes in requiredInfoTypes on infoStore.InformationType equals requiredTypes
             select infoStore;


      foreach (InfoPiece piece in tmp)
          piece.WaitForUpdate();
      */

      foreach (InfoType type in requiredInfoTypes)
        infoList[type].WaitForUpdate();
    }

    /// <summary>
    /// Set's all update flags to false
    /// </summary>
    public void ResetAllUpdateFlags()
    {
      if (!infoListHardLocked)
        throw new Exception("You are only allowed to reset update flags once the list has been hardlocked.");

      foreach (InfoPiece infoPiece in infoList.Values)
        infoPiece.ResetUpdateFlag();
    }
  }
}
