using Alta.Inventory;
using UnityEngine;

namespace ItemsToBags
{
    // Used by both the server and the PC side: can these two items go in a pouch, and are they touching?
    public static class Touch
    {
        // What the hand is really holding. If it's a part of a crafted item, we want the whole item.
        public static Pickup Root(Interactable held)
        {
            if (held == null) return null;
            Pickup p = held as Pickup ?? held.GetComponentInParent<Pickup>();
            int guard = 0;
            while (p != null && p.IsCraftedChild && p.CraftingPart != null && p.CraftingPart.ConnectedTo != null && guard++ < 32)
                p = p.CraftingPart.ConnectedTo.Pickup;
            return p;
        }

        public static Pickup HeldBy(Interactor interactor)
        {
            return interactor != null && interactor.IsInteracting ? Root(interactor.InteractingWith) : null;
        }

        public static bool IsPouch(Pickup p)
        {
            return p != null && IsPouch(p.Entity);
        }

        public static bool IsPouch(Alta.Networking.NetworkEntity e)
        {
            Alta.Networking.NetworkPrefab pouch = CommonPrefabs.PouchPrefab;
            return e != null && e.Prefab != null && pouch != null && e.Prefab.Hash == pouch.Hash;
        }

        // Two separate items of the same kind that a pouch would hold together.
        public static bool CanPair(Pickup a, Pickup b)
        {
            if (a == null || b == null || a == b) return false;
            if (a.Dock != null || b.Dock != null) return false;
            Item item = a.Item;
            if (item == null || item != b.Item || !item.IsStackable) return false;
            if (item == CommonPrefabs.PouchItem || IsPouch(a) || IsPouch(b)) return false;
            if (a.IsCrafted || b.IsCrafted)
            {
                if (a.IsCrafted != b.IsCrafted || !item.IsStackableWhenCrafted) return false;
                if (!Alta.Crafting.CraftingPart.CompareBuild(a.CraftingPart, b.CraftingPart)) return false;
            }
            return true;
        }

        // Are the two items within maxGap metres of each other?
        public static bool Touching(Pickup a, Pickup b, float maxGap)
        {
            Bounds ba, bb;
            if (!SolidBounds(a, out ba) || !SolidBounds(b, out bb)) return false;
            ba.Expand(maxGap * 2f);
            if (!ba.Intersects(bb)) return false;

            foreach (Collider ca in a.Colliders)
            {
                if (!Solid(ca)) continue;
                Bounds ea = ca.bounds; ea.Expand(maxGap * 2f);
                foreach (Collider cb in b.Colliders)
                {
                    if (!Solid(cb) || !ea.Intersects(cb.bounds)) continue;
                    Vector3 dir; float dist;
                    if (CanPenetrate(ca, cb) && Physics.ComputePenetration(ca, ca.transform.position, ca.transform.rotation, cb, cb.transform.position, cb.transform.rotation, out dir, out dist))
                        return true;
                    // Bounce between the closest points a couple of times to find the real gap.
                    Vector3 pb = Closest(cb, ca.bounds.center);
                    Vector3 pa = Closest(ca, pb);
                    pb = Closest(cb, pa);
                    pa = Closest(ca, pb);
                    if ((pa - pb).sqrMagnitude <= maxGap * maxGap) return true;
                }
            }
            return false;
        }

        static bool Solid(Collider c)
        {
            return c != null && c.enabled && !c.isTrigger && c.gameObject.activeInHierarchy;
        }

        static bool SolidBounds(Pickup p, out Bounds b)
        {
            b = default(Bounds);
            bool any = false;
            if (p == null || p.Colliders == null) return false;
            foreach (Collider c in p.Colliders)
            {
                if (!Solid(c)) continue;
                if (!any) { b = c.bounds; any = true; } else b.Encapsulate(c.bounds);
            }
            return any;
        }

        static bool Convex(Collider c)
        {
            MeshCollider m = c as MeshCollider;
            return m == null || m.convex;
        }

        // ComputePenetration needs at least one convex collider and doesn't work on terrain.
        static bool CanPenetrate(Collider a, Collider b) { return (Convex(a) || Convex(b)) && !(a is TerrainCollider) && !(b is TerrainCollider); }

        // ClosestPoint only works on convex shapes, so use the bounding box for anything else.
        static Vector3 Closest(Collider c, Vector3 p)
        {
            return Convex(c) && !(c is TerrainCollider) ? c.ClosestPoint(p) : c.bounds.ClosestPoint(p);
        }
    }
}
