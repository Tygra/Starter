﻿#region Refs
using System;
using System.Data;
using System.IO;
using System.IO.Streams;
using System.ComponentModel;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

//Terraria related refs
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using TShockAPI.DB;
using TShockAPI.Localization;
using Newtonsoft.Json;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
#endregion


namespace Starter
{
    [ApiVersion(2, 1)]
    public class StarterCommand : TerrariaPlugin
    {
        #region Info
        public DateTime LastCheck = DateTime.UtcNow;
        public string SavePath = TShock.SavePath;
        internal IDbConnection database;
        public StarterPlayer[] Playerlist = new StarterPlayer[256];
        public Region Region { get; set; }
        public override string Name { get { return "Starter commands"; } }
        public override string Author { get { return "Tygra"; } }
        public override string Description { get { return "Starter command"; } }
        public override Version Version { get { return new Version(0, 1); } }

        public StarterCommand(Main game)
            : base(game)
        {
            Order = 1;
        }
        #endregion

        #region Initialize
        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("geldar.admin", Reloadcfg, "starterreload"));
            Commands.ChatCommands.Add(new Command("geldar.admin", Starter, "starter"));
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            if (!Config.ReadConfig())
            {
                TShock.Log.ConsoleError("Config loading failed. Consider deleting it.");
            }
        }
        #endregion

        #region Dispose
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
            }
            base.Dispose(disposing);
        }
        #endregion

        #region OnInitialize
        private void OnInitialize(EventArgs args)
        {
            switch (TShock.Config.StorageType.ToLower())
            {
                case "mysql":
                    string[] host = TShock.Config.MySqlHost.Split(':');
                    database = new MySqlConnection()
                    {
                        ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4}",
                        host[0],
                        host.Length == 1 ? "3306" : host[1],
                        TShock.Config.MySqlDbName,
                        TShock.Config.MySqlUsername,
                        TShock.Config.MySqlPassword)
                    };
                    break;

                case "sqlite":
                    string sql = Path.Combine(TShock.SavePath, "starter.sqlite");
                    database = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
                    break;
            }

            SqlTableCreator sqlcreator = new SqlTableCreator(database, database.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            sqlcreator.EnsureTableStructure(new SqlTable("misc",
                new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("User", MySqlDbType.Text) { Length = 30 },
                new SqlColumn("CommandID", MySqlDbType.Text),
                new SqlColumn("Date", MySqlDbType.Int32),
                new SqlColumn("Expiration", MySqlDbType.Int32)
                ));
        }
        #endregion

        #region Playerlist Join/Leave
        public void OnJoin(JoinEventArgs args)
        {
            Playerlist[args.Who] = new StarterPlayer(args.Who);
        }

        public void OnLeave(LeaveEventArgs args)
        {
            Playerlist[args.Who] = null;
        }
        #endregion

        #region TimeStamp
        public static int UnixTimestamp()
        {
            int unixtime = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            return unixtime;
        }
        #endregion

        #region TimeSpan
        public static TimeSpan ParseTimeSpan(string s)
        {
            const string Quantity = "quantity";
            const string Unit = "unit";
            const string Days = @"(d(ays?)?)";
            const string Hours = @"(h((ours?)|(rs?))?)";
            const string Minutes = @"(m((inutes?)|(ins?))?)";
            const string Seconds = @"(s((econds?)|(ecs?))?)";

            Regex timeSpanRegex = new Regex(string.Format(@"\s*(?<{0}>\d+)\s*(?<{1}>({2}|{3}|{4}|{5}|\Z))", Quantity, Unit, Days, Hours, Minutes, Seconds), RegexOptions.IgnoreCase);
            MatchCollection matches = timeSpanRegex.Matches(s);
            int l;
            TimeSpan ts = new TimeSpan();
            if (!Int32.TryParse(s.Substring(0, 1), out l))
            {
                return ts;
            }
            foreach (Match match in matches)
            {
                if (Regex.IsMatch(match.Groups[Unit].Value, @"\A" + Days))
                {
                    ts = ts.Add(TimeSpan.FromDays(double.Parse(match.Groups[Quantity].Value)));
                }
                else if (Regex.IsMatch(match.Groups[Unit].Value, Hours))
                {
                    ts = ts.Add(TimeSpan.FromHours(double.Parse(match.Groups[Quantity].Value)));
                }
                else if (Regex.IsMatch(match.Groups[Unit].Value, Minutes))
                {
                    ts = ts.Add(TimeSpan.FromMinutes(double.Parse(match.Groups[Quantity].Value)));
                }
                else if (Regex.IsMatch(match.Groups[Unit].Value, Seconds))
                {
                    ts = ts.Add(TimeSpan.FromSeconds(double.Parse(match.Groups[Quantity].Value)));
                }
                else
                {
                    ts = ts.Add(TimeSpan.FromMinutes(double.Parse(match.Groups[Quantity].Value)));
                }
            }
            return ts;
        }
        #endregion

        #region Starter
        private void Starter(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendMessage("Info: If you have lost your starter weapon, here you can replace it.", Color.Goldenrod);
                args.Player.SendMessage("Info: Use the command appropriate to your class /starter mage, warrior or ranger", Color.Goldenrod);
                return;
            }
            if (args.Parameters.Count > 1)
            {
                args.Player.SendErrorMessage("Invalid syntax. Use /starter mage, warrior or ranger");
                return;
            }
            switch (args.Parameters[0])
            {
                #region Mage
                case "mage":
                    {

                    }
                    break;
                #endregion

                #region Warrior
                case "warrior":
                    {

                    }
                    break;
                #endregion

                #region Ranger
                case "ranger":
                    {

                    }
                    break;
                #endregion

                #region Default fallback
                default:
                    {
                        args.Player.SendErrorMessage("Wrong subcommand.");
                    }
                    break;
                    #endregion
            }
        }
        #endregion

        #region Config reload
        public void Reloadcfg(CommandArgs args)
        {
            if (Config.ReadConfig())
            {
                args.Player.SendMessage("Starter config reloaded", Color.Goldenrod);
            }
            else
            {
                args.Player.SendErrorMessage("Something went wrong.");
            }
        }
        #endregion
    }
}