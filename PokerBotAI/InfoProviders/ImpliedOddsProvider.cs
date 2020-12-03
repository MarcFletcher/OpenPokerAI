using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Definitions;

namespace PokerBot.AI.InfoProviders
{
  class ImpliedOddsProvider : InfoProviderBase
  {
    public ImpliedOddsProvider(InfoCollection information, Dictionary<InfoProviderType, InfoProviderBase> allInformationProviders, AIRandomControl aiRandomControl)
        : base(information, InfoProviderType.ImpliedOdds, allInformationProviders, aiRandomControl)
    {
      //requiredInfoProviders = new List<InfoProviderType>() {InfoProviderType.PlayerActionPrediction};
      providedInfoTypes = new List<InfoPiece>() { new InfoPiece(InfoType.IO_ImpliedPotOdds_Double, 0), };

      /*
      requiredInfoTypes = new List<InfoType>() { InfoType.BP_MinimumCallAmount_Decimal,          //Provided
                                                  InfoType.BP_TotalPotAmount_Decimal,             //Provided
                                                  InfoType.GP_GameStage_Byte,
                                                  InfoType.PAP_FoldToBotCall_Prob,
                                                  InfoType.BP_PlayerBetAmountCurrentRound_Decimal,
                                                  InfoType.BP_ImmediatePotOdds_Double,
                                                  InfoType.GP_NumUnactedPlayers_Byte,
                                                  };
      */
      AddProviderInformationTypes();
    }

    protected override void updateInfo()
    {
      //DateTime startTime = DateTime.Now;

      SetAllProvidedTypesToDefault((from current in providedInfoTypes select current.InformationType).ToList());
      return;
      /*
      PlayerActionPrediction predictedAction;

      if (allInformationProviders==null)
          throw new Exception("allInformationProviders should not be null!!");

      PlayerActionPredictionProvider PlayerActionPredictionProvider = (PlayerActionPredictionProvider)(from
          infoProviders in allInformationProviders
                                            where infoProviders.ProviderType == InfoProviderType.PlayerActionPrediction
                                            select infoProviders).First();
      */
      //PlayerActionPredictionProvider.
      //byte[] positionsLeftToAct = cache.getActivePositionsLeftToAct(playerId);
      //byte[] activePositions = cache.getActivePositions(cache.getCurrentHandDetails().dealerPosition);

      decimal impliedOdds = 1;

      decimal foldToBotCall = infoStore.GetInfoValue(InfoType.PAP_FoldToBotCall_Prob);
      //double numActivePlayers = infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte);
      decimal numUnactedPlayers = infoStore.GetInfoValue(InfoType.GP_NumUnactedPlayers_Byte);
      //double betsToCall = infoStore.GetInfoValue(InfoType.BP_BetsToCall_Byte);
      decimal minimumCallAmount = infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal);
      //double totalNumCalls = infoStore.GetInfoValue(InfoType.BP_TotalNumCalls_Byte);
      //double totalNumRaises = infoStore.GetInfoValue(InfoType.BP_TotalNumRaises_Byte);
      decimal totalPotAmount = infoStore.GetInfoValue(InfoType.BP_TotalPotAmount_Decimal);
      decimal gameStage = infoStore.GetInfoValue(InfoType.GP_GameStage_Byte);
      //List<byte> simulatedFoldPositions = new List<byte>();

      decimal immPotOdds = infoStore.GetInfoValue(InfoType.BP_ImmediatePotOdds_Double);
      decimal playerCurrentRoundBetAmount = infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);

      //cache.getPlayerCurrentRoundBetAmount(playerId, out playerCurrentRoundBetAmount);

      if (minimumCallAmount > 0 && gameStage > 0)
      {
        decimal endRoundPredictedPotAmount = totalPotAmount + ((numUnactedPlayers - 1) * (1 - foldToBotCall) * minimumCallAmount);

        impliedOdds = ((endRoundPredictedPotAmount / (minimumCallAmount - playerCurrentRoundBetAmount)) / 10.0m) - 0.1m;
        if (impliedOdds > 1)
          impliedOdds = 1;
        else if (impliedOdds < 0)
          impliedOdds = 0;

        if (impliedOdds < immPotOdds)
          throw new Exception("What the fuck!!");
      }
      else
        impliedOdds = immPotOdds;


      infoStore.SetInformationValue(InfoType.IO_ImpliedPotOdds_Double, impliedOdds);
      //Debug.Print("{0} executed in {1}ms.", this.providerType, (DateTime.Now-startTime).TotalMilliseconds);
    }
  }
}
