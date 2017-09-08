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
    public class Config
    {
        public static Contents contents;

        #region Config create
        public static void CreateConfig()
        {
            string filepath = Path.Combine(TShock.SavePath, "starter.json");
            try
            {
                using (var stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    using (var sr = new StreamWriter(stream))
                    {
                        contents = new Contents();
                        var configString = JsonConvert.SerializeObject(contents, Formatting.Indented);
                        sr.Write(configString);
                    }
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.Message);
            }
        }
        #endregion

        #region Config Read
        public static bool ReadConfig()
        {
            string filepath = Path.Combine(TShock.SavePath, "starter.json");
            try
            {
                if (File.Exists(filepath))
                {
                    using (var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            var configString = sr.ReadToEnd();
                            contents = JsonConvert.DeserializeObject<Contents>(configString);
                        }
                        stream.Close();
                    }
                    return true;
                }
                else
                {
                    TShock.Log.ConsoleError("Starter config not found, how about a new one?");
                    CreateConfig();
                    return true;
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.Message);
            }
            return false;
        }
        #endregion

        #region Config
        public class Contents
        {
            public int startermage = 3069;
            public int starterwarrior = 280;
            public int starterranger = 3492;
            public int startersummoner = 1309;
            public string startercommandID = "1";
            public string startercooldown = "4h";
        }
        #endregion        
    }
}
