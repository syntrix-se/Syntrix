# Credits

Syntrix ships one piece of code from another project:

- **Rich HUD Framework** (client and shared part) by Zach Hembree, MIT License, unchanged in
  `Data/Scripts/Syntrix/RichHudFramework/` with its licence text (`LICENSE.txt`) and source
  (`SOURCE.txt`). The UI service is built on it: its elements, controls, HUD spaces, drawing helpers
  and terminal pages are the Rich HUD ones. **Rich HUD Master** (Workshop 1965654081) must be
  loaded with Syntrix; it is a dependency.

Everything else is Syntrix's own code. The docking idea follows public patterns of:

- **Rich HUD Framework / Rich HUD Master** by Zach Hembree (MIT): registration with an API
  version, refusal of outdated clients, several framework copies where the newest wins.
- **WeaponCore / CoreSystems**: endpoint dictionary `Dictionary<string, Delegate>` handed over
  through `SendModMessage`.
- **ToolCore**: the framework pushes its endpoints on load and answers every later request.

Thanks to everybody who documented the Space Engineers ModAPI and its pitfalls.
