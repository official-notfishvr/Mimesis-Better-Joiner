using BetterJoiner.Core.Config;
using UnityEngine;

namespace BetterJoiner.Core
{
    public class MainGUI : MonoBehaviour
    {
        private ConfigManager configManager;

        void Start()
        {
            configManager = new ConfigManager();

            Patches.ApplyPatches(configManager);
        }
    }
}
