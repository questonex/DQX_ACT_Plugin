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
  public class DQX_ACT_Plugin: UserControl, Advanced_Combat_Tracker.IActPluginV1
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

        Advanced_Combat_Tracker.ActGlobals.oFormActMain.ZoneChangeRegex = new Regex(@"test", RegexOptions.Compiled);

        Advanced_Combat_Tracker.ActGlobals.oFormActMain.OnCombatEnd += new CombatToggleEventDelegate(OnCombatEnd);

        //

        if (!CombatantData.ColumnDefs.ContainsKey("Class"))
        {
          CombatantData.ColumnDefs.Add("Class", new CombatantData.ColumnDef("Class", false, "CHAR(64)", "Class", (Data) => { return (string)Data.Tags["Class"]; }, (Data) => { return (string)Data.Tags["Class"]; }, (Left, Right) => { return 0; }));
          CombatantData.ExportVariables.Add("Class", new CombatantData.TextExportFormatter("class", "Class", "class", (Data, Extra) => { return (string)Data.Tags["Class"]; }));
          ActGlobals.oFormActMain.ValidateTableSetup();
          ActGlobals.oFormActMain.ValidateLists();
        }

#if DEBUGG
        Debug.Initialize();
#endif

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

#if DEBUGG
      Debug.Uninitialize();
#endif

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

      if (LogParse.Allies != null && LogParse.Allies.Count > 0)
      {
        List<CombatantData> localAllies = new List<CombatantData>(LogParse.Allies.Count);
        foreach (var name in LogParse.Allies)
        {
          var combatant = encounterInfo.encounter.GetCombatant(name);
          if (combatant != null)
          {
            localAllies.Add(encounterInfo.encounter.GetCombatant(name));
          }
        }
        encounterInfo.encounter.SetAllies(localAllies);
      }

      foreach (var data in encounterInfo.encounter.Items.Values)
      {
        if (LogParse.NameClass.ContainsKey(data.Name))
        {
          data.Tags["Class"] = LogParse.NameClass[data.Name];
        }
        else
        {
          data.Tags["Class"] = "";
        }
      }
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

    public static void BeforeLogLineRead(bool isImport, Advanced_Combat_Tracker.LogLineEventArgs logInfo)
    {
      string l = logInfo.logLine;

      try
      {
        DateTime timestamp = ParseLogDateTime(l);

        char[] dt = { '\t' };
        string[] logParts = l.Split(dt);
        int flag = Convert.ToInt32(logParts[2], 16);
        if (flag > 7)
        {
          return;
        }

        string id = logParts[3];
        l = logParts[7];

        Match m;

        // open
        m = regex_open.Match(l);
        if (m.Success)
        {
          string target = m.Groups["target"].Success ? DecodeString(m.Groups["target"].Value) : "";
          encounter = target;
          Advanced_Combat_Tracker.ActGlobals.oFormActMain.SetEncounter(timestamp, encounter, encounter);
          NameClass.Clear();
          Allies.Clear();
          return;
        }

        // close
        m = regex_close.Match(l);
        if (m.Success)
        {
          Advanced_Combat_Tracker.ActGlobals.oFormActMain.EndCombat(true);
          // Advanced_Combat_Tracker.ActGlobals.oFormActMain.ChangeZone(encounter);
          return;
        }

        // action
        if (!l.StartsWith(" →"))
        {
          m = regex_action.Match(l);
          if (m.Success)
          {
            actor = m.Groups["actor"].Success ? DecodeString(m.Groups["actor"].Value) : "";
            action = m.Groups["action"].Success ? DecodeString(m.Groups["action"].Value) : "";

            if (!NameClass.ContainsKey(actor))
            {
              if (SkillClass.ContainsKey(action))
              {
                NameClass.Add(actor, SkillClass[action]);
              }
            }

            if ((id.Length > 0) && (actor.Length > 0) && !Allies.Contains(actor))
            {
              m = regex_foe.Match(l);
              if (!m.Success)
              {
                // DQX_ACT_Plugin.LogParserMessage(" Add Allies: ." + actor+"."+action+"."+id+".");
                Allies.Add(actor);
              }
            }

            return;
          }
          m = regex_action2.Match(l);
          if (m.Success)
          {
            actor = "不明";
            action = m.Groups["action"].Success ? DecodeString(m.Groups["action"].Value) : "";
            return;
          }

          // death
          m = regex_dead2.Match(l);
          if (m.Success)
          {
            string target = m.Groups["target"].Success ? DecodeString(m.Groups["target"].Value) : "";
            Advanced_Combat_Tracker.ActGlobals.oFormActMain.AddCombatAction(
              (int)Advanced_Combat_Tracker.SwingTypeEnum.Healing,
              false,
              "",
              "不明",
              "Death",
              Advanced_Combat_Tracker.Dnum.Death,
              timestamp,
              Advanced_Combat_Tracker.ActGlobals.oFormActMain.GlobalTimeSorter,
              target,
              "");
            return;
          }
          return;
        }

        if (!Advanced_Combat_Tracker.ActGlobals.oFormActMain.InCombat)
        {
          return;
        }

        // crit
        m = regex_crit.Match(l);
        if (m.Success)
        {
          isCritical = true;
          return;
        }

        // damage
        m = regex_hit.Match(l);
        if (m.Success)
        {
          string target = m.Groups["target"].Success ? DecodeString(m.Groups["target"].Value) : "";

          // if (Advanced_Combat_Tracker.ActGlobals.oFormActMain.SetEncounter(timestamp, actor, encounter))
          {
            MasterSwing ms = new MasterSwing(
              (int)Advanced_Combat_Tracker.SwingTypeEnum.NonMelee,
              isCritical,
              "",
              new Advanced_Combat_Tracker.Dnum(int.Parse(m.Groups["damage"].Value, System.Globalization.NumberStyles.AllowThousands)),
              timestamp,
              Advanced_Combat_Tracker.ActGlobals.oFormActMain.GlobalTimeSorter,
              action,
              actor,
              "",
              target);

            Advanced_Combat_Tracker.ActGlobals.oFormActMain.AddCombatAction(ms);
          }

          isCritical = false;
          return;
        }

        // miss
        m = regex_miss.Match(l);
        if (m.Success)
        {
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
        if (m.Success)
        {
          string target = m.Groups["target"].Success ? DecodeString(m.Groups["target"].Value) : "";

          //          if (Advanced_Combat_Tracker.ActGlobals.oFormActMain.SetEncounter(timestamp, actor, encounter))
          {
            MasterSwing ms = new MasterSwing(
              (int)Advanced_Combat_Tracker.SwingTypeEnum.Healing,
              isCritical,
              "",
              new Advanced_Combat_Tracker.Dnum(int.Parse(m.Groups["damage"].Value, System.Globalization.NumberStyles.AllowThousands)),
              timestamp,
              Advanced_Combat_Tracker.ActGlobals.oFormActMain.GlobalTimeSorter,
              action,
              actor,
              "",
              target);

            Advanced_Combat_Tracker.ActGlobals.oFormActMain.AddCombatAction(ms);
          }

          isCritical = false;
          return;
        }

        // death
        m = regex_dead.Match(l);
        if (m.Success)
        {
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

    public static Dictionary<string, string> SkillClass = new Dictionary<string, string>()
    {
      { "かばう", "戦士" },
      { "たいあたり", "戦士" },
      { "やいばくだき", "戦士" },
      { "チャージタックル", "戦士" },
      { "真・やいばくだき", "戦士" },
      { "ロストブレイク", "戦士" },
      //
      { "魔力の息吹", "魔法使い" },
      { "魔力かくせい", "魔法使い" },
      { "マヒャデドス", "魔法使い" },
      { "メラガイアー", "魔法使い" },
      { "ぶきみな閃光", "魔法使い" },
      //
      { "マホトラのころも", "僧侶" },
      { "聖女の守り ", "僧侶" },
      { "天使の守り", "僧侶" },
      { "聖なる祈り", "僧侶" },
      { "ホーリーライト", "僧侶" },
      { "女神の祝福", "僧侶" },
      { "ベホマ", "僧侶" },
      //
      { "ためる", "武闘家" },
      { "不撓不屈", "武闘家" },
      { "めいそう", "武闘家" },
      { "ためる弐", "武闘家" },
      { "無念無想", "武闘家" },
      { "ためる参", "武闘家" },
      { "行雲流水", "武闘家" },
      //
      { "ぬすむ", "盗賊" },
      { "バナナトラップ ", "盗賊" },
      { "メガボンバー", "盗賊" },
      { "ギガボンバー", "盗賊" },
      { "サプライズラッシュ", "盗賊" },
      { "テンション強奪拳", "盗賊" },
      //
      { "タップダンス", "旅芸人" },
      { "キラージャグリング", "旅芸人" },
      { "ハッスルダンス", "旅芸人" },
      { "エンドオブシーン", "旅芸人" },
      { "ゴッドジャグリング", "旅芸人" },
      { "たたかいのビート", "旅芸人" },
      { "超ハッスルダンス", "旅芸人" },
      //
      { "におうだち", "パラディン" },
      { "ヘヴィチャージ", "パラディン" },
      { "大ぼうぎょ", "パラディン" },
      { "グランドネビュラ", "パラディン" },
      { "聖騎士の堅陣", "パラディン" },
      { "不動のかまえ", "パラディン" },
      //
      { "てなずける", "レンジャー" },
      { "メタルトラップ", "レンジャー" },
      { "まもりのきり", "レンジャー" },
      { "オオカミアタック", "レンジャー" },
      { "あんこくのきり", "レンジャー" },
      { "ジバルンバ", "レンジャー" },
      { "フェンリルアタック", "レンジャー" },
      { "ケルベロスロンド", "レンジャー" },
      //
      { "ファイアフォース", "魔法戦士" },
      { "アイスフォース", "魔法戦士" },
      { "ストームフォース", "魔法戦士" },
      { "ダークフォース", "魔法戦士" },
      { "ＭＰパサー", "魔法戦士" },
      { "ライトフォース", "魔法戦士" },
      { "フォースブレイク", "魔法戦士" },
      { "マダンテ", "魔法戦士" },
      { "クロックチャージ", "魔法戦士" },
      //
      { "サインぜめ", "スーパースター" },
      { "スキャンダル", "スーパースター" },
      { "メイクアップ", "スーパースター" },
      { "ボディーガード呼び", "スーパースター" },
      { "ベストスマイル", "スーパースター" },
      { "ゴールドシャワー", "スーパースター" },
      { "バギムーチョ", "スーパースター" },
      { "ミリオンスマイル", "スーパースター" },
      { "ラグジュアルリム", "スーパースター" },
      //
      { "とうこん討ち", "バトルマスター" },
      { "すてみ", "バトルマスター" },
      { "もろば斬り", "バトルマスター" },
      { "無心こうげき", "バトルマスター" },
      { "天下無双", "バトルマスター" },
      { "テンションバーン", "バトルマスター" },
      { "ミラクルブースト", "バトルマスター" },
      { "灼熱とうこん討ち", "バトルマスター" },
      //
      { "いやしの雨", "賢者" },
      { "魔導の書", "賢者" },
      { "しんぴのさとり", "賢者" },
      { "零の洗礼", "賢者" },
      { "イオグランデ", "賢者" },
      { "むげんのさとり", "賢者" },
      { "ドルマドン", "賢者" },
      { "きせきの雨", "賢者" },
      //
      { "かわいがる", "まもの使い" },
      { "ブレスクラッシュ", "まもの使い" },
      { "ＨＰリンク", "まもの使い" },
      { "ＭＰリンク", "まもの使い" },
      { "エモノ呼び", "まもの使い" },
      { "スキルクラッシュ", "まもの使い" },
      { "ウォークライ", "まもの使い" },
      { "エモノ呼びの咆哮", "まもの使い" },
      //
      { "チューンナップ", "どうぐ使い" },
      { "トラップジャマー", "どうぐ使い" },
      { "磁界シールド", "どうぐ使い" },
      { "メディカルデバイス", "どうぐ使い" },
      { "プラズマリムーバー", "どうぐ使い" },
      { "どうぐ最適術", "どうぐ使い" },
      //
      { "もうどくブルース", "踊り子" },
      { "会心まいしんラップ", "踊り子" },
      { "祈りのゴスペル", "踊り子" },
      { "覚醒のアリア", "踊り子" },
      { "よみがえり節", "踊り子" },
      { "魔力のバラード", "踊り子" },
      { "ふういんのダンス", "踊り子" },
      { "こんらんのダンス", "踊り子" },
      { "ねむりのダンス", "踊り子" },
      { "ドラゴンステップ", "踊り子" },
      { "ビーナスステップ", "踊り子" },
      { "ロイヤルステップ", "踊り子" },
      { "つるぎの舞", "踊り子" },
      { "戦鬼の乱れ舞", "踊り子" },
      { "神速シャンソン", "踊り子" },
      { "ギラグレイド", "踊り子" },
      //
      { "ライトフリング", "占い師" },
      { "レフトフリング", "占い師" },
      { "リセットベール", "占い師" },
      { "エンゼルのみちびき", "占い師" },
      { "魅惑の水晶球", "占い師" },
      { "ゾディアックコード", "占い師" },
      { "魔王のいざない", "占い師" },
      // { "", "" },
      { "ダミー", "ダミー" }
    };

    public static Dictionary<string, string> NameClass = new Dictionary<string, string>()
    {
    };

    public static List<string> Allies = new List<string>();

    public static Regex regex_open = new Regex(@"(?<target>[^ ]+?)と 戦闘開始！$");
    public static Regex regex_close = new Regex(@" (やっつけた！|いなくなった！|ぜんめつした。|戦いをやめた。|勝利した！)$");
    public static Regex regex_action = new Regex(@"^(?<actor>.+?)(の|は|が) (?<action>.+?)(を まいおどった|をうたった|)！$");
    public static Regex regex_action2 = new Regex(@"^(?<action>[^ ]+?)！$");
    public static Regex regex_hit = new Regex(@"^ → (?<target>.+?)(に|は) (?<damage>\d+)のダメージ(！|を うけた！)$");
    public static Regex regex_miss = new Regex(@"^ → ミス！ (?<target>.+?)(に|は) ダメージを (あたえられない|うけない)！$");
    public static Regex regex_crit = new Regex(@"^ → (かいしんの|つうこんの|じゅもんが) .+?！$");

    public static Regex regex_heal = new Regex(@"^ → (?<target>.+?)の ＨＰが (?<damage>\d+)回復した！$");
    public static Regex regex_dead = new Regex(@"^ → (?<target>.+?)は しんでしまった。$");
    public static Regex regex_dead2 = new Regex(@"^(?<target>.+?)は しんでしまった。$");

    public static Regex regex_foe = new Regex(@"(怒った|激怒した|みとれている)");

    public static string encounter;
    private static string actor;
    private static string action;

    private static bool isCritical = false;
  }

  #endregion

}
