using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using PokerBot.Definitions;
using PokerBot.Database;

// We want to create 10 new players with random WR limits (between 0 and 5)
// We play the game until the first player is sat out
// We then record each players stack amount in some statistics field (maybe best to record win or lose amount from starting stack)
// Then start again
// After many many many iterations we should be able to plot
//  Correlation between win amount and ratio between play and raise
//  Correlation between win amount and play ratio
//  Correlation between win amount and raise ratio

namespace PokerBot.BotGame
{
  public partial class BotGame : Form
  {
    databaseCacheClient clientCache;
    PokerGameBase pokerGame;

    List<Control> neuralTrainingOutputFields = new List<Control>();
    List<string> neuralPlayerNames = new List<string>();
    List<Control> neuralPlayerActionLog = new List<Control>();

    public BotGame()
    {
      //By default set the database offline
      databaseQueries.SetDatabaseLocalMode("Resources\\ManualPlayersTable.csv");

      InitializeComponent();
    }

    /// <summary>
    /// Closes any remaining game threads
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Main_FormClosing(object sender, FormClosingEventArgs e)
    {
      try
      {
        if (pokerGame != null)
        {
          pokerGame.EndGame = true;
        }
      }
      catch (Exception)
      {
        //MessageBox.Show("The bot game thread did not close correctly (May never have been started).");
      }
    }

    /// <summary>
    /// Show the poker table for the bot vs. bot game
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void viewCacheMonitor_Click(object sender, EventArgs e)
    {
      if (clientCache != null)
      {
        CacheMonitor.CacheMonitor form = new CacheMonitor.CacheMonitor(clientCache);
        form.Show();
      }
      else
        throw new Exception("Can't view the table unless the cache has been created.");
    }

    public void SetupNeuralTraining()
    {
      neuralTrainingOutputFields.AddRange(new List<Control> {aiSuggestion, winPercentage, raiseRatio,potRatio,raisedLastRound,calledLastRound,impliedPotOdds,immPotOdds,lastActionRaise, lastRoundBetsToCall, playerMoneyInPot, raiseToCheck,
                                                    raiseToCall,foldToCall,raiseToStealSuccess,raiseToCallAmount,raiseToStealAmount, currentPlayerId, weightedWR});

      neuralPlayerActionLog.AddRange(new List<Control> { player1NoLog, player2NoLog, player3NoLog, player4NoLog,
                                player5NoLog,player6NoLog,player7NoLog,player8NoLog,player9NoLog});

      neuralPlayerNames.AddRange(new List<string> { "Neural1", "Neural2", "Neural3", "Neural4", "Neural5", "Neural6", "Neural7", "Neural8", "Neural9" });
    }

    /// <summary>
    /// Starts the neural training game
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void startNeuralTraining_Click(object sender, EventArgs e)
    {
      if (startNeuralTraining.Text == "Start Game")
      {
        SetupNeuralTraining();
        clientCache = new databaseCacheClient(short.Parse(clientId.Text), this.gameName.Text, decimal.Parse(this.littleBlind.Text), decimal.Parse(this.bigBlind.Text), decimal.Parse(this.startingStack.Text), 9, HandDataSource.NeuralTraining);
        pokerGame = new ManualNeuralTrainingPokerGame(PokerGameType.ManualNeuralTraining, clientCache, 0, neuralPlayerNames.ToArray(), Decimal.Parse(startingStack.Text), 0, 0, neuralTrainingOutputFields, neuralPlayerActionLog);
        pokerGame.startGameTask();

        viewNerualTrainingTable.Enabled = true;
        startNeuralTraining.Text = "End Game";
      }
      else
      {
        startNeuralTraining.Text = "Ending Game";
        startNeuralTraining.Enabled = false;

        pokerGame.EndGame = true;

        startNeuralTraining.Text = "Start Game";
        startNeuralTraining.Enabled = true;
      }

    }

    void setBettingButtons(bool enable)
    {
      checkfoldAction.Enabled = enable;
      callAction.Enabled = enable;
      raiseToCallAction.Enabled = enable;
      raiseToStealAction.Enabled = enable;
      allInAction.Enabled = enable;
    }

    private void checkfoldAction_Click(object sender, EventArgs e)
    {
      setBettingButtons(false);
      decimal playerBetAmount;
      clientCache.getPlayerCurrentRoundBetAmount(long.Parse(currentPlayerId.Text), out playerBetAmount);

      if (clientCache.getMinimumPlayAmount() - playerBetAmount > 0)
        ((ManualNeuralTrainingPokerGame)pokerGame).SetManualDecision(new Play(PokerAction.Fold, 0, 0, clientCache.getCurrentHandId(), 0), 0);
      else
        ((ManualNeuralTrainingPokerGame)pokerGame).SetManualDecision(new Play(PokerAction.Check, 0, 0, clientCache.getCurrentHandId(), 0), 0);
    }

    private void callAction_Click(object sender, EventArgs e)
    {
      setBettingButtons(false);

      decimal playerBetAmount;
      decimal playerStackAmount;
      decimal minimumCallAmount;

      clientCache.getPlayerCurrentRoundBetAmount(long.Parse(currentPlayerId.Text), out playerBetAmount);
      playerStackAmount = clientCache.getPlayerStack(long.Parse(currentPlayerId.Text));
      minimumCallAmount = clientCache.getMinimumPlayAmount();

      if (minimumCallAmount - playerBetAmount < playerStackAmount)
        ((ManualNeuralTrainingPokerGame)pokerGame).SetManualDecision(new Play(PokerAction.Call, minimumCallAmount - playerBetAmount, 0, clientCache.getCurrentHandId(), 0), 0);
      else
        ((ManualNeuralTrainingPokerGame)pokerGame).SetManualDecision(new Play(PokerAction.Call, playerStackAmount, 0, clientCache.getCurrentHandId(), 0), 0);
    }

    private void raiseToCallAction_Click(object sender, EventArgs e)
    {
      setBettingButtons(false);
      ((ManualNeuralTrainingPokerGame)pokerGame).SetManualDecision(new Play(PokerAction.Raise, decimal.Parse(raiseToCallAmount.Text), 0, clientCache.getCurrentHandId(), 0), 0);
    }

    private void raiseToStealAction_Click(object sender, EventArgs e)
    {
      setBettingButtons(false);
      ((ManualNeuralTrainingPokerGame)pokerGame).SetManualDecision(new Play(PokerAction.Raise, decimal.Parse(raiseToStealAmount.Text), 0, clientCache.getCurrentHandId(), 0), 1);
    }

    private void currentPlayerId_TextChanged(object sender, EventArgs e)
    {
      if (currentPlayerId.Text != "")
        setBettingButtons(true);
    }

    private void viewNerualTrainingTable_Click(object sender, EventArgs e)
    {
      if (clientCache != null)
      {
        CacheMonitor.CacheMonitor form = new CacheMonitor.CacheMonitor(clientCache, false, true);
        form.Show();
      }
      else
        throw new Exception("Can't view the table unless the cache has been created.");
    }

    private void allInAction_Click(object sender, EventArgs e)
    {
      setBettingButtons(false);
      ((ManualNeuralTrainingPokerGame)pokerGame).SetManualDecision(new Play(PokerAction.Raise, clientCache.getPlayerStack(long.Parse(currentPlayerId.Text)), 0, clientCache.getCurrentHandId(), 0), 2);
    }

    private void playPoker_Click(object sender, EventArgs e)
    {

      if (playPoker.Text == "Play Poker")
      {
        int actionPauseTime = int.Parse(this.actionPause.Text);
        bool useRemoteAiServer = this.useAiServer.Checked;
        byte minNumTablePlayers = 2;

        AISelection[] selectedPlayers = this.aiSelectionControl1.AISelection();

        //Select the playerId's for all of the bot players
        if (selectedPlayers.Length > 10)
          throw new Exception("A maximum of 10 players is allowed.");

        long[] newPlayerIds = PokerHelper.CreateOpponentPlayers(selectedPlayers, obfuscateBots.Checked, 18);
        string[] selectedPlayerNames = new string[newPlayerIds.Length];
        for (int i = 0; i < newPlayerIds.Length; i++)
          selectedPlayerNames[i] = databaseQueries.convertToPlayerNameFromId(newPlayerIds[i]);

        //Shuffle the player list so we have absolutly no idea who is who.
        selectedPlayerNames = shuffleList(selectedPlayerNames.ToList()).ToArray();

        databaseCache.InitialiseRAMDatabase();
        clientCache = new databaseCacheClient(18, this.gameName.Text, decimal.Parse(this.littleBlind.Text), decimal.Parse(this.bigBlind.Text), decimal.Parse(this.startingStack.Text), 10, HandDataSource.PlayingTest);
        CacheMonitor.CacheMonitor cacheMonitor = new PokerBot.CacheMonitor.CacheMonitor(clientCache, !showAllCards.Checked);

        pokerGame = new BotVsHumanPokerGame(PokerGameType.BotVsHuman, clientCache, minNumTablePlayers, selectedPlayerNames, Decimal.Parse(startingStack.Text), 0, Int16.Parse(actionPause.Text), cacheMonitor);
        pokerGame.startGameTask();

        cacheMonitor.Show();
        playPoker.Text = "End Game";
      }
      else
      {
        playPoker.Text = "Ending Game";
        playPoker.Enabled = false;

        pokerGame.ShutdownAIOnFinish();
        pokerGame.EndGame = true;

        playPoker.Text = "Play Poker";
        playPoker.Enabled = true;
      }
    }

    /// <summary>
    /// Used to randomise the order of inputs should it be so desired!
    /// </summary>
    /// <param name="inputList"></param>
    /// <returns></returns>
    private List<string> shuffleList(List<string> inputList)
    {
      List<string> randomList = new List<string>();
      if (inputList.Count == 0)
        return randomList;

      Random r = new Random();
      int randomIndex = 0;
      while (inputList.Count > 0)
      {
        randomIndex = r.Next(0, inputList.Count); //Choose a random object in the list
        randomList.Add(inputList[randomIndex]); //add it to the new, random list<
        inputList.RemoveAt(randomIndex); //remove to avoid duplicates
      }

      //clean up
      inputList.Clear();
      inputList = null;
      r = null;

      return randomList; //return the new random list
    }
  }

}
