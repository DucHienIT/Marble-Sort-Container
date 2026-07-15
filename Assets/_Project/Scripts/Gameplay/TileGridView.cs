using System.Collections.Generic;
using UnityEngine;
using MarbleSort.Core;
using MarbleSort.Config;
using MarbleSort.Utils;

namespace MarbleSort.Gameplay
{
    /// <summary>
    /// The candy tile grid + its container tray + the funnel down to the belt, matching the
    /// reference mock-up: tiles sit in a *diamond* arrangement (rows grow 2→4→6→7…) inside a
    /// stepped teal tray whose light border hugs the grid silhouette. The top row is all grey
    /// covers, a numbered blocker + flanking empty holes sit low in the grid, and the diamond
    /// tapers to a 1–4 tile tip that stands *inside* the funnel mouth. Unused cells show lighter
    /// markers (also revealed when a tile pours away). References Tile.prefab.
    /// </summary>
    public class TileGridView : MonoBehaviour
    {
        [Header("Grid layout (world units)")]
        public float gridCenterY = 0.65f;
        public int cols = 7;
        public float tileSize = 0.9f;
        public float gap = 0.1f;
        public int maxStack = 4;

        [Header("Container tray")]
        public float trayBorder = 0.24f;   // light-teal stroke thickness beyond the cells
        public float trayDepth = 0.55f;

        [Header("Chute / funnel")]
        public float throatHalfWidth = 0.55f;
        public float wallThickness = 0.42f;

        [SerializeField] private TileView tilePrefab;

        private ISourceHost _host;
        private float Spacing { get { return tileSize + gap; } }

        private struct Cell { public int Row; public int Col; public int RowWidth; public Vector3 Pos; }

        public void Build(LevelDefinition level, ISourceHost host, Vector2 spout)
        {
            _host = host;
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);

            var stacks = BuildStacks(level);
            string[] coverLabels = BuildCoverLabels(stacks.Count);
            int content = stacks.Count + coverLabels.Length;

            // pick the inner-hole count so the bottom row ends up only partially filled (tapered tip)
            int holes = 2;
            var cells = BuildDiamondCells(content + holes);
            for (int h = 2; h <= 5; h++)
            {
                var candidate = BuildDiamondCells(content + h);
                int lastW = candidate[candidate.Count - 1].RowWidth;
                int occ = content + h - (candidate.Count - lastW);
                if (occ >= 1 && occ <= lastW - 2) { holes = h; cells = candidate; break; }
            }

            var coverCells = new List<int>();
            var stackCells = new List<int>();
            var emptyCells = new List<int>();
            AssignCells(cells, coverLabels.Length, stacks.Count, holes, coverCells, stackCells, emptyCells);

            // trim last-row empties out of the tray so the diamond tapers into the funnel
            int lastRow = cells[cells.Count - 1].Row;
            var trimmed = new HashSet<int>();
            foreach (int i in emptyCells) if (cells[i].Row == lastRow) trimmed.Add(i);

            // tray bounds + widest row from the remaining diamond cells
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            int maxRowW = 1;
            for (int i = 0; i < cells.Count; i++)
            {
                if (trimmed.Contains(i)) continue;
                minX = Mathf.Min(minX, cells[i].Pos.x); maxX = Mathf.Max(maxX, cells[i].Pos.x);
                minY = Mathf.Min(minY, cells[i].Pos.y); maxY = Mathf.Max(maxY, cells[i].Pos.y);
                maxRowW = Mathf.Max(maxRowW, cells[i].RowWidth);
            }

            float bottomTrayW = BuildTray(cells, trimmed, lastRow);
            // funnel mouth opens at the bottom edge of the second-to-last row; the taper tiles
            // stand inside it (like the reference's bottom tile sitting in the marble pool)
            float funnelTopY = minY + Spacing * 0.5f;
            float mouthW = Mathf.Clamp(bottomTrayW * 0.68f, 2.4f, 5.6f);
            BuildFunnel(funnelTopY, mouthW, spout);

            for (int i = 0; i < stackCells.Count; i++)
            {
                var tile = Spawn("Tile" + i);
                bool bubble = (i % 3 == 1);   // reference mixes ~1/3 pop-it tiles among flat candy tiles
                tile.InitCandy(i, stacks[i].Color, stacks[i].Balls, _host, tileSize, cells[stackCells[i]].Pos, bubble);
            }
            for (int j = 0; j < coverCells.Count; j++)
            {
                string lbl = coverLabels[j];
                var tile = Spawn("Cover" + j);
                tile.InitCover(1000 + j, tileSize, cells[coverCells[j]].Pos, lbl, lbl != "?");
            }
        }

        private TileView Spawn(string name)
        {
            TileView tile;
            if (tilePrefab != null) tile = Instantiate(tilePrefab, transform);
            else { var go = new GameObject(name); go.transform.SetParent(transform, false); tile = go.AddComponent<TileView>(); }
            tile.name = name;
            return tile;
        }

        private struct Stack { public BallColor Color; public List<BallColor> Balls; }

        /// <summary>Group every marble by colour, then emit tiles round-robin across colours (so the
        /// grid reads as a colourful mix rather than sorted blocks). Each tile holds up to maxStack.</summary>
        private List<Stack> BuildStacks(LevelDefinition level)
        {
            var counts = new int[BallColorUtil.Count];
            foreach (var truck in level.Trucks)
                foreach (var b in truck.Balls) counts[(int)b]++;

            var result = new List<Stack>();
            bool any = true;
            while (any)
            {
                any = false;
                for (int c = 0; c < BallColorUtil.Count; c++)
                {
                    if (counts[c] <= 0) continue;
                    int take = Mathf.Min(maxStack, counts[c]);
                    counts[c] -= take;
                    var balls = new List<BallColor>();
                    for (int k = 0; k < take; k++) balls.Add((BallColor)c);
                    result.Add(new Stack { Color = (BallColor)c, Balls = balls });
                    any = true;
                }
            }
            return result;
        }

        private static string[] BuildCoverLabels(int stackCount)
        {
            // index 2 lands on the inner blocker above the taper; index 6 on a row edge — both "3"
            string[] pat = { "?", "?", "3", "?", "?", "?", "3", "?" };
            int n = Mathf.Clamp(stackCount / 2, 4, 8);
            var result = new string[n];
            for (int i = 0; i < n; i++) result[i] = pat[i % pat.Length];
            return result;
        }

        /// <summary>Diamond-shaped cell layout: rows grow 2→4→6… (capped at cols; even rows sit
        /// half-offset), centred on x=0, generated top-down until at least
        /// <paramref name="minCells"/> cells exist.</summary>
        private List<Cell> BuildDiamondCells(int minCells)
        {
            var widths = new List<int>();
            int sum = 0, w = 2;
            while (sum < minCells)
            {
                widths.Add(w);
                sum += w;
                w = Mathf.Min(cols, w + 2);
            }

            float topY = gridCenterY + (widths.Count - 1) * Spacing * 0.5f;
            var cells = new List<Cell>();
            for (int r = 0; r < widths.Count; r++)
            {
                int rw = widths[r];
                for (int c = 0; c < rw; c++)
                {
                    float x = (c - (rw - 1) * 0.5f) * Spacing;
                    cells.Add(new Cell { Row = r, Col = c, RowWidth = rw, Pos = new Vector3(x, topY - r * Spacing, 0f) });
                }
            }
            return cells;
        }

        /// <summary>Covers claim the whole top row, then an inner blocker above the taper, then row
        /// edges top-down. Inner holes flank the blocker. Stacks fill the rest reading-order with
        /// the final row filling centre-out so leftover empties taper the diamond symmetrically.</summary>
        private void AssignCells(List<Cell> cells, int coverCount, int stackCount, int holeCount,
            List<int> coverCells, List<int> stackCells, List<int> emptyCells)
        {
            var taken = new bool[cells.Count];
            int bottomRow = cells[cells.Count - 1].Row;

            // cover priority: top row → inner blocker → row edges (never the taper rows)
            var coverOrder = new List<int>();
            for (int i = 0; i < cells.Count; i++) if (cells[i].Row == 0) coverOrder.Add(i);
            for (int i = 0; i < cells.Count; i++)
                if (cells[i].Row == bottomRow - 1 && cells[i].Col == cells[i].RowWidth / 2) coverOrder.Add(i);
            for (int i = 0; i < cells.Count; i++)
            {
                var c = cells[i];
                if (c.Row == 0 || c.Row >= bottomRow - 1) continue;
                if (c.Col == 0 || c.Col == c.RowWidth - 1) coverOrder.Add(i);
            }
            for (int k = 0; k < coverCount && k < coverOrder.Count; k++)
            {
                coverCells.Add(coverOrder[k]);
                taken[coverOrder[k]] = true;
            }

            // inner holes: flank the blocker, then centre / left of the row above
            var holeOrder = new List<int>();
            for (int i = 0; i < cells.Count; i++)
                if (cells[i].Row == bottomRow - 1 &&
                    (cells[i].Col == cells[i].RowWidth / 2 - 1 || cells[i].Col == cells[i].RowWidth / 2 + 1))
                    holeOrder.Add(i);
            for (int i = 0; i < cells.Count; i++)
                if (cells[i].Row == bottomRow - 2 && cells[i].Col == cells[i].RowWidth / 2) holeOrder.Add(i);
            for (int i = 0; i < cells.Count; i++)
                if (cells[i].Row == bottomRow - 2 && cells[i].Col == 1) holeOrder.Add(i);
            int placed = 0;
            foreach (int i in holeOrder)
            {
                if (placed >= holeCount) break;
                if (taken[i]) continue;
                taken[i] = true;
                emptyCells.Add(i);
                placed++;
            }

            // stacks: reading order, but the last row centre-out
            var order = new List<int>();
            for (int i = 0; i < cells.Count; i++) if (cells[i].Row != bottomRow) order.Add(i);
            var last = new List<int>();
            for (int i = 0; i < cells.Count; i++) if (cells[i].Row == bottomRow) last.Add(i);
            last.Sort((a, b) => Mathf.Abs(cells[a].Pos.x).CompareTo(Mathf.Abs(cells[b].Pos.x)));
            order.AddRange(last);

            foreach (int i in order)
            {
                if (taken[i]) continue;
                if (stackCells.Count < stackCount) { stackCells.Add(i); taken[i] = true; }
            }
            for (int i = 0; i < cells.Count; i++) if (!taken[i]) emptyCells.Add(i);
        }

        // ---- tray ---------------------------------------------------------

        /// <summary>
        /// The tray is one enclosing vessel like the reference: per-ROW slabs (light border behind
        /// a dark fill) whose widths follow a *smoothed* profile — starting a couple of cells wider
        /// than the top tile row and growing ~1.2 cells per row up to the widest row — so the tile
        /// diamond floats inside with a dark margin all around. The top row gets an extra arch.
        /// Bottom-taper cells stand inside the funnel (marker only). Returns the width of the
        /// lowest tray row so the funnel mouth can follow it.
        /// </summary>
        private float BuildTray(List<Cell> cells, HashSet<int> trimmed, int lastRow)
        {
            float S = Spacing;

            // collect per-row tile width + centre y (rows are centred on x = 0)
            var rowW = new Dictionary<int, int>();
            var rowY = new Dictionary<int, float>();
            int maxRowW = 1;
            for (int i = 0; i < cells.Count; i++)
            {
                var c = cells[i];
                if (c.Row == lastRow) continue;   // taper row lives inside the funnel
                rowW[c.Row] = c.RowWidth;
                rowY[c.Row] = c.Pos.y;
                maxRowW = Mathf.Max(maxRowW, c.RowWidth);
            }
            if (rowW.Count == 0) return 3f * S;

            int topTileW = rowW.ContainsKey(0) ? rowW[0] : 2;
            float bottomW = 3f * S;
            foreach (var kv in rowW)
            {
                int r = kv.Key;
                // smoothed container profile (in cells): wider than the tiles, gentle steps
                float cw = Mathf.Min(maxRowW + 0.8f, topTileW + 2.3f + 1.2f * r) * S;
                float extraTop = (r == 0) ? 0.45f : 0f;   // arched head room above the top row
                float y = rowY[r] + extraTop * 0.5f;
                float jz = r * 0.003f;   // per-row z offset so coplanar overlaps don't flicker
                AddBox("TrayRim" + r, new Vector3(0f, y, 0.66f + jz),
                    new Vector3(cw + trayBorder * 2f, S + trayBorder * 2f + extraTop, 0.3f), 0.3f,
                    Mat3D.Get(Palette.ContainerRim, 0.5f, 0f, 0.3f), 5, false);   // slight glow keeps the stroke light
                AddBox("TrayFill" + r, new Vector3(0f, y, 0.54f + jz),
                    new Vector3(cw, S + 0.14f + extraTop, 0.24f), 0.18f,
                    Mat3D.Matte(Palette.ContainerInner), 5, false);
                if (r == lastRow - 1) bottomW = cw;
            }

            // a light marker on every cell — visible on empty cells and revealed when a tile clears
            for (int i = 0; i < cells.Count; i++)
            {
                if (trimmed.Contains(i)) continue;
                Vector3 p = cells[i].Pos;
                AddBox("CellMark" + i, new Vector3(p.x, p.y, 0.40f + (i % 9) * 0.002f),
                    new Vector3(tileSize * 0.88f, tileSize * 0.88f, 0.08f), 0.15f,
                    Mat3D.Matte(Palette.ContainerCell), 4, false);
            }
            return bottomW;
        }

        private MeshRenderer AddBox(string name, Vector3 pos, Vector3 size, float radius, Material mat, int res, bool castShadow = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = MeshFactory.RoundedBox(size, radius, res);
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = castShadow
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
            return mr;
        }

        // ---- funnel --------------------------------------------------------

        /// <summary>The tray bottom converges into a funnel where poured marbles pool: a dark
        /// tapered interior (backdrop for the pool + the taper tiles) with thick light walls
        /// continuing the tray border, ending in a short spout above the belt.</summary>
        private void BuildFunnel(float topY, float mouthW, Vector2 spout)
        {
            float throatY = spout.y + 0.25f;
            float h = topY - throatY;
            if (h < 0.6f) h = 0.6f;
            float throatW = throatHalfWidth * 2f;

            // dark interior — behind the tray cell slabs so taper tiles/markers sit on it
            var body = new GameObject("FunnelBody");
            body.transform.SetParent(transform, false);
            body.transform.position = new Vector3(spout.x, throatY + h * 0.5f, 0.78f);
            var bmf = body.AddComponent<MeshFilter>();
            var bmr = body.AddComponent<MeshRenderer>();
            bmf.sharedMesh = MeshFactory.TaperedBox(mouthW, throatW + wallThickness, h, 0.35f);
            bmr.sharedMaterial = Mat3D.Matte(Palette.ContainerInner);

            // short throat interior down to the spout
            AddBox("FunnelNeck", new Vector3(spout.x, (throatY + spout.y) * 0.5f, 0.78f),
                new Vector3(throatW + wallThickness, throatY - spout.y + 0.3f, 0.35f), 0.1f,
                Mat3D.Matte(Palette.ContainerInner), 3, false);

            // thick light walls along the slanted edges + short spout guides
            Beam("FunnelLeft", new Vector2(spout.x - mouthW * 0.5f, topY + 0.25f),
                new Vector2(spout.x - throatHalfWidth - wallThickness * 0.5f, throatY));
            Beam("FunnelRight", new Vector2(spout.x + mouthW * 0.5f, topY + 0.25f),
                new Vector2(spout.x + throatHalfWidth + wallThickness * 0.5f, throatY));
            Beam("GuideLeft", new Vector2(spout.x - throatHalfWidth - wallThickness * 0.5f, throatY),
                new Vector2(spout.x - throatHalfWidth - wallThickness * 0.5f, spout.y));
            Beam("GuideRight", new Vector2(spout.x + throatHalfWidth + wallThickness * 0.5f, throatY),
                new Vector2(spout.x + throatHalfWidth + wallThickness * 0.5f, spout.y));
        }

        private void Beam(string name, Vector2 a, Vector2 b)
        {
            Vector2 mid = (a + b) * 0.5f;
            Vector2 dir = b - a;
            float len = Mathf.Max(0.2f, dir.magnitude) + wallThickness * 0.8f;
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(mid.x, mid.y, 0.45f);
            go.transform.rotation = Quaternion.Euler(0, 0, ang);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = MeshFactory.RoundedBox(new Vector3(len, wallThickness, trayDepth * 0.9f), wallThickness * 0.45f, 4);
            mr.sharedMaterial = Mat3D.Get(Palette.ContainerRim, 0.5f, 0f, 0.3f);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }
}
