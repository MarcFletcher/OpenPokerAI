using System;
using System.Drawing;
using System.Windows.Forms;
using PokerBot.Definitions;
using PokerBot.Database;
using System.IO;

namespace PokerBot.CacheMonitor
{
  public partial class CacheMonitor : Form
  {
    public CacheMonitor(databaseCache genericCache, bool testMode, bool showActivePlayerCardsOnly)
    {
      InitializeComponent();

      this.Icon = global::PokerBot.CacheMonitor.Properties.Resources.chip;

      this.showActivePlayerCardsOnly = showActivePlayerCardsOnly;
      this.testMode = testMode;

      if (testMode)
        timer.Interval = 2000;

      InitialiseMonitor(genericCache);
    }

    public CacheMonitor(databaseCache genericCache)
    {
      InitializeComponent();
      this.testMode = false;
      InitialiseMonitor(genericCache);
    }

    public CacheMonitor(databaseCache genericCache, bool showNonBotPlayerCardsOnly)
    {
      InitializeComponent();
      this.testMode = false;
      this.showNonBotPlayerCardsOnly = showNonBotPlayerCardsOnly;
      InitialiseMonitor(genericCache);
    }

    private void timer_Tick(object sender, EventArgs e)
    {
      //TimeSpan span = DateTime.Now.Subtract(genericCache.StartTime);
      //this.timeElapsed.Text = span.Hours.ToString() + " Hr, " + span.Minutes.ToString() + " Min, " + span.Seconds.ToString() + " Sec";
      updateTableUI();

      if (testMode)
      {
        //For each playerControl
        for (int i = 0; i < 10; i++)
        {
          for (int y = 0; y < 7; y++)
          {
            if (playerControls[i, y].Text == "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx")
              playerControls[i, y].Text = "yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy";
            else
              playerControls[i, y].Text = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
          }
        }

        //Dealer position
        moveDealerButton(dealerPosition);
        dealerPosition++;
        if (dealerPosition == 10)
          dealerPosition = 0;
      }
    }

    private void mainMenuButton_MouseEnter(object sender, EventArgs e)
    {
      this.mainMenuButton.BackgroundImage = global::PokerBot.CacheMonitor.Properties.Resources.chip2WBG;
    }

    private void mainMenuButton_MouseLeave(object sender, EventArgs e)
    {
      this.mainMenuButton.BackgroundImage = global::PokerBot.CacheMonitor.Properties.Resources.chipWBG;
    }

    private void mainMenuButton_Click(object sender, EventArgs e)
    {
      try
      {
        if (openCacheFileDialog.ShowDialog() == DialogResult.OK)
        {
          handHistory.Clear();
          this.genericCache = databaseCache.DeSerialise(File.ReadAllBytes(openCacheFileDialog.FileName));
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.ToString());
      }
    }

    private void mainMenuButton_MouseDown(object sender, MouseEventArgs e)
    {
      this.mainMenuButton.BackgroundImage = global::PokerBot.CacheMonitor.Properties.Resources.chip2WBG_Click;
    }

    private void mainMenuButton_MouseUp(object sender, MouseEventArgs e)
    {
      this.mainMenuButton.BackgroundImage = global::PokerBot.CacheMonitor.Properties.Resources.chip2WBG;
    }

    private void closeButton_Click(object sender, EventArgs e)
    {
      this.Close();
    }

    private void pokerTable_MouseDown(object sender, MouseEventArgs e)
    {
      pokerTable.MouseMove += new MouseEventHandler(pokerTable_MouseMove);
      initialButtonOffset = e.Location;
    }

    private void pokerTable_MouseUp(object sender, MouseEventArgs e)
    {
      pokerTable.MouseMove -= new MouseEventHandler(pokerTable_MouseMove);
    }

    private void pokerTable_MouseMove(object sender, MouseEventArgs e)
    {
      this.Location = new Point(this.Location.X + e.Location.X - initialButtonOffset.X, this.Location.Y + e.Location.Y - initialButtonOffset.Y);
    }

    private void minimiseButton_Click(object sender, EventArgs e)
    {
      this.WindowState = FormWindowState.Minimized;
    }

    private void checkFoldButton_Click(object sender, EventArgs e)
    {
      showHideManualButtons(false);

      long currentActivePlayerId = genericCache.getPlayerId(genericCache.getCurrentActiveTablePosition());
      if (genericCache.getMinimumPlayAmount() - genericCache.getPlayerCurrentRoundBetAmount(currentActivePlayerId) == 0)
        manualDecision = new Play(PokerAction.Check, 0, 0, genericCache.getCurrentHandId(), currentActivePlayerId);
      else
        manualDecision = new Play(PokerAction.Fold, 0, 0, genericCache.getCurrentHandId(), currentActivePlayerId);

      manualDecisionMade = true;
    }

    private void callButton_Click(object sender, EventArgs e)
    {
      showHideManualButtons(false);

      long currentActivePlayerId = genericCache.getPlayerId(genericCache.getCurrentActiveTablePosition());
      decimal callAmount = genericCache.getMinimumPlayAmount() - genericCache.getPlayerCurrentRoundBetAmount(currentActivePlayerId);

      manualDecision = new Play(PokerAction.Call, callAmount, 0, genericCache.getCurrentHandId(), currentActivePlayerId);

      manualDecisionMade = true;
    }

    private void raiseButton_Click(object sender, EventArgs e)
    {
      showHideManualButtons(false);

      long currentActivePlayerId = genericCache.getPlayerId(genericCache.getCurrentActiveTablePosition());
      manualDecision = new Play(PokerAction.Raise, decimal.Parse(this.raiseAmount.Text), 0, genericCache.getCurrentHandId(), currentActivePlayerId);

      manualDecisionMade = true;
    }
  }

}
