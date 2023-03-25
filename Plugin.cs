using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using ServerSync;
using System;
using System.IO;
using System.Threading.Tasks;


#pragma warning disable CS8632
namespace BossDespawn
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        #region values
        private const string ModName = "BossDespawn", ModVersion = "1.0.0", ModGUID = "com.Frogger." + ModName;
        private static readonly Harmony harmony = new(ModGUID);
        public static Plugin _self;
        #endregion
        #region ConfigSettings
        static string ConfigFileName = "com.Frogger.BossDespawn.cfg";
        DateTime LastConfigChange;
        public static readonly ConfigSync configSync = new(ModName) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
        public static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = _self.Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }
        private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }
        void SetCfgValue<T>(Action<T> setter, ConfigEntry<T> config)
        {
            setter(config.Value);
            config.SettingChanged += (_, _) => setter(config.Value);
        }
        public enum Toggle
        {
            On = 1,
            Off = 0
        }
        public class ConfigurationManagerAttributes
        {
            public int? Order;
            public bool? HideSettingName;
            public bool? HideDefaultButton;
            public string? DispName;
            public Action<ConfigEntryBase>? CustomDrawer;
        }
        #endregion
        #region values
        internal static ConfigEntry<float> radiusConfig;
        internal static float radius;
        #endregion


        private void Awake()
        {
            _self = this;

            #region config
            Config.SaveOnConfigSet = false;

            radiusConfig = config("General", "Despawn radius", 110f, new ConfigDescription("", new AcceptableValueRange<float>(15f, 250)));

            SetupWatcherOnConfigFile();
            Config.ConfigReloaded += (_, _) => { UpdateConfiguration(); };
            Config.SaveOnConfigSet = true;
            Config.Save();
            #endregion


            harmony.PatchAll();
        }

        #region tools
        public static void Debug(string msg)
        {
            _self.DebugPrivate(msg);
        }
        public static void DebugError(string msg)
        {
            _self.DebugErrorPrivate(msg);
        }
        private void DebugPrivate(string msg)
        {
            Logger.LogInfo(msg);
        }
        private void DebugErrorPrivate(string msg)
        {
            Logger.LogError($"{msg} Write to the developer and moderator if this happens often.");
        }
        #endregion
        #region Config
        public void SetupWatcherOnConfigFile()
        {
            FileSystemWatcher fileSystemWatcherOnConfig = new(Paths.ConfigPath, ConfigFileName);
            fileSystemWatcherOnConfig.Changed += ConfigChanged;
            fileSystemWatcherOnConfig.IncludeSubdirectories = true;
            fileSystemWatcherOnConfig.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            fileSystemWatcherOnConfig.EnableRaisingEvents = true;
        }
        private void ConfigChanged(object sender, FileSystemEventArgs e)
        {
            if((DateTime.Now - LastConfigChange).TotalSeconds <= 5.0)
            {
                return;
            }
            LastConfigChange = DateTime.Now;
            try
            {
                Config.Reload();
                Debug("Reloading Config...");
            }
            catch
            {
                DebugError("Can't reload Config");
            }
        }
        private void UpdateConfiguration()
        {
            Task task = null;
            task = Task.Run(() =>
            {
                radius = radiusConfig.Value;
            });

            Task.WaitAll();
            Debug("Configuration Received");
        }
        #endregion
    }
}