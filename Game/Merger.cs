using System;
using System.Collections.Generic;
using Alta.Chunks;
using Alta.Inventory;
using Alta.Networking;
using UnityEngine;

namespace ItemsToBags
{
    // Runs on the server. Looks at what everyone is holding, and when someone holds two of the same item together
    // long enough, puts them both in a new pouch.
    public static class Merger
    {
        const float Tick = 0.1f;

        static readonly Dictionary<long, ContactTimer> Timers = new Dictionary<long, ContactTimer>();
        static readonly List<Pickup> Held = new List<Pickup>();
        static readonly List<long> Drop = new List<long>();
        static float _next;
        public static int Made;

        public static void Update()
        {
            if (!Main.Settings.Enabled) { Timers.Clear(); return; }
            float now = Time.unscaledTime;
            if (now < _next) return;
            float dt = _next == 0f ? Tick : Mathf.Min(now - _next + Tick, 0.5f);
            _next = now + Tick;

            CollectHeld();
            float hold = (float)Main.Settings.HoldSeconds, gap = (float)Main.Settings.TouchCm / 100f;
            HashSet<long> seen = new HashSet<long>();
            for (int i = 0; i < Held.Count; i++)
                for (int j = i + 1; j < Held.Count; j++)
                {
                    Pickup a = Held[i], b = Held[j];
                    if (!Touch.CanPair(a, b)) continue;
                    long key = Key(a, b);
                    seen.Add(key);
                    ContactTimer t;
                    if (!Timers.TryGetValue(key, out t)) { t = new ContactTimer(); Timers[key] = t; }
                    if (t.Update(Touch.Touching(a, b, gap), dt, hold))
                    {
                        Timers.Remove(key);
                        try { Merge(a, b); }
                        catch (Exception e) { Main.Log.Warning("merge failed: " + e); }
                        return; // the list changed, try again next tick
                    }
                }

            Drop.Clear();
            foreach (KeyValuePair<long, ContactTimer> kv in Timers)
            {
                if (!seen.Contains(kv.Key)) kv.Value.Update(false, dt, hold);
                if (kv.Value.IsStale) Drop.Add(kv.Key);
            }
            foreach (long k in Drop) Timers.Remove(k);
        }

        static long Key(Pickup a, Pickup b)
        {
            long x = a.GetInstanceID(), y = b.GetInstanceID();
            return x < y ? (x << 32) ^ (y & 0xffffffffL) : (y << 32) ^ (x & 0xffffffffL);
        }

        static void CollectHeld()
        {
            Held.Clear();
            foreach (Alta.Networking.Scripts.Player.IPlayer ip in Player.AllPlayers)
            {
                Player p = ip as Player;
                if (p == null || p.PlayerController == null) continue;
                Add(Touch.HeldBy(p.PlayerController.LeftController != null ? p.PlayerController.LeftController.Interactor : null));
                Add(Touch.HeldBy(p.PlayerController.RightController != null ? p.PlayerController.RightController.Interactor : null));
            }
        }

        static void Add(Pickup p)
        {
            if (p != null && p.Dock == null && !Held.Contains(p)) Held.Add(p);
        }

        // Spawns a pouch between the two items and puts both in. The game stacks the second onto the first and
        // deletes the extra.
        public static bool Merge(Pickup a, Pickup b)
        {
            if (!Touch.CanPair(a, b)) return false;
            Chunk chunk = a.Entity.Chunk ?? b.Entity.Chunk;
            Vector3 pos = (a.transform.position + b.transform.position) * 0.5f;
            if (chunk == null) chunk = LocationChunkHelper.GetBestContaining(pos);
            if (chunk == null) { Main.Log.Warning("no chunk for a pouch at " + pos); return false; }
            Vector3 fwd = a.transform.forward; fwd.y = 0f;
            Quaternion rot = fwd.sqrMagnitude > 0.001f ? Quaternion.LookRotation(fwd.normalized, Vector3.up) : Quaternion.identity;

            string name = a.Item.name, result = null;
            bool ok = false;
            NetworkEntity pouch = SpawnHelper.Spawn(CommonPrefabs.PouchPrefab, SpawnData.Default, chunk, pos, rot, delegate (NetworkEntity e)
            {
                PickupDock dock = e.GetComponentInChildren<PickupDock>();
                if (dock == null) { result = "the pouch has no dock"; return; }
                PickupDock final;
                PickupDock.AllowedResult r = dock.CheckAllowed(null, a, 1, out final);
                if (r != PickupDock.AllowedResult.Ok) { result = "a pouch won't take " + name + ": " + r; return; }
                // Has to be forced or the player's game keeps holding them.
                a.ForceDrop(true);
                b.ForceDrop(true);
                r = a.DockInto(null, dock, 1, true);
                if (r != PickupDock.AllowedResult.Ok) { result = "docking the first " + name + " failed: " + r; return; }
                ok = true;
                r = b.DockInto(null, dock, 1, true);
                if (r != PickupDock.AllowedResult.Ok) result = "docking the second " + name + " failed: " + r;
            });

            if (!ok)
            {
                if (pouch != null) pouch.SceneDestroy();
                Main.Log.Msg("no pouch made: " + result);
                return false;
            }
            Made++;
            PickupDock d = pouch.GetComponentInChildren<PickupDock>();
            Main.Log.Msg("made a pouch of " + (d != null ? d.QuantityStored : 0) + " " + name + (result != null ? " (" + result + ")" : ""));
            return true;
        }

        public static string Describe()
        {
            return "pouches made " + Made + "; pairs being pushed " + Timers.Count;
        }
    }
}
