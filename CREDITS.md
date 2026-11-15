# Credits

Syntrix contains no code from other mods. The docking idea follows public patterns of:

- **Rich HUD Framework / Rich HUD Master** by Zach Hembree (MIT): registration with an API
  version, refusal of outdated clients, several framework copies where the newest wins.
- **WeaponCore / CoreSystems**: endpoint dictionary `Dictionary<string, Delegate>` handed over
  through `SendModMessage`.
- **ToolCore**: the framework pushes its endpoints on load and answers every later request.

Thanks to everybody who documented the Space Engineers ModAPI and its pitfalls.
