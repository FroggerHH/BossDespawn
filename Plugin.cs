using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;

namespace BossDespawn;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
    private const string ModName = "BossDespawn",
        ModVersion = "1.2.0",
        ModGUID = $"com.{ModAuthor}.{ModName}",
        ModAuthor = "Frogger";

    private void Awake()
    {
        CreateMod(this, ModName, ModAuthor, ModVersion);
        mod.OnConfigurationChanged += UpdateConfiguration;

        radiusConfig = mod.config("General", "Despawn radius", 110f,
            new ConfigDescription(string.Empty, new AcceptableValueRange<float>(25f, 250f)));
        filterModeConfig = mod.config("General", "Boss filter mode", filterMode, new ConfigDescription(string.Empty));
        whiteListConfig = mod.config("General", "Bosses white list", whiteList.GetString(),
            new ConfigDescription(
                "Only the listed bosses will disappear. List with \", \". Don't forget to specify the whitelist mode."));
        blackListConfig = mod.config("General", "Bosses black list", blackList.GetString(),
            new ConfigDescription(
                "The listed bosses will not disappear. List with \", \". Don't forget to specify the blacklist mode."));
        despawnDelayConfig = mod.config("General", "Despawn delay", despawnDelay,
            new ConfigDescription(
                "In minutes! At the moment when there is not a single player left around the boss, the timer starts for this time."
                +
                " After it expires, the boss will check if there are players around him, and if not, it will destroy itself."));
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
            despawnDelay = despawnDelayConfig.Value;
        });

        await task;
        Debug("Configuration Received");
    }

    #region values

    internal static ConfigEntry<float> radiusConfig;
    internal static ConfigEntry<BossFilterMode> filterModeConfig;
    internal static ConfigEntry<string> whiteListConfig;

    internal static ConfigEntry<string> blackListConfig;

    internal static ConfigEntry<float> despawnDelayConfig;
    internal static float radius;
    internal static BossFilterMode filterMode;
    internal static List<string> whiteList = new();

    internal static List<string> blackList = new();
    internal static float despawnDelay;

    #endregion
}