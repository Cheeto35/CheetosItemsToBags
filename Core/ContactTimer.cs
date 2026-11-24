namespace ItemsToBags
{
    // Keeps track of how long two items have been touching. Hands shake and the network lags, so a quick break
    // in contact (under Grace seconds) doesn't reset it. Goes off once when the time is up.
    public sealed class ContactTimer
    {
        public const float Grace = 0.3f;

        public float Held { get; private set; }     // seconds of contact so far
        public float Apart { get; private set; }    // seconds since the items last touched
        public bool Fired { get; private set; }

        public bool IsTouching { get { return Held > 0f && Apart <= Grace; } }

        // Returns true on the one update where the hold time is reached.
        public bool Update(bool touching, float dt, float hold)
        {
            if (touching)
            {
                Apart = 0f;
                Held += dt;
            }
            else
            {
                Apart += dt;
                if (Apart > Grace) { Held = 0f; Fired = false; }
            }
            if (!Fired && Held >= hold) { Fired = true; return true; }
            return false;
        }

        public float Progress(float hold) { return hold <= 0f ? 1f : (Held >= hold ? 1f : Held / hold); }

        // The items have been apart long enough that we can forget about them.
        public bool IsStale { get { return Apart > Grace * 4f; } }
    }
}
