using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;

namespace ultrakillParrySoundAndAudioReplacer
{
    public class HarmonyPatches
    {
        private static Harmony instance;

        public static bool IsPatched { get; private set; }
        public const string InstanceId = PluginInfo.PLUGIN_GUID;

        internal static void ApplyHarmonyPatches()
        {
            if (!IsPatched)
            {
                if (instance == null)
                {
                    instance = new Harmony(InstanceId);
                    Console.WriteLine("Patched");
                }

                instance.PatchAll(Assembly.GetExecutingAssembly());
                IsPatched = true;
            }
        }

        internal static void RemoveHarmonyPatches()
        {
            if (instance != null && IsPatched) 
            {
                instance.UnpatchSelf();
                IsPatched = false;
            }
        }
    }
}