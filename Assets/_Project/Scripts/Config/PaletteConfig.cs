using UnityEngine;
using MarbleSort.Core;

namespace MarbleSort.Config
{
    /// <summary>
    /// Every colour used by the game. ballColors is indexed by <see cref="BallColor"/> so recolouring
    /// is data-only. Edit Resources/Config/PaletteConfig.asset; read via the <see cref="Palette"/> facade.
    /// Tuned to the candy "colour hopper" look (teal backdrop, glossy candy tiles/marbles).
    /// </summary>
    [CreateAssetMenu(fileName = "PaletteConfig", menuName = "MarbleSort/Palette Config", order = 1)]
    public class PaletteConfig : ScriptableObject
    {
        [Header("Background (teal)")]
        public Color backgroundTop = new Color(0.30f, 0.62f, 0.68f, 1f);
        public Color backgroundBottom = new Color(0.28f, 0.58f, 0.64f, 1f);

        [Header("Marble / tile colours (indexed by BallColor enum)")]
        public Color[] ballColors = new Color[]
        {
            new Color(0.94f, 0.29f, 0.44f, 1f), // Red (raspberry pink)
            new Color(0.27f, 0.77f, 0.96f, 1f), // Blue (sky cyan)
            new Color(0.44f, 0.83f, 0.24f, 1f), // Green (candy green)
            new Color(1.00f, 0.84f, 0.23f, 1f), // Yellow
            new Color(0.63f, 0.36f, 0.89f, 1f), // Purple
            new Color(1.00f, 0.60f, 0.24f, 1f), // Orange
        };
        public Color ballHighlight = new Color(1f, 1f, 1f, 0.85f);
        public Color tileHidden = new Color(0.55f, 0.60f, 0.66f, 1f);

        [Header("Container tray (3D)")]
        public Color containerRim = new Color(0.49f, 0.79f, 0.83f, 1f);      // light teal border stroke
        public Color containerFrame = new Color(0.40f, 0.70f, 0.73f, 1f);
        public Color containerInner = new Color(0.22f, 0.51f, 0.58f, 1f);    // dark teal tray fill
        public Color containerCell = new Color(0.42f, 0.72f, 0.78f, 1f);     // empty grid-cell marker

        [Header("Mystery / numbered tiles")]
        public Color tileMystery = new Color(0.50f, 0.56f, 0.62f, 1f);
        public Color tileMysteryMark = new Color(0.76f, 0.80f, 0.85f, 1f);
        public Color tileNumberBar = new Color(0.17f, 0.22f, 0.30f, 1f);

        [Header("Lighting (3D)")]
        public Color lightColor = new Color(1f, 0.98f, 0.94f, 1f);
        public Color ambientColor = new Color(0.74f, 0.79f, 0.84f, 1f);

        [Header("Chute / funnel")]
        public Color hopperFrame = new Color(0.80f, 0.86f, 0.90f, 1f);
        public Color hopperInner = new Color(0.62f, 0.72f, 0.78f, 1f);
        public Color hopperWall = new Color(0.70f, 0.78f, 0.84f, 1f);

        [Header("Truck")]
        public Color truckBody = new Color(0.95f, 0.72f, 0.30f, 1f);
        public Color truckBin = new Color(0.36f, 0.42f, 0.52f, 1f);
        public Color truckCab = new Color(0.98f, 0.86f, 0.55f, 1f);
        public Color truckWindow = new Color(0.55f, 0.80f, 0.92f, 1f);
        public Color truckWheel = new Color(0.20f, 0.22f, 0.28f, 1f);

        [Header("Conveyor")]
        public Color conveyorBelt = new Color(0.25f, 0.31f, 0.45f, 1f);      // inner navy well
        public Color conveyorFrame = new Color(0.31f, 0.38f, 0.53f, 1f);     // navy housing
        public Color conveyorTread = new Color(0.42f, 0.46f, 0.55f, 1f);
        public Color roller = new Color(0.86f, 0.90f, 0.94f, 1f);
        public Color beltRail = new Color(0.78f, 0.83f, 0.87f, 1f);          // metallic centre rail
        public Color globe = new Color(0.35f, 0.70f, 0.85f, 1f);

        [Header("Destination bin")]
        public Color binFrame = new Color(0.56f, 0.64f, 0.20f, 1f);          // olive-green bin body
        public Color binSlat = new Color(0.68f, 0.77f, 0.27f, 1f);           // lighter front slats
        public Color binInner = new Color(0.27f, 0.34f, 0.10f, 1f);          // dark green interior
        public Color binCompleted = new Color(0.32f, 0.86f, 0.46f, 1f);

        [Header("UI")]
        public Color uiPanel = new Color(0.20f, 0.42f, 0.47f, 0.96f);
        public Color uiPanelLight = new Color(0.30f, 0.55f, 0.60f, 1f);
        public Color uiButton = new Color(0.36f, 0.62f, 0.98f, 1f);
        public Color uiButtonAlt = new Color(0.99f, 0.62f, 0.30f, 1f);
        public Color uiButtonDanger = new Color(0.95f, 0.40f, 0.44f, 1f);
        public Color uiText = new Color(0.99f, 0.99f, 1f, 1f);
        public Color uiTextDim = new Color(0.82f, 0.90f, 0.92f, 1f);
        public Color uiShadow = new Color(0f, 0.10f, 0.12f, 0.28f);
        public Color uiCoin = new Color(0.99f, 0.80f, 0.28f, 1f);
        public Color uiLevelPill = new Color(0.55f, 0.52f, 0.92f, 1f);
        public Color uiShop = new Color(0.96f, 0.44f, 0.60f, 1f);
        public Color uiBooster = new Color(0.42f, 0.80f, 0.38f, 1f);
        public Color uiBadge = new Color(0.93f, 0.26f, 0.30f, 1f);

        public Color BallColorOf(BallColor c)
        {
            int i = (int)c;
            if (ballColors != null && i >= 0 && i < ballColors.Length) return ballColors[i];
            return Color.magenta;
        }

        // ---- never-null access ------------------------------------------
        private static PaletteConfig _active;
        public static PaletteConfig Active
        {
            get
            {
                if (_active == null) _active = Resources.Load<PaletteConfig>("Config/PaletteConfig");
                if (_active == null) _active = CreateInstance<PaletteConfig>();
                return _active;
            }
        }
        public static void SetActive(PaletteConfig p) { if (p != null) _active = p; }
    }

    /// <summary>Static facade for colours.</summary>
    public static class Palette
    {
        public static PaletteConfig P { get { return PaletteConfig.Active; } }

        public static Color BackgroundTop { get { return P.backgroundTop; } }
        public static Color BackgroundBottom { get { return P.backgroundBottom; } }
        public static Color Ball(BallColor c) { return P.BallColorOf(c); }
        public static Color BallHighlight { get { return P.ballHighlight; } }
        public static Color TileHidden { get { return P.tileHidden; } }

        public static Color ContainerRim { get { return P.containerRim; } }
        public static Color ContainerFrame { get { return P.containerFrame; } }
        public static Color ContainerInner { get { return P.containerInner; } }
        public static Color ContainerCell { get { return P.containerCell; } }
        public static Color TileMystery { get { return P.tileMystery; } }
        public static Color TileMysteryMark { get { return P.tileMysteryMark; } }
        public static Color TileNumberBar { get { return P.tileNumberBar; } }
        public static Color LightColor { get { return P.lightColor; } }
        public static Color AmbientColor { get { return P.ambientColor; } }

        public static Color HopperFrame { get { return P.hopperFrame; } }
        public static Color HopperInner { get { return P.hopperInner; } }
        public static Color HopperWall { get { return P.hopperWall; } }

        public static Color TruckBody { get { return P.truckBody; } }
        public static Color TruckBin { get { return P.truckBin; } }
        public static Color TruckCab { get { return P.truckCab; } }
        public static Color TruckWindow { get { return P.truckWindow; } }
        public static Color TruckWheel { get { return P.truckWheel; } }

        public static Color ConveyorBelt { get { return P.conveyorBelt; } }
        public static Color ConveyorFrame { get { return P.conveyorFrame; } }
        public static Color ConveyorTread { get { return P.conveyorTread; } }
        public static Color Roller { get { return P.roller; } }
        public static Color BeltRail { get { return P.beltRail; } }
        public static Color Globe { get { return P.globe; } }

        public static Color BinFrame { get { return P.binFrame; } }
        public static Color BinSlat { get { return P.binSlat; } }
        public static Color BinInner { get { return P.binInner; } }
        public static Color BinCompleted { get { return P.binCompleted; } }

        public static Color UiPanel { get { return P.uiPanel; } }
        public static Color UiPanelLight { get { return P.uiPanelLight; } }
        public static Color UiButton { get { return P.uiButton; } }
        public static Color UiButtonAlt { get { return P.uiButtonAlt; } }
        public static Color UiButtonDanger { get { return P.uiButtonDanger; } }
        public static Color UiText { get { return P.uiText; } }
        public static Color UiTextDim { get { return P.uiTextDim; } }
        public static Color UiShadow { get { return P.uiShadow; } }
        public static Color UiCoin { get { return P.uiCoin; } }
        public static Color UiLevelPill { get { return P.uiLevelPill; } }
        public static Color UiShop { get { return P.uiShop; } }
        public static Color UiBooster { get { return P.uiBooster; } }
        public static Color UiBadge { get { return P.uiBadge; } }
    }
}
