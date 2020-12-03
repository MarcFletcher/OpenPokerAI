using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Definitions;
using System.IO;
using Microsoft.Win32;

namespace PokerBot.AI.InfoProviders
{
  public class CardsProvider : InfoProviderBase
  {
    #region HardCodedCardUsageFiles
    private static Dictionary<string, Dictionary<byte, Dictionary<byte, decimal>>> suitedHoleCardUsageDict = new Dictionary<string, Dictionary<byte, Dictionary<byte, decimal>>>();
    private static Dictionary<string, Dictionary<byte, Dictionary<byte, decimal>>> unSuitedHoleCardUsageDict = new Dictionary<string, Dictionary<byte, Dictionary<byte, decimal>>>();

    private static void LoadCardUsage(string nameKey)
    {
      lock (locker)
      {
        string holeCardUsageDir;
        string[] unsuitedCardUsageLines = null;
        string[] suitedCardUsageLines = null;

        if (CurrentJob == null)
        {
          holeCardUsageDir = Environment.GetEnvironmentVariable("HoleCardUsageDir");
          if (holeCardUsageDir == null)
            throw new Exception("HoleCarUsage registry key does not exist.");

          unsuitedCardUsageLines = File.ReadAllLines(Path.Combine(holeCardUsageDir + "\\" + nameKey, "UnSuited.csv"));
          suitedCardUsageLines = File.ReadAllLines(Path.Combine(holeCardUsageDir + "\\" + nameKey, "Suited.csv"));
        }
        else
        {
          //unsuitedCardUsageLines = CurrentJob.HoleCardUsageData[nameKey + "\\UnSuited.csv"];
          //suitedCardUsageLines = CurrentJob.HoleCardUsageData[nameKey + "\\Suited.csv"];
        }

        if (!suitedHoleCardUsageDict.ContainsKey(nameKey))
          suitedHoleCardUsageDict.Add(nameKey, new Dictionary<byte, Dictionary<byte, decimal>>());
        if (!unSuitedHoleCardUsageDict.ContainsKey(nameKey))
          unSuitedHoleCardUsageDict.Add(nameKey, new Dictionary<byte, Dictionary<byte, decimal>>());

        for (int i = 0; i < unsuitedCardUsageLines.Length; i++)
        {
          string[] curentLineStrings = unsuitedCardUsageLines[i].Split(',');
          byte card1 = Convert.ToByte(curentLineStrings[0]);
          byte card2 = Convert.ToByte(curentLineStrings[1]);
          decimal cardsPlayability = Convert.ToDecimal(curentLineStrings[2]);

          if (unSuitedHoleCardUsageDict[nameKey].ContainsKey(Convert.ToByte(card1)))
          {
            if (!unSuitedHoleCardUsageDict[nameKey][card1].ContainsKey(card2))
              //    throw new Exception("This key should not exist.");
              //else
              unSuitedHoleCardUsageDict[nameKey][card1].Add(card2, cardsPlayability);
          }
          else
          {
            Dictionary<byte, decimal> child = new Dictionary<byte, decimal>();
            child.Add(card2, cardsPlayability);
            unSuitedHoleCardUsageDict[nameKey].Add(card1, child);
          }
        }

        for (int i = 0; i < suitedCardUsageLines.Length; i++)
        {
          string[] curentLineStrings = suitedCardUsageLines[i].Split(',');
          byte card1 = Convert.ToByte(curentLineStrings[0]);
          byte card2 = Convert.ToByte(curentLineStrings[1]);
          decimal cardsPlayability = Convert.ToDecimal(curentLineStrings[2]);

          if (suitedHoleCardUsageDict[nameKey].ContainsKey(Convert.ToByte(card1)))
          {
            if (!suitedHoleCardUsageDict[nameKey][card1].ContainsKey(card2))
              //    throw new Exception("This key should not exist.");
              //else
              suitedHoleCardUsageDict[nameKey][card1].Add(card2, cardsPlayability);
          }
          else
          {
            Dictionary<byte, decimal> child = new Dictionary<byte, decimal>();
            child.Add(card2, cardsPlayability);
            suitedHoleCardUsageDict[nameKey].Add(card1, child);
          }
        }
      }
    }
    #endregion

    private static object locker = new object();

    public CardsProvider(InfoCollection information, Dictionary<InfoProviderType, InfoProviderBase> allInformationProviders, AIRandomControl aiRandomControl)
        : base(information, InfoProviderType.Cards, allInformationProviders, aiRandomControl)
    {
      requiredInfoTypes = new List<InfoType>() { };
      providedInfoTypes = new List<InfoPiece> { new InfoPiece(InfoType.CP_AOnBoard_Bool, 1),
                                                        new InfoPiece(InfoType.CP_KOnBoard_Bool,1),
                                                        new InfoPiece(InfoType.CP_FlushPossible_Bool,1),
                                                        new InfoPiece(InfoType.CP_StraightPossible_Bool,1),
                                                        new InfoPiece(InfoType.CP_AKQToBoardRatio_Real,1),
                                                        new InfoPiece(InfoType.CP_TableStraightDraw_Bool, 1),
                                                        new InfoPiece(InfoType.CP_TableFlushDraw_Bool,1),

                                                        new InfoPiece(InfoType.CP_HoleCardsAAPair_Bool,0),
                                                        new InfoPiece(InfoType.CP_HoleCardsKKPair_Bool,0),
                                                        new InfoPiece(InfoType.CP_HoleCardsOtherHighPair_Bool, 0),
                                                        new InfoPiece(InfoType.CP_HoleCardsOtherLowPair_Bool, 0),
                                                        new InfoPiece(InfoType.CP_HoleCardsOtherPair_Bool,0),
                                                        new InfoPiece(InfoType.CP_HoleCardsAK_Bool,0),
                                                        new InfoPiece(InfoType.CP_HoleCardsTroubleHand_Bool, 0),
                                                        new InfoPiece(InfoType.CP_HoleCardsMidConnector_Bool,0),
                                                        new InfoPiece(InfoType.CP_HoleCardsLowConnector_Bool,0),
                                                        new InfoPiece(InfoType.CP_HoleCardsSuited_Bool,0),
                                                        new InfoPiece(InfoType.CP_HoleCardsFlushDraw_Bool,0),
                                                        new InfoPiece(InfoType.CP_HoleCardsStraightDraw_Bool,0),
                                                        new InfoPiece(InfoType.CP_HoleCardsOuterStraightDrawWithHC_Bool,0),
                                                        new InfoPiece(InfoType.CP_HoleCardsInnerStraightDrawWithHC_Bool,0),
                                                        new InfoPiece(InfoType.CP_HoleCards3KindOrBetterMadeWithHC_Bool,0),
                                                        new InfoPiece(InfoType.CP_HoleCardsTopOrTwoPair_Bool,0),
                                                        new InfoPiece(InfoType.CP_HoleCardsAOrKInHand_Bool,0),
                                                        new InfoPiece(InfoType.CP_HoleCardsMatchedPlayability, 0),
                                                    };

      AddProviderInformationTypes();

      LoadCardUsage("Marc");
      LoadCardUsage("Ailwyn");
      LoadCardUsage("SimpleV7");
      LoadCardUsage("CheatV1");
    }

    protected override void updateInfo()
    {
      //First get a copy of all table cards and bot hole cards
      var handDetails = decisionRequest.Cache.getCurrentHandDetails();
      var playerHoleCards = decisionRequest.Cache.getPlayerHoleCards(decisionRequest.PlayerId);

      #region Setup
      byte[] tableCards = new byte[]{handDetails.tableCard1,
                                            handDetails.tableCard2,
                                            handDetails.tableCard3,
                                            handDetails.tableCard4,
                                            handDetails.tableCard5};

      int numTableCards = 0;
      if (tableCards[0] == 0)
        numTableCards = 0;
      else if (tableCards[3] == 0)
        numTableCards = 3;
      else if (tableCards[4] == 0)
        numTableCards = 4;
      else
        numTableCards = 5;

      int numAKQ = 0;
      bool AOnBoard = false, KOnBoard = false;
      for (int i = 0; i < numTableCards; i++)
      {
        if (tableCards[i] > 40)
        {
          if (tableCards[i] > 48)
            AOnBoard = true;
          else if (tableCards[i] > 44)
            KOnBoard = true;

          numAKQ++;
        }
      }

      long deck = 0;
      for (int i = 0; i < numTableCards; i++)
        deck += 1L << (tableCards[i] - 1);
      #endregion

      #region tableCards
      if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_AOnBoard_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_KOnBoard_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_AKQToBoardRatio_Real) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_FlushPossible_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_StraightPossible_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_TableFlushDraw_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_TableStraightDraw_Bool))
      {

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_AOnBoard_Bool))
          infoStore.SetInformationValue(InfoType.CP_AOnBoard_Bool, AOnBoard ? 1 : 0);

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_KOnBoard_Bool))
          infoStore.SetInformationValue(InfoType.CP_KOnBoard_Bool, KOnBoard ? 1 : 0);

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_AKQToBoardRatio_Real))
        {
          if (numTableCards != 0)
            infoStore.SetInformationValue(InfoType.CP_AKQToBoardRatio_Real, (decimal)numAKQ / (decimal)numTableCards);
          else
            infoStore.SetInformationValue(InfoType.CP_AKQToBoardRatio_Real, 0);
        }

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_FlushPossible_Bool))
        {
          bool flushPossible = PokerBot.Definitions.Deck.FlushPossible(deck);
          infoStore.SetInformationValue(InfoType.CP_FlushPossible_Bool, flushPossible ? 1 : 0);
        }

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_StraightPossible_Bool))
        {
          bool straightPossible = PokerBot.Definitions.Deck.StraightPossible(deck);
          infoStore.SetInformationValue(InfoType.CP_StraightPossible_Bool, straightPossible ? 1 : 0);
        }

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_TableFlushDraw_Bool))
        {
          bool flushDrawPossibleOnTable = PokerBot.Definitions.Deck.FlushDrawPossibleOnTable(deck);
          infoStore.SetInformationValue(InfoType.CP_TableFlushDraw_Bool, flushDrawPossibleOnTable ? 1 : 0);
        }

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_TableStraightDraw_Bool))
        {
          bool straightDrawPossibleOnTable = PokerBot.Definitions.Deck.StraightDrawPossibleOnTable(deck);
          infoStore.SetInformationValue(InfoType.CP_TableStraightDraw_Bool, straightDrawPossibleOnTable ? 1 : 0);
        }
      }
      #endregion

      #region holeCards

      if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsAAPair_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsKKPair_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsOtherHighPair_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsOtherHighPair_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsOtherHighPair_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsOtherHighPair_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsOtherLowPair_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsAK_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsMidConnector_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsLowConnector_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsTroubleHand_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsAOrKInHand_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCards3KindOrBetterMadeWithHC_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsInnerStraightDrawWithHC_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsOuterStraightDrawWithHC_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsTopOrTwoPair_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsStraightDraw_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsFlushDraw_Bool))
      {
        bool suited;
        int highCard;
        //Find out if hole cards are pair, connector or split connector and set neccesary boolian values

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsAAPair_Bool) ||
        decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsKKPair_Bool) ||
        decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsOtherHighPair_Bool) ||
        decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsOtherHighPair_Bool) ||
        decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsOtherHighPair_Bool) ||
        decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsOtherHighPair_Bool) ||
        decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsOtherLowPair_Bool) ||
        decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsAK_Bool) ||
        decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsMidConnector_Bool) ||
        decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsLowConnector_Bool))
        {
          #region BigSwitch
          switch ((int)Math.Abs(((playerHoleCards.holeCard1 - 1) / 4) - ((playerHoleCards.holeCard2 - 1) / 4)))
          {
            //Pair
            case 0:
              switch ((playerHoleCards.holeCard1 - 1) / 4)
              {
                //AA
                case 12:
                  SetHoleInfoTypeToTrue(InfoType.CP_HoleCardsAAPair_Bool, false);
                  break;
                //KK
                case 11:
                  SetHoleInfoTypeToTrue(InfoType.CP_HoleCardsKKPair_Bool, false);
                  break;
                //QQ
                case 10:
                  SetHoleInfoTypeToTrue(InfoType.CP_HoleCardsOtherHighPair_Bool, false);
                  break;
                //JJ
                case 9:
                  SetHoleInfoTypeToTrue(InfoType.CP_HoleCardsOtherHighPair_Bool, false);
                  break;
                //XX
                case 8:
                  SetHoleInfoTypeToTrue(InfoType.CP_HoleCardsOtherHighPair_Bool, false);
                  break;
                //99
                case 7:
                  SetHoleInfoTypeToTrue(InfoType.CP_HoleCardsOtherHighPair_Bool, false);
                  break;
                //Other
                default:
                  SetHoleInfoTypeToTrue(InfoType.CP_HoleCardsOtherLowPair_Bool, false);
                  break;
              }
              break;
            //Connectors
            case 1:
              //Get the highest card number
              highCard = (playerHoleCards.holeCard1 > playerHoleCards.holeCard2 ? playerHoleCards.holeCard1 : playerHoleCards.holeCard2);
              highCard = (highCard - 1) / 4;
              //Are hole cards suited
              suited = (playerHoleCards.holeCard1 - playerHoleCards.holeCard2) % 4 == 0;

              //If AK have to set that directly SetInfoTypeToTrue function handles rest
              if (highCard == 12)
                SetHoleInfoTypeToTrue(InfoType.CP_HoleCardsAK_Bool, suited);
              //Otherwise find out which type of connector we have and set appropriate bools 
              else if (highCard >= 7)
                SetAllHoleBoolToFalse(suited);
              else if (highCard >= 3)
                SetHoleInfoTypeToTrue(InfoType.CP_HoleCardsMidConnector_Bool, suited);
              else if (highCard < 3)
                SetHoleInfoTypeToTrue(InfoType.CP_HoleCardsLowConnector_Bool, suited);
              break;
            //Connectors with A 2
            case 12:
              suited = (playerHoleCards.holeCard1 - playerHoleCards.holeCard2) % 4 == 0;
              SetHoleInfoTypeToTrue(InfoType.CP_HoleCardsLowConnector_Bool, suited);
              break;
            //Split connectors
            case 2:
              //Get high card value
              highCard = (playerHoleCards.holeCard1 > playerHoleCards.holeCard2 ? playerHoleCards.holeCard1 : playerHoleCards.holeCard2);
              highCard = (highCard - 1) / 4;
              //Find out if suited
              suited = (playerHoleCards.holeCard1 - playerHoleCards.holeCard2) % 4 == 0;

              if (highCard >= 7)
                SetAllHoleBoolToFalse(suited);
              else if (highCard >= 3)
                SetHoleInfoTypeToTrue(InfoType.CP_HoleCardsMidConnector_Bool, suited);
              else if (highCard < 3)
                SetHoleInfoTypeToTrue(InfoType.CP_HoleCardsLowConnector_Bool, suited);
              break;
            //Possible split connectors with A 3
            case 11:
              if (((playerHoleCards.holeCard1 - 1) / 4) == 3 || ((playerHoleCards.holeCard1 - 1) / 4) == 12)
              {
                suited = (playerHoleCards.holeCard1 - playerHoleCards.holeCard2) % 4 == 0;
                SetHoleInfoTypeToTrue(InfoType.CP_HoleCardsLowConnector_Bool, suited);
              }
              else
              {
                suited = (playerHoleCards.holeCard1 - playerHoleCards.holeCard2) % 4 == 0;
                SetAllHoleBoolToFalse(suited);
              }
              break;
            default:
              suited = (playerHoleCards.holeCard1 - playerHoleCards.holeCard2) % 4 == 0;
              SetAllHoleBoolToFalse(suited);
              break;
          }
          #endregion
        }

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsTroubleHand_Bool))
        {
          if ((playerHoleCards.holeCard1 > (short)Card.Spades8 && playerHoleCards.holeCard2 > (short)Card.Spades8 ||
              (playerHoleCards.holeCard1 > (short)Card.Spades7 && playerHoleCards.holeCard1 < (short)Card.Clubs10 &&
               playerHoleCards.holeCard2 > (short)Card.Spades7 && playerHoleCards.holeCard2 < (short)Card.Clubs10)) &&
              ((int)Math.Abs(((playerHoleCards.holeCard1 - 1) / 4) - ((playerHoleCards.holeCard2 - 1) / 4))) != 0 &&
              (playerHoleCards.holeCard1 < (short)Card.ClubsK || playerHoleCards.holeCard2 < (short)Card.ClubsK))
            infoStore.SetInformationValue(InfoType.CP_HoleCardsTroubleHand_Bool, 1);
          else
            infoStore.SetInformationValue(InfoType.CP_HoleCardsTroubleHand_Bool, 0);
        }

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsAOrKInHand_Bool))
        {
          if (playerHoleCards.holeCard1 > (short)Card.SpadesQ || playerHoleCards.holeCard2 > (short)Card.SpadesQ)
            infoStore.SetInformationValue(InfoType.CP_HoleCardsAOrKInHand_Bool, 1);
          else
            infoStore.SetInformationValue(InfoType.CP_HoleCardsAOrKInHand_Bool, 0);
        }

        //We can just do this anyway because it's all bitshiffting, i.e. fast
        long holeCards = (1L << (playerHoleCards.holeCard1 - 1)) + (1L << (playerHoleCards.holeCard2 - 1));

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCards3KindOrBetterMadeWithHC_Bool))
        {
          bool holeCards3KindOrBetterWithHC = Deck.ThreeOfKindMadeWithHoleCards(holeCards, deck) ||
              Deck.StraightMadeWithHoleCards(holeCards, deck) ||
              Deck.FlushMadeWithHoleCards(holeCards, deck) ||
              Deck.FourOfKindMadeWithHoleCards(holeCards, deck) ||
              Deck.FullHouseWithHoleCards(holeCards, deck);

          infoStore.SetInformationValue(InfoType.CP_HoleCards3KindOrBetterMadeWithHC_Bool, holeCards3KindOrBetterWithHC ? 1 : 0);
        }

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsInnerStraightDrawWithHC_Bool))
          infoStore.SetInformationValue(InfoType.CP_HoleCardsInnerStraightDrawWithHC_Bool, Deck.InsideStraightDrawWithHoleCards(holeCards, deck) ? 1 : 0);

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsOuterStraightDrawWithHC_Bool))
          infoStore.SetInformationValue(InfoType.CP_HoleCardsOuterStraightDrawWithHC_Bool, Deck.OutsideStraightDrawWithHoleCards(holeCards, deck) ? 1 : 0);

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsTopOrTwoPair_Bool))
        {
          bool holeCardTopPairOrTwoPair = Deck.TopPairMadeWithHoleCards(holeCards, deck) || Deck.TwoPairMadeWithHoleCards(holeCards, deck);
          infoStore.SetInformationValue(InfoType.CP_HoleCardsTopOrTwoPair_Bool, holeCardTopPairOrTwoPair ? 1 : 0);
        }

        deck += 1L << (playerHoleCards.holeCard1 - 1);
        deck += 1L << (playerHoleCards.holeCard2 - 1);

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsStraightDraw_Bool))
          infoStore.SetInformationValue(InfoType.CP_HoleCardsStraightDraw_Bool, Deck.StraightDrawPossible(deck) ? 1 : 0);

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsFlushDraw_Bool))
          infoStore.SetInformationValue(InfoType.CP_HoleCardsFlushDraw_Bool, Deck.FlushDrawPossible(deck) ? 1 : 0);
      }

      #endregion

      #region HoleCardsMatchedPlayability
      if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.CP_HoleCardsMatchedPlayability))
      {
        if (decisionRequest.Cache.getBettingRound() == 0)
        {
          if (decisionRequest.RequiredInfoTypeUpdateConfigs.ContainsKey(InfoType.CP_HoleCardsMatchedPlayability))
            infoStore.SetInformationValue(InfoType.CP_HoleCardsMatchedPlayability, GetPlayerHoleCardPlayability(playerHoleCards, decisionRequest.RequiredInfoTypeUpdateConfigs[InfoType.CP_HoleCardsMatchedPlayability]));
          else
            infoStore.SetInformationValue(InfoType.CP_HoleCardsMatchedPlayability, GetPlayerHoleCardPlayability(playerHoleCards));
        }
        else
          infoStore.SetInformationValue(InfoType.CP_HoleCardsMatchedPlayability, 0);
      }

      #endregion
    }

    public static decimal GetPlayerHoleCardPlayability(Database.databaseCache.playerCards playerHoleCards, string nameKey = "")
    {
      Card card1 = (Card)playerHoleCards.holeCard1;
      Card card2 = (Card)playerHoleCards.holeCard2;
      bool holeCardsSuited = Deck.GetCardSuit(card1) == Deck.GetCardSuit(card2);

      if (holeCardsSuited)
      {
        if (Deck.GetCardNumber(card1) > Deck.GetCardNumber(card2))
        {
          if (nameKey != "")
          {
            if (!suitedHoleCardUsageDict.ContainsKey(nameKey))
              throw new Exception("Hole card usage data not available for nameKey = " + nameKey);

            return suitedHoleCardUsageDict[nameKey][Deck.GetCardNumber(card1)][Deck.GetCardNumber(card2)];
          }
          else
            return suitedHoleCardUsageDict["Marc"][Deck.GetCardNumber(card1)][Deck.GetCardNumber(card2)];
        }
        else
        {
          if (nameKey != "")
          {
            if (!suitedHoleCardUsageDict.ContainsKey(nameKey))
              throw new Exception("Hole card usage data not available for nameKey = " + nameKey);

            return suitedHoleCardUsageDict[nameKey][Deck.GetCardNumber(card2)][Deck.GetCardNumber(card1)];
          }
          else
            return suitedHoleCardUsageDict["Marc"][Deck.GetCardNumber(card2)][Deck.GetCardNumber(card1)];
        }
      }
      else
      {
        if (Deck.GetCardNumber(card1) > Deck.GetCardNumber(card2))
        {
          if (nameKey != "")
          {
            if (!unSuitedHoleCardUsageDict.ContainsKey(nameKey))
              throw new Exception("Hole card usage data not available for nameKey = " + nameKey);

            return unSuitedHoleCardUsageDict[nameKey][Deck.GetCardNumber(card1)][Deck.GetCardNumber(card2)];
          }
          else
            return unSuitedHoleCardUsageDict["Marc"][Deck.GetCardNumber(card1)][Deck.GetCardNumber(card2)];
        }
        else
        {
          if (nameKey != "")
          {
            if (!unSuitedHoleCardUsageDict.ContainsKey(nameKey))
              throw new Exception("Hole card usage data not available for nameKey = " + nameKey);

            return unSuitedHoleCardUsageDict[nameKey][Deck.GetCardNumber(card2)][Deck.GetCardNumber(card1)];
          }
          else
            return unSuitedHoleCardUsageDict["Marc"][Deck.GetCardNumber(card2)][Deck.GetCardNumber(card1)];
        }
      }
    }

    private void SetHoleInfoTypeToTrue(InfoType info, bool suited)
    {
      infoStore.SetInformationValue(info, 1);
      infoStore.SetInformationValue(InfoType.CP_HoleCardsSuited_Bool, (suited ? 1 : 0));

      if (info == InfoType.CP_HoleCardsOtherHighPair_Bool)
        infoStore.SetInformationValue(InfoType.CP_HoleCardsOtherPair_Bool, 1);
      else
      {
        infoStore.SetInformationValue(InfoType.CP_HoleCardsOtherHighPair_Bool, 0);

        if (info != InfoType.CP_HoleCardsOtherLowPair_Bool)
          infoStore.SetInformationValue(InfoType.CP_HoleCardsOtherPair_Bool, 0);
      }

      if (info == InfoType.CP_HoleCardsOtherLowPair_Bool)
        infoStore.SetInformationValue(InfoType.CP_HoleCardsOtherPair_Bool, 1);
      else
      {
        infoStore.SetInformationValue(InfoType.CP_HoleCardsOtherLowPair_Bool, 0);

        if (info != InfoType.CP_HoleCardsOtherHighPair_Bool)
          infoStore.SetInformationValue(InfoType.CP_HoleCardsOtherPair_Bool, 0);
      }

      if (info != InfoType.CP_HoleCardsAAPair_Bool)
        infoStore.SetInformationValue(InfoType.CP_HoleCardsAAPair_Bool, 0);
      if (info != InfoType.CP_HoleCardsKKPair_Bool)
        infoStore.SetInformationValue(InfoType.CP_HoleCardsKKPair_Bool, 0);
      if (info != InfoType.CP_HoleCardsLowConnector_Bool)
        infoStore.SetInformationValue(InfoType.CP_HoleCardsLowConnector_Bool, 0);
      if (info != InfoType.CP_HoleCardsMidConnector_Bool)
        infoStore.SetInformationValue(InfoType.CP_HoleCardsMidConnector_Bool, 0);
      if (info != InfoType.CP_HoleCardsAK_Bool)
        infoStore.SetInformationValue(InfoType.CP_HoleCardsAK_Bool, 0);
    }

    private void SetAllHoleBoolToFalse(bool suited)
    {
      infoStore.SetInformationValue(InfoType.CP_HoleCardsAAPair_Bool, 0);
      infoStore.SetInformationValue(InfoType.CP_HoleCardsAK_Bool, 0);
      infoStore.SetInformationValue(InfoType.CP_HoleCardsKKPair_Bool, 0);
      infoStore.SetInformationValue(InfoType.CP_HoleCardsLowConnector_Bool, 0);
      infoStore.SetInformationValue(InfoType.CP_HoleCardsMidConnector_Bool, 0);
      infoStore.SetInformationValue(InfoType.CP_HoleCardsOtherPair_Bool, 0);
      infoStore.SetInformationValue(InfoType.CP_HoleCardsOtherHighPair_Bool, 0);
      infoStore.SetInformationValue(InfoType.CP_HoleCardsOtherLowPair_Bool, 0);
      infoStore.SetInformationValue(InfoType.CP_HoleCardsSuited_Bool, suited ? 1 : 0);
    }
  }
}

