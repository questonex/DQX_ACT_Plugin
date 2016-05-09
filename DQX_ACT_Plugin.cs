#region License
// ========================================================================
// DQX_ACT_Plugin.cs
// Advanced Combat Tracker Plugin for DQX
// https://github.com/questonex/DQX_ACT_Plugin
// 
// The MIT License(MIT)
//
// Copyright (c) 2016 Ravahn
// Copyright (c) 2016 questonex
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// ========================================================================
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Text.RegularExpressions;

using Advanced_Combat_Tracker;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Net;

namespace DQX_ACT_Plugin
{
#region ACT Plugin Code
  public class DQX_ACT_Plugin : UserControl, Advanced_Combat_Tracker.IActPluginV1
  {
#region Designer Created Code (Avoid editing)
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

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.label1 = new System.Windows.Forms.Label();
      lstMessages = new System.Windows.Forms.ListBox();
      this.cmdClearMessages = new System.Windows.Forms.Button();
      this.cmdCopyProblematic = new System.Windows.Forms.Button();
      this.SuspendLayout();
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(11, 12);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(88, 13);
      this.label1.TabIndex = 82;
      this.label1.Text = "Parser Messages";
      // 
      // lstMessages
      // 
      lstMessages.FormattingEnabled = true;
      lstMessages.Location = new System.Drawing.Point(14, 41);
      lstMessages.Name = "lstMessages";
      lstMessages.ScrollAlwaysVisible = true;
      lstMessages.Size = new System.Drawing.Size(700, 264);
      lstMessages.TabIndex = 81;
      // 
      // cmdClearMessages
      // 
      this.cmdClearMessages.Location = new System.Drawing.Point(88, 311);
      this.cmdClearMessages.Name = "cmdClearMessages";
      this.cmdClearMessages.Size = new System.Drawing.Size(106, 26);
      this.cmdClearMessages.TabIndex = 84;
      this.cmdClearMessages.Text = "Clear";
      this.cmdClearMessages.UseVisualStyleBackColor = true;
      this.cmdClearMessages.Click += new System.EventHandler(this.cmdClearMessages_Click);
      // 
      // cmdCopyProblematic
      // 
      this.cmdCopyProblematic.Location = new System.Drawing.Point(478, 311);
      this.cmdCopyProblematic.Name = "cmdCopyProblematic";
      this.cmdCopyProblematic.Size = new System.Drawing.Size(118, 26);
      this.cmdCopyProblematic.TabIndex = 85;
      this.cmdCopyProblematic.Text = "Copy to Clipboard";
      this.cmdCopyProblematic.UseVisualStyleBackColor = true;
      this.cmdCopyProblematic.Click += new System.EventHandler(this.cmdCopyProblematic_Click);
      // 
      // UserControl1
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.Controls.Add(this.cmdCopyProblematic);
      this.Controls.Add(this.cmdClearMessages);
      this.Controls.Add(this.label1);
      this.Controls.Add(lstMessages);
      this.Name = "UserControl1";
      this.Size = new System.Drawing.Size(728, 356);
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    private System.Windows.Forms.Label label1;
    private static System.Windows.Forms.ListBox lstMessages;
    private System.Windows.Forms.Button cmdClearMessages;
    private System.Windows.Forms.Button cmdCopyProblematic;

#endregion

    public DQX_ACT_Plugin()
    {
      InitializeComponent();
    }
    // reference to the ACT plugin status label
    private Label lblStatus = null;

    public void InitPlugin(System.Windows.Forms.TabPage pluginScreenSpace, System.Windows.Forms.Label pluginStatusText)
    {
      // store a reference to plugin's status label
      lblStatus = pluginStatusText;

      try
      {
        // Configure ACT for updates, and check for update.
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.UpdateCheckClicked += new Advanced_Combat_Tracker.FormActMain.NullDelegate(UpdateCheckClicked);
        if (Advanced_Combat_Tracker.ActGlobals.oFormActMain.GetAutomaticUpdatesAllowed())
        {
          Thread updateThread = new Thread(new ThreadStart(UpdateCheckClicked));
          updateThread.IsBackground = true;
          updateThread.Start();
        }

        // Update the listing of columns inside ACT.
        UpdateACTTables();

        pluginScreenSpace.Controls.Add(this);   // Add this UserControl to the tab ACT provides
        this.Dock = DockStyle.Fill; // Expand the UserControl to fill the tab's client space

        // character name cannot be parsed from logfile name
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.LogPathHasCharName = false;
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.LogFileFilter = "*.log";

        // Default Timestamp length, but this can be overridden in parser code.
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.TimeStampLen = DateTime.Now.ToString("HH:mm:ss.fff").Length + 1;

        // Set Date time format parsing. 
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.GetDateTimeFromLog = new Advanced_Combat_Tracker.FormActMain.DateTimeLogParser(LogParse.ParseLogDateTime);

        // Set primary parser delegate for processing data
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.BeforeLogLineRead += LogParse.BeforeLogLineRead;

        Advanced_Combat_Tracker.ActGlobals.oFormActMain.OnCombatEnd += new CombatToggleEventDelegate(OnCombatEnd);
        
        // TODO: set up Zone Name

        lblStatus.Text = "DQX Plugin Started.";
      }
      catch (Exception ex)
      {
        LogParserMessage("Exception during InitPlugin: " + ex.ToString().Replace(Environment.NewLine, " "));
        lblStatus.Text = "InitPlugin Error.";
      }
    }

    public void DeInitPlugin()
    {
      // remove event handler
      Advanced_Combat_Tracker.ActGlobals.oFormActMain.UpdateCheckClicked -= this.UpdateCheckClicked;
      Advanced_Combat_Tracker.ActGlobals.oFormActMain.BeforeLogLineRead -= LogParse.BeforeLogLineRead;
      Advanced_Combat_Tracker.ActGlobals.oFormActMain.OnCombatEnd -= OnCombatEnd;
      
      if (lblStatus != null)
      {
        lblStatus.Text = "DQX Plugin Unloaded.";
        lblStatus = null;
      }
    }


    public void UpdateCheckClicked()
    {
    }

    private void UpdateACTTables()
    {

    }

    public static void LogParserMessage(string message)
    {
      lstMessages.Invoke(new Action(() => lstMessages.Items.Add(message)));
    }

    private void cmdClearMessages_Click(object sender, EventArgs e)
    {
      lstMessages.Items.Clear();
    }

    private void cmdCopyProblematic_Click(object sender, EventArgs e)
    {
      StringBuilder sb = new StringBuilder();
      foreach (object itm in lstMessages.Items)
        sb.AppendLine((itm ?? "").ToString());

      if (sb.Length > 0)
        System.Windows.Forms.Clipboard.SetText(sb.ToString());
    }

    void OnCombatEnd(bool isImport, CombatToggleEventArgs encounterInfo)
    {
      encounterInfo.encounter.Title = LogParse.encounter;
    }

  }
#endregion

#region Parser Code
  public static class LogParse
  {
    public static DateTime ParseLogDateTime(string message)
    {
      DateTime ret = DateTime.MinValue;

      try
      {
        if (message == null || message.IndexOf("\t") != 19)
          return ret;

        if (!DateTime.TryParse(message.Substring(0, message.IndexOf("\t")), out ret))
          return DateTime.MinValue;
      }
      catch (Exception ex)
      {
        DQX_ACT_Plugin.LogParserMessage("Error [ParseLogDateTime] " + ex.ToString().Replace(Environment.NewLine, " "));
      }
      return ret;
    }

    public static Regex regex_open = new Regex(@"(?<target>[^ ]+?)と 戦闘開始！$");
    public static Regex regex_close = new Regex(@" (やっつけた！|いなくなった！|ぜんめつした。|戦いをやめた。)$");
    public static Regex regex_action = new Regex(@"^(?<actor>.+?)(の|は) (?<action>.+?)！$");
    public static Regex regex_hit = new Regex(@"^ → (?<target>.+?)(に|は) (?<damage>\d+)のダメージ(！|を うけた！)$");
    public static Regex regex_miss = new Regex(@"^ → ミス！ (?<target>.+?)(に|は) ダメージを (あたえられない|うけない)！$");
    public static Regex regex_crit = new Regex(@"^ → (かいしんの|つうこんの|じゅもんが) .+?！$");

    public static Regex regex_heal = new Regex(@"^ → (?<target>.+?)の ＨＰが (?<damage>\d+)回復した！$");
    public static Regex regex_dead = new Regex(@"^ → (?<target>.+?)は しんでしまった。$");

    public static string encounter;
    private static string actor;
    private static string action;

    private static bool isCritical = false;

    public static void BeforeLogLineRead(bool isImport, Advanced_Combat_Tracker.LogLineEventArgs logInfo)
    {
      string l = logInfo.logLine;
      
      try
      {
        DateTime timestamp = ParseLogDateTime(l);

        char[] dt = {'\t'};
        string[] logParts = l.Split(dt);
        int flag = Convert.ToInt32(logParts[2], 16);
        if (flag > 7) {
          return;
        }

        l = logParts[7];
        
        Match m;

        // open
        m = regex_open.Match(l);
        if (m.Success) {
          string target = m.Groups["target"].Success ? DecodeString(m.Groups["target"].Value) : "";
          encounter = target;
          Advanced_Combat_Tracker.ActGlobals.oFormActMain.SetEncounter(timestamp, encounter, encounter);
          // DQX_ACT_Plugin.LogParserMessage("Open: "+target);
          return;
        }

        // close
        m = regex_close.Match(l);
        if (m.Success) {
          //          Advanced_Combat_Tracker.ActGlobals.oFormActMain.ChangeZone(target);
          Advanced_Combat_Tracker.ActGlobals.oFormActMain.EndCombat(true);
          // DQX_ACT_Plugin.LogParserMessage("Close: ");
          return;
        }
        
        // action
        if (!l.StartsWith(" →")) {
          m = regex_action.Match(l);
          if (!m.Success) return;
          actor = m.Groups["actor"].Success ? DecodeString(m.Groups["actor"].Value) : "";
          action = m.Groups["action"].Success ? DecodeString(m.Groups["action"].Value) : "";
          return;
        }

        if (!Advanced_Combat_Tracker.ActGlobals.oFormActMain.InCombat) {
          return;
        }

        // crit
        m = regex_crit.Match(l);
        if (m.Success) {
          isCritical = true;
          return;
        }

        // damage
        m = regex_hit.Match(l);
        if (m.Success) {
          string target = m.Groups["target"].Success ? DecodeString(m.Groups["target"].Value) : "";

          // if (Advanced_Combat_Tracker.ActGlobals.oFormActMain.SetEncounter(timestamp, actor, encounter))
          {
            Advanced_Combat_Tracker.ActGlobals.oFormActMain.AddCombatAction(
              (int)Advanced_Combat_Tracker.SwingTypeEnum.NonMelee,
              isCritical,
              "",
              actor,
              action,
              new Advanced_Combat_Tracker.Dnum(int.Parse(m.Groups["damage"].Value, System.Globalization.NumberStyles.AllowThousands)),
              timestamp,
              Advanced_Combat_Tracker.ActGlobals.oFormActMain.GlobalTimeSorter,
              target,
              "");
          }
          isCritical = false;
          return;
        }

        // miss
        m = regex_miss.Match(l);
        if (m.Success) {
          string target = m.Groups["target"].Success ? DecodeString(m.Groups["target"].Value) : "";

          //          if (Advanced_Combat_Tracker.ActGlobals.oFormActMain.SetEncounter(timestamp, actor, encounter))
          {
            Advanced_Combat_Tracker.ActGlobals.oFormActMain.AddCombatAction(
              (int)Advanced_Combat_Tracker.SwingTypeEnum.NonMelee,
              isCritical,
              "",
              actor,
              action,
              Advanced_Combat_Tracker.Dnum.Miss,
              timestamp,
              Advanced_Combat_Tracker.ActGlobals.oFormActMain.GlobalTimeSorter,
              target,
              "");
          }
          isCritical = false;
          return;
        }

        // heal
        m = regex_heal.Match(l);
        if (m.Success) {
          string target = m.Groups["target"].Success ? DecodeString(m.Groups["target"].Value) : "";

          //          if (Advanced_Combat_Tracker.ActGlobals.oFormActMain.SetEncounter(timestamp, actor, encounter))
          {
            Advanced_Combat_Tracker.ActGlobals.oFormActMain.AddCombatAction(
              (int)Advanced_Combat_Tracker.SwingTypeEnum.Healing,
              isCritical,
              "",
              actor,
              action,
              new Advanced_Combat_Tracker.Dnum(int.Parse(m.Groups["damage"].Value, System.Globalization.NumberStyles.AllowThousands)),
              timestamp,
              Advanced_Combat_Tracker.ActGlobals.oFormActMain.GlobalTimeSorter,
              target,
              "");
          }
          isCritical = false;
          return;
        }

        // death
        m = regex_dead.Match(l);
        if (m.Success) {
          string target = m.Groups["target"].Success ? DecodeString(m.Groups["target"].Value) : "";

          //          if (Advanced_Combat_Tracker.ActGlobals.oFormActMain.SetEncounter(timestamp, actor, encounter))
          {
            Advanced_Combat_Tracker.ActGlobals.oFormActMain.AddCombatAction(
              (int)Advanced_Combat_Tracker.SwingTypeEnum.Healing,
              isCritical,
              "",
              actor,
              "Death",
              Advanced_Combat_Tracker.Dnum.Death,
              timestamp,
              Advanced_Combat_Tracker.ActGlobals.oFormActMain.GlobalTimeSorter,
              target,
              "");
          }
          isCritical = false;
          return;
        }

      }
      catch (Exception ex)
      {
        string exception = ex.ToString().Replace(Environment.NewLine, " ");
        if (ex.InnerException != null)
          exception += " " + ex.InnerException.ToString().Replace(Environment.NewLine, " ");

        DQX_ACT_Plugin.LogParserMessage("Error [LogParse.BeforeLogLineRead] " + exception + " " + logInfo.logLine);
      }

      // For debugging
      // if (!string.IsNullOrWhiteSpace(l))
      //   DQX_ACT_Plugin.LogParserMessage("Unhandled Line: " + logInfo.logLine);
    }

    private static string DecodeString(string data)
    {
      string ret = data.Replace("&apos;", "'")
        .Replace("&amp;", "&");

      return ret;
    }
  }

#endregion
}
