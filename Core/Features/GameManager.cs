using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using ReluProtocol;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BetterJoiner.Core.Features
{
    public class GameManager : MonoBehaviour
    {
        private int currentPage = 0;
        private int itemsPerPage = 4;
        private List<SaveSlotData> allSaves = new List<SaveSlotData>();
        private List<RoomInfo> roomList = new List<RoomInfo>();

        private UIPrefab_LoadTram loadTramUI;
        private UIPrefab_NewTram newTramUI;
        private bool isSaveMode = false;
        private MainMenu mainMenuInstance;

        private GameObject mainPanel;
        private Transform savesContentTransform;
        private Transform roomsContentTransform;
        private TextMeshProUGUI pageText;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI titleText;
        private List<GameObject> displayCards = new List<GameObject>();

        private float lastRefreshTime;
        public float RefreshInterval { get; set; } = 5f;
        private CallResult<LobbyMatchList_t> lobbySearchResult;
        private bool isSearching = false;
        private Dictionary<string, RoomInfo> cachedRooms = new Dictionary<string, RoomInfo>();
        private int nextAvailableSlot = 0;

        private int currentTab = 0;
        private GameObject savesContainer;
        private GameObject roomsContainer;
        private List<Button> tabButtons = new List<Button>();
        private ScrollRect savesScrollRect;
        private ScrollRect roomsScrollRect;

        private int searchAttempts = 0;
        private const int MAX_SEARCH_ATTEMPTS = 100;

        public void Initialize()
        {
            InitializeSteamCallbacks();
            MelonLogger.Msg("[GameManager] Initialized");
        }

        private void InitializeSteamCallbacks()
        {
            try
            {
                if (SteamManager.Initialized)
                {
                    if (lobbySearchResult == null)
                    {
                        lobbySearchResult = CallResult<LobbyMatchList_t>.Create(OnLobbySearchComplete);
                        MelonLogger.Msg("[GameManager] Steam callbacks initialized");
                    }
                    else
                    {
                        MelonLogger.Msg("[GameManager] Steam callbacks already initialized");
                    }
                }
                else
                {
                    MelonLogger.Error("[GameManager] Steam not initialized!");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[GameManager] Failed to initialize Steam callbacks: {ex.Message}");
            }
        }

        public void InitializeLoadUI(UIPrefab_LoadTram ui)
        {
            Initialize();
            loadTramUI = ui;
            isSaveMode = true;
            mainMenuInstance = FindObjectOfType<MainMenu>();
            currentPage = 0;
            LoadAllSaves();

            if (mainPanel == null)
            {
                CreateUI();
                MelonLogger.Msg("[GameManager] InitializeLoadUI - UI Created");
            }
            else
            {
                mainPanel.SetActive(true);
                currentPage = 0;
                currentTab = 0;
                SwitchTab(0);
                MelonLogger.Msg("[GameManager] InitializeLoadUI - UI Shown");
            }

            MelonLogger.Msg($"[GameManager] InitializeLoadUI - Found {allSaves.Count} saves");
        }

        public void InitializeNewTramUI(UIPrefab_NewTram ui)
        {
            Initialize();
            newTramUI = ui;
            isSaveMode = false;
            mainMenuInstance = FindObjectOfType<MainMenu>();
            currentPage = 0;
            LoadAllSaves();

            if (mainPanel == null)
            {
                CreateUI();
                MelonLogger.Msg("[GameManager] InitializeNewTramUI - UI Created");
            }
            else
            {
                mainPanel.SetActive(true);
                currentPage = 0;
                currentTab = 0;
                SwitchTab(0);
                MelonLogger.Msg("[GameManager] InitializeNewTramUI - UI Shown");
            }

            MelonLogger.Msg($"[GameManager] InitializeNewTramUI - Found {allSaves.Count} saves");
        }

        private void CreateUI()
        {
            try
            {
                Canvas mainCanvas = null;
                Canvas[] canvases = FindObjectsOfType<Canvas>();

                foreach (Canvas c in canvases)
                {
                    if (c.transform.parent == null)
                    {
                        mainCanvas = c;
                        break;
                    }
                }

                if (mainCanvas == null && canvases.Length > 0)
                    mainCanvas = canvases[canvases.Length - 1];

                if (mainCanvas == null)
                {
                    MelonLogger.Error("[GameManager] No Canvas found!");
                    return;
                }

                mainPanel = new GameObject("GameManagerPanel");
                mainPanel.transform.SetParent(mainCanvas.transform, false);
                mainPanel.transform.SetAsLastSibling();

                RectTransform panelRect = mainPanel.AddComponent<RectTransform>();
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;

                Image panelImage = mainPanel.AddComponent<Image>();
                panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.98f);

                CanvasGroup canvasGroup = mainPanel.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;

                GraphicRaycaster raycaster = mainPanel.AddComponent<GraphicRaycaster>();

                GameObject containerObj = new GameObject("Container");
                containerObj.transform.SetParent(mainPanel.transform, false);
                RectTransform containerRect = containerObj.AddComponent<RectTransform>();
                containerRect.anchorMin = new Vector2(0.5f, 0.5f);
                containerRect.anchorMax = new Vector2(0.5f, 0.5f);
                containerRect.pivot = new Vector2(0.5f, 0.5f);
                containerRect.anchoredPosition = Vector2.zero;
                containerRect.sizeDelta = new Vector2(800, 750);

                Image containerImage = containerObj.AddComponent<Image>();
                containerImage.color = new Color(0.1f, 0.1f, 0.15f, 0.98f);

                GameObject titleObj = new GameObject("Title");
                titleObj.transform.SetParent(containerObj.transform, false);
                RectTransform titleRect = titleObj.AddComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0.5f, 1f);
                titleRect.anchorMax = new Vector2(0.5f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.anchoredPosition = new Vector2(0, -20);
                titleRect.sizeDelta = new Vector2(760, 50);

                titleText = titleObj.AddComponent<TextMeshProUGUI>();
                titleText.text = "GAME MANAGER";
                titleText.fontSize = 48;
                titleText.fontStyle = FontStyles.Bold;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = Color.white;

                GameObject tabObj = new GameObject("TabButtons");
                tabObj.transform.SetParent(containerObj.transform, false);
                RectTransform tabRect = tabObj.AddComponent<RectTransform>();
                tabRect.anchorMin = new Vector2(0.5f, 1f);
                tabRect.anchorMax = new Vector2(0.5f, 1f);
                tabRect.pivot = new Vector2(0.5f, 1f);
                tabRect.anchoredPosition = new Vector2(0, -80);
                tabRect.sizeDelta = new Vector2(760, 50);

                Image tabImage = tabObj.AddComponent<Image>();
                tabImage.color = new Color(0.1f, 0.1f, 0.15f, 0.99f);

                GraphicRaycaster tabRaycaster = tabObj.AddComponent<GraphicRaycaster>();

                HorizontalLayoutGroup tabLayout = tabObj.AddComponent<HorizontalLayoutGroup>();
                tabLayout.spacing = 10;
                tabLayout.padding = new RectOffset(10, 10, 5, 5);
                tabLayout.childForceExpandWidth = true;
                tabLayout.childForceExpandHeight = true;

                CreateTabButton(tabObj, "SAVES", 0);
                CreateTabButton(tabObj, "ROOMS", 1);

                GameObject statusObj = new GameObject("Status");
                statusObj.transform.SetParent(containerObj.transform, false);
                RectTransform statusRect = statusObj.AddComponent<RectTransform>();
                statusRect.anchorMin = new Vector2(0.5f, 1f);
                statusRect.anchorMax = new Vector2(0.5f, 1f);
                statusRect.pivot = new Vector2(0.5f, 1f);
                statusRect.anchoredPosition = new Vector2(0, -135);
                statusRect.sizeDelta = new Vector2(760, 35);

                Image statusImage = statusObj.AddComponent<Image>();
                statusImage.color = new Color(0.15f, 0.15f, 0.2f, 1);

                GameObject statusTextObj = new GameObject("Text");
                statusTextObj.transform.SetParent(statusObj.transform, false);
                RectTransform statusTextRect = statusTextObj.AddComponent<RectTransform>();
                statusTextRect.anchorMin = Vector2.zero;
                statusTextRect.anchorMax = Vector2.one;
                statusTextRect.offsetMin = Vector2.zero;
                statusTextRect.offsetMax = Vector2.zero;

                statusText = statusTextObj.AddComponent<TextMeshProUGUI>();
                statusText.text = "Ready";
                statusText.fontSize = 18;
                statusText.alignment = TextAlignmentOptions.Center;
                statusText.color = new Color(0.8f, 0.8f, 0.85f, 1);
                statusText.raycastTarget = false;

                GameObject contentContainerObj = new GameObject("ContentContainer");
                contentContainerObj.transform.SetParent(containerObj.transform, false);
                RectTransform contentContainerRect = contentContainerObj.AddComponent<RectTransform>();
                contentContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
                contentContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
                contentContainerRect.pivot = new Vector2(0.5f, 0.5f);
                contentContainerRect.anchoredPosition = new Vector2(0, 10);
                contentContainerRect.sizeDelta = new Vector2(760, 450);

                Image contentContainerImage = contentContainerObj.AddComponent<Image>();
                contentContainerImage.color = new Color(0, 0, 0, 0);
                contentContainerImage.raycastTarget = false;

                savesContainer = new GameObject("SavesContainer");
                savesContainer.transform.SetParent(contentContainerObj.transform, false);
                RectTransform savesRect = savesContainer.AddComponent<RectTransform>();
                savesRect.anchorMin = Vector2.zero;
                savesRect.anchorMax = Vector2.one;
                savesRect.offsetMin = Vector2.zero;
                savesRect.offsetMax = Vector2.zero;

                savesScrollRect = savesContainer.AddComponent<ScrollRect>();
                Image savesImage = savesContainer.AddComponent<Image>();
                savesImage.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);

                GameObject savesViewportObj = new GameObject("Viewport");
                savesViewportObj.transform.SetParent(savesContainer.transform, false);
                RectTransform savesViewportRect = savesViewportObj.AddComponent<RectTransform>();
                savesViewportRect.anchorMin = Vector2.zero;
                savesViewportRect.anchorMax = Vector2.one;
                savesViewportRect.offsetMin = Vector2.zero;
                savesViewportRect.offsetMax = Vector2.zero;

                Image savesViewportImage = savesViewportObj.AddComponent<Image>();
                savesViewportImage.color = new Color(0.08f, 0.08f, 0.12f, 1);

                Mask savesMask = savesViewportObj.AddComponent<Mask>();
                savesMask.showMaskGraphic = false;

                GameObject savesContentObj = new GameObject("Content");
                savesContentObj.transform.SetParent(savesViewportObj.transform, false);
                RectTransform savesContentRect = savesContentObj.AddComponent<RectTransform>();
                savesContentTransform = savesContentObj.transform;
                savesContentRect.anchorMin = new Vector2(0.5f, 1);
                savesContentRect.anchorMax = new Vector2(0.5f, 1);
                savesContentRect.pivot = new Vector2(0.5f, 1);
                savesContentRect.anchoredPosition = Vector2.zero;
                savesContentRect.sizeDelta = new Vector2(740, 0);

                VerticalLayoutGroup savesLayoutGroup = savesContentObj.AddComponent<VerticalLayoutGroup>();
                savesLayoutGroup.spacing = 12;
                savesLayoutGroup.padding = new RectOffset(10, 10, 10, 10);
                savesLayoutGroup.childForceExpandHeight = false;
                savesLayoutGroup.childForceExpandWidth = true;

                ContentSizeFitter savesFitter = savesContentObj.AddComponent<ContentSizeFitter>();
                savesFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                savesScrollRect.content = savesContentRect;
                savesScrollRect.viewport = savesViewportRect;
                savesScrollRect.vertical = true;
                savesScrollRect.horizontal = false;
                savesScrollRect.movementType = ScrollRect.MovementType.Elastic;
                savesScrollRect.elasticity = 0.1f;

                roomsContainer = new GameObject("RoomsContainer");
                roomsContainer.transform.SetParent(contentContainerObj.transform, false);
                RectTransform roomsRect = roomsContainer.AddComponent<RectTransform>();
                roomsRect.anchorMin = Vector2.zero;
                roomsRect.anchorMax = Vector2.one;
                roomsRect.offsetMin = Vector2.zero;
                roomsRect.offsetMax = Vector2.zero;
                roomsContainer.SetActive(false);

                roomsScrollRect = roomsContainer.AddComponent<ScrollRect>();
                Image roomsImage = roomsContainer.AddComponent<Image>();
                roomsImage.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);

                GameObject roomsViewportObj = new GameObject("Viewport");
                roomsViewportObj.transform.SetParent(roomsContainer.transform, false);
                RectTransform roomsViewportRect = roomsViewportObj.AddComponent<RectTransform>();
                roomsViewportRect.anchorMin = Vector2.zero;
                roomsViewportRect.anchorMax = Vector2.one;
                roomsViewportRect.offsetMin = Vector2.zero;
                roomsViewportRect.offsetMax = Vector2.zero;

                Image roomsViewportImage = roomsViewportObj.AddComponent<Image>();
                roomsViewportImage.color = new Color(0.08f, 0.08f, 0.12f, 1);

                Mask roomsMask = roomsViewportObj.AddComponent<Mask>();
                roomsMask.showMaskGraphic = false;

                GameObject roomsContentObj = new GameObject("Content");
                roomsContentObj.transform.SetParent(roomsViewportObj.transform, false);
                RectTransform roomsContentRect = roomsContentObj.AddComponent<RectTransform>();
                roomsContentTransform = roomsContentObj.transform;
                roomsContentRect.anchorMin = new Vector2(0.5f, 1);
                roomsContentRect.anchorMax = new Vector2(0.5f, 1);
                roomsContentRect.pivot = new Vector2(0.5f, 1);
                roomsContentRect.anchoredPosition = Vector2.zero;
                roomsContentRect.sizeDelta = new Vector2(740, 0);

                VerticalLayoutGroup roomsLayoutGroup = roomsContentObj.AddComponent<VerticalLayoutGroup>();
                roomsLayoutGroup.spacing = 12;
                roomsLayoutGroup.padding = new RectOffset(10, 10, 10, 10);
                roomsLayoutGroup.childForceExpandHeight = false;
                roomsLayoutGroup.childForceExpandWidth = true;

                ContentSizeFitter roomsFitter = roomsContentObj.AddComponent<ContentSizeFitter>();
                roomsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                roomsScrollRect.content = roomsContentRect;
                roomsScrollRect.viewport = roomsViewportRect;
                roomsScrollRect.vertical = true;
                roomsScrollRect.horizontal = false;

                GameObject navObj = new GameObject("Navigation");
                navObj.transform.SetParent(containerObj.transform, false);
                RectTransform navRect = navObj.AddComponent<RectTransform>();
                navRect.anchorMin = new Vector2(0.5f, 0f);
                navRect.anchorMax = new Vector2(0.5f, 0f);
                navRect.pivot = new Vector2(0.5f, 0f);
                navRect.anchoredPosition = new Vector2(0, 15);
                navRect.sizeDelta = new Vector2(760, 60);

                HorizontalLayoutGroup navLayout = navObj.AddComponent<HorizontalLayoutGroup>();
                navLayout.spacing = 15;
                navLayout.padding = new RectOffset(15, 15, 5, 5);
                navLayout.childForceExpandHeight = true;
                navLayout.childForceExpandWidth = true;

                Button prevBtn = CreateButton(navObj, "Prev", ButtonType.Secondary);
                prevBtn.onClick.AddListener(() => PreviousPage());

                GameObject pageObj = new GameObject("PageText");
                pageObj.transform.SetParent(navObj.transform, false);
                RectTransform pageRect = pageObj.AddComponent<RectTransform>();

                LayoutElement pageLayout = pageObj.AddComponent<LayoutElement>();
                pageLayout.preferredWidth = 150;
                pageLayout.flexibleWidth = 0;

                pageText = pageObj.AddComponent<TextMeshProUGUI>();
                pageText.text = "Page 1 / 1";
                pageText.alignment = TextAlignmentOptions.Center;
                pageText.fontSize = 26;
                pageText.fontStyle = FontStyles.Bold;
                pageText.color = Color.white;

                Button nextBtn = CreateButton(navObj, "Next", ButtonType.Secondary);
                nextBtn.onClick.AddListener(() => NextPage());

                Button closeBtn = CreateButton(navObj, "Close", ButtonType.Destructive);
                closeBtn.onClick.AddListener(() => CloseUI());

                RefreshDisplay();
                MelonLogger.Msg("[GameManager] UI Created successfully");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[GameManager] Fatal error creating UI: {ex.Message}");
            }
        }

        private void CreateTabButton(GameObject parent, string text, int tabIndex)
        {
            GameObject btnObj = new GameObject($"Tab_{text}");
            btnObj.transform.SetParent(parent.transform, false);

            LayoutElement btnLayout = btnObj.AddComponent<LayoutElement>();
            btnLayout.preferredHeight = 50;
            btnLayout.preferredWidth = 150;

            Button btn = btnObj.AddComponent<Button>();
            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.25f, 0.25f, 0.3f, 1);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
            btnText.text = text;
            btnText.fontSize = 20;
            btnText.fontStyle = FontStyles.Bold;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = Color.white;
            btnText.raycastTarget = false;

            btn.onClick.AddListener(() => SwitchTab(tabIndex));
            tabButtons.Add(btn);

            ColorBlock colors = btn.colors;
            colors.normalColor = btnImage.color;
            colors.highlightedColor = btnImage.color * 1.2f;
            btn.colors = colors;
        }

        private void SwitchTab(int tab)
        {
            try
            {
                currentTab = tab;
                currentPage = 0;

                if (savesContainer == null || roomsContainer == null)
                {
                    MelonLogger.Error("[GameManager] Containers not initialized in SwitchTab");
                    return;
                }

                if (tab == 0)
                {
                    savesContainer.SetActive(true);
                    roomsContainer.SetActive(false);
                    if (titleText != null)
                        titleText.text = isSaveMode ? "LOAD GAME" : "CREATE NEW GAME";
                    LoadAllSaves();
                }
                else
                {
                    savesContainer.SetActive(false);
                    roomsContainer.SetActive(true);
                    if (titleText != null)
                        titleText.text = "FIND ROOMS";
                    searchAttempts = 0;
                    isSearching = false;
                    MelonLogger.Msg("[GameManager] Switched to Rooms tab, starting search");
                    SearchForRooms();
                }

                RefreshDisplay();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[GameManager] Error switching tab: {ex.Message}");
            }
        }

        private Button CreateButton(GameObject parent, string text, ButtonType type)
        {
            GameObject btnObj = new GameObject($"Button_{text}");
            btnObj.transform.SetParent(parent.transform, false);
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(120, 50);

            Button btn = btnObj.AddComponent<Button>();
            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.raycastTarget = true;

            switch (type)
            {
                case ButtonType.Primary:
                    btnImage.color = new Color(0.2f, 0.5f, 0.8f, 1);
                    break;
                case ButtonType.Secondary:
                    btnImage.color = new Color(0.35f, 0.35f, 0.4f, 1);
                    break;
                case ButtonType.Destructive:
                    btnImage.color = new Color(0.75f, 0.2f, 0.2f, 1);
                    break;
            }

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
            btnText.text = text;
            btnText.fontSize = 22;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = Color.white;
            btnText.raycastTarget = false;

            ColorBlock colors = btn.colors;
            colors.normalColor = btnImage.color;
            colors.highlightedColor = btnImage.color * 1.15f;
            colors.pressedColor = btnImage.color * 0.9f;
            btn.colors = colors;

            return btn;
        }

        private void RefreshDisplay()
        {
            try
            {
                displayCards.Clear();

                if (currentTab == 0)
                    RefreshSavesDisplay();
                else
                    RefreshRoomsDisplay();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[GameManager] Error in RefreshDisplay: {ex.Message}");
            }
        }

        private void RefreshSavesDisplay()
        {
            if (savesContentTransform == null)
            {
                MelonLogger.Error("[GameManager] savesContentTransform is NULL!");
                return;
            }

            foreach (Transform child in savesContentTransform)
            {
                Destroy(child.gameObject);
            }

            if (!isSaveMode)
            {
                CreateNewSaveCard();
            }

            int startIdx = currentPage * itemsPerPage;
            int endIdx = Mathf.Min(startIdx + itemsPerPage - 1, allSaves.Count);

            for (int i = startIdx; i < endIdx; i++)
            {
                if (i >= 0 && i < allSaves.Count)
                    CreateSaveCard(allSaves[i]);
            }

            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)(allSaves.Count + (isSaveMode ? 0 : 1)) / itemsPerPage));
            pageText.text = $"Page {currentPage + 1} / {totalPages}";

            if (savesScrollRect != null)
                savesScrollRect.verticalNormalizedPosition = 1;
        }

        private void RefreshRoomsDisplay()
        {
            if (roomsContentTransform == null)
                return;

            foreach (Transform child in roomsContentTransform)
            {
                Destroy(child.gameObject);
            }

            int startIdx = currentPage * itemsPerPage;
            int endIdx = Mathf.Min(startIdx + itemsPerPage, roomList.Count);

            if (roomList.Count == 0)
            {
                GameObject emptyMsg = new GameObject("EmptyMessage");
                emptyMsg.transform.SetParent(roomsContentTransform, false);

                LayoutElement emptyLayout = emptyMsg.AddComponent<LayoutElement>();
                emptyLayout.preferredHeight = 100;

                TextMeshProUGUI emptyText = emptyMsg.AddComponent<TextMeshProUGUI>();
                emptyText.text = isSearching ? "Searching for rooms..." : "No rooms found";
                emptyText.fontSize = 24;
                emptyText.alignment = TextAlignmentOptions.Center;
                emptyText.color = new Color(0.6f, 0.6f, 0.65f, 0.8f);

                displayCards.Add(emptyMsg);
            }
            else
            {
                for (int i = startIdx; i < endIdx; i++)
                {
                    if (i >= 0 && i < roomList.Count)
                    {
                        CreateRoomCard(roomList[i]);
                        MelonLogger.Msg($"[GameManager] Created room card {i}: {roomList[i].DungeonName} - {roomList[i].PlayerCount}/{roomList[i].MaxPlayers}");
                    }
                }
            }

            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)roomList.Count / itemsPerPage));
            pageText.text = $"Page {currentPage + 1} / {totalPages}";

            if (roomsScrollRect != null)
                roomsScrollRect.verticalNormalizedPosition = 1;

            MelonLogger.Msg($"[GameManager] RefreshRoomsDisplay complete - showing rooms {startIdx} to {endIdx} of {roomList.Count}");
        }

        private void CreateNewSaveCard()
        {
            try
            {
                GameObject card = new GameObject("SaveCard_Create");
                card.transform.SetParent(savesContentTransform, false);
                RectTransform cardRect = card.AddComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(720, 60);

                LayoutElement cardLayout = card.AddComponent<LayoutElement>();
                cardLayout.preferredHeight = 60;

                Image cardImage = card.AddComponent<Image>();
                cardImage.color = new Color(0.25f, 0.35f, 0.25f, 1);
                cardImage.raycastTarget = true;

                Button cardBtn = card.AddComponent<Button>();
                cardBtn.onClick.AddListener(() => OnCreateNewSave());

                GameObject titleObj = new GameObject("Title");
                titleObj.transform.SetParent(card.transform, false);
                RectTransform titleRect = titleObj.AddComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0.5f, 0.5f);
                titleRect.anchorMax = new Vector2(0.5f, 0.5f);
                titleRect.pivot = new Vector2(0.5f, 0.5f);
                titleRect.anchoredPosition = Vector2.zero;
                titleRect.sizeDelta = new Vector2(680, 50);

                TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
                titleText.text = "CREATE NEW SAVE";
                titleText.fontSize = 18;
                titleText.fontStyle = FontStyles.Bold;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = Color.white;
                titleText.raycastTarget = false;

                ColorBlock colors = cardBtn.colors;
                colors.normalColor = cardImage.color;
                colors.highlightedColor = new Color(0.35f, 0.45f, 0.35f, 1);
                colors.pressedColor = new Color(0.2f, 0.3f, 0.2f, 1);
                cardBtn.colors = colors;

                displayCards.Add(card);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[GameManager] Error creating new save card: {ex.Message}");
            }
        }

        private void CreateSaveCard(SaveSlotData save)
        {
            try
            {
                GameObject card = new GameObject($"SaveCard_{save.SlotId}");
                card.transform.SetParent(savesContentTransform, false);
                RectTransform cardRect = card.AddComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(720, 130);

                LayoutElement cardLayout = card.AddComponent<LayoutElement>();
                cardLayout.preferredHeight = 130;

                Image cardImage = card.AddComponent<Image>();
                cardImage.color = new Color(0.18f, 0.18f, 0.22f, 1);
                cardImage.raycastTarget = true;

                Button cardBtn = card.AddComponent<Button>();
                int slotId = save.SlotId;
                cardBtn.onClick.AddListener(() => OnSaveSelected(slotId));

                GameObject titleObj = new GameObject("Title");
                titleObj.transform.SetParent(card.transform, false);
                RectTransform titleRect = titleObj.AddComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0, 1);
                titleRect.anchorMax = new Vector2(1, 1);
                titleRect.pivot = new Vector2(0.5f, 1);
                titleRect.anchoredPosition = new Vector2(0, -12);
                titleRect.sizeDelta = new Vector2(-20, 28);

                TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
                titleText.text = $"<b>Slot {save.SlotId}</b> - Cycle {save.SaveData.CycleCount}";
                titleText.fontSize = 24;
                titleText.alignment = TextAlignmentOptions.TopLeft;
                titleText.color = Color.white;

                GameObject dateObj = new GameObject("Date");
                dateObj.transform.SetParent(card.transform, false);
                RectTransform dateRect = dateObj.AddComponent<RectTransform>();
                dateRect.anchorMin = new Vector2(0, 1);
                dateRect.anchorMax = new Vector2(1, 1);
                dateRect.pivot = new Vector2(0.5f, 1);
                dateRect.anchoredPosition = new Vector2(0, -45);
                dateRect.sizeDelta = new Vector2(-20, 22);

                TextMeshProUGUI dateText = dateObj.AddComponent<TextMeshProUGUI>();
                dateText.text = save.SaveData.RegDateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                dateText.fontSize = 18;
                dateText.alignment = TextAlignmentOptions.TopLeft;
                dateText.color = new Color(0.75f, 0.75f, 0.78f, 1);

                GameObject playersObj = new GameObject("Players");
                playersObj.transform.SetParent(card.transform, false);
                RectTransform playersRect = playersObj.AddComponent<RectTransform>();
                playersRect.anchorMin = new Vector2(0, 1);
                playersRect.anchorMax = new Vector2(1, 1);
                playersRect.pivot = new Vector2(0.5f, 1);
                playersRect.anchoredPosition = new Vector2(0, -72);
                playersRect.sizeDelta = new Vector2(-20, 22);

                string playerNames = save.SaveData.PlayerNames != null && save.SaveData.PlayerNames.Count > 0 ? string.Join(", ", save.SaveData.PlayerNames.Take(3)) : "No players";

                TextMeshProUGUI playersText = playersObj.AddComponent<TextMeshProUGUI>();
                playersText.text = $"<i>Players: {playerNames}</i>";
                playersText.fontSize = 16;
                playersText.alignment = TextAlignmentOptions.TopLeft;
                playersText.color = new Color(0.65f, 0.68f, 0.7f, 1);

                ColorBlock colors = cardBtn.colors;
                colors.normalColor = cardImage.color;
                colors.highlightedColor = cardImage.color * 1.2f;
                colors.pressedColor = cardImage.color * 0.85f;
                cardBtn.colors = colors;

                displayCards.Add(card);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[GameManager] Error creating save card: {ex.Message}");
            }
        }

        private void CreateRoomCard(RoomInfo room)
        {
            try
            {
                GameObject card = new GameObject($"RoomCard_{room.RoomId}");
                card.transform.SetParent(roomsContentTransform, false);

                LayoutElement cardLayout = card.AddComponent<LayoutElement>();
                cardLayout.preferredHeight = 120;

                Image cardImage = card.AddComponent<Image>();
                cardImage.color = new Color(0.18f, 0.18f, 0.22f, 1);

                Button cardBtn = card.AddComponent<Button>();
                cardBtn.onClick.AddListener(() => JoinRoom(room));

                GameObject titleObj = new GameObject("Title");
                titleObj.transform.SetParent(card.transform, false);
                RectTransform titleRect = titleObj.AddComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0, 1);
                titleRect.anchorMax = new Vector2(1, 1);
                titleRect.pivot = new Vector2(0, 1);
                titleRect.anchoredPosition = new Vector2(10, -10);
                titleRect.sizeDelta = new Vector2(-20, 30);

                TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
                titleText.text = $"<b>{room.DungeonName}</b> ({room.Status})";
                titleText.fontSize = 22;
                titleText.alignment = TextAlignmentOptions.TopLeft;
                titleText.color = Color.white;

                GameObject playersObj = new GameObject("Players");
                playersObj.transform.SetParent(card.transform, false);
                RectTransform playersRect = playersObj.AddComponent<RectTransform>();
                playersRect.anchorMin = new Vector2(0, 1);
                playersRect.anchorMax = new Vector2(1, 1);
                playersRect.pivot = new Vector2(0, 1);
                playersRect.anchoredPosition = new Vector2(10, -45);
                playersRect.sizeDelta = new Vector2(-20, 25);

                TextMeshProUGUI playersText = playersObj.AddComponent<TextMeshProUGUI>();
                playersText.text = $"Players: <color=#7FD8FF>{room.PlayerCount}</color>/{room.MaxPlayers}";
                playersText.fontSize = 18;
                playersText.alignment = TextAlignmentOptions.TopLeft;
                playersText.color = new Color(0.75f, 0.75f, 0.78f, 1);

                GameObject idObj = new GameObject("RoomID");
                idObj.transform.SetParent(card.transform, false);
                RectTransform idRect = idObj.AddComponent<RectTransform>();
                idRect.anchorMin = new Vector2(0, 1);
                idRect.anchorMax = new Vector2(1, 1);
                idRect.pivot = new Vector2(0, 1);
                idRect.anchoredPosition = new Vector2(10, -70);
                idRect.sizeDelta = new Vector2(-20, 20);

                TextMeshProUGUI idText = idObj.AddComponent<TextMeshProUGUI>();
                idText.text = $"<size=14>ID: {room.RoomId.Substring(0, Mathf.Min(8, room.RoomId.Length))}</size>";
                idText.fontSize = 16;
                idText.alignment = TextAlignmentOptions.TopLeft;
                idText.color = new Color(0.65f, 0.68f, 0.7f, 1);

                ColorBlock colors = cardBtn.colors;
                colors.normalColor = cardImage.color;
                colors.highlightedColor = cardImage.color * 1.2f;
                colors.pressedColor = cardImage.color * 0.85f;
                cardBtn.colors = colors;

                displayCards.Add(card);

                MelonLogger.Msg($"[GameManager] Room card created: {room.DungeonName} ({room.PlayerCount}/{room.MaxPlayers})");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[GameManager] Error creating room card: {ex.Message}");
            }
        }

        private void OnCreateNewSave()
        {
            try
            {
                MelonLogger.Msg($"[GameManager] Creating new save in slot {nextAvailableSlot}");
                OnSaveSelected(nextAvailableSlot);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error creating new save: {ex.Message}");
            }
        }

        private void OnSaveSelected(int slotId)
        {
            var save = allSaves.FirstOrDefault(s => s.SlotId == slotId);

            try
            {
                if (isSaveMode && loadTramUI != null)
                {
                    if (save == null)
                    {
                        MelonLogger.Warning($"[GameManager] Attempted to load non-existent save slot {slotId}");
                        return;
                    }

                    MelonLogger.Msg($"[GameManager] Loading save slot {slotId}");
                    var loadSaveMethod = typeof(MainMenu).GetMethod("LoadSaveAndCreateRoom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (loadSaveMethod != null && mainMenuInstance != null)
                    {
                        loadSaveMethod.Invoke(mainMenuInstance, new object[] { loadTramUI, slotId });
                        MelonLogger.Msg($"[GameManager] Invoked LoadSaveAndCreateRoom for slot {slotId}");
                    }

                    CloseUI();
                }
                else if (!isSaveMode && newTramUI != null)
                {
                    MelonLogger.Msg($"[GameManager] Creating new game in slot {slotId}");
                    var createNewGameMethod = typeof(MainMenu).GetMethod("CreateNewGameInSlot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new System.Type[] { typeof(UIPrefab_NewTram), typeof(int) }, null);

                    if (createNewGameMethod != null && mainMenuInstance != null)
                    {
                        createNewGameMethod.Invoke(mainMenuInstance, new object[] { newTramUI, slotId });
                        MelonLogger.Msg($"[GameManager] Invoked CreateNewGameInSlot for slot {slotId}");
                    }

                    CloseUI();
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error selecting save slot: {ex.Message}");
            }
        }

        private void JoinRoom(RoomInfo room)
        {
            try
            {
                MelonLogger.Msg($"[GameManager] JoinRoom called for: {room.DungeonName} (ID: {room.RoomId})");

                if (string.IsNullOrEmpty(room.RoomId))
                {
                    MelonLogger.Error("[GameManager] Room ID is empty!");
                    statusText.text = "Error: Invalid room ID";
                    return;
                }

                if (ulong.TryParse(room.RoomId, out ulong lobbyId))
                {
                    MelonLogger.Msg($"[GameManager] Parsed lobby ID: {lobbyId}");
                    CSteamID steamLobbyId = new CSteamID(lobbyId);

                    if (steamLobbyId.IsValid())
                    {
                        MelonLogger.Msg($"[GameManager] Steam lobby ID is valid, joining...");
                        SteamMatchmaking.JoinLobby(steamLobbyId);
                        statusText.text = $"Joining {room.DungeonName}...";
                        CloseUI();
                    }
                    else
                    {
                        MelonLogger.Error($"[GameManager] Invalid Steam lobby ID: {steamLobbyId}");
                        statusText.text = "Error: Invalid lobby ID";
                    }
                }
                else
                {
                    MelonLogger.Error($"[GameManager] Failed to parse room ID as ulong: {room.RoomId}");
                    statusText.text = "Error: Invalid room ID format";
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[GameManager] Error joining room: {ex.Message}");
                statusText.text = "Error joining room";
            }
        }

        private void PreviousPage()
        {
            if (currentPage > 0)
            {
                currentPage--;
                RefreshDisplay();
            }
        }

        private void NextPage()
        {
            int itemCount = currentTab == 0 ? allSaves.Count + (isSaveMode ? 0 : 1) : roomList.Count;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)itemCount / itemsPerPage));
            if (currentPage < totalPages - 1)
            {
                currentPage++;
                RefreshDisplay();
            }
        }

        private void CloseUI()
        {
            try
            {
                var uiman = FindObjectOfType<UIManager>();

                if (mainPanel != null)
                {
                    mainPanel.SetActive(false);
                }

                if (isSaveMode && loadTramUI != null)
                {
                    if (uiman != null)
                        uiman.ui_escapeStack.Remove(loadTramUI);
                }
                else if (!isSaveMode && newTramUI != null)
                {
                    if (uiman != null)
                        uiman.ui_escapeStack.Remove(newTramUI);
                }

                MelonLogger.Msg("[GameManager] UI Hidden");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Error closing UI: {ex.Message}");
            }
        }

        public void Update()
        {
            if (currentTab == 1 && Time.time - lastRefreshTime > RefreshInterval && !isSearching)
            {
                SearchForRooms();
                lastRefreshTime = Time.time;
            }
        }

        private void SearchForRooms()
        {
            try
            {
                if (!SteamManager.Initialized)
                {
                    MelonLogger.Warning("[GameManager] Steam not initialized");
                    if (statusText != null)
                        statusText.text = "Steam not initialized";
                    return;
                }

                if (isSearching)
                {
                    MelonLogger.Msg("[GameManager] Already searching, skipping request");
                    return;
                }

                if (statusText == null)
                {
                    MelonLogger.Warning("[GameManager] statusText is null, UI may be closed");
                    return;
                }

                isSearching = true;
                searchAttempts++;

                if (searchAttempts > MAX_SEARCH_ATTEMPTS)
                    searchAttempts = 0;

                MelonLogger.Msg($"[GameManager] Starting room search (attempt {searchAttempts})");
                statusText.text = "Searching for rooms...";

                SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);
                SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterClose);
                SteamMatchmaking.AddRequestLobbyListResultCountFilter(50);

                var searchHandle = SteamMatchmaking.RequestLobbyList();
                MelonLogger.Msg($"[GameManager] RequestLobbyList returned handle: {searchHandle}");

                if (lobbySearchResult != null)
                {
                    lobbySearchResult.Set(searchHandle, OnLobbySearchComplete);
                    MelonLogger.Msg("[GameManager] Callback registered");
                }
                else
                {
                    MelonLogger.Error("[GameManager] lobbySearchResult is NULL!");
                    isSearching = false;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[GameManager] Room search error: {ex.Message}");
                isSearching = false;
            }
        }

        private void OnLobbySearchComplete(LobbyMatchList_t result, bool bIOFailure)
        {
            try
            {
                MelonLogger.Msg($"[GameManager] OnLobbySearchComplete called - IOFailure: {bIOFailure}, Lobbies: {result.m_nLobbiesMatching}");

                if (bIOFailure)
                {
                    MelonLogger.Error("[GameManager] Lobby search IO failure!");
                    statusText.text = "Search failed - retrying...";
                    isSearching = false;
                    return;
                }

                cachedRooms.Clear();
                roomList.Clear();

                MelonLogger.Msg($"[GameManager] Processing {result.m_nLobbiesMatching} lobbies");

                for (int i = 0; i < result.m_nLobbiesMatching; i++)
                {
                    CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
                    MelonLogger.Msg($"[GameManager] Processing lobby {i}: {lobbyId}");

                    var roomInfo = ExtractRoomInfo(lobbyId);
                    if (roomInfo != null)
                    {
                        cachedRooms[roomInfo.RoomId] = roomInfo;
                        roomList.Add(roomInfo);
                        MelonLogger.Msg($"[GameManager] Added room: {roomInfo.DungeonName} ({roomInfo.PlayerCount}/{roomInfo.MaxPlayers})");
                    }
                    else
                    {
                        MelonLogger.Warning($"[GameManager] Failed to extract room info for lobby {i}");
                    }
                }

                roomList = roomList.OrderByDescending(r => r.PlayerCount).ToList();

                if (roomList.Count > 0)
                {
                    statusText.text = "Ready";
                    MelonLogger.Msg($"[GameManager] Found {roomList.Count} available rooms");
                }
                else
                {
                    statusText.text = "No rooms found - searching again...";
                    MelonLogger.Msg("[GameManager] No rooms found, will retry on next refresh");
                }

                RefreshRoomsDisplay();
                isSearching = false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[GameManager] Lobby search completion error: {ex.Message}");
                isSearching = false;
            }
        }

        private RoomInfo ExtractRoomInfo(CSteamID lobbyId)
        {
            try
            {
                string dungeonName = SteamMatchmaking.GetLobbyData(lobbyId, "DungeonName");
                string status = SteamMatchmaking.GetLobbyData(lobbyId, "Status");
                int playerCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);

                if (string.IsNullOrEmpty(dungeonName))
                {
                    dungeonName = $"Room {lobbyId.ToString().Substring(0, 8)}";
                }
                if (string.IsNullOrEmpty(status))
                {
                    status = "Waiting";
                }

                var roomInfo = new RoomInfo
                {
                    RoomId = lobbyId.m_SteamID.ToString(),
                    HostSteamId = SteamMatchmaking.GetLobbyOwner(lobbyId).ToString(),
                    PlayerCount = playerCount,
                    MaxPlayers = 4,
                    Status = status,
                    DungeonName = dungeonName,
                    CreatedTime = Time.time,
                };

                MelonLogger.Msg($"[GameManager] Extracted room: {dungeonName} | ID: {roomInfo.RoomId} | Players: {playerCount}/4 | Status: {status}");
                return roomInfo;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[GameManager] Error extracting room info: {ex.Message}");
                return null;
            }
        }

        private void LoadAllSaves()
        {
            allSaves.Clear();

            try
            {
                for (int i = 0; i < 10000; i++)
                {
                    string fileName = MMSaveGameData.GetSaveFileName(i);

                    try
                    {
                        if (MonoSingleton<PlatformMgr>.Instance.IsSaveFileExist(fileName))
                        {
                            var saveData = MonoSingleton<PlatformMgr>.Instance.Load<MMSaveGameData>(fileName);
                            if (saveData != null)
                            {
                                allSaves.Add(
                                    new SaveSlotData
                                    {
                                        SlotId = i,
                                        FileName = fileName,
                                        SaveData = saveData,
                                        LastModified = DateTime.Now,
                                    }
                                );
                                MelonLogger.Msg($"[GameManager] Loaded save from slot {i}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning($"Error loading save slot {i}: {ex.Message}");
                    }
                }

                allSaves = allSaves.OrderByDescending(s => s.LastModified).ToList();

                nextAvailableSlot = 0;
                foreach (var save in allSaves)
                {
                    if (save.SlotId >= nextAvailableSlot)
                        nextAvailableSlot = save.SlotId + 1;
                }

                MelonLogger.Msg($"[GameManager] Loaded {allSaves.Count} saves. Next available slot: {nextAvailableSlot}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Error loading saves: {ex.Message}");
            }
        }

        public void RefreshUI()
        {
            try
            {
                MelonLogger.Msg("[GameManager] RefreshUI called");

                if (mainPanel == null)
                {
                    MelonLogger.Error("[GameManager] mainPanel is null, cannot refresh");
                    return;
                }

                currentPage = 0;
                currentTab = 0;
                isSearching = false;

                LoadAllSaves();
                mainPanel.SetActive(true);
                RefreshDisplay();

                MelonLogger.Msg("[GameManager] RefreshUI completed - UI shown");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[GameManager] Error in RefreshUI: {ex.Message}");
            }
        }

        public void Cleanup()
        {
            try
            {
                allSaves.Clear();
                roomList.Clear();
                cachedRooms.Clear();
                if (lobbySearchResult != null)
                    lobbySearchResult = null;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[GameManager] Cleanup error: {ex.Message}");
            }
        }

        public class SaveSlotData
        {
            public int SlotId { get; set; }
            public string FileName { get; set; }
            public MMSaveGameData SaveData { get; set; }
            public DateTime LastModified { get; set; }
        }
    }

    public class RoomInfo
    {
        public string RoomId { get; set; } = "";
        public int PlayerCount { get; set; }
        public int MaxPlayers { get; set; }
        public string HostSteamId { get; set; } = "";
        public string Status { get; set; } = "Unknown";
        public string DungeonName { get; set; } = "Unknown";
        public float CreatedTime { get; set; }
    }

    public enum ButtonType
    {
        Primary,
        Secondary,
        Destructive,
    }
}
