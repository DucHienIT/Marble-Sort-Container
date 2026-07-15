using System;
using System.Collections.Generic;

namespace MarbleSort.Core
{
    /// <summary>
    /// Data-transfer objects matching the level JSON schema (Resources/Levels/level_NNN.json).
    /// Parsed with UnityEngine.JsonUtility. The source is a set of trucks, each carrying a list of
    /// marble colours; tapping a truck dumps all of them down the chute onto the belt.
    /// </summary>
    [Serializable]
    public class TruckJson
    {
        public string id;
        public string[] balls;
    }

    [Serializable]
    public class DestinationJson
    {
        public string color;
        public int capacity;
    }

    [Serializable]
    public class LevelJson
    {
        public int levelId;
        public int conveyorCapacity;
        public int activeDestinationCount;
        public TruckJson[] trucks;
        public DestinationJson[] destinationQueue;
    }

    /// <summary>Typed truck: an ordered list of marble colours it carries.</summary>
    public class TruckDefinition
    {
        public string Id;
        public List<BallColor> Balls = new List<BallColor>();
    }

    /// <summary>Typed destination entry (colour + capacity).</summary>
    public class BoxDefinition
    {
        public BallColor Color;
        public int Capacity;
    }

    /// <summary>
    /// Validated, typed level. Built from <see cref="LevelJson"/>. Exposes helper counts so the
    /// generator/tests and the runtime share one notion of "is this level well-formed".
    /// </summary>
    public class LevelDefinition
    {
        public int LevelId;
        public int ConveyorCapacity;
        public int ActiveDestinationCount;
        public List<TruckDefinition> Trucks = new List<TruckDefinition>();
        public List<BoxDefinition> DestinationQueue = new List<BoxDefinition>();

        public int TotalBalls
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Trucks.Count; i++) n += Trucks[i].Balls.Count;
                return n;
            }
        }

        public static LevelDefinition FromJson(LevelJson j)
        {
            var def = new LevelDefinition
            {
                LevelId = j.levelId,
                ConveyorCapacity = j.conveyorCapacity,
                ActiveDestinationCount = j.activeDestinationCount,
            };
            if (j.trucks != null)
            {
                foreach (var t in j.trucks)
                {
                    var td = new TruckDefinition { Id = t.id };
                    if (t.balls != null)
                        foreach (var b in t.balls) td.Balls.Add(BallColorUtil.Parse(b));
                    def.Trucks.Add(td);
                }
            }
            if (j.destinationQueue != null)
            {
                foreach (var d in j.destinationQueue)
                    def.DestinationQueue.Add(new BoxDefinition { Color = BallColorUtil.Parse(d.color), Capacity = d.capacity });
            }
            return def;
        }

        /// <summary>Distinct ball colours present across all trucks (used to size the active box set).</summary>
        public List<BallColor> DistinctColors()
        {
            var seen = new List<BallColor>();
            foreach (var t in Trucks)
                foreach (var b in t.Balls)
                    if (!seen.Contains(b)) seen.Add(b);
            return seen;
        }

        /// <summary>
        /// A level is winnable-by-totals when, for every colour, the sum of destination
        /// capacities equals the number of marbles of that colour. Returns null if valid,
        /// else a human-readable reason. (Overflow loss is still possible — that's the game.)
        /// </summary>
        public string ValidateTotals()
        {
            var ballCount = new int[BallColorUtil.Count];
            var boxCap = new int[BallColorUtil.Count];
            foreach (var t in Trucks)
                foreach (var b in t.Balls) ballCount[(int)b]++;
            foreach (var d in DestinationQueue) boxCap[(int)d.Color] += d.Capacity;
            for (int c = 0; c < BallColorUtil.Count; c++)
            {
                if (ballCount[c] != boxCap[c])
                    return string.Format("Colour {0}: {1} marbles but {2} box capacity",
                        (BallColor)c, ballCount[c], boxCap[c]);
            }
            return null;
        }
    }
}
