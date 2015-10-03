using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Data;
using System.ComponentModel;
using System.Reflection;
using System.Drawing;
using System.IO.Compression;

using Terraria;
using TShockAPI;
using Newtonsoft.Json;
using System.Threading;
using TerrariaApi.Server;
using Newtonsoft.Json.Linq;

namespace PruneLog
{
    [ApiVersion(1, 21)]
    public class PruneLog : TerrariaPlugin
    {
        private static Config pruneLogConfig;
        private static int keepOnlyLogs;
        private static int keepForDays;
        private static bool pruneByDate;
        private bool verbose = false;
        private bool preview = false;
        private string archiveFileName;
        public override string Name
        {
            get { return "PruneLog"; }
        }
        public override string Author
        {
            get { return "Granpa-G"; }
        }
        public override string Description
        {
            get { return "Deletes log files base upon criteria."; }
        }
        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }
        public PruneLog(Main game)
            : base(game)
        {
            Order = -1;
        }
        public override void Initialize()
        {
            if (TShock.Config.UseSqlLogs)
            {
                Console.WriteLine("This command only used with text log files.");
                return;
            }

            Commands.ChatCommands.Add(new Command("PruneLog.allow", pruneLog, "prunelogs", "pl"));

            var path = Path.Combine(TShock.SavePath, "prunelog.json");
            (pruneLogConfig = Config.Read(path)).Write(path);

            keepOnlyLogs = pruneLogConfig.keepOnlyLogs;
            keepForDays = pruneLogConfig.keepForDays;
            pruneByDate = pruneLogConfig.pruneByDate;
            archiveFileName = pruneLogConfig.archiveFileName;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }

        private void pruneLog(CommandArgs args)
        {
            PruneLogListArguments arguments = new PruneLogListArguments(args.Parameters.ToArray());

            if (arguments.Contains("-help"))
            {
                args.Player.SendMessage("Syntax: /prunelog [-help] ", Color.Red);
                args.Player.SendMessage("Flags: ", Color.LightSalmon);
                args.Player.SendMessage(" -prune        starts prune operation", Color.LightSalmon);
                args.Player.SendMessage(" -reload/-r    reloads options from prunelog.json config file", Color.LightSalmon);
                args.Player.SendMessage(" -verbose/-v   show each file pruned", Color.LightSalmon);
                args.Player.SendMessage(" -!verbose/-!v don't show each file pruned", Color.LightSalmon);
                args.Player.SendMessage(" -keep n       keeps n number of log files from most recent", Color.LightSalmon);
                args.Player.SendMessage(" -days n       prunes any log files created before n days from today", Color.LightSalmon);
                args.Player.SendMessage(" -bydays       prune criteria set by value of days (keepForDays option in config file)", Color.LightSalmon);
                args.Player.SendMessage(" -bycount      prune criteria set by value of keep (keepOnlyLogs option in config file)", Color.LightSalmon);
                args.Player.SendMessage(" -preview/p    negates preview, thus files will be deleted", Color.LightSalmon);
                args.Player.SendMessage(" -!preview/!p  performs prune action without actually deleting any files", Color.LightSalmon);
                args.Player.SendMessage(" -list/l       show current prune criteria", Color.LightSalmon);
                args.Player.SendMessage(" -help         this information", Color.LightSalmon);
                return;
            }

            if (arguments.Contains("-r") || arguments.Contains("-reload"))
            {
                var path = Path.Combine(TShock.SavePath, "prunelog.json");
                pruneLogConfig = Config.Read(path);
                keepOnlyLogs = pruneLogConfig.keepOnlyLogs;
                keepForDays = pruneLogConfig.keepForDays;
                pruneByDate = pruneLogConfig.pruneByDate;
                return;
            }

            if (arguments.Contains("-v") || arguments.Contains("-verbose"))
            {
                verbose = true;
                return;
            }
            if (arguments.Contains("-!v") || arguments.Contains("-!verbose"))
            {
                verbose = false;
                return;
            }
            if (arguments.Contains("-p") || arguments.Contains("-preview"))
            {
                preview = true;
                return;
            }
            if (arguments.Contains("-!p") || arguments.Contains("-!preview"))
            {
                preview = false;
                return;
            }

            if (arguments.Contains("-l") || arguments.Contains("-list"))
            {
                Console.WriteLine("Current options for PruneLog version " + Assembly.GetExecutingAssembly().GetName().Version);
                Console.WriteLine(" keepForDays=" + keepForDays);
                Console.WriteLine(" keepOnlyLogs=" + keepOnlyLogs);
                Console.WriteLine(" pruneByDate is " + pruneByDate.ToString());
                Console.WriteLine(" verbose is " + verbose.ToString());
                Console.WriteLine(" preview is " + preview.ToString());
                return;
            }

            if (arguments.Contains("-keep"))
            {
                if (args.Parameters.Count > 1)
                    keepOnlyLogs = Int32.Parse(args.Parameters[1]);
                return;
            }
            if (arguments.Contains("-days"))
            {
                if (args.Parameters.Count > 1)
                    keepForDays = Int32.Parse(args.Parameters[1]);
                return;
            }

            if (arguments.Contains("-bydays"))
            {
                pruneByDate = true;
                return;
            }
            if (arguments.Contains("-bycount"))
            {
                pruneByDate = false;
                return;
            }

            if (arguments.Contains("-prune"))
            {
                string logFilename = TShock.Log.FileName;

                DirectoryInfo directoryInfo = new DirectoryInfo(TShock.Config.LogPath);
                FileInfo[] files = directoryInfo.GetFiles("*.log", SearchOption.AllDirectories).OrderBy(t => t.CreationTime).ToArray();

                int pruneCount = 0;
                if (pruneByDate)
                {
                    DateTime keepDate = DateTime.Today.AddDays(-keepForDays);

                    string fileName; 
                    if (keepForDays > 0)
                        foreach (FileInfo f in files)
                        {
                            fileName = directoryInfo + "\\" + f.Name;
                            if (!fileName.Equals(TShock.Log.FileName))
                            {
                                if (f.CreationTime < keepDate)
                                {
                                    if (!preview)
                                 try 
                                {
                                       f.Delete();
                                }
                                 catch (Exception ex)
                                 {
                                     TShock.Log.Error(ex.ToString());
                                     Console.WriteLine(ex.StackTrace);
                                 }
                                    if (verbose)
                                        Console.WriteLine("Prune: " + fileName);
                                    pruneCount++;
                                }
                            }
                        }
                }
                else
                {
                    string fileName;
                    for (int i = files.Count() - 1; i >= keepOnlyLogs; i--)
                    {
                        fileName = directoryInfo + "\\" + files[i].Name;
                        if (!fileName.Equals(TShock.Log.FileName))
                        {
                            if (!preview)
                                try 
                                {
                                    File.Delete(fileName);
                                }
                                catch (Exception ex)
                                {
                                    TShock.Log.Error(ex.ToString());
                                    Console.WriteLine(ex.StackTrace);
                                }

                            if (verbose)
                                Console.WriteLine("Prune: " + fileName);
                            pruneCount++;
                        }
                    }
                }
                Console.WriteLine(pruneCount + " logs Pruned");
                return;
            }
             Console.WriteLine(" Invalid PruneLog option:" + string.Join(" ", args.Parameters));
        }
    }
    #region application specific commands
    public class PruneLogListArguments : InputArguments
    {
        public string Verbose
        {
            get { return GetValue("-verbose"); }
        }
        public string VerboseShort
        {
            get { return GetValue("-v"); }
        }

        public string Help
        {
            get { return GetValue("-help"); }
        }


        public PruneLogListArguments(string[] args)
            : base(args)
        {
        }

        protected bool GetBoolValue(string key)
        {
            string adjustedKey;
            if (ContainsKey(key, out adjustedKey))
            {
                bool res;
                bool.TryParse(_parsedArguments[adjustedKey], out res);
                return res;
            }
            return false;
        }
    }
    #endregion

}
