using Alta.Inventory;
using UnityEngine;

namespace ItemsToBags
{
    // Runs on the player's PC. Buzzes both hands while you hold two of the same item together, gives one big buzz
    // when the server turns them into a pouch, then puts the pouch in whichever hand is closest.
    public static class HandFeel
    {
        static readonly ContactTimer Timer = new ContactTimer();
        static Pickup _pairL, _pairR;
        static float _nextPulse, _armedUntil;
        static Pickup _lastL, _lastR;      // what each hand was holding right before the pouch got made
        static Vector3 _posL, _posR;
        static Item _item;
        static float _nextSearch;
        static float _grabUntil;
        static int _grabHand = -1;         // 0 = left, 1 = right
        static Pickup _grabPouch;

        public static void Update()
        {
            PlayerController pc = PlayerController.Current;
            if (pc == null || pc.LeftController == null || pc.RightController == null) return;
            if (!Main.Settings.Enabled) return;
            Controller left = pc.LeftController, right = pc.RightController;
            float now = Time.unscaledTime, dt = Time.unscaledDeltaTime;
            float hold = (float)Main.Settings.HoldSeconds;

            Pickup l = Touch.HeldBy(left.Interactor), r = Touch.HeldBy(right.Interactor);

            // Both hands hold the same kind of item and they're touching.
            if (Touch.CanPair(l, r))
            {
                if (l != _pairL || r != _pairR) { Timer.Update(false, 10f, hold); _pairL = l; _pairR = r; }
                Timer.Update(Touch.Touching(l, r, (float)Main.Settings.TouchCm / 100f), dt, hold);
                if (Timer.IsTouching)
                {
                    float p = Timer.Progress(hold);
                    if (now >= _nextPulse)
                    {
                        Buzz(left, HapticRamp.Strength(p), HapticRamp.PulseLength);
                        Buzz(right, HapticRamp.Strength(p), HapticRamp.PulseLength);
                        _nextPulse = now + HapticRamp.Interval(p);
                    }
                    // Our timer and the server's never line up exactly, so start looking for the pouch at the halfway mark.
                    if (p >= 0.5f) { _armedUntil = now + 2.5f; _lastL = l; _lastR = r; _item = l.Item; _posL = l.transform.position; _posR = r.transform.position; }
                }
            }
            else Timer.Update(false, dt, hold);

            if (now < _armedUntil && _grabHand < 0) WatchForPouch(now);
            if (_grabHand >= 0) TryGrab(_grabHand == 0 ? left : right, now);
        }

        // Once the server makes the pouch, the items in our hands get deleted (the pouch comes with its own copies)
        // and a pouch of that item shows up between where they were.
        static void WatchForPouch(float now)
        {
            bool goneL = _lastL == null || _lastL.Dock != null, goneR = _lastR == null || _lastR.Dock != null;
            if (_lastL != null && _lastL.Dock == null) _posL = _lastL.transform.position;
            if (_lastR != null && _lastR.Dock == null) _posR = _lastR.transform.position;
            if (!goneL && !goneR) return;
            if (now < _nextSearch) return;
            _nextSearch = now + 0.05f;

            Pickup pouch = FindPouch((_posL + _posR) * 0.5f);
            if (pouch == null) return;

            PlayerController pc = PlayerController.Current;
            Buzz(pc.LeftController, HapticRamp.DoneStrength, HapticRamp.DoneLength);
            Buzz(pc.RightController, HapticRamp.DoneStrength, HapticRamp.DoneLength);
            _armedUntil = 0f; _lastL = _lastR = null; _pairL = _pairR = null;
            Timer.Update(false, 10f, (float)Main.Settings.HoldSeconds);
            if (!Main.Settings.GrabPouch) return;
            // The hand nearest the pouch takes it.
            Vector3 at = pouch.transform.position;
            _grabHand = Vector3.Distance(pc.LeftController.transform.position, at) <= Vector3.Distance(pc.RightController.transform.position, at) ? 0 : 1;
            _grabPouch = pouch; _grabUntil = now + 1.5f;
        }

        static readonly Collider[] Near = new Collider[64];

        // Finds a pouch lying near the spot that has our item in it.
        static Pickup FindPouch(Vector3 at)
        {
            int n = Physics.OverlapSphereNonAlloc(at, 0.6f, Near, ~0, QueryTriggerInteraction.Collide);
            Pickup best = null; float bestD = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                Pickup p = Near[i] != null ? Near[i].GetComponentInParent<Pickup>() : null;
                if (p == null || !Touch.IsPouch(p) || p.Dock != null || p.IsInteractedWith) continue;
                PickupDock d = p.GetComponentInChildren<Alta.Inventory.PickupDock>();
                if (d == null || d.DockedPickup == null || d.DockedPickup.Item != _item) continue;
                float dist = (p.transform.position - at).sqrMagnitude;
                if (dist < bestD) { bestD = dist; best = p; }
            }
            return best;
        }

        // Waits until the hand is empty, then grabs the pouch.
        static void TryGrab(Controller hand, float now)
        {
            if (now > _grabUntil || _grabPouch == null || _grabPouch.Dock != null || _grabPouch.IsInteractedWith) { _grabHand = -1; _grabPouch = null; return; }
            Interactor i = hand.Interactor;
            if (i == null || i.IsInteracting) return;
            i.ResetTimeout();
            i.StartInteract(_grabPouch);
            _grabHand = -1; _grabPouch = null;
        }

        static void Buzz(Controller c, float strength, float seconds)
        {
            if (c == null || !Main.Settings.Haptics) return;
            float s = strength * (float)Main.Settings.HapticStrength;
            if (s <= 0f) return;
            c.TriggerHaptic(s, seconds);
        }
    }
}
