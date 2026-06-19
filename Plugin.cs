using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace WeightedCategoryPatch
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("SoftDiamond.BrutalCompanyMinusExtraReborn", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        private readonly Harmony _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            Log = Logger;
            _harmony.PatchAll(typeof(EventManagerPatch));
            Log.LogInfo("WeightedCategoryPatch v1.0.0 loaded.");
        }
    }

    internal static class MyPluginInfo
    {
        public const string PLUGIN_GUID    = "PikaWarrior.WeightedCategoryPatch";
        public const string PLUGIN_NAME    = "WeightedCategoryPatch";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}