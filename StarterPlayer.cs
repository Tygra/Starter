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
    public class StarterPlayer
    {
        public int Index { get; set; }
        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public StarterPlayer(int index)
        {
            this.Index = index;
        }
    }
}
