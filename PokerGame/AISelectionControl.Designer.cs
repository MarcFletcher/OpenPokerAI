using System.Windows.Forms;
using PokerBot.AI;
using System;
using System.Drawing;
using System.Collections.Generic;
using PokerBot.Database;
using PokerBot.Definitions;

namespace PokerBot.BotGame
{
  public class AISelection
  {
    AIGeneration aiGeneration;
    string configStr;

    public AISelection(AIGeneration aiGeneration, string configStr)
    {
      this.aiGeneration = aiGeneration;
      this.configStr = configStr;
    }

    public AIGeneration AiGeneration
    {
      get { return aiGeneration; }
    }

    public string ConfigStr
    {
      get { return configStr; }
      set { configStr = value; }
    }
  }

  partial class AISelectionControl : UserControl
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

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.SuspendLayout();
      // 
      // AISelectionControl
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.Name = "AISelectionControl";
      this.Size = new System.Drawing.Size(166, 200);

      this.AutoScroll = true;

      SetupColumnHeadings();
      SetupRows();

      this.ResumeLayout(false);
      this.PerformLayout();
    }

    //Setup the column headings
    private void SetupColumnHeadings()
    {
      Label header1 = new Label();
      header1.AutoSize = false;
      header1.Location = new System.Drawing.Point(0, 0);
      header1.Name = "AIGenerations";
      header1.Size = new System.Drawing.Size(150, 30);
      header1.TabIndex = 0;
      header1.Text = "Available AI Gens";
      header1.Font = new System.Drawing.Font(header1.Font, FontStyle.Bold);
      header1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      this.Controls.Add(header1);

      Label header2 = new Label();
      header2.AutoSize = false;
      header2.Location = new System.Drawing.Point(70, 0);
      header2.Name = "Num";
      header2.Size = new System.Drawing.Size(120, 30);
      header2.TabIndex = 0;
      header2.Text = "Num";
      header2.Font = new System.Drawing.Font(header1.Font, FontStyle.Bold);
      header2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      this.Controls.Add(header2);

      Label header3 = new Label();
      header3.AutoSize = false;
      header3.Location = new System.Drawing.Point(200, 0);
      header3.Name = "ConfigStr";
      header3.Size = new System.Drawing.Size(145, 30);
      header3.TabIndex = 0;
      header3.Text = "ConfigStr";
      header3.Font = new System.Drawing.Font(header1.Font, FontStyle.Bold);
      header3.TextAlign = ContentAlignment.MiddleLeft;
      this.Controls.Add(header3);
    }

    private void SetupRows()
    {
      for (int i = 0; i < typeof(AIGeneration).GetFields().Length - 2; i++)
      {
        #region rowFields
        //Create the label
        Label labelOne = new Label();
        labelOne.AutoSize = false;
        labelOne.Location = new System.Drawing.Point(0, i * 25 + 30);
        labelOne.Name = "lbl_" + Enum.GetName(typeof(AIGeneration), i);
        labelOne.Size = new System.Drawing.Size(150, 30);
        labelOne.TabIndex = 0;
        labelOne.Text = Enum.GetName(typeof(AIGeneration), i);
        labelOne.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        this.Controls.Add(labelOne);

        TextBox textBox = new TextBox();
        textBox.Size = new Size(20, 30);
        textBox.Name = Enum.GetName(typeof(AIGeneration), i) + "_quantity";
        textBox.Location = new Point(165, i * 25 + 35);
        textBox.Text = "0";
        this.Controls.Add(textBox);

        ComboBox comboBox = new ComboBox();
        comboBox.Size = new Size(200, 30);
        comboBox.Name = Enum.GetName(typeof(AIGeneration), i) + "_config";
        comboBox.Location = new Point(200, i * 25 + 35);
        comboBox.DataSource = databaseQueries.AiDefaultConfigs(i);

        this.Controls.Add(comboBox);
        #endregion
      }
    }

    /// <summary>
    /// Returns the correctly formatted selection made within this control
    /// </summary>
    public AISelection[] AISelection()
    {
      List<AISelection> selectedAIs = new List<AISelection>();

      //Need to go through each control and pick out the number of ai's and config strings
      for (int i = 0; i < typeof(AIGeneration).GetFields().Length - 2; i++)
        for (int j = 0; j < int.Parse(this.Controls.Find(Enum.GetName(typeof(AIGeneration), i) + "_quantity", false)[0].Text); j++)
          selectedAIs.Add(new AISelection((AIGeneration)i, ((ComboBox)this.Controls.Find(Enum.GetName(typeof(AIGeneration), i) + "_config", false)[0]).SelectedItem.ToString()));

      return selectedAIs.ToArray();
    }

    #endregion
  }
}
