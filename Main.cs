using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;

[assembly: MelonInfo(typeof(ItemsToBags.Main), "Items To Bags", "1.0.0", "Cheeto35")]
[assembly: MelonGame("Alta", "A Township Tale")]

namespace ItemsToBags
{
    public class Main : MelonMod
    {
        public static MelonLogger.Instance Log;
        public static bool IsServer;
        public static Settings Settings = new Settings();

        static string Dir { get { return Path.Combine(Path.Combine(Path.GetDirectoryName(UnityEngine.Application.dataPath), "UserData"), "ItemsToBags"); } }
        static string ConfigPath { get { return Path.Combine(Dir, "config.txt"); } }

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            IsServer = ApplicationManager.IsHeadless
                || CommandLineArguments.Contains(CommandLineArguments.StartServerArgument);
            Log.Msg(LoadSettings());
            Alta.Console.CommandService.CommandCollection.Collect(typeof(Main).Assembly);
            Log.Msg("Items To Bags 1.0.0 ready (" + (IsServer ? "server" : "player") + "), hold " + Settings.HoldSeconds + " s");
        }

        public override void OnUpdate()
        {
            try
            {
                if (IsServer) Merger.Update();
                else HandFeel.Update();
            }
            catch (Exception e)
            {
                if (UnityEngine.Time.unscaledTime > _nextError) { Log.Warning(e.ToString()); _nextError = UnityEngine.Time.unscaledTime + 10f; }
            }
        }

        float _nextError;

        public static string LoadSettings()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                if (!File.Exists(ConfigPath)) File.WriteAllText(ConfigPath, Settings.DefaultFile.Replace("\n", Environment.NewLine));
                List<string> warnings = new List<string>();
                Settings = Settings.Parse(File.ReadAllText(ConfigPath), warnings);
                foreach (string w in warnings) Log.Warning("config: " + w);
                return "loaded " + ConfigPath + (warnings.Count > 0 ? " with " + warnings.Count + " warning(s)" : "");
            }
            catch (Exception e)
            {
                Settings = new Settings();
                return "couldn't read " + ConfigPath + ", using defaults: " + e.Message;
            }
        }
    }
}
