using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PokerBot.Definitions;
using PokerBot.Database;

namespace PokerBot.CacheMonitor
{
  public partial class CacheMonitor : Form
  {
    Control[,] playerControls;

    databaseCache genericCache;
    object locker = new object();

    databaseCache.handDetails currentHandDetails;
    databaseCache.playerDetails[] currentPlayerDetails;
    databaseCache.playActions[] playActions;
    //DateTime lastActionTime;

    long currentHandId;
    string actionString;
    byte[] activePositions;
    byte echoHoleCards;
    bool showActivePlayerCardsOnly;
    bool showNonBotPlayerCardsOnly;

    bool testMode;
    byte dealerPosition = 0;
    DateTime startTime = DateTime.Now;

    //Used for the monitor positioning.
    Point initialButtonOffset;

    //Used when manual decisions are required.
    volatile bool manualDecisionMade;
    Play manualDecision;

    long onlyShowCardsForPlayerId = -1;

    public void InitialiseMonitor(databaseCache genericCache, long playerIdCards)
    {
      this.genericCache = genericCache;
      playerControls = new Control[10, 7]
          {
                    {player0Name, player0Stack, player0Card1, player0Card2, player0Action, pos0Sitout, pos0Disabled},
                    {player1Name, player1Stack, player1Card1, player1Card2, player1Action, pos1Sitout, pos1Disabled},
                    {player2Name, player2Stack, player2Card1, player2Card2, player2Action, pos2Sitout, pos2Disabled},
                    {player3Name, player3Stack, player3Card1, player3Card2, player3Action, pos3Sitout, pos3Disabled},
                    {player4Name, player4Stack, player4Card1, player4Card2, player4Action, pos4Sitout, pos4Disabled},
                    {player5Name, player5Stack, player5Card1, player5Card2, player5Action, pos5Sitout, pos5Disabled},
                    {player6Name, player6Stack, player6Card1, player6Card2, player6Action, pos6Sitout, pos6Disabled},
                    {player7Name, player7Stack, player7Card1, player7Card2, player7Action, pos7Sitout, pos7Disabled},
                    {player8Name, player8Stack, player8Card1, player8Card2, player8Action, pos8Sitout, pos8Disabled},
                    {player9Name, player9Stack, player9Card1, player9Card2, player9Action, pos9Sitout, pos9Disabled},
          };

      //By default we hide the manual buttons.
      showHideManualButtons(false);
      echoHoleCards = 0;
      onlyShowCardsForPlayerId = playerIdCards;
      updateTableUI();
    }

    public void InitialiseMonitor(databaseCache genericCache)
    {
      InitialiseMonitor(genericCache, -1);
    }

    /// <summary>
    /// Returns a pointer to the textbox 'chat' window of the cacheMonitor.
    /// </summary>
    public TextBox CacheMonitorTextBox
    {
      get { return this.handHistory; }
    }

    /// <summary>
    /// Get and set the cache currently being used. If cache is set to null the cachemonitor will just blank all controls and wait for a valid cache.
    /// </summary>
    public databaseCache MonitorCache
    {
      get { return genericCache; }
      set { lock (locker) genericCache = value; }
    }

    /// <summary>
    /// Returns true once a manual decision request has been satisfied.
    /// </summary>
    public bool ManualDecisionMade
    {
      get { return manualDecisionMade; }
      set { manualDecisionMade = value; }
    }

    /// <summary>
    /// Returns the manual decision.
    /// </summary>
    public Play ManualDecision
    {
      get { return manualDecision; }
    }

    private long[] lastLocalIndex = new long[2] { -1, -1 };

    /// <summary>
    /// Refreshes the cache monitor screen ouput with the current state of the cache.
    /// </summary>
    public void updateTableUI()
    {
      lock (locker)
      {
        if (genericCache == null)
        {
          //Just clear everything and wait for the cache to be set.
          resetAllTableElements();
        }
        else
        {
          #region updatingNow
          //Prevent any more changes from happening to the cache while the table is updated
          genericCache.startRead();

          //Update hands played
          handsPlayed.Text = genericCache.getNumHandsPlayed().ToString();

          //It's possible the table tries to update exactly after a hand has finished
          //In this particular case for now we are just going to exit updateTableUI
          if (genericCache.currentHandExists())
          {

            currentHandDetails = genericCache.getCurrentHandDetails();
            currentPlayerDetails = genericCache.getPlayerDetails();

            //Disable unused seats
            disableUnusedSeats(genericCache.NumSeats);

            //Get any new actions
            if (lastLocalIndex[0] == -1)
              playActions = genericCache.getAllHandActions();
            else
              playActions = genericCache.getHandActionsBasedOnLocalIndex(lastLocalIndex[0], (byte)lastLocalIndex[1]);

            //Once we have actions we record the last one we go
            if (playActions.Length > 0)
            {
              lastLocalIndex[0] = playActions.Last().handId;
              lastLocalIndex[1] = playActions.Last().localIndex;
            }

            //Only update hand information if a current hand exists
            if (currentHandDetails.currentHandExists)
            {
              if (currentHandId != currentHandDetails.handId)
              {
                currentHandId = currentHandDetails.handId;
                handHistory.AppendText("\n\nNew Hand Started - HandId " + currentHandId.ToString() + "\n\n");
                echoHoleCards = 0;
                resetAllTableElements();
              }

              moveDealerButton(currentHandDetails.dealerPosition);

              potValue.Text = currentHandDetails.potValue.ToString();

              if (currentHandDetails.tableCard1 != (byte)Card.NoCard)
                setCard(currentHandDetails.tableCard1, tableCard1);
              if (currentHandDetails.tableCard2 != (byte)Card.NoCard)
                setCard(currentHandDetails.tableCard2, tableCard2);
              if (currentHandDetails.tableCard3 != (byte)Card.NoCard)
                setCard(currentHandDetails.tableCard3, tableCard3);
              if (currentHandDetails.tableCard4 != (byte)Card.NoCard)
                setCard(currentHandDetails.tableCard4, tableCard4);
              if (currentHandDetails.tableCard5 != (byte)Card.NoCard)
                setCard(currentHandDetails.tableCard5, tableCard5);
            }

            //Actions which also update the useraction box
            //Call, Raise & Check
            //Big & Little Blind
            //Fold
            //Wins

            //Need to update actions
            //Add all actions in playActions to chat window
            for (int i = 0; i < playActions.GetLength(0); i++)
            {
              actionString = "Player Name: " + genericCache.getPlayerName(playActions.ElementAt(i).playerId)
                  + ", Action: " + playActions.ElementAt(i).actionType.ToString() + ", Action Value: " + playActions.ElementAt(i).actionValue.ToString() + "\n";

              #region actionStringSwtich

              switch (playActions.ElementAt(i).actionType)
              {
                case PokerAction.JoinTable:
                  actionString = "[Dealer] " + genericCache.getPlayerName(playActions.ElementAt(i).playerId) + " sits down at the table.";
                  break;
                case PokerAction.LeaveTable:
                  actionString = "[Dealer] " + genericCache.getPlayerName(playActions.ElementAt(i).playerId) + " leaves the table.";
                  break;
                case PokerAction.SitOut:
                  sitInOutPlayer(genericCache.getPlayerPosition(playActions.ElementAt(i).playerId), false);
                  actionString = "[Dealer] " + genericCache.getPlayerName(playActions.ElementAt(i).playerId) + " is now sitting out.";
                  break;
                case PokerAction.SitIn:
                  sitInOutPlayer(genericCache.getPlayerPosition(playActions.ElementAt(i).playerId), true);
                  actionString = "[Dealer] " + genericCache.getPlayerName(playActions.ElementAt(i).playerId) + " is now playing again.";
                  break;
                case PokerAction.LittleBlind:
                  actionString = genericCache.getPlayerName(playActions.ElementAt(i).playerId) + " posts the little blind.";
                  playerControls[genericCache.getPlayerPosition(playActions.ElementAt(i).playerId), 4].Text = "LB - " + genericCache.LittleBlind.ToString();
                  break;
                case PokerAction.BigBlind:
                  actionString = genericCache.getPlayerName(playActions.ElementAt(i).playerId) + " posts the big blind.";
                  playerControls[genericCache.getPlayerPosition(playActions.ElementAt(i).playerId), 4].Text = "BB - " + genericCache.BigBlind.ToString();
                  break;
                case PokerAction.Fold:
                  actionString = genericCache.getPlayerName(playActions.ElementAt(i).playerId) + " folds.";
                  playerControls[genericCache.getPlayerPosition(playActions.ElementAt(i).playerId), 4].Text = "Fold";
                  clearCard((PictureBox)playerControls[genericCache.getPlayerPosition(playActions.ElementAt(i).playerId), 2]);
                  clearCard((PictureBox)playerControls[genericCache.getPlayerPosition(playActions.ElementAt(i).playerId), 3]);
                  break;
                case PokerAction.Check:
                  actionString = genericCache.getPlayerName(playActions.ElementAt(i).playerId) + " checks.";
                  playerControls[genericCache.getPlayerPosition(playActions.ElementAt(i).playerId), 4].Text = "Check";
                  break;
                case PokerAction.Call:
                  actionString = genericCache.getPlayerName(playActions.ElementAt(i).playerId) + " calls " + playActions.ElementAt(i).actionValue.ToString() + ".";
                  playerControls[genericCache.getPlayerPosition(playActions.ElementAt(i).playerId), 4].Text = "Call - " + playActions.ElementAt(i).actionValue.ToString();
                  break;
                case PokerAction.Raise:
                  actionString = genericCache.getPlayerName(playActions.ElementAt(i).playerId) + " raises to " + playActions.ElementAt(i).actionValue.ToString() + ".";
                  playerControls[genericCache.getPlayerPosition(playActions.ElementAt(i).playerId), 4].Text = "Raise - " + playActions.ElementAt(i).actionValue.ToString();
                  break;
                case PokerAction.WinPot:
                  actionString = genericCache.getPlayerName(playActions.ElementAt(i).playerId) + " wins " + playActions.ElementAt(i).actionValue.ToString() + ".";
                  playerControls[genericCache.getPlayerPosition(playActions.ElementAt(i).playerId), 4].Text = "Wins " + playActions.ElementAt(i).actionValue.ToString();
                  echoHoleCards = 1;
                  break;
                case PokerAction.AddStackCash:
                  actionString = genericCache.getPlayerName(playActions.ElementAt(i).playerId) + " adds " + playActions.ElementAt(i).actionValue.ToString() + " to their stack.";
                  break;
                case PokerAction.DeadBlind:
                  actionString = genericCache.getPlayerName(playActions.ElementAt(i).playerId) + " posts a dead blind.";
                  playerControls[genericCache.getPlayerPosition(playActions.ElementAt(i).playerId), 4].Text = "DB - " + playActions.ElementAt(i).actionValue.ToString();
                  break;
                case PokerAction.ReturnBet:
                  actionString = "Uncalled bet of " + playActions.ElementAt(i).actionValue.ToString() + " returned to " + genericCache.getPlayerName(playActions.ElementAt(i).playerId);
                  playerControls[genericCache.getPlayerPosition(playActions.ElementAt(i).playerId), 4].Text = "Bet Returned - " + playActions.ElementAt(i).actionValue.ToString();
                  break;
                case PokerAction.DealFlop:
                  actionString = "[Dealer] Flop Cards Revealed (" + ((Card)genericCache.getCurrentHandDetails().tableCard1).ToString() + "), (" + ((Card)genericCache.getCurrentHandDetails().tableCard2).ToString() + "), (" + ((Card)genericCache.getCurrentHandDetails().tableCard3).ToString() + ").";
                  newRoundTableRefresh();
                  break;
                case PokerAction.DealTurn:
                  actionString = "[Dealer] Turn Card Revealed (" + ((Card)genericCache.getCurrentHandDetails().tableCard4).ToString() + ").";
                  newRoundTableRefresh();
                  break;
                case PokerAction.DealRiver:
                  actionString = "[Dealer] River Card Revealed (" + ((Card)genericCache.getCurrentHandDetails().tableCard5).ToString() + ").";
                  newRoundTableRefresh();
                  break;
                case PokerAction.TableRake:
                  actionString = "[Dealer] Table rake of " + playActions.ElementAt(i).actionValue.ToString("#0.00");
                  break;
              }

              #endregion actionStringSwtich

              handHistory.AppendText(actionString + "\n");
              //lastActionTime = playActions.ElementAt(i).actionTime;
            }

            activePositions = genericCache.getActivePositions();

            //Only update player details if they exist
            if (activePositions.Length != 0)
            {
              bool endOfHand = false;

              if (genericCache.getBettingRound() == 3 && genericCache.getActivePositionsLeftToAct().Length == 0)
                endOfHand = true;

              for (int i = 0; i < currentPlayerDetails.Length; i++)
              {
                //{player0Name, player0Stack, player0Card1, player0Card2, player0Action},
                playerControls[currentPlayerDetails[i].position, 0].Text = currentPlayerDetails[i].playerName;
                playerControls[currentPlayerDetails[i].position, 1].Text = currentPlayerDetails[i].stack.ToString();

                //If echoHoleCards is set to true then echo out any known hole cards
                if (echoHoleCards == 1)
                {
                  if (genericCache.getPlayerHoleCards(currentPlayerDetails[i].playerId).holeCard1 != 0)
                  {
                    actionString = "[Hole Cards] " + currentPlayerDetails[i].playerName + " (" + (Card)genericCache.getPlayerHoleCards(currentPlayerDetails[i].playerId).holeCard1 + ", " + (Card)genericCache.getPlayerHoleCards(currentPlayerDetails[i].playerId).holeCard2 + ").";
                    handHistory.AppendText(actionString + "\n");
                  }
                }

                var isActive =
                    from ac in activePositions
                    where ac == currentPlayerDetails[i].position
                    select ac;

                var currentActivePosition = genericCache.getCurrentActiveTablePosition();

                if (onlyShowCardsForPlayerId != -1 && currentPlayerDetails[i].playerId == onlyShowCardsForPlayerId)
                {
                  setCard(genericCache.getPlayerHoleCards((long)currentPlayerDetails[i].playerId).holeCard1, (PictureBox)playerControls[currentPlayerDetails[i].position, 2]);
                  setCard(genericCache.getPlayerHoleCards((long)currentPlayerDetails[i].playerId).holeCard2, (PictureBox)playerControls[currentPlayerDetails[i].position, 3]);
                }
                else if (isActive.Count() == 1 && onlyShowCardsForPlayerId != -1 && currentPlayerDetails[i].playerId != onlyShowCardsForPlayerId)
                {
                  blankCard((PictureBox)playerControls[currentPlayerDetails[i].position, 2]);
                  blankCard((PictureBox)playerControls[currentPlayerDetails[i].position, 3]);
                }
                else if ((showNonBotPlayerCardsOnly && isActive.Count() == 1 && !currentPlayerDetails[i].isBot) || (endOfHand && isActive.Count() == 1))
                {
                  //We are only showing non bot player cards and we are currently at a non bot position.
                  //It is the end of the hand and this position is still active (it's a showdown folks).
                  setCard(genericCache.getPlayerHoleCards((long)currentPlayerDetails[i].playerId).holeCard1, (PictureBox)playerControls[currentPlayerDetails[i].position, 2]);
                  setCard(genericCache.getPlayerHoleCards((long)currentPlayerDetails[i].playerId).holeCard2, (PictureBox)playerControls[currentPlayerDetails[i].position, 3]);
                }
                else if (showNonBotPlayerCardsOnly && isActive.Count() == 1 && currentPlayerDetails[i].isBot)
                {
                  //We are only showing non bot player cards and we are currently at a bot position.
                  blankCard((PictureBox)playerControls[currentPlayerDetails[i].position, 2]);
                  blankCard((PictureBox)playerControls[currentPlayerDetails[i].position, 3]);
                }
                else if ((!showActivePlayerCardsOnly && isActive.Count() == 1) || (showActivePlayerCardsOnly && currentActivePosition == currentPlayerDetails[i].position))
                {
                  //show cards if we are showing everyones cards and this current position is active
                  //show cards if we are only showing active positions and we are currently in the active position.
                  setCard(genericCache.getPlayerHoleCards((long)currentPlayerDetails[i].playerId).holeCard1, (PictureBox)playerControls[currentPlayerDetails[i].position, 2]);
                  setCard(genericCache.getPlayerHoleCards((long)currentPlayerDetails[i].playerId).holeCard2, (PictureBox)playerControls[currentPlayerDetails[i].position, 3]);
                }
                else if (isActive.Count() == 1)
                {
                  //If we get here just show the back of the cards
                  blankCard((PictureBox)playerControls[currentPlayerDetails[i].position, 2]);
                  blankCard((PictureBox)playerControls[currentPlayerDetails[i].position, 3]);
                }

                //Player might be dead
                if (currentPlayerDetails[i].isDead)
                  sitInOutPlayer(currentPlayerDetails[i].position, false);
                else if (currentActivePosition == currentPlayerDetails[i].position)
                  setPlayerTurn(currentPlayerDetails[i].position);
                else
                  sitInOutPlayer(currentPlayerDetails[i].position, true);
              }

              echoHoleCards = 2;
            }

          }
          genericCache.endRead();
          #endregion
        }

        //lastLocalIndex = genericCache.getMostRecentLocalIndex();

        //Call garbage collection to keep memory usage under control (without this it will quite happily hit 0.5Gb usage)
        GC.Collect();
      }


      this.Invalidate();
    }

    /// <summary>
    /// Blacks out unused seats.
    /// </summary>
    /// <param name="numSeats"></param>
    public void disableUnusedSeats(byte numSeats)
    {

      for (int i = 0; i < (10 - numSeats); i++)
      {
        playerControls[(9 - i), 6].Visible = true;
      }

    }

    /// <summary>
    /// Sits a player in or out by changing background and text box appropriately.
    /// </summary>
    /// <param name="position">Position to modify.</param>
    /// <param name="sitPlayerIn">If set to true will sit position in, if set to false will sit position out.</param>
    public void sitInOutPlayer(byte position, bool sitPlayerIn)
    {
      if (sitPlayerIn)
      {
        playerControls[position, 5].Visible = false;
        playerControls[position, 0].BackColor = Color.FromArgb(209, 38, 40);
        playerControls[position, 1].BackColor = Color.FromArgb(209, 38, 40);
      }
      else
      {
        playerControls[position, 5].Visible = true;
        playerControls[position, 0].BackColor = Color.Silver;
        playerControls[position, 1].BackColor = Color.Silver;
        playerControls[position, 5].BackColor = Color.Silver;
      }
    }

    /// <summary>
    /// Highlights the current position which must act.
    /// </summary>
    /// <param name="position"></param>
    public void setPlayerTurn(byte position)
    {
      double phase = ((DateTime.Now - startTime).TotalSeconds / 2.0) * 2.0 * Math.PI;

      Color oscillate = Color.FromArgb(209, (int)(123 - 85 * Math.Cos(phase)), (int)(125 - 85 * Math.Cos(phase)));

      playerControls[position, 5].Visible = true;
      playerControls[position, 0].BackColor = oscillate;
      playerControls[position, 1].BackColor = oscillate;
      playerControls[position, 5].BackColor = oscillate;
    }

    /// <summary>
    /// Resets the controls specific to each hand which might not otherwise be reset
    /// </summary>
    public void resetAllTableElements()
    {
      potValue.Text = "";

      clearCard(tableCard1);
      clearCard(tableCard2);
      clearCard(tableCard3);
      clearCard(tableCard4);
      clearCard(tableCard5);

      for (int i = 0; i < playerControls.GetLength(0); i++)
      {
        clearCard((PictureBox)playerControls[i, 2]);
        clearCard((PictureBox)playerControls[i, 3]);
        //clearCard((PictureBox)playerControls[i, 4]);
        playerControls[i, 0].Text = "";
        playerControls[i, 1].Text = "";
        playerControls[i, 4].Text = "";
        sitInOutPlayer((byte)i, true);
      }
    }

    /// <summary>
    /// Removes any betting action strings from the table ready for the next round.
    /// </summary>
    public void newRoundTableRefresh()
    {
      for (int i = 0; i < playerControls.GetLength(0); i++)
      {
        playerControls[i, 4].Text = "";
      }
    }

    /// <summary>
    /// Sets the provided cardBox image equal to the card provided and makes the cardBox visible.
    /// </summary>
    /// <param name="card"></param>
    /// <param name="cardBox"></param>
    /// <returns></returns>
    private void setCard(byte card, System.Windows.Forms.PictureBox cardBox)
    {
      if (card != 0)
      {
        System.Drawing.Bitmap allCards = global::PokerBot.CacheMonitor.Properties.Resources.compactCards1;

        int xStart = (((int)(card - 1) / 4) * 52) + 2;
        int yStart = ((card - 1) % 4 * 70) + 1;

        cardBox.Image = (System.Drawing.Bitmap)allCards.Clone(new System.Drawing.Rectangle(xStart, yStart, 49, 68), allCards.PixelFormat);
        cardBox.SizeMode = PictureBoxSizeMode.StretchImage;
        cardBox.Visible = true;
      }
      else
        blankCard(cardBox);
    }

    /// <summary>
    /// Clears a cardBox and makes it invisible.
    /// </summary>
    /// <param name="cardBox"></param>
    private void clearCard(System.Windows.Forms.PictureBox cardBox)
    {
      cardBox.Image = null;
      cardBox.Visible = false;
    }

    /// <summary>
    /// Blanks a card, since a card does exist, we just don't know it!
    /// </summary>
    /// <param name="cardBox"></param>
    private void blankCard(System.Windows.Forms.PictureBox cardBox)
    {
      System.Drawing.Bitmap allCards = global::PokerBot.CacheMonitor.Properties.Resources.compactCards1;
      cardBox.Image = (System.Drawing.Bitmap)allCards.Clone(new System.Drawing.Rectangle(2, 278, 1, 1), allCards.PixelFormat);
      cardBox.SizeMode = PictureBoxSizeMode.StretchImage;
      cardBox.Visible = true;
    }

    /// <summary>
    /// Shows and hides the manual decision buttons. Making sure those buttons available are allowed. Also sets the default raise amount to the minimum allowable raise.
    /// </summary>
    /// <param name="buttonsVisible"></param>
    public void showHideManualButtons(bool buttonsVisible)
    {
      this.checkFoldButton.Visible = buttonsVisible;
      this.checkFoldButton.Enabled = true;

      this.callButton.Visible = buttonsVisible;
      this.callButton.Enabled = true;

      this.raiseAmount.Visible = buttonsVisible;
      this.raiseAmount.Enabled = true;

      this.raiseButton.Visible = buttonsVisible;
      this.raiseButton.Enabled = true;

      //If we are going to show the buttons we can decide at this point what the valid decisions are.
      if (buttonsVisible)
      {
        manualDecisionMade = false;
        long currentActivePlayerId = genericCache.getPlayerId(genericCache.getCurrentActiveTablePosition());

        if (genericCache.getMinimumPlayAmount() - genericCache.getPlayerCurrentRoundBetAmount(currentActivePlayerId) == 0)
        {
          //If the game is post flop and no-one has yet bet 
          this.checkFoldButton.Text = "Check";
          this.callButton.Enabled = false;
          this.raiseAmount.Text = genericCache.BigBlind.ToString();
        }
        else
        {
          decimal minCallAmount = genericCache.getMinimumPlayAmount();
          decimal currentRoundBetAmount = genericCache.getPlayerCurrentRoundBetAmount(currentActivePlayerId);
          decimal lastAdditionalRaiseAmount = genericCache.getCurrentRoundLastRaiseAmount();
          decimal minimumRaiseToAmount = (minCallAmount - lastAdditionalRaiseAmount) + (lastAdditionalRaiseAmount * 2);

          //Check is no longer the option.
          this.checkFoldButton.Text = "Fold";

          //If raising is no longer an option disable that.
          if (minCallAmount - currentRoundBetAmount > genericCache.getPlayerStack(currentActivePlayerId))
          {
            this.raiseButton.Enabled = false;
            this.raiseAmount.Enabled = false;
          }
          else
            //Set the text box to the minimum allowable raise amount.
            this.raiseAmount.Text = minimumRaiseToAmount.ToString();
        }
      }
    }

    /// <summary>
    /// Moves the dealer button to the provided table position
    /// </summary>
    /// <param name="dealerPosition"></param>
    private void moveDealerButton(byte dealerPosition)
    {
      System.Drawing.Point position0 = new Point(572, 114);
      System.Drawing.Point position1 = new Point(640, 157);
      System.Drawing.Point position2 = new Point(665, 264);
      System.Drawing.Point position3 = new Point(590, 323);
      System.Drawing.Point position4 = new Point(423, 327);
      System.Drawing.Point position5 = new Point(220, 325);
      System.Drawing.Point position6 = new Point(144, 277);
      System.Drawing.Point position7 = new Point(127, 182);
      System.Drawing.Point position8 = new Point(201, 119);
      System.Drawing.Point position9 = new Point(309, 105);

      #region dealerPositionSwitch
      switch (dealerPosition)
      {
        case 0:
          this.dealerButton.Location = position0;
          break;
        case 1:
          this.dealerButton.Location = position1;
          break;
        case 2:
          this.dealerButton.Location = position2;
          break;
        case 3:
          this.dealerButton.Location = position3;
          break;
        case 4:
          this.dealerButton.Location = position4;
          break;
        case 5:
          this.dealerButton.Location = position5;
          break;
        case 6:
          this.dealerButton.Location = position6;
          break;
        case 7:
          this.dealerButton.Location = position7;
          break;
        case 8:
          this.dealerButton.Location = position8;
          break;
        case 9:
          this.dealerButton.Location = position9;
          break;
      }
      #endregion dealerPositionSwitch
    }

    public delegate void triggerDelegate();

    public void triggerManualDecision()
    {
      ManualDecisionMade = false;
      showHideManualButtons(true);
    }
  }
}
