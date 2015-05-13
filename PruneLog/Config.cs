using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Net;
using Terraria;
using System.ComponentModel;

namespace PruneLog
{
    public class Config
    {
        public bool debugPL = false;
        [Description("How many log files will be kept from now.")]
        public int keepOnlyLogs = 20;
        [Description("How many days a log will be kept before purging.")]
        public int keepForDays = 3;
        [Description("Should logs be archived before purging?")]
        public bool archiveLogs = false;
        [Description("Log archive file name.")]
        public string archiveFileName = "oldLogs";
        [Description("Prune criteria method, date = true, count = false.")]
        public bool pruneByDate = true;

        public void Write(string path)
		{
			File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
		}

		public static Config Read(string path)
		{
			return !File.Exists(path)
				? new Config()
				: JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
		}
	}
}

