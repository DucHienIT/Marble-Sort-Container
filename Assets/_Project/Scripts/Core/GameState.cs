using System;
using System.Collections.Generic;

namespace MarbleSort.Core
{
    public enum LevelStatus { Playing, Won, LostOverflow }

    public enum BoxState { Active, Completed }

    /// <summary>Runtime state of one destination box occupying a slot.</summary>
    public class BoxModel
    {
        public BallColor Color;
        public int Capacity;
        public int Filled;
        public BoxState State = BoxState.Active;
        public bool IsFull { get { return Filled >= Capacity; } }
    }

    /// <summary>
    /// Deterministic authority for a level. Physics (truck spill / ramp slide) is cosmetic;
    /// every win/lose decision and every ball's fate is decided here by integer bookkeeping.
    ///
    /// Conservation invariant at all times:
    ///   TotalBalls == BallsInTrucks + BallsInFlight + ConveyorCount + DeliveredBalls
    ///
    /// The view layer drives three transitions: ReleaseBall (truck -> flight),
    /// TryIntake (flight -> conveyor, may overflow = lose), TryDeliver (conveyor -> box).
    /// </summary>
    public class GameState
    {
        public readonly LevelDefinition Level;
        public LevelStatus Status { get; private set; }

        public int TotalBalls { get; private set; }
        public int BallsInTrucks { get; private set; }
        public int BallsInFlight { get; private set; }
        public int ConveyorCount { get; private set; }
        public int DeliveredBalls { get; private set; }

        public int ConveyorCapacity { get { return Level.ConveyorCapacity; } }
        public int FreeSlots { get { return Level.ConveyorCapacity - ConveyorCount; } }

        // Active box slots (fixed positions in the view); index 0..ActiveSlotCount-1.
        public int ActiveSlotCount { get; private set; }
        private readonly BoxModel[] _activeBoxes;
        private readonly List<BoxModel> _pending = new List<BoxModel>();

        // Events (view subscribes; no MonoBehaviour here).
        public event Action StateChanged;
        public event Action<int, BoxModel> BoxActivated;   // slot, box (box may be null => slot emptied)
        public event Action<int, BoxModel> BoxCompleted;   // slot, completed box
        public event Action<int, BallColor> BallDelivered; // slot, colour (for view fx)
        public event Action Won;
        public event Action LostOverflow;

        public GameState(LevelDefinition level)
        {
            Level = level;
            TotalBalls = level.TotalBalls;
            BallsInTrucks = TotalBalls;
            Status = LevelStatus.Playing;

            // Build the pending queue, then fill active slots keeping colour coverage.
            foreach (var d in level.DestinationQueue)
                _pending.Add(new BoxModel { Color = d.Color, Capacity = d.Capacity });

            // Slots come straight from the level (the reference game runs 4 bins with 6+ colours):
            // marbles whose colour has no active box simply keep circulating on the belt until a
            // matching box rotates in. Box activation still *prefers* uncovered colours, so the
            // belt drains as fast as the queue allows; the loss condition remains intake overflow.
            ActiveSlotCount = Math.Max(1, level.ActiveDestinationCount);
            _activeBoxes = new BoxModel[ActiveSlotCount];
            for (int s = 0; s < ActiveSlotCount; s++)
                _activeBoxes[s] = TakeNextBox(coverageColorsMissing: null);
        }

        public BoxModel ActiveBox(int slot)
        {
            return (slot >= 0 && slot < ActiveSlotCount) ? _activeBoxes[slot] : null;
        }

        // ---- transitions -------------------------------------------------

        /// <summary>A marble has left the hopper (a tile released it) and begun sliding.</summary>
        public void ReleaseBall()
        {
            if (BallsInTrucks > 0) BallsInTrucks--;
            BallsInFlight++;
            RaiseChanged();
        }

        /// <summary>
        /// A sliding ball reached the intake zone. If the conveyor is full this is an overflow
        /// (the player mistimed a tap) -> immediate loss. Otherwise the ball takes a slot.
        /// </summary>
        public bool TryIntake(BallColor color)
        {
            if (Status != LevelStatus.Playing) return false;
            if (ConveyorCount >= Level.ConveyorCapacity)
            {
                Status = LevelStatus.LostOverflow;
                RaiseChanged();
                var l = LostOverflow; if (l != null) l();
                return false;
            }
            if (BallsInFlight > 0) BallsInFlight--;
            ConveyorCount++;
            RaiseChanged();
            return true;
        }

        /// <summary>
        /// A circulating ball is offered for delivery. Returns the active slot it was absorbed
        /// into, or -1 if no active box currently matches (ball keeps circulating).
        /// </summary>
        public int TryDeliver(BallColor color)
        {
            if (Status != LevelStatus.Playing) return -1;
            for (int s = 0; s < ActiveSlotCount; s++)
            {
                var b = _activeBoxes[s];
                if (b != null && !b.IsFull && b.Color == color)
                    return TryDeliverToSlot(s, color) ? s : -1;
            }
            return -1;
        }

        /// <summary>Deliver a ball to a specific gate slot (used when a ball passes that gate).
        /// Returns false if the slot's box doesn't match / is full / game over.</summary>
        public bool TryDeliverToSlot(int slot, BallColor color)
        {
            if (Status != LevelStatus.Playing) return false;
            if (slot < 0 || slot >= ActiveSlotCount) return false;
            var check = _activeBoxes[slot];
            if (check == null || check.IsFull || check.Color != color) return false;

            var box = _activeBoxes[slot];
            box.Filled++;
            if (ConveyorCount > 0) ConveyorCount--;
            DeliveredBalls++;
            _deliveredByColor[(int)color]++;

            var bd = BallDelivered; if (bd != null) bd(slot, color);

            if (box.IsFull)
            {
                box.State = BoxState.Completed;
                var bc = BoxCompleted; if (bc != null) bc(slot, box);
                // Replace with next box, preferring colours currently uncovered.
                _activeBoxes[slot] = TakeNextBox(coverageColorsMissing: MissingColors(slot));
                var ba = BoxActivated; if (ba != null) ba(slot, _activeBoxes[slot]);
            }

            RaiseChanged();
            CheckWin();
            return true;
        }

        // ---- helpers -----------------------------------------------------

        /// <summary>
        /// Pull the next pending box. If <paramref name="coverageColorsMissing"/> is provided,
        /// prefer a box whose colour is not currently active (keeps every live colour covered,
        /// which is what prevents a soft-lock — the only real loss is overflow).
        /// </summary>
        private BoxModel TakeNextBox(List<BallColor> coverageColorsMissing)
        {
            if (_pending.Count == 0) return null;
            int pick = -1;
            if (coverageColorsMissing != null && coverageColorsMissing.Count > 0)
            {
                for (int i = 0; i < _pending.Count; i++)
                {
                    if (coverageColorsMissing.Contains(_pending[i].Color)) { pick = i; break; }
                }
            }
            if (pick < 0) pick = 0; // FIFO fallback
            var box = _pending[pick];
            _pending.RemoveAt(pick);
            return box;
        }

        /// <summary>Colours that still have balls to deliver but are not covered by an active box
        /// (ignoring the slot being refilled).</summary>
        private List<BallColor> MissingColors(int excludeSlot)
        {
            var remaining = RemainingByColor();
            var covered = new bool[BallColorUtil.Count];
            for (int s = 0; s < ActiveSlotCount; s++)
            {
                if (s == excludeSlot) continue;
                var b = _activeBoxes[s];
                if (b != null && !b.IsFull) covered[(int)b.Color] = true;
            }
            var missing = new List<BallColor>();
            for (int c = 0; c < BallColorUtil.Count; c++)
                if (remaining[c] > 0 && !covered[c]) missing.Add((BallColor)c);
            return missing;
        }

        /// <summary>Balls of each colour not yet delivered (in trucks + in flight + on conveyor).</summary>
        private int[] RemainingByColor()
        {
            var total = new int[BallColorUtil.Count];
            foreach (var t in Level.Trucks)
                foreach (var b in t.Balls) total[(int)b]++;
            for (int c = 0; c < BallColorUtil.Count; c++)
                total[c] -= _deliveredByColor[c];
            return total;
        }

        private readonly int[] _deliveredByColor = new int[BallColorUtil.Count];

        private void CheckWin()
        {
            if (Status != LevelStatus.Playing) return;
            if (DeliveredBalls >= TotalBalls)
            {
                Status = LevelStatus.Won;
                RaiseChanged();
                var w = Won; if (w != null) w();
            }
        }

        private void RaiseChanged()
        {
            var sc = StateChanged; if (sc != null) sc();
        }
    }
}
