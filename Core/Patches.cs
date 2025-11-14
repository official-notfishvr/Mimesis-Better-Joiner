using System;
using BetterJoiner.Core.Config;
using BetterJoiner.Core.Features;
using HarmonyLib;
using MelonLoader;
using Steamworks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterJoiner.Core
{
    public static class Patches
    {
        public static bool enhancedSaveUI = true;

        private static ConfigManager configManager;

        public static void ApplyPatches(ConfigManager config)
        {
            try
            {
                configManager = config;
                LoadConfig();

                HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("com.Mimesis.BetterJoiner");
                harmony.PatchAll(typeof(Patches).Assembly);

                MelonLogger.Msg("Harmony patches applied successfully");
                SaveConfig();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error applying patches: {ex.Message}");
            }
        }

        private static void LoadConfig()
        {
            if (configManager == null)
                return;

            try
            {
                enhancedSaveUI = configManager.GetBool("enhancedSaveUI", true);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error loading patch config: {ex.Message}");
            }
        }

        public static void SaveConfig()
        {
            if (configManager == null)
                return;

            try
            {
                configManager.SetBool("enhancedSaveUI", enhancedSaveUI);
                configManager.SaveMainConfig();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error saving patch config: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(SteamMatchmaking), nameof(SteamMatchmaking.JoinLobby))]
    internal static class LobbyJoinLoggingPatch
    {
        private static void Prefix(CSteamID steamIDLobby)
        {
            MelonLogger.Msg($"[PATCH] Attempting to join lobby: {steamIDLobby.m_SteamID}");
        }
    }

    [HarmonyPatch(typeof(SteamMatchmaking), nameof(SteamMatchmaking.GetNumLobbyMembers))]
    internal static class GetLobbyMemberCountPatch
    {
        private static void Postfix(CSteamID steamIDLobby, ref int __result)
        {
            MelonLogger.Msg($"[PATCH] Room member count for {steamIDLobby.m_SteamID}: {__result}");
        }
    }

    [HarmonyPatch(typeof(SteamMatchmaking), nameof(SteamMatchmaking.GetLobbyData))]
    internal static class GetLobbyDataPatch
    {
        private static void Postfix(CSteamID steamIDLobby, string pchKey, ref string __result)
        {
            if (!string.IsNullOrEmpty(__result))
            {
                MelonLogger.Msg($"[PATCH] Lobby data - Key: {pchKey}, Value: {__result}");
            }
        }
    }

    [HarmonyPatch(typeof(SteamMatchmaking), nameof(SteamMatchmaking.CreateLobby))]
    internal static class CreateLobbyPatch
    {
        private static void Prefix(ELobbyType eLobbyType, int cMaxMembers)
        {
            MelonLogger.Msg($"[PATCH] Creating lobby - Type: {eLobbyType}, Max Members: {cMaxMembers}");
        }
    }

    [HarmonyPatch(typeof(UIPrefab_LoadTram), nameof(UIPrefab_LoadTram.InitSaveInfoList))]
    internal static class LoadTramEnhancedUIPatch
    {
        private static void Postfix(UIPrefab_LoadTram __instance)
        {
            if (!Patches.enhancedSaveUI)
                return;

            try
            {
                var manager = __instance.gameObject.GetComponent<GameManager>();
                if (manager == null)
                {
                    manager = __instance.gameObject.AddComponent<GameManager>();
                    manager.InitializeLoadUI(__instance);
                }
                else
                {
                    manager.RefreshUI();
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error enhancing load tram UI: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(UIPrefab_NewTram), nameof(UIPrefab_NewTram.InitSaveInfoList))]
    internal static class NewTramEnhancedUIPatch
    {
        private static void Postfix(UIPrefab_NewTram __instance)
        {
            if (!Patches.enhancedSaveUI)
                return;

            try
            {
                var manager = __instance.gameObject.GetComponent<GameManager>();
                if (manager == null)
                {
                    manager = __instance.gameObject.AddComponent<GameManager>();
                    manager.InitializeNewTramUI(__instance);
                }
                else
                {
                    manager.RefreshUI();
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error enhancing new tram UI: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(UIPrefab_NewTramPopUp), "OnShow")]
    internal static class SaveConfirmationUIEnhancePatch
    {
        private static void Postfix(UIPrefab_NewTramPopUp __instance)
        {
            if (!Patches.enhancedSaveUI)
                return;

            try
            {
                var popup = __instance.GetComponent<Image>();
                if (popup != null)
                {
                    popup.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
                }

                var buttons = __instance.GetComponentsInChildren<Button>();
                foreach (var button in buttons)
                {
                    if (button.name == "OK")
                    {
                        var colors = button.colors;
                        colors.normalColor = new Color(0.2f, 0.4f, 0.2f, 0.9f);
                        colors.highlightedColor = new Color(0.3f, 0.6f, 0.3f, 1f);
                        button.colors = colors;
                    }
                    else if (button.name == "Cancel")
                    {
                        var colors = button.colors;
                        colors.normalColor = new Color(0.4f, 0.2f, 0.2f, 0.9f);
                        colors.highlightedColor = new Color(0.6f, 0.3f, 0.3f, 1f);
                        button.colors = colors;
                    }
                }

                MelonLogger.Msg("[PATCH] Enhanced confirmation popup UI applied");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Could not enhance confirmation popup: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(UIPrefab_MainMenu), "Start")]
    internal static class MainMenuUIEnhancePatch
    {
        private static void Postfix(UIPrefab_MainMenu __instance)
        {
            if (!Patches.enhancedSaveUI)
                return;

            try
            {
                var hostButton = __instance.UE_HostButton;
                var loadButton = __instance.UE_LoadButton;
                var joinButton = __instance.UE_JoinButton;

                if (hostButton != null)
                {
                    ApplyButtonStyle(hostButton, new Color(0.2f, 0.35f, 0.5f, 0.9f), new Color(0.3f, 0.5f, 0.7f, 1f));
                }

                if (loadButton != null)
                {
                    ApplyButtonStyle(loadButton, new Color(0.35f, 0.2f, 0.5f, 0.9f), new Color(0.5f, 0.3f, 0.7f, 1f));
                }

                if (joinButton != null)
                {
                    ApplyButtonStyle(joinButton, new Color(0.5f, 0.35f, 0.2f, 0.9f), new Color(0.7f, 0.5f, 0.3f, 1f));
                }

                MelonLogger.Msg("[PATCH] Enhanced main menu UI applied");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Could not enhance main menu: {ex.Message}");
            }
        }

        private static void ApplyButtonStyle(Button button, Color normalColor, Color highlightColor)
        {
            try
            {
                var colors = button.colors;
                colors.normalColor = normalColor;
                colors.highlightedColor = highlightColor;
                colors.pressedColor = new Color(highlightColor.r * 0.7f, highlightColor.g * 0.7f, highlightColor.b * 0.7f, 1f);
                button.colors = colors;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Error applying button style: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(MainMenu), "Start")]
    internal static class MainMenuStartPatch
    {
        private static GameManager currentManager = null;

        private static void Postfix(MainMenu __instance)
        {
            if (!Patches.enhancedSaveUI)
                return;

            try
            {
                var uimanField = typeof(MainMenu).GetProperty("uiman", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (uimanField == null)
                    return;

                var uiman = uimanField.GetValue(__instance) as UIManager;
                if (uiman == null)
                    return;

                var ui_mainmenuField = typeof(MainMenu).GetField("ui_mainmenu", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var uiprefab_loadtramField = typeof(MainMenu).GetField("uiprefab_loadtram", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var uiprefab_newtramField = typeof(MainMenu).GetField("uiprefab_newtram", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (ui_mainmenuField == null || uiprefab_loadtramField == null || uiprefab_newtramField == null)
                    return;

                var ui_mainmenu = ui_mainmenuField.GetValue(__instance) as UIPrefab_MainMenu;
                var uiprefab_loadtram = uiprefab_loadtramField.GetValue(__instance) as GameObject;
                var uiprefab_newtram = uiprefab_newtramField.GetValue(__instance) as GameObject;

                if (ui_mainmenu == null || uiprefab_loadtram == null || uiprefab_newtram == null)
                    return;

                var loadtram = uiman.InstatiateUIPrefab<UIPrefab_LoadTram>(uiprefab_loadtram, eUIHeight.Top);
                var newtram = uiman.InstatiateUIPrefab<UIPrefab_NewTram>(uiprefab_newtram, eUIHeight.Top);
                loadtram.Hide();
                newtram.Hide();

                ui_mainmenu.OnLoadButton = delegate(string _)
                {
                    try
                    {
                        EventSystem current = EventSystem.current;
                        if (current != null)
                        {
                            current.SetSelectedGameObject(null);
                        }

                        var manager = loadtram.gameObject.GetComponent<GameManager>();
                        if (manager == null)
                        {
                            manager = loadtram.gameObject.AddComponent<GameManager>();
                            manager.InitializeLoadUI(loadtram);
                        }
                        else
                        {
                            manager.RefreshUI();
                        }

                        currentManager = manager;

                        if (loadtram.UE_rootNode != null)
                            loadtram.UE_rootNode.gameObject.SetActive(false);

                        uiman.ui_escapeStack.Add(loadtram);
                        loadtram.Show();

                        MelonLogger.Msg("[PATCH] Infinite Save UI opened for loading");
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Error($"Error opening load UI: {ex.Message}");
                    }
                };

                ui_mainmenu.OnHostButton = delegate(string _)
                {
                    try
                    {
                        EventSystem current = EventSystem.current;
                        if (current != null)
                        {
                            current.SetSelectedGameObject(null);
                        }

                        var manager = newtram.gameObject.GetComponent<GameManager>();
                        if (manager == null)
                        {
                            manager = newtram.gameObject.AddComponent<GameManager>();
                            manager.InitializeNewTramUI(newtram);
                        }
                        else
                        {
                            manager.RefreshUI();
                        }

                        currentManager = manager;

                        if (newtram.UE_rootNode != null)
                            newtram.UE_rootNode.gameObject.SetActive(false);

                        uiman.ui_escapeStack.Add(newtram);
                        newtram.Show();

                        MelonLogger.Msg("[PATCH] Infinite Save UI opened for new game");
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Error($"Error opening new game UI: {ex.Message}");
                    }
                };

                MelonLogger.Msg("[PATCH] MainMenu Start patched successfully");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Could not patch MainMenu Start: {ex.Message}");
            }
        }
    }
}
