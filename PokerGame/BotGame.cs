using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PokerBot.Database;
using PokerBot.Definitions;

namespace PokerBot.BotGame
{
  public partial class BotGame : Form
  {
    private static readonly string RESOURCES_PATH = "..\\..\\..\\Resources";

    databaseCacheClient clientCache;
    PokerGameBase pokerGame;

    List<Control> neuralTrainingOutputFields = new List<Control>();
    List<string> neuralPlayerNames = new List<string>();
    List<Control> neuralPlayerActionLog = new List<Control>();

    public BotGame()
    {
      // Change these two paths if you put the large data files somewhere else
      string handRanksAbsoluteDir = "..\\..\\..\\Resources\\HandRanksFile";
      string wpLookupTablesAbsoluteDir = "..\\..\\..\\Resources\\WPLookupTables";

      Environment.SetEnvironmentVariable("HandRanksFile", Path.Combine(handRanksAbsoluteDir, "HandRanks.dat"));

      Environment.SetEnvironmentVariable("preflopWPFile", Path.Combine(wpLookupTablesAbsoluteDir, "preflopWP.dat"));
      Environment.SetEnvironmentVariable("preflopRanksFile", Path.Combine(wpLookupTablesAbsoluteDir, "preflopRanks.dat"));
      Environment.SetEnvironmentVariable("flopWPFile", Path.Combine(wpLookupTablesAbsoluteDir, "flopWP.dat"));
      Environment.SetEnvironmentVariable("turnWPFile", Path.Combine(wpLookupTablesAbsoluteDir, "turnWP.dat"));
      Environment.SetEnvironmentVariable("riverWPFile", Path.Combine(wpLookupTablesAbsoluteDir, "riverWP.dat"));
      Environment.SetEnvironmentVariable("flopIndexesFile", Path.Combine(wpLookupTablesAbsoluteDir, "Indexes\\flopIndexes.dat"));
      Environment.SetEnvironmentVariable("turnIndexesFile", Path.Combine(wpLookupTablesAbsoluteDir, "Indexes\\turnIndexes.dat"));
      Environment.SetEnvironmentVariable("riverIndexesFile", Path.Combine(wpLookupTablesAbsoluteDir, "Indexes\\riverIndexes.dat"));
      Environment.SetEnvironmentVariable("flopLocationsFile", Path.Combine(wpLookupTablesAbsoluteDir, "Locations\\flopLocations.dat"));
      Environment.SetEnvironmentVariable("turnLocationsFile", Path.Combine(wpLookupTablesAbsoluteDir, "Locations\\turnLocations.dat"));
      Environment.SetEnvironmentVariable("riverLocationsFile", Path.Combine(wpLookupTablesAbsoluteDir, "Locations\\riverLocations.dat"));

      Environment.SetEnvironmentVariable("PlayerNetworkStoreDir", GetFullPathToResources("PlayerNetworkStore"));

      Environment.SetEnvironmentVariable("HoleCardUsageDir", GetFullPathToResources("HoleCardUsageDat"));
      Environment.SetEnvironmentVariable("PlayerActionPredictionDir", GetFullPathToResources("PlayerActionPrediction"));
      Environment.SetEnvironmentVariable("WeightedWinRatioDir", GetFullPathToResources("WeightedWinRatioDat"));

      databaseQueries.SetDatabaseLocalMode(GetFullPathToResources("ManualPlayersTable.csv"));

      InitializeComponent();
    }

    /// <summary>
    /// Get the absolute path to a file/folder that is in the resources folder.
    /// </summary>
    /// <param name="relativeResource">The relative path from within the resource directory.</param>
    /// <returns>The absolute path to the resource.</returns>
    private static string GetFullPathToResources(string relativeResource) =>
      Path.Combine(Directory.GetCurrentDirectory(), RESOURCES_PATH, relativeResource);

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
      databaseCache.InitialiseRAMDatabase();

      neuralTrainingOutputFields.AddRange(new List<Control> {aiSuggestion, aiData,
        raiseToCallAmount, raiseToStealAmount, currentPlayerId });

      neuralPlayerActionLog.AddRange(new List<Control> { player1NoLog, player2NoLog, player3NoLog, player4NoLog,
                                player5NoLog,player6NoLog,player7NoLog,player8NoLog,player9NoLog});


      //Create the AISelection[]
      List<AISelection> aiSelection = new List<AISelection>();
      for (int i = 0; i < 9; i++)
      {
        aiSelection.Add(new AISelection(AIGeneration.NeuralV4, "FixedNeuralPlayers\\neuralV4_Marc.eNN"));
      }

      long[] newPlayerIds = PokerHelper.CreateOpponentPlayers(aiSelection.ToArray(), false, 1);
      string[] selectedPlayerNames = new string[newPlayerIds.Length];
      for (int i = 0; i < newPlayerIds.Length; i++)
      {
        selectedPlayerNames[i] = databaseQueries.convertToPlayerNameFromId(newPlayerIds[i]);
      }

      neuralPlayerNames.AddRange(selectedPlayerNames);
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
        PokerClients client = PokerClients.HumanVsBots;
        int actionPauseTime = int.Parse(this.actionPause.Text);
        byte minNumTablePlayers = 2;

        AISelection[] selectedPlayers = this.aiSelectionControl1.AISelection();

        //Select the playerId's for all of the bot players
        if (selectedPlayers.Length > 10)
          throw new Exception("A maximum of 10 players is allowed.");

        databaseCache.InitialiseRAMDatabase();

        long[] newPlayerIds = PokerHelper.CreateOpponentPlayers(selectedPlayers, obfuscateBots.Checked, (short)client);
        string[] selectedPlayerNames = new string[newPlayerIds.Length];
        for (int i = 0; i < newPlayerIds.Length; i++)
          selectedPlayerNames[i] = databaseQueries.convertToPlayerNameFromId(newPlayerIds[i]);

        //Shuffle the player list so we have absolutly no idea who is who.
        selectedPlayerNames = shuffleList(selectedPlayerNames.ToList()).ToArray();
        clientCache = new databaseCacheClient((short)client, this.gameName.Text, decimal.Parse(this.littleBlind.Text), decimal.Parse(this.bigBlind.Text), decimal.Parse(this.startingStack.Text), 10, HandDataSource.PlayingTest);
        CacheMonitor.CacheMonitor cacheMonitor = new PokerBot.CacheMonitor.CacheMonitor(clientCache, !showAllCards.Checked);

        pokerGame = new BotVsHumanPokerGame(PokerGameType.BotVsHuman, clientCache, minNumTablePlayers, selectedPlayerNames, Decimal.Parse(startingStack.Text), 0, Int16.Parse(actionPause.Text), cacheMonitor);
        pokerGame.startGameTask();
        pokerGame.ShutdownAIOnFinish();

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
