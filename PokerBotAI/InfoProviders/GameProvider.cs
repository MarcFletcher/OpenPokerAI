using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Definitions;

namespace PokerBot.AI.InfoProviders
{
  class GameProvider : InfoProviderBase
  {
    public GameProvider(InfoCollection information, Dictionary<InfoProviderType, InfoProviderBase> allInformationProviders, AIRandomControl aiRandomControl)
        : base(information, InfoProviderType.Game, allInformationProviders, aiRandomControl)
    {
      requiredInfoTypes = new List<InfoType>() { };
      providedInfoTypes = new List<InfoPiece> { new InfoPiece(InfoType.GP_NumTableSeats_Byte, 10),
                                                         new InfoPiece(InfoType.GP_NumPlayersDealtIn_Byte, 10),
                                                         new InfoPiece(InfoType.GP_NumActivePlayers_Byte, 10),
                                                         new InfoPiece(InfoType.GP_NumUnactedPlayers_Byte, 0),
                                                         new InfoPiece(InfoType.GP_GameStage_Byte, 0),
                                                         new InfoPiece(InfoType.GP_DealerDistance_Byte, 1)
                                                         };

      AddProviderInformationTypes();
    }

    protected override void updateInfo()
    {
      if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.GP_NumTableSeats_Byte))
        infoStore.SetInformationValue(InfoType.GP_NumTableSeats_Byte, decisionRequest.Cache.NumSeats);

      if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.GP_NumPlayersDealtIn_Byte) || decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.GP_NumActivePlayers_Byte))
      {
        decimal numDealtInPlayers = decisionRequest.Cache.getSatInPositions().Count();
        decimal numActivePlayers = decisionRequest.Cache.getActivePositions().Count();

        if (numDealtInPlayers < numActivePlayers)
          throw new Exception("Dealt In Players should never be less than active players.");

        infoStore.SetInformationValue(InfoType.GP_NumPlayersDealtIn_Byte, numDealtInPlayers);
        infoStore.SetInformationValue(InfoType.GP_NumActivePlayers_Byte, numActivePlayers);
      }

      if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.GP_GameStage_Byte))
      {
        var handInfo = decisionRequest.Cache.getCurrentHandDetails();
        decimal gameStage = 0;
        if (handInfo.tableCard1 == 0)
          gameStage = 0;
        else if (handInfo.tableCard4 == 0)
          gameStage = 1;
        else if (handInfo.tableCard5 == 0)
          gameStage = 2;
        else
          gameStage = 3;

        infoStore.SetInformationValue(InfoType.GP_GameStage_Byte, gameStage);
      }

      if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.GP_NumUnactedPlayers_Byte))
        infoStore.SetInformationValue(InfoType.GP_NumUnactedPlayers_Byte, decisionRequest.Cache.getActivePositionsLeftToAct().Length);

      if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.GP_DealerDistance_Byte))
        infoStore.SetInformationValue(InfoType.GP_DealerDistance_Byte, decisionRequest.Cache.getActivePlayerDistanceToDealer(decisionRequest.PlayerId));
    }
  }
}
