using UnityEngine;
using UnityEngine.UI;
using MarbleSort.Core;
using MarbleSort.Config;
using MarbleSort.Systems;
using MarbleSort.Utils;

namespace MarbleSort.UI
{
    /// <summary>
    /// Builds and toggles every screen (MainMenu, LevelSelect, HUD, Win, Lose) in code under the
    /// UICanvas. Screens are plain child GameObjects switched with SetActive; the HUD binds to the
    /// live <see cref="GameState"/> for progress/slots. Author on the UICanvas
    /// (Canvas + CanvasScaler 1080x1920 + GraphicRaycaster); missing components are added defensively.
    /// </summary>
    public class UIController : MonoBehaviour
    {
        private GameManager _gm;
        private RectTransform _root;

        private GameObject _menu, _levelSelect, _hud, _win, _lose;
        private Text _hudLevel, _hudProgress, _hudSlots;
        private Image _slotsBar;
        private Transform _levelGrid;
        private Text _winTitle, _menuUnlocked;
        private Button _nextButton;
        private GameState _bound;

        public void Init(GameManager gm)
        {
            _gm = gm;
            EnsureCanvas();
            _root = GetComponent<RectTransform>();
            BuildMenu();
            BuildLevelSelect();
            BuildHud();
            BuildWin();
            BuildLose();
            HideAll();
        }

        private void EnsureCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }

        // ---- screen switching -------------------------------------------

        private void HideAll()
        {
            _menu.SetActive(false);
            _levelSelect.SetActive(false);
            _hud.SetActive(false);
            _win.SetActive(false);
            _lose.SetActive(false);
        }

        public void ShowMainMenu(int maxUnlocked, int levelCount)
        {
            Unbind();
            HideAll();
            _menuUnlocked.text = "Level " + Mathf.Min(maxUnlocked, Mathf.Max(1, levelCount)) + " / " + levelCount;
            _menu.SetActive(true);
        }

        public void ShowLevelSelect()
        {
            HideAll();
            RebuildLevelGrid();
            _levelSelect.SetActive(true);
        }

        public void ShowHud(GameManager gm)
        {
            HideAll();
            _hud.SetActive(true);
            BindState(gm.State);
        }

        public void ShowWin(GameManager gm)
        {
            _winTitle.text = gm.HasNextLevel ? "Level Complete!" : "All Levels Clear!";
            _nextButton.gameObject.SetActive(gm.HasNextLevel);
            _win.SetActive(true);
        }

        public void ShowLose(GameManager gm)
        {
            _lose.SetActive(true);
        }

        // ---- HUD binding -------------------------------------------------

        private void BindState(GameState s)
        {
            Unbind();
            _bound = s;
            if (_bound != null) _bound.StateChanged += RefreshHud;
            RefreshHud();
        }

        private void Unbind()
        {
            if (_bound != null) _bound.StateChanged -= RefreshHud;
            _bound = null;
        }

        private void RefreshHud()
        {
            if (_gm == null) return;
            _hudLevel.text = "Level " + _gm.CurrentLevel;
            if (_bound != null)
            {
                _hudSlots.text = "Belt " + _bound.ConveyorCount + "/" + _bound.ConveyorCapacity
                    + "   ✓ " + _bound.DeliveredBalls + "/" + _bound.TotalBalls;
                float fill = _bound.ConveyorCapacity > 0
                    ? _bound.ConveyorCount / (float)_bound.ConveyorCapacity : 0f;
                _slotsBar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fill), 1f);
                _slotsBar.color = Color.Lerp(Palette.Ball(BallColor.Green), Palette.Ball(BallColor.Red),
                    Mathf.Clamp01((fill - 0.5f) / 0.5f));
            }
        }

        private void OnDestroy() { Unbind(); }

        // ---- builders ----------------------------------------------------

        private GameObject Screen(string name)
        {
            var go = UIFactory.Node(name, transform);
            UIFactory.Fill(UIFactory.Rect(go));
            return go;
        }

        private void BuildMenu()
        {
            _menu = Screen("MainMenu");
            var bg = UIFactory.RawImage(_menu.transform, "Bg", SpriteFactory.Square(4), Palette.BackgroundBottom);
            UIFactory.Fill(UIFactory.Rect(bg.gameObject));

            var title = UIFactory.Label(_menu.transform, "Title", "MARBLE\nSORT", 130, Palette.UiText);
            UIFactory.Anchor(UIFactory.Rect(title.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(900, 320));

            var subtitle = UIFactory.Label(_menu.transform, "Sub", "tap a tile  ·  fill the bins  ·  don't jam the belt",
                34, Palette.UiTextDim);
            UIFactory.Anchor(UIFactory.Rect(subtitle.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -560f), new Vector2(980, 60));

            _menuUnlocked = UIFactory.Label(_menu.transform, "Unlocked", "Level 1 / 3", 40, Palette.UiTextDim);
            UIFactory.Anchor(UIFactory.Rect(_menuUnlocked.gameObject), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(600, 60));

            var play = UIFactory.Button(_menu.transform, "Play", "PLAY", Palette.UiButton, 60, 48);
            UIFactory.Anchor(UIFactory.Rect(play.gameObject), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(620, 180));
            play.onClick.AddListener(() => _gm.StartLevel(SaveSystem.MaxUnlockedLevel));

            var levels = UIFactory.Button(_menu.transform, "Levels", "LEVELS", Palette.UiButtonAlt, 48, 44);
            UIFactory.Anchor(UIFactory.Rect(levels.gameObject), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -180f), new Vector2(520, 150));
            levels.onClick.AddListener(ShowLevelSelect);
        }

        private void BuildLevelSelect()
        {
            _levelSelect = Screen("LevelSelect");
            var bg = UIFactory.RawImage(_levelSelect.transform, "Bg", SpriteFactory.Square(4), Palette.BackgroundBottom);
            UIFactory.Fill(UIFactory.Rect(bg.gameObject));

            var title = UIFactory.Label(_levelSelect.transform, "Title", "SELECT LEVEL", 64, Palette.UiText);
            UIFactory.Anchor(UIFactory.Rect(title.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -260f), new Vector2(900, 100));

            var gridGo = UIFactory.Node("Grid", _levelSelect.transform);
            UIFactory.Anchor(UIFactory.Rect(gridGo), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(900, 900));
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(200, 200);
            grid.spacing = new Vector2(30, 30);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            _levelGrid = gridGo.transform;

            var back = UIFactory.Button(_levelSelect.transform, "Back", "BACK", Palette.UiButtonAlt, 44, 40);
            UIFactory.Anchor(UIFactory.Rect(back.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 220f), new Vector2(420, 140));
            back.onClick.AddListener(() => _gm.ShowMenu());
        }

        private void RebuildLevelGrid()
        {
            for (int i = _levelGrid.childCount - 1; i >= 0; i--) Destroy(_levelGrid.GetChild(i).gameObject);
            int count = Mathf.Max(1, _gm.LevelCount);
            int maxUnlocked = SaveSystem.MaxUnlockedLevel;
            for (int n = 1; n <= count; n++)
            {
                bool unlocked = n <= maxUnlocked;
                int levelNumber = n;
                var btn = UIFactory.Button(_levelGrid, "Lvl" + n, unlocked ? n.ToString() : "🔒",
                    unlocked ? Palette.UiButton : Palette.UiPanelLight, 64, 36);
                if (unlocked) btn.onClick.AddListener(() => _gm.StartLevel(levelNumber));
                else btn.interactable = false;
            }
        }

        private void BuildHud()
        {
            _hud = Screen("HUD");

            // --- top row: shop | coins | level pill | gear + restart ---
            // Shop (left)
            var shop = UIFactory.Button(_hud.transform, "Shop", "Shop", Palette.UiShop, 34, 30);
            UIFactory.Anchor(UIFactory.Rect(shop.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(90f, -150f), new Vector2(150, 150));

            // Coin pill (purple-bordered light pill, gold coin)
            var coinPill = UIFactory.Panel(_hud.transform, "CoinPill", Palette.UiLevelPill, 30);
            UIFactory.Anchor(coinPill.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(275f, -150f), new Vector2(230, 96));
            var coinIcon = UIFactory.RawImage(coinPill.transform, "Coin", SpriteFactory.Ball(64), Palette.UiCoin);
            UIFactory.Anchor(coinIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(48f, 0f), new Vector2(62, 62));
            var coinTxt = UIFactory.Label(coinPill.transform, "Amt", "2,450", 40, Palette.UiText, TextAnchor.MiddleCenter);
            UIFactory.Anchor(UIFactory.Rect(coinTxt.gameObject), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(28f, 0f), new Vector2(150, 60));

            // Level pill (centre-right)
            var levelPill = UIFactory.Panel(_hud.transform, "LevelPill", Palette.UiLevelPill, 30);
            UIFactory.Anchor(levelPill.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-300f, -150f), new Vector2(300, 96));
            _hudLevel = UIFactory.Label(levelPill.transform, "Level", "Level 1", 42, Palette.UiText);
            UIFactory.Fill(UIFactory.Rect(_hudLevel.gameObject));

            // Gear + restart (right, purple) with procedural icons
            var gear = UIFactory.Button(_hud.transform, "Gear", "", Palette.UiLevelPill, 44, 30);
            UIFactory.Anchor(UIFactory.Rect(gear.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-90f, -150f), new Vector2(120, 120));
            var gIcon = UIFactory.RawImage(gear.transform, "Icon", SpriteFactory.Gear(96), Palette.UiText);
            UIFactory.Anchor(gIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(64, 64));
            gear.onClick.AddListener(OnMenuPressed);

            var restart = UIFactory.Button(_hud.transform, "Restart", "", Palette.UiLevelPill, 46, 30);
            UIFactory.Anchor(UIFactory.Rect(restart.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-90f, -290f), new Vector2(120, 120));
            var rIcon = UIFactory.RawImage(restart.transform, "Icon", SpriteFactory.Ring(64, 0.30f), Palette.UiText);
            UIFactory.Anchor(rIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(56, 56));
            restart.onClick.AddListener(() => _gm.RestartLevel());

            // --- booster row (decorative): magnet, knot-tool, gumball machine ---
            Sprite[] boosterIcons = { SpriteFactory.Magnet(96), SpriteFactory.KnotTool(96), SpriteFactory.Gumball(96) };
            for (int i = 0; i < 3; i++)
            {
                var b = UIFactory.Button(_hud.transform, "Booster" + i, "", Palette.UiBooster, 52, 34);
                UIFactory.Anchor(UIFactory.Rect(b.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2((i - 1) * 195f, -320f), new Vector2(150, 150));
                var icon = UIFactory.RawImage(b.transform, "Icon", boosterIcons[i], Color.white);
                UIFactory.Anchor(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(76, 76));
                // red count badge (bottom centre, like the reference)
                var badge = UIFactory.RawImage(b.transform, "Badge", SpriteFactory.Circle(48), Palette.UiBadge);
                UIFactory.Anchor(badge.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(50, 50));
                var bt = UIFactory.Label(badge.transform, "N", "1", 30, Palette.UiText);
                UIFactory.Fill(UIFactory.Rect(bt.gameObject));
            }

            // --- belt-capacity gauge (bottom, overflow warning) ---
            var barBgGo = UIFactory.Panel(_hud.transform, "SlotsBg", Palette.UiPanel, 20);
            UIFactory.Anchor(barBgGo.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(760, 72));

            var fillGo = UIFactory.Node("Fill", barBgGo.transform);
            var fillRt = UIFactory.Rect(fillGo);
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.offsetMin = new Vector2(7, 7);
            fillRt.offsetMax = new Vector2(-7, -7);
            _slotsBar = fillGo.AddComponent<Image>();
            _slotsBar.sprite = SpriteFactory.RoundedRectUI(64, 16);
            _slotsBar.type = Image.Type.Sliced;
            _slotsBar.color = Palette.Ball(BallColor.Green);

            _hudSlots = UIFactory.Label(barBgGo.transform, "SlotsLabel", "Belt 0 / 0", 30, Palette.UiText);
            UIFactory.Fill(UIFactory.Rect(_hudSlots.gameObject));
            _hudProgress = _hudSlots; // progress folded into the belt label
        }

        private void OnMenuPressed()
        {
            _gm.SetPaused(false);
            _gm.ShowMenu();
        }

        private void BuildWin()
        {
            _win = Screen("Win");
            UIFactory.Overlay(_win.transform, "Overlay");
            var panel = UIFactory.Panel(_win.transform, "Panel", Palette.UiPanel, 40);
            UIFactory.Anchor(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860, 900));

            _winTitle = UIFactory.Label(panel.transform, "Title", "Level Complete!", 66, Palette.UiText);
            UIFactory.Anchor(UIFactory.Rect(_winTitle.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(780, 120));

            var star = UIFactory.RawImage(panel.transform, "Star", SpriteFactory.Star(128), Palette.Ball(BallColor.Yellow));
            UIFactory.Anchor(star.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(220, 220));

            _nextButton = UIFactory.Button(panel.transform, "Next", "NEXT", Palette.UiButton, 56, 44);
            UIFactory.Anchor(UIFactory.Rect(_nextButton.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 350f), new Vector2(620, 160));
            _nextButton.onClick.AddListener(() => _gm.NextLevel());

            var retry = UIFactory.Button(panel.transform, "Retry", "REPLAY", Palette.UiButtonAlt, 44, 40);
            UIFactory.Anchor(UIFactory.Rect(retry.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-165f, 165f), new Vector2(290, 140));
            retry.onClick.AddListener(() => _gm.RestartLevel());

            var menu = UIFactory.Button(panel.transform, "Menu", "MENU", Palette.UiButtonAlt, 44, 40);
            UIFactory.Anchor(UIFactory.Rect(menu.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(165f, 165f), new Vector2(290, 140));
            menu.onClick.AddListener(() => _gm.ShowMenu());
        }

        private void BuildLose()
        {
            _lose = Screen("Lose");
            UIFactory.Overlay(_lose.transform, "Overlay");
            var panel = UIFactory.Panel(_lose.transform, "Panel", Palette.UiPanel, 40);
            UIFactory.Anchor(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860, 760));

            var title = UIFactory.Label(panel.transform, "Title", "Belt Jammed!", 64, Palette.UiButtonDanger);
            UIFactory.Anchor(UIFactory.Rect(title.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(780, 120));

            var msg = UIFactory.Label(panel.transform, "Msg", "The conveyor overflowed.\nTry a different order!",
                38, Palette.UiTextDim);
            UIFactory.Anchor(UIFactory.Rect(msg.gameObject), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(760, 200));

            var retry = UIFactory.Button(panel.transform, "Retry", "RETRY", Palette.UiButton, 56, 44);
            UIFactory.Anchor(UIFactory.Rect(retry.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(620, 160));
            retry.onClick.AddListener(() => _gm.RestartLevel());

            var menu = UIFactory.Button(panel.transform, "Menu", "MENU", Palette.UiButtonAlt, 44, 40);
            UIFactory.Anchor(UIFactory.Rect(menu.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 160f), new Vector2(420, 140));
            menu.onClick.AddListener(() => _gm.ShowMenu());
        }
    }
}
