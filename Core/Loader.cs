using BetterJoiner.Core;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(Loader), "BetterJoiner", "1.0.0", "notfishvr")]
[assembly: MelonGame("ReLUGames", "MIMESIS")]

namespace BetterJoiner.Core
{
    public class Loader : MelonMod
    {
        private GameObject gui;
        private MainGUI mainGUI;

        public override void OnInitializeMelon() { }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (gui == null)
            {
                gui = new GameObject(nameof(MainGUI));
                GameObject.DontDestroyOnLoad(gui);

                mainGUI = gui.AddComponent<MainGUI>();
            }
        }
    }
}
