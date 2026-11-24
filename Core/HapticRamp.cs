namespace ItemsToBags
{
    // How the buzzing feels: short pulses that get stronger and quicker the longer you hold, then one long buzz
    // when the pouch is done.
    public static class HapticRamp
    {
        public const float PulseLength = 0.04f;
        public const float DoneStrength = 0.9f, DoneLength = 0.18f;

        // Pulse strength at progress 0..1.
        public static float Strength(float progress)
        {
            progress = Clamp01(progress);
            return 0.12f + 0.48f * progress;
        }

        // Seconds between pulses at progress 0..1: 0.25 s at the start, 0.07 s near the end.
        public static float Interval(float progress)
        {
            progress = Clamp01(progress);
            return 0.25f - 0.18f * progress;
        }

        static float Clamp01(float v) { return v < 0f ? 0f : (v > 1f ? 1f : v); }
    }
}
