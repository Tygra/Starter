#region Refs
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
                new SqlColumn("Username", MySqlDbType.Text) { Length = 30 },
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
                        if (args.Player.Group.HasPermission("geldar.starter.mage"))
                        {
                            if (!args.Player.Group.HasPermission("geldar.bypass.cd"))
                            {
                                var player = Playerlist[args.Player.Index];
                                TimeSpan time = ParseTimeSpan(Config.contents.startercooldown);
                                int currentdate = UnixTimestamp();
                                int expiration = currentdate + (int)time.TotalSeconds;
                                List<string> startermage = new List<string>();
                                using (var reader = database.QueryReader("SELECT * FROM misc WHERE Username=@0 AND CommandID=@1;", args.Player.Name, Config.contents.startercommandID))
                                {
                                    while (reader.Read())
                                    {
                                        startermage.Add(reader.Get<string>("Username"));
                                    }
                                }
                                if (startermage.Count < 1)
                                {
                                    if (args.Player.InventorySlotAvailable)
                                    {
                                        database.Query("INSERT INTO misc(Username, CommandID, Date, Expiration) VALUES(@0, @1, @2, @3);", args.Player.Name, Config.contents.startercommandID, currentdate, expiration);
                                        Item itemById = TShock.Utils.GetItemById(Config.contents.startermage);
                                        args.Player.GiveItem(itemById.type, itemById.Name, itemById.width, itemById.height, 1, 0);
                                        args.Player.SendSuccessMessage("{0} was put into your inventory.", Config.contents.startermage);
                                    }
                                    else
                                    {
                                        args.Player.SendErrorMessage("Your inventory seems to be full. Free up one slot, and try again.");
                                        return;
                                    }
                                }
                                else
                                {
                                    if (args.Player.InventorySlotAvailable)
                                    {
                                        database.Query("UPDATE misc SET Date=@0 AND Expiration=@1 WHERE Username=@2 AND CommandID=@3;", currentdate, expiration, args.Player.Name, Config.contents.startercommandID);
                                        Item itemById = TShock.Utils.GetItemById(Config.contents.startermage);
                                        args.Player.GiveItem(itemById.type, itemById.Name, itemById.width, itemById.height, 1, 0);
                                        args.Player.SendSuccessMessage("{0} was put into your inventory.", Config.contents.startermage);
                                    }
                                    else
                                    {
                                        args.Player.SendErrorMessage("Your inventory seems to be full. Free up one slot, and try again.");
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                Item itemById = TShock.Utils.GetItemById(Config.contents.startermage);
                                args.Player.GiveItem(itemById.type, itemById.Name, itemById.width, itemById.height, 1, 0);
                                args.Player.SendSuccessMessage("{0} was put into your inventory.", itemById.Name);
                            }
                        }
                    }
                    break;
                #endregion

                #region Warrior
                case "warrior":
                    {
                        if (args.Player.Group.HasPermission("geldar.starter.warrior"))
                        {
                            if (!args.Player.Group.HasPermission("geldar.bypass.cd"))
                            {
                                var player = Playerlist[args.Player.Index];
                                TimeSpan time = ParseTimeSpan(Config.contents.startercooldown);
                                int currentdate = UnixTimestamp();
                                int expiration = currentdate + (int)time.TotalSeconds;
                                List<string> starterwarrior = new List<string>();
                                using (var reader = database.QueryReader("SELECT * FROM misc WHERE Username=@0 AND CommandID=@1;", args.Player.Name, Config.contents.startercommandID))
                                {
                                    while (reader.Read())
                                    {
                                        starterwarrior.Add(reader.Get<string>("Username"));
                                    }
                                }
                                if (starterwarrior.Count < 1)
                                {
                                    //insert mert nincs még az adatbázisban
                                }
                                else
                                {
                                    //update date és expiration mert már létezik a user
                                }
                            }
                        }
                        else
                        {
                            //ha van permission
                        }
                    }
                    break;
                #endregion

                #region Ranger
                case "ranger":
                    {
                        if (args.Player.Group.HasPermission("geldar.starter.ranger"))
                        {
                            if (!args.Player.Group.HasPermission("geldar.bypass.cd"))
                            {
                                var player = Playerlist[args.Player.Index];
                                TimeSpan time = ParseTimeSpan(Config.contents.startercooldown);
                                int currentdate = UnixTimestamp();
                                int expiration = currentdate + (int)time.TotalSeconds;
                                List<string> starterranger = new List<string>();
                                using (var reader = database.QueryReader("SELECT * FROM misc WHERE Username=@0 AND CommandID=@1;", args.Player.Name, Config.contents.startercommandID))
                                {
                                    while (reader.Read())
                                    {
                                        starterranger.Add(reader.Get<string>("Username"));
                                    }
                                }
                                if (starterranger.Count < 1)
                                {
                                    //insert mert nincs még az adatbázisban
                                }
                                else
                                {
                                    //update date és expiration mert már létezik a user
                                }
                            }
                        }
                        else
                        {
                            //ha van permission
                        }
                    }
                    break;
                #endregion

                #region Summoner
                case "summoner":
                    {
                        if (args.Player.Group.HasPermission("geldar.starter.summoenr"))
                        {
                            if (!args.Player.Group.HasPermission("geldar.bypass.cd"))
                            {
                                var player = Playerlist[args.Player.Index];
                                TimeSpan time = ParseTimeSpan(Config.contents.startercooldown);
                                int currentdate = UnixTimestamp();
                                int expiration = currentdate + (int)time.TotalSeconds;
                                List<string> startersummoner = new List<string>();
                                using (var reader = database.QueryReader("SELECT * FROM misc WHERE Username=@0 AND CommandID=@1;", args.Player.Name, Config.contents.startercommandID))
                                {
                                    while (reader.Read())
                                    {
                                        startersummoner.Add(reader.Get<string>("Username"));
                                    }
                                }
                                if (startersummoner.Count < 1)
                                {
                                    //insert mert nincs még az adatbázisban
                                }
                                else
                                {
                                    //update date és expiration mert már létezik a user
                                }
                            }
                        }
                        else
                        {
                            //ha van permission
                        }
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

        #region Database things
        private void addstartercd(string username, int commandid, int date, int expiration)
        {
            database.Query("INSERT INTO misc(Username, CommandID, Date, Expiration) VALUES(@0, @1, @2, @3);", username, commandid, date, expiration);
        }
        private void checkstartercd(string username, int commandid)
        {
            QueryResult reader;
            reader = database.QueryReader("SELECT FROM misc WHERE Username=@0 AND CommandID=@1;", username, commandid);
            while (reader.Read())
            {
                var qusername = reader.Get<string>("Username");
                var qcommandid = reader.Get<int>("CommandID");
                var qdate = reader.Get<int>("Date");
                var qexpiration = reader.Get<int>("Expiration");
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
