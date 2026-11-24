using System;
using System.Collections.Generic;
using System.Globalization;

namespace ItemsToBags
{
    // Reads UserData/ItemsToBags/config.txt. One "key = value" per line, "#" for comments. Anything that doesn't
    // make sense keeps its default.
    public sealed class Settings
    {
        public bool Enabled = true;
        public double HoldSeconds = 2;      // seconds to hold the items together
        public double TouchCm = 3;          // how close counts as touching
        public bool Haptics = true;         // PC only: buzzing on or off
        public double HapticStrength = 1;   // PC only: 0-1
        public bool GrabPouch = true;       // PC only: put the pouch in the closest hand

        public const string DefaultFile =
            "# Items To Bags settings. Delete this file to go back to the defaults.\n" +
            "enabled = true          # false turns the mod off\n" +
            "hold_seconds = 2        # how long to hold two items together to make a pouch (0.5-10)\n" +
            "touch_cm = 3            # how close the items have to be to count as touching, in cm (0.5-15)\n" +
            "haptics = true          # buzz your controllers while you do it\n" +
            "haptic_strength = 1     # 0-1, how strong the buzz is\n" +
            "grab_pouch = true       # the new pouch goes straight into your hand\n";

        public static Settings Parse(string text, List<string> warnings)
        {
            Settings s = new Settings();
            if (text == null) return s;
            string[] lines = text.Replace("\r", "").Split('\n');
            for (int n = 0; n < lines.Length; n++)
            {
                string line = lines[n];
                int hash = line.IndexOf('#');
                if (hash >= 0) line = line.Substring(0, hash);
                line = line.Trim();
                if (line.Length == 0) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) { warnings.Add("line " + (n + 1) + ": expected key = value"); continue; }
                string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                string val = line.Substring(eq + 1).Trim();
                switch (key)
                {
                    case "enabled": s.Enabled = Bool(val, s.Enabled, key, warnings); break;
                    case "hold_seconds": s.HoldSeconds = Num(val, s.HoldSeconds, 0.5, 10, key, warnings); break;
                    case "touch_cm": s.TouchCm = Num(val, s.TouchCm, 0.5, 15, key, warnings); break;
                    case "haptics": s.Haptics = Bool(val, s.Haptics, key, warnings); break;
                    case "haptic_strength": s.HapticStrength = Num(val, s.HapticStrength, 0, 1, key, warnings); break;
                    case "grab_pouch": s.GrabPouch = Bool(val, s.GrabPouch, key, warnings); break;
                    default: warnings.Add("unknown setting '" + key + "'"); break;
                }
            }
            return s;
        }

        static bool Bool(string val, bool def, string key, List<string> warnings)
        {
            switch (val.ToLowerInvariant())
            {
                case "true": case "yes": case "on": case "1": return true;
                case "false": case "no": case "off": case "0": return false;
            }
            warnings.Add(key + ": '" + val + "' must be true or false; keeping " + (def ? "true" : "false"));
            return def;
        }

        static double Num(string val, double def, double min, double max, string key, List<string> warnings)
        {
            double d;
            if (!double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
            {
                warnings.Add(key + ": '" + val + "' isn't a number; keeping " + def.ToString(CultureInfo.InvariantCulture));
                return def;
            }
            if (d < min || d > max)
            {
                double c = Math.Max(min, Math.Min(max, d));
                warnings.Add(key + ": " + val + " is outside " + min.ToString(CultureInfo.InvariantCulture) + "-" + max.ToString(CultureInfo.InvariantCulture) + "; using " + c.ToString(CultureInfo.InvariantCulture));
                return c;
            }
            return d;
        }
    }
}
