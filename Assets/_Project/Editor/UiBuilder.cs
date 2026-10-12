using Fireflies.App;
using Fireflies.Config;
using Fireflies.Presentation;
using Fireflies.UI;
using Fireflies.UI.Overlays;
using Fireflies.UI.Screens;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fireflies.EditorTools
{
    public static class UiBuilder
    {
        private const string UiFolder = "Assets/_Project/UI";
        private const string PanelSettingsPath = UiFolder + "/GamePanelSettings.asset";
        private const string ThemePath = UiFolder + "/Styles/Theme.tss";
        private const string RootLayoutPath = UiFolder + "/Uxml/GameUi.uxml";
        private const string PalettePath = "Assets/_Project/Settings/PlayerPalette.asset";
        private const string PortraitsFolder = "Assets/_Project/Art/UI/Portraits";

        public static PlayerPalette LoadOrCreatePalette()
        {
            PlayerPalette palette = SettingsAssetFactory.LoadOrCreate<PlayerPalette>(PalettePath);
            palette.Configure(new[]
            {
                Slot("Ámbar", "#FFD23F", "PortraitYellow"),
                Slot("Cian", "#3BCEAC", "PortraitCyan"),
                Slot("Magenta", "#EE4266", "PortraitMagenta"),
                Slot("Verde", "#7BD389", "PortraitGreen")
            });
            EditorUtility.SetDirty(palette);
            return palette;
        }

        public static void Build(SessionRunner runner, PlayerPalette palette, WorldPresenter world, Camera camera)
        {
            GameObject root = new GameObject("UI");
            UIDocument document = root.AddComponent<UIDocument>();
            document.panelSettings = LoadOrCreatePanelSettings();
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(RootLayoutPath);

            MainMenuScreen mainMenu = AddScreen<MainMenuScreen>(root, document, runner, palette);
            AdminPanelScreen admin = AddScreen<AdminPanelScreen>(root, document, runner, palette);
            LobbyScreen lobby = AddScreen<LobbyScreen>(root, document, runner, palette);
            SerializedFieldWriter.Set(lobby, "_adminPanel", admin);
            HudScreen hud = AddScreen<HudScreen>(root, document, runner, palette);
            SerializedFieldWriter.Set(hud, "_adminPanel", admin);
            ResultsScreen results = AddScreen<ResultsScreen>(root, document, runner, palette);
            ConnectingOverlay connecting = AddScreen<ConnectingOverlay>(root, document, runner, null);
            DialogOverlay dialog = AddScreen<DialogOverlay>(root, document, runner, null);
            AddScreen<ToastFeed>(root, document, runner, palette);
            AddScreen<PingTableOverlay>(root, document, runner, palette);
            AddScreen<SyncDebugOverlay>(root, document, runner, null);
            WorldLabelsOverlay labels = AddScreen<WorldLabelsOverlay>(root, document, runner, palette);
            SerializedFieldWriter.Set(labels, "_world", world);
            SerializedFieldWriter.Set(labels, "_camera", camera);

            UiFlowController flow = root.AddComponent<UiFlowController>();
            SerializedFieldWriter.Set(flow, "_document", document);
            SerializedFieldWriter.Set(flow, "_runner", runner);
            SerializedFieldWriter.Set(flow, "_mainMenu", mainMenu);
            SerializedFieldWriter.Set(flow, "_connecting", connecting);
            SerializedFieldWriter.Set(flow, "_lobby", lobby);
            SerializedFieldWriter.Set(flow, "_hud", hud);
            SerializedFieldWriter.Set(flow, "_results", results);
            SerializedFieldWriter.Set(flow, "_adminPanel", admin);
            SerializedFieldWriter.Set(flow, "_dialog", dialog);
        }

        private static T AddScreen<T>(GameObject root, UIDocument document, SessionRunner runner, PlayerPalette palette) where T : SessionScreen
        {
            T screen = root.AddComponent<T>();
            SerializedFieldWriter.Set(screen, "_document", document);
            SerializedFieldWriter.Set(screen, "_runner", runner);
            if (palette != null)
            {
                SerializedFieldWriter.Set(screen, "_palette", palette);
            }

            return screen;
        }

        private static PanelSettings LoadOrCreatePanelSettings()
        {
            PanelSettings settings = SettingsAssetFactory.LoadOrCreate<PanelSettings>(PanelSettingsPath);
            settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            settings.clearColor = false;
            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static PlayerPalette.SlotAppearance Slot(string displayName, string html, string portraitName)
        {
            ColorUtility.TryParseHtmlString(html, out Color color);
            Sprite portrait = AssetDatabase.LoadAssetAtPath<Sprite>($"{PortraitsFolder}/{portraitName}.png");
            return new PlayerPalette.SlotAppearance(displayName, color, portrait);
        }
    }
}
