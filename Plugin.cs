using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ServerSync;

namespace BossDespawn;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
    #region values

    private const string ModName = "BossDespawn", ModVersion = "1.0.3", ModGUID = "com.Frogger." + ModName;
    public static Plugin _self;

    #endregion

    #region ConfigSettings

    static string ConfigFileName = "com.Frogger.BossDespawn.cfg";
    DateTime LastConfigChange;

    public static readonly ConfigSync configSync = new(ModName)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

    public static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
        bool synchronizedSetting = true)
    {
        ConfigEntry<T> configEntry = _self.Config.Bind(group, name, value, description);

        SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    private ConfigEntry<T> config<T>(string group, string name, T value, string description,
        bool synchronizedSetting = true)
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
    internal static ConfigEntry<BossFilterMode> filterModeConfig;
    internal static ConfigEntry<string> whiteListConfig;

    internal static ConfigEntry<string> blackListConfig;

    //internal static ConfigEntry<float> despawnDelayConfig;
    internal static float radius;
    internal static BossFilterMode filterMode;
    internal static List<string> whiteList = new();

    internal static List<string> blackList = new();
    //internal static float despawnDelay = new();

    #endregion

    private void Awake()
    {
        _self = this;

        #region config

        Config.SaveOnConfigSet = false;

        radiusConfig = config("General", "Despawn radius", 110f,
            new ConfigDescription(string.Empty, new AcceptableValueRange<float>(25f, 250f)));
        filterModeConfig = config("General", "Boss filter mode", filterMode, new ConfigDescription(string.Empty));
        whiteListConfig = config("General", "Bosses white list", whiteList.GetString(),
            new ConfigDescription(
                "Only the listed bosses will disappear. List with \", \". Don't forget to specify the whitelist mode."));
        blackListConfig = config("General", "Bosses black list", blackList.GetString(),
            new ConfigDescription(
                "The listed bosses will not disappear. List with \", \". Don't forget to specify the blacklist mode."));
        // despawnDelayConfig = config("General", "Despawn delay", despawnDelay,
        //     new ConfigDescription(
        //         "In seconds! At the moment when there is not a single player left around the boss, the timer starts for this time." +
        //         " After it expires, the boss will check if there are players around him, and if not, it will destroy itself."));

        SetupWatcherOnConfigFile();
        Config.ConfigReloaded += (_, _) => { UpdateConfiguration(); };
        Config.SaveOnConfigSet = true;
        Config.Save();

        #endregion

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
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
        if ((DateTime.Now - LastConfigChange).TotalSeconds <= 5.0)
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

    private async void UpdateConfiguration()
    {
        Task task = null;
        task = Task.Run(() =>
        {
            radius = radiusConfig.Value;
            filterMode = filterModeConfig.Value;
            blackList = blackListConfig.Value.Split(new string[1] { ", " }, StringSplitOptions.None).ToList();
            whiteList = whiteListConfig.Value.Split(new string[1] { ", " }, StringSplitOptions.None).ToList();
            //despawnDelay = despawnDelayConfig.Value;
        });

        await task;
        Debug("Configuration Received");
    }

    #endregion
}