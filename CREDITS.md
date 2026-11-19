# Credits

Syntrix is its own code. The docking idea follows public patterns of:

- **Rich HUD Framework / Rich HUD Master** by Zach Hembree (MIT): registration with an API
  version, refusal of outdated clients, several framework copies where the newest wins.
  Syntrix 1.0 and 1.1 shipped its client; since 1.2 it lives on as source in the module
  [Syntrix.RichHud](https://github.com/syntrix-se/Syntrix.RichHud).
- **WeaponCore / CoreSystems**: endpoint dictionary `Dictionary<string, Delegate>` handed over
  through `SendModMessage`.
- **ToolCore**: the framework pushes its endpoints on load and answers every later request.

The window service draws with the textures of Space Engineers' own GUI (Keen Software House), which
every player has; Syntrix ships none of them.

Thanks to everybody who documented the Space Engineers ModAPI and its pitfalls.
