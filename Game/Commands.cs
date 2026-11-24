using System.Globalization;
using Alta.Console;

namespace ItemsToBags
{
    // Server console commands, like "itemstobags status".
    [Module("itemstobags", "Items To Bags: hold two of the same item together to make a pouch")]
    public static class Commands
    {
        [Command("status", "Show the settings and how many pouches have been made")]
        public static string Status()
        {
            Settings s = Main.Settings;
            return "enabled " + s.Enabled + "; hold " + s.HoldSeconds.ToString(CultureInfo.InvariantCulture) + " s; touch "
                + s.TouchCm.ToString(CultureInfo.InvariantCulture) + " cm; " + Merger.Describe();
        }

        [Command("reload", "Reload UserData/ItemsToBags/config.txt")]
        public static string Reload()
        {
            return Main.LoadSettings();
        }
    }
}
