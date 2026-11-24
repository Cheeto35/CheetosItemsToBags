# Items To Bags

A mod for A Township Tale. Grab two of the same thing, one in each hand, and hold them together for a couple of
seconds. They turn into a pouch with both of them inside, and the pouch ends up in your hand.

- Works on anything you could normally put in a pouch: ore, ingots, coins, gems, arrows, food and so on. Tools,
  weapons and other stuff a pouch won't take are ignored.
- Your controllers buzz while you hold the items together, a little harder and faster as it goes, then one big buzz
  when the pouch is made.
- Want more in the pouch? Just put items in it like you normally would.

## Install

Drop `ItemsToBags.dll` into the game's `Mods` folder. You need MelonLoader.

- **Server:** this is what actually makes the pouches, so it has to be on the server.
- **Players:** put it on your PC too if you want the buzzing and the pouch going into your hand. Without it you can
  still make pouches, the pouch just falls instead.

## Settings

It makes `UserData/ItemsToBags/config.txt` the first time it runs:

| Setting | Default | What it does |
| --- | --- | --- |
| `enabled` | `true` | set to `false` to turn the mod off |
| `hold_seconds` | `2` | how long to hold the items together (0.5-10). Use the same number on the server and your PC |
| `touch_cm` | `3` | how close the items have to be to count as touching, in cm (0.5-15) |
| `haptics` | `true` | turns the buzzing on or off |
| `haptic_strength` | `1` | how strong the buzz is, 0-1 |
| `grab_pouch` | `true` | put the new pouch in your hand |

On the server console, `itemstobags status` shows the settings and `itemstobags reload` reloads the config file.

## Building

Run `.\build.ps1`. Add `-GamePath` if the game isn't in `C:\Games\Alta\A Township Tale`, and `-Install` to copy the
DLL into the game's `Mods` folder.
