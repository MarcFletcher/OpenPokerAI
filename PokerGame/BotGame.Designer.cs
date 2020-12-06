namespace PokerBot.BotGame
{
  partial class BotGame
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.gameModes = new System.Windows.Forms.TabControl();
      this.botVHuman = new System.Windows.Forms.TabPage();
      this.showAllCards = new System.Windows.Forms.CheckBox();
      this.label16 = new System.Windows.Forms.Label();
      this.actionPause = new System.Windows.Forms.TextBox();
      this.playPoker = new System.Windows.Forms.Button();
      this.obfuscateBots = new System.Windows.Forms.CheckBox();
      this.label17 = new System.Windows.Forms.Label();
      this.nerualTraining = new System.Windows.Forms.TabPage();
      this.allInAction = new System.Windows.Forms.Button();
      this.aiSuggestion = new System.Windows.Forms.TextBox();
      this.raiseToStealAmount = new System.Windows.Forms.TextBox();
      this.raiseToCallAmount = new System.Windows.Forms.TextBox();
      this.raiseToStealAction = new System.Windows.Forms.Button();
      this.raiseToCallAction = new System.Windows.Forms.Button();
      this.checkfoldAction = new System.Windows.Forms.Button();
      this.callAction = new System.Windows.Forms.Button();
      this.label33 = new System.Windows.Forms.Label();
      this.clientId = new System.Windows.Forms.TextBox();
      this.viewNerualTrainingTable = new System.Windows.Forms.Button();
      this.currentPlayerId = new System.Windows.Forms.TextBox();
      this.player9NoLog = new System.Windows.Forms.CheckBox();
      this.player8NoLog = new System.Windows.Forms.CheckBox();
      this.player7NoLog = new System.Windows.Forms.CheckBox();
      this.player6NoLog = new System.Windows.Forms.CheckBox();
      this.player5NoLog = new System.Windows.Forms.CheckBox();
      this.player4NoLog = new System.Windows.Forms.CheckBox();
      this.player3NoLog = new System.Windows.Forms.CheckBox();
      this.player2NoLog = new System.Windows.Forms.CheckBox();
      this.player1NoLog = new System.Windows.Forms.CheckBox();
      this.startNeuralTraining = new System.Windows.Forms.Button();
      this.panel1 = new System.Windows.Forms.Panel();
      this.aiData = new System.Windows.Forms.TextBox();
      this.label14 = new System.Windows.Forms.Label();
      this.startingStack = new System.Windows.Forms.TextBox();
      this.label12 = new System.Windows.Forms.Label();
      this.bigBlind = new System.Windows.Forms.TextBox();
      this.label13 = new System.Windows.Forms.Label();
      this.littleBlind = new System.Windows.Forms.TextBox();
      this.label11 = new System.Windows.Forms.Label();
      this.gameName = new System.Windows.Forms.TextBox();
      this.label1 = new System.Windows.Forms.Label();
      this.aiSelectionControl1 = new PokerBot.BotGame.AISelectionControl();
      this.gameModes.SuspendLayout();
      this.botVHuman.SuspendLayout();
      this.nerualTraining.SuspendLayout();
      this.panel1.SuspendLayout();
      this.SuspendLayout();
      // 
      // gameModes
      // 
      this.gameModes.Controls.Add(this.botVHuman);
      this.gameModes.Controls.Add(this.nerualTraining);
      this.gameModes.Location = new System.Drawing.Point(2, 61);
      this.gameModes.Name = "gameModes";
      this.gameModes.SelectedIndex = 0;
      this.gameModes.Size = new System.Drawing.Size(447, 396);
      this.gameModes.TabIndex = 0;
      // 
      // botVHuman
      // 
      this.botVHuman.Controls.Add(this.showAllCards);
      this.botVHuman.Controls.Add(this.aiSelectionControl1);
      this.botVHuman.Controls.Add(this.label16);
      this.botVHuman.Controls.Add(this.actionPause);
      this.botVHuman.Controls.Add(this.playPoker);
      this.botVHuman.Controls.Add(this.obfuscateBots);
      this.botVHuman.Controls.Add(this.label17);
      this.botVHuman.Location = new System.Drawing.Point(4, 22);
      this.botVHuman.Name = "botVHuman";
      this.botVHuman.Padding = new System.Windows.Forms.Padding(3);
      this.botVHuman.Size = new System.Drawing.Size(439, 370);
      this.botVHuman.TabIndex = 3;
      this.botVHuman.Text = "Bots vs. Human";
      this.botVHuman.UseVisualStyleBackColor = true;
      // 
      // showAllCards
      // 
      this.showAllCards.AutoSize = true;
      this.showAllCards.Location = new System.Drawing.Point(330, 16);
      this.showAllCards.Name = "showAllCards";
      this.showAllCards.Size = new System.Drawing.Size(97, 17);
      this.showAllCards.TabIndex = 53;
      this.showAllCards.Text = "Show All Cards";
      this.showAllCards.UseVisualStyleBackColor = true;
      // 
      // label16
      // 
      this.label16.AutoSize = true;
      this.label16.Location = new System.Drawing.Point(17, 40);
      this.label16.Name = "label16";
      this.label16.Size = new System.Drawing.Size(98, 13);
      this.label16.TabIndex = 44;
      this.label16.Text = "Action Pause (ms): ";
      // 
      // actionPause
      // 
      this.actionPause.Location = new System.Drawing.Point(118, 37);
      this.actionPause.Name = "actionPause";
      this.actionPause.Size = new System.Drawing.Size(59, 20);
      this.actionPause.TabIndex = 43;
      this.actionPause.Text = "1000";
      // 
      // playPoker
      // 
      this.playPoker.Location = new System.Drawing.Point(162, 300);
      this.playPoker.Name = "playPoker";
      this.playPoker.Size = new System.Drawing.Size(114, 45);
      this.playPoker.TabIndex = 17;
      this.playPoker.Text = "Play Poker";
      this.playPoker.UseVisualStyleBackColor = true;
      this.playPoker.Click += new System.EventHandler(this.playPoker_Click);
      // 
      // obfuscateBots
      // 
      this.obfuscateBots.AutoSize = true;
      this.obfuscateBots.Location = new System.Drawing.Point(330, 39);
      this.obfuscateBots.Name = "obfuscateBots";
      this.obfuscateBots.Size = new System.Drawing.Size(99, 17);
      this.obfuscateBots.TabIndex = 4;
      this.obfuscateBots.Text = "Obfuscate Bots";
      this.obfuscateBots.UseVisualStyleBackColor = true;
      // 
      // label17
      // 
      this.label17.AutoSize = true;
      this.label17.Location = new System.Drawing.Point(6, 7);
      this.label17.Name = "label17";
      this.label17.Size = new System.Drawing.Size(266, 13);
      this.label17.TabIndex = 3;
      this.label17.Text = "Can you beat the bots? Poker Client 18 | Max 9 Players";
      // 
      // nerualTraining
      // 
      this.nerualTraining.Controls.Add(this.label1);
      this.nerualTraining.Controls.Add(this.allInAction);
      this.nerualTraining.Controls.Add(this.aiSuggestion);
      this.nerualTraining.Controls.Add(this.raiseToStealAmount);
      this.nerualTraining.Controls.Add(this.raiseToCallAmount);
      this.nerualTraining.Controls.Add(this.raiseToStealAction);
      this.nerualTraining.Controls.Add(this.raiseToCallAction);
      this.nerualTraining.Controls.Add(this.checkfoldAction);
      this.nerualTraining.Controls.Add(this.callAction);
      this.nerualTraining.Controls.Add(this.label33);
      this.nerualTraining.Controls.Add(this.clientId);
      this.nerualTraining.Controls.Add(this.viewNerualTrainingTable);
      this.nerualTraining.Controls.Add(this.currentPlayerId);
      this.nerualTraining.Controls.Add(this.player9NoLog);
      this.nerualTraining.Controls.Add(this.player8NoLog);
      this.nerualTraining.Controls.Add(this.player7NoLog);
      this.nerualTraining.Controls.Add(this.player6NoLog);
      this.nerualTraining.Controls.Add(this.player5NoLog);
      this.nerualTraining.Controls.Add(this.player4NoLog);
      this.nerualTraining.Controls.Add(this.player3NoLog);
      this.nerualTraining.Controls.Add(this.player2NoLog);
      this.nerualTraining.Controls.Add(this.player1NoLog);
      this.nerualTraining.Controls.Add(this.startNeuralTraining);
      this.nerualTraining.Controls.Add(this.panel1);
      this.nerualTraining.Location = new System.Drawing.Point(4, 22);
      this.nerualTraining.Name = "nerualTraining";
      this.nerualTraining.Padding = new System.Windows.Forms.Padding(3);
      this.nerualTraining.Size = new System.Drawing.Size(439, 370);
      this.nerualTraining.TabIndex = 4;
      this.nerualTraining.Text = "Neural Training";
      this.nerualTraining.UseVisualStyleBackColor = true;
      // 
      // allInAction
      // 
      this.allInAction.Enabled = false;
      this.allInAction.Location = new System.Drawing.Point(351, 342);
      this.allInAction.Name = "allInAction";
      this.allInAction.Size = new System.Drawing.Size(43, 23);
      this.allInAction.TabIndex = 58;
      this.allInAction.Text = "All-In";
      this.allInAction.UseVisualStyleBackColor = true;
      this.allInAction.Click += new System.EventHandler(this.allInAction_Click);
      // 
      // aiSuggestion
      // 
      this.aiSuggestion.Enabled = false;
      this.aiSuggestion.Location = new System.Drawing.Point(11, 315);
      this.aiSuggestion.Name = "aiSuggestion";
      this.aiSuggestion.Size = new System.Drawing.Size(190, 20);
      this.aiSuggestion.TabIndex = 55;
      // 
      // raiseToStealAmount
      // 
      this.raiseToStealAmount.Location = new System.Drawing.Point(279, 316);
      this.raiseToStealAmount.Name = "raiseToStealAmount";
      this.raiseToStealAmount.Size = new System.Drawing.Size(62, 20);
      this.raiseToStealAmount.TabIndex = 5;
      // 
      // raiseToCallAmount
      // 
      this.raiseToCallAmount.Location = new System.Drawing.Point(207, 315);
      this.raiseToCallAmount.Name = "raiseToCallAmount";
      this.raiseToCallAmount.Size = new System.Drawing.Size(65, 20);
      this.raiseToCallAmount.TabIndex = 4;
      // 
      // raiseToStealAction
      // 
      this.raiseToStealAction.Enabled = false;
      this.raiseToStealAction.Location = new System.Drawing.Point(262, 342);
      this.raiseToStealAction.Name = "raiseToStealAction";
      this.raiseToStealAction.Size = new System.Drawing.Size(86, 23);
      this.raiseToStealAction.TabIndex = 3;
      this.raiseToStealAction.Text = "Raise To Steal";
      this.raiseToStealAction.UseVisualStyleBackColor = true;
      this.raiseToStealAction.Click += new System.EventHandler(this.raiseToStealAction_Click);
      // 
      // raiseToCallAction
      // 
      this.raiseToCallAction.Enabled = false;
      this.raiseToCallAction.Location = new System.Drawing.Point(175, 342);
      this.raiseToCallAction.Name = "raiseToCallAction";
      this.raiseToCallAction.Size = new System.Drawing.Size(86, 23);
      this.raiseToCallAction.TabIndex = 2;
      this.raiseToCallAction.Text = "Raise To Call";
      this.raiseToCallAction.UseVisualStyleBackColor = true;
      this.raiseToCallAction.Click += new System.EventHandler(this.raiseToCallAction_Click);
      // 
      // checkfoldAction
      // 
      this.checkfoldAction.Enabled = false;
      this.checkfoldAction.Location = new System.Drawing.Point(9, 342);
      this.checkfoldAction.Name = "checkfoldAction";
      this.checkfoldAction.Size = new System.Drawing.Size(79, 23);
      this.checkfoldAction.TabIndex = 1;
      this.checkfoldAction.Text = "Check / Fold";
      this.checkfoldAction.UseVisualStyleBackColor = true;
      this.checkfoldAction.Click += new System.EventHandler(this.checkfoldAction_Click);
      // 
      // callAction
      // 
      this.callAction.Enabled = false;
      this.callAction.Location = new System.Drawing.Point(94, 342);
      this.callAction.Name = "callAction";
      this.callAction.Size = new System.Drawing.Size(75, 23);
      this.callAction.TabIndex = 0;
      this.callAction.Text = "Call";
      this.callAction.UseVisualStyleBackColor = true;
      this.callAction.Click += new System.EventHandler(this.callAction_Click);
      // 
      // label33
      // 
      this.label33.AutoSize = true;
      this.label33.BackColor = System.Drawing.Color.Transparent;
      this.label33.Location = new System.Drawing.Point(25, 40);
      this.label33.Name = "label33";
      this.label33.Size = new System.Drawing.Size(48, 13);
      this.label33.TabIndex = 60;
      this.label33.Text = "Client Id:";
      // 
      // clientId
      // 
      this.clientId.Location = new System.Drawing.Point(73, 37);
      this.clientId.Name = "clientId";
      this.clientId.Size = new System.Drawing.Size(53, 20);
      this.clientId.TabIndex = 59;
      this.clientId.Text = "1";
      // 
      // viewNerualTrainingTable
      // 
      this.viewNerualTrainingTable.Enabled = false;
      this.viewNerualTrainingTable.Location = new System.Drawing.Point(132, 35);
      this.viewNerualTrainingTable.Name = "viewNerualTrainingTable";
      this.viewNerualTrainingTable.Size = new System.Drawing.Size(75, 23);
      this.viewNerualTrainingTable.TabIndex = 31;
      this.viewNerualTrainingTable.Text = "View Table";
      this.viewNerualTrainingTable.UseVisualStyleBackColor = true;
      this.viewNerualTrainingTable.Click += new System.EventHandler(this.viewNerualTrainingTable_Click);
      // 
      // currentPlayerId
      // 
      this.currentPlayerId.Location = new System.Drawing.Point(294, 37);
      this.currentPlayerId.Name = "currentPlayerId";
      this.currentPlayerId.Size = new System.Drawing.Size(100, 20);
      this.currentPlayerId.TabIndex = 57;
      this.currentPlayerId.Visible = false;
      this.currentPlayerId.TextChanged += new System.EventHandler(this.currentPlayerId_TextChanged);
      // 
      // player9NoLog
      // 
      this.player9NoLog.AutoSize = true;
      this.player9NoLog.Checked = true;
      this.player9NoLog.CheckState = System.Windows.Forms.CheckState.Checked;
      this.player9NoLog.Location = new System.Drawing.Point(252, 104);
      this.player9NoLog.Name = "player9NoLog";
      this.player9NoLog.Size = new System.Drawing.Size(116, 17);
      this.player9NoLog.TabIndex = 47;
      this.player9NoLog.Text = "Position 8 (No Log)";
      this.player9NoLog.UseVisualStyleBackColor = true;
      // 
      // player8NoLog
      // 
      this.player8NoLog.AutoSize = true;
      this.player8NoLog.Checked = true;
      this.player8NoLog.CheckState = System.Windows.Forms.CheckState.Checked;
      this.player8NoLog.Location = new System.Drawing.Point(252, 86);
      this.player8NoLog.Name = "player8NoLog";
      this.player8NoLog.Size = new System.Drawing.Size(116, 17);
      this.player8NoLog.TabIndex = 46;
      this.player8NoLog.Text = "Position 7 (No Log)";
      this.player8NoLog.UseVisualStyleBackColor = true;
      // 
      // player7NoLog
      // 
      this.player7NoLog.AutoSize = true;
      this.player7NoLog.Checked = true;
      this.player7NoLog.CheckState = System.Windows.Forms.CheckState.Checked;
      this.player7NoLog.Location = new System.Drawing.Point(252, 68);
      this.player7NoLog.Name = "player7NoLog";
      this.player7NoLog.Size = new System.Drawing.Size(116, 17);
      this.player7NoLog.TabIndex = 45;
      this.player7NoLog.Text = "Position 6 (No Log)";
      this.player7NoLog.UseVisualStyleBackColor = true;
      // 
      // player6NoLog
      // 
      this.player6NoLog.AutoSize = true;
      this.player6NoLog.Checked = true;
      this.player6NoLog.CheckState = System.Windows.Forms.CheckState.Checked;
      this.player6NoLog.Location = new System.Drawing.Point(138, 104);
      this.player6NoLog.Name = "player6NoLog";
      this.player6NoLog.Size = new System.Drawing.Size(116, 17);
      this.player6NoLog.TabIndex = 44;
      this.player6NoLog.Text = "Position 5 (No Log)";
      this.player6NoLog.UseVisualStyleBackColor = true;
      // 
      // player5NoLog
      // 
      this.player5NoLog.AutoSize = true;
      this.player5NoLog.Checked = true;
      this.player5NoLog.CheckState = System.Windows.Forms.CheckState.Checked;
      this.player5NoLog.Location = new System.Drawing.Point(138, 86);
      this.player5NoLog.Name = "player5NoLog";
      this.player5NoLog.Size = new System.Drawing.Size(116, 17);
      this.player5NoLog.TabIndex = 43;
      this.player5NoLog.Text = "Position 4 (No Log)";
      this.player5NoLog.UseVisualStyleBackColor = true;
      // 
      // player4NoLog
      // 
      this.player4NoLog.AutoSize = true;
      this.player4NoLog.Checked = true;
      this.player4NoLog.CheckState = System.Windows.Forms.CheckState.Checked;
      this.player4NoLog.Location = new System.Drawing.Point(138, 68);
      this.player4NoLog.Name = "player4NoLog";
      this.player4NoLog.Size = new System.Drawing.Size(116, 17);
      this.player4NoLog.TabIndex = 42;
      this.player4NoLog.Text = "Position 3 (No Log)";
      this.player4NoLog.UseVisualStyleBackColor = true;
      // 
      // player3NoLog
      // 
      this.player3NoLog.AutoSize = true;
      this.player3NoLog.Checked = true;
      this.player3NoLog.CheckState = System.Windows.Forms.CheckState.Checked;
      this.player3NoLog.Location = new System.Drawing.Point(27, 104);
      this.player3NoLog.Name = "player3NoLog";
      this.player3NoLog.Size = new System.Drawing.Size(116, 17);
      this.player3NoLog.TabIndex = 41;
      this.player3NoLog.Text = "Position 2 (No Log)";
      this.player3NoLog.UseVisualStyleBackColor = true;
      // 
      // player2NoLog
      // 
      this.player2NoLog.AutoSize = true;
      this.player2NoLog.Checked = true;
      this.player2NoLog.CheckState = System.Windows.Forms.CheckState.Checked;
      this.player2NoLog.Location = new System.Drawing.Point(27, 86);
      this.player2NoLog.Name = "player2NoLog";
      this.player2NoLog.Size = new System.Drawing.Size(116, 17);
      this.player2NoLog.TabIndex = 40;
      this.player2NoLog.Text = "Position 1 (No Log)";
      this.player2NoLog.UseVisualStyleBackColor = true;
      // 
      // player1NoLog
      // 
      this.player1NoLog.AutoSize = true;
      this.player1NoLog.Checked = true;
      this.player1NoLog.CheckState = System.Windows.Forms.CheckState.Checked;
      this.player1NoLog.Location = new System.Drawing.Point(27, 68);
      this.player1NoLog.Name = "player1NoLog";
      this.player1NoLog.Size = new System.Drawing.Size(116, 17);
      this.player1NoLog.TabIndex = 39;
      this.player1NoLog.Text = "Position 0 (No Log)";
      this.player1NoLog.UseVisualStyleBackColor = true;
      // 
      // startNeuralTraining
      // 
      this.startNeuralTraining.Location = new System.Drawing.Point(213, 35);
      this.startNeuralTraining.Name = "startNeuralTraining";
      this.startNeuralTraining.Size = new System.Drawing.Size(75, 23);
      this.startNeuralTraining.TabIndex = 38;
      this.startNeuralTraining.Text = "Start Game";
      this.startNeuralTraining.UseVisualStyleBackColor = true;
      this.startNeuralTraining.Click += new System.EventHandler(this.startNeuralTraining_Click);
      // 
      // panel1
      // 
      this.panel1.BackColor = System.Drawing.Color.DarkGray;
      this.panel1.Controls.Add(this.aiData);
      this.panel1.Location = new System.Drawing.Point(6, 128);
      this.panel1.Name = "panel1";
      this.panel1.Size = new System.Drawing.Size(425, 182);
      this.panel1.TabIndex = 56;
      // 
      // aiData
      // 
      this.aiData.Location = new System.Drawing.Point(3, 3);
      this.aiData.Multiline = true;
      this.aiData.Name = "aiData";
      this.aiData.ReadOnly = true;
      this.aiData.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
      this.aiData.Size = new System.Drawing.Size(419, 175);
      this.aiData.TabIndex = 34;
      // 
      // label14
      // 
      this.label14.AutoSize = true;
      this.label14.Location = new System.Drawing.Point(250, 11);
      this.label14.Name = "label14";
      this.label14.Size = new System.Drawing.Size(80, 13);
      this.label14.TabIndex = 28;
      this.label14.Text = "Starting Stack: ";
      // 
      // startingStack
      // 
      this.startingStack.Location = new System.Drawing.Point(336, 8);
      this.startingStack.Name = "startingStack";
      this.startingStack.Size = new System.Drawing.Size(64, 20);
      this.startingStack.TabIndex = 27;
      this.startingStack.Text = "25";
      // 
      // label12
      // 
      this.label12.AutoSize = true;
      this.label12.Location = new System.Drawing.Point(111, 35);
      this.label12.Name = "label12";
      this.label12.Size = new System.Drawing.Size(51, 13);
      this.label12.TabIndex = 26;
      this.label12.Text = "Big Blind:";
      // 
      // bigBlind
      // 
      this.bigBlind.Location = new System.Drawing.Point(168, 32);
      this.bigBlind.Name = "bigBlind";
      this.bigBlind.Size = new System.Drawing.Size(33, 20);
      this.bigBlind.TabIndex = 25;
      this.bigBlind.Text = "0.25";
      // 
      // label13
      // 
      this.label13.AutoSize = true;
      this.label13.Location = new System.Drawing.Point(7, 35);
      this.label13.Name = "label13";
      this.label13.Size = new System.Drawing.Size(58, 13);
      this.label13.TabIndex = 24;
      this.label13.Text = "Little Blind:";
      // 
      // littleBlind
      // 
      this.littleBlind.Location = new System.Drawing.Point(65, 32);
      this.littleBlind.Name = "littleBlind";
      this.littleBlind.Size = new System.Drawing.Size(29, 20);
      this.littleBlind.TabIndex = 23;
      this.littleBlind.Text = "0.1";
      // 
      // label11
      // 
      this.label11.AutoSize = true;
      this.label11.Location = new System.Drawing.Point(7, 11);
      this.label11.Name = "label11";
      this.label11.Size = new System.Drawing.Size(72, 13);
      this.label11.TabIndex = 22;
      this.label11.Text = "Game Name: ";
      // 
      // gameName
      // 
      this.gameName.Location = new System.Drawing.Point(76, 8);
      this.gameName.Name = "gameName";
      this.gameName.Size = new System.Drawing.Size(125, 20);
      this.gameName.TabIndex = 21;
      this.gameName.Text = "Bot Game";
      // 
      // label1
      // 
      this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.label1.Location = new System.Drawing.Point(3, 3);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(428, 31);
      this.label1.TabIndex = 61;
      this.label1.Text = "Training environment for recording training inputs and ideal actions. Not fully f" +
    "unctional right now.";
      // 
      // aiSelectionControl1
      // 
      this.aiSelectionControl1.AutoScroll = true;
      this.aiSelectionControl1.Location = new System.Drawing.Point(9, 62);
      this.aiSelectionControl1.Name = "aiSelectionControl1";
      this.aiSelectionControl1.Size = new System.Drawing.Size(422, 218);
      this.aiSelectionControl1.TabIndex = 52;
      // 
      // BotGame
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(449, 458);
      this.Controls.Add(this.gameModes);
      this.Controls.Add(this.label14);
      this.Controls.Add(this.startingStack);
      this.Controls.Add(this.label12);
      this.Controls.Add(this.gameName);
      this.Controls.Add(this.bigBlind);
      this.Controls.Add(this.label11);
      this.Controls.Add(this.label13);
      this.Controls.Add(this.littleBlind);
      this.Name = "BotGame";
      this.Text = "FullBotPoker Bot Games";
      this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Main_FormClosing);
      this.gameModes.ResumeLayout(false);
      this.botVHuman.ResumeLayout(false);
      this.botVHuman.PerformLayout();
      this.nerualTraining.ResumeLayout(false);
      this.nerualTraining.PerformLayout();
      this.panel1.ResumeLayout(false);
      this.panel1.PerformLayout();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.TabControl gameModes;
    private System.Windows.Forms.TabPage botVHuman;
    private System.Windows.Forms.Label label11;
    private System.Windows.Forms.TextBox gameName;
    private System.Windows.Forms.Label label12;
    private System.Windows.Forms.TextBox bigBlind;
    private System.Windows.Forms.Label label13;
    private System.Windows.Forms.TextBox littleBlind;
    private System.Windows.Forms.Label label14;
    private System.Windows.Forms.TextBox startingStack;
    private System.Windows.Forms.Label label17;
    private System.Windows.Forms.TabPage nerualTraining;
    private System.Windows.Forms.Button raiseToStealAction;
    private System.Windows.Forms.Button raiseToCallAction;
    private System.Windows.Forms.Button checkfoldAction;
    private System.Windows.Forms.Button callAction;
    private System.Windows.Forms.TextBox raiseToStealAmount;
    private System.Windows.Forms.TextBox raiseToCallAmount;
    private System.Windows.Forms.TextBox aiData;
    private System.Windows.Forms.Button startNeuralTraining;
    private System.Windows.Forms.CheckBox player9NoLog;
    private System.Windows.Forms.CheckBox player8NoLog;
    private System.Windows.Forms.CheckBox player7NoLog;
    private System.Windows.Forms.CheckBox player6NoLog;
    private System.Windows.Forms.CheckBox player5NoLog;
    private System.Windows.Forms.CheckBox player4NoLog;
    private System.Windows.Forms.CheckBox player3NoLog;
    private System.Windows.Forms.CheckBox player2NoLog;
    private System.Windows.Forms.CheckBox player1NoLog;
    private System.Windows.Forms.TextBox aiSuggestion;
    private System.Windows.Forms.TextBox currentPlayerId;
    private System.Windows.Forms.Button viewNerualTrainingTable;
    private System.Windows.Forms.Button allInAction;
    private System.Windows.Forms.TextBox clientId;
    private System.Windows.Forms.Label label33;
    private System.Windows.Forms.CheckBox obfuscateBots;
    private System.Windows.Forms.Button playPoker;
    private System.Windows.Forms.Label label16;
    private System.Windows.Forms.TextBox actionPause;
    private AISelectionControl aiSelectionControl1;
    private System.Windows.Forms.CheckBox showAllCards;
    private System.Windows.Forms.Panel panel1;
    private System.Windows.Forms.Label label1;
  }
}