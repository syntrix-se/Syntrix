# Notes for AI coding assistants

This file is for AI assistants (Claude, Copilot, Codex and others) that help someone write a
Space Engineers mod which uses Syntrix, or that work on Syntrix itself. Humans are welcome too.

## Read first

- [`README.md`](README.md): what Syntrix is, the files a mod copies, all services, quick start.
- The [wiki](../../wiki): Getting Started, Services, UI, Docking and Versions, Troubleshooting.
- [`Examples/ExampleMod`](Examples/ExampleMod): a complete mod that uses every service once.
- [`CHANGELOG.md`](CHANGELOG.md): which endpoints exist since which version.

## Writing a mod that uses Syntrix

1. Add Syntrix as a Workshop dependency (Required items). Rich HUD Master (1965654081) comes with it.
2. Copy `Api/SyntrixApi.cs` into the mod's script folder. For windows, HUD elements or Rich HUD
   terminal pages also copy `Api/SyntrixUi.cs`, `Api/SyntrixUi.Controls.cs` and
   `Api/SyntrixUi.Terminal.cs`, always the three together.
3. **Never edit the dock files.** Update them only by copying newer ones from this repository.
   Do not rename their namespace or classes.
4. One `SyntrixApi` per mod, created in a session component: `Load("ModName", OnReady)` in
   `LoadData`, `Unload()` in `UnloadData` (UI first: `_ui.Unload()`, then `SyntrixControls<T>.Unload()`
   per block type, then `_syx.Unload()`).
5. Registrations (network channels, block change handlers, chat commands, event subscriptions,
   settings, terminal controls) may come right after `Load`. **Sending** (network, events,
   notifications) only works after `OnReady`. `OnReady` runs once on every machine.
6. Prefix everything with the mod name: net channels (`MyMod.sync`), event names (`MyMod.alarm`),
   the `modKey` of block settings and control ids. Names are global across all docked mods.
7. The mod must still load without Syntrix: calls then do nothing, nothing throws. Do not add own
   fallbacks for missing Syntrix; the Workshop dependency handles that.
8. On dedicated servers call `SyntrixControls<T>.EnsureCreated()` from the block's game logic if
   toolbar actions or script properties are needed there.
9. Check in game: `/syntrix` in chat lists the docked mods. Problems are in the game log, lines
   start with `Syntrix` or with the mod name.

## Space Engineers script rules that trip assistants

- Mod scripts run under the ModAPI whitelist: no reflection, no `System.IO` (use `SyntrixFiles` or
  `MyAPIGateway.Utilities` storage), no threads, no `dynamic`, no C# tuples (use `MyTuple`).
- Mods cannot call each other's compiled code. Everything between mods goes through Syntrix's
  dictionary of delegates with simple types only (`string`, `long`, `byte[]`, `MyTuple`, `Action`,
  `Func`). Never pass own classes across mods.
- Do not use Rich HUD classes directly in a mod that docks to Syntrix; use the `SyntrixUi` handles.
- Code that only compiles is not tested: the game compiles mod scripts itself at world load. Ask the
  user for the game log after a test.

## Working on Syntrix itself

- Compatibility promise (README): endpoint names are never removed and never change meaning; a
  changed signature gets a new name. Raise `ApiVersion` in `Data/Scripts/Syntrix/Protocol.cs` when
  endpoints are added, keep `MinApiVersion` unless the user decides otherwise.
- Every change goes into `CHANGELOG.md` under its version, with new or deprecated endpoints.
- Code style: see README, `.editorconfig`, XML documentation on everything public, English only.
- `Data/Scripts/Syntrix/RichHudFramework/` is the unchanged Rich HUD client. Do not edit it.
- Files use CRLF line endings.
- Branches: `main` is the state in the Workshop, `dev` is the next version.
