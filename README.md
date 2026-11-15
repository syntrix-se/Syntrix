# Syntrix

Shared services for Space Engineers mods. Instead of copying the same network, settings,
terminal and chat code into every mod, mods **dock** to Syntrix at runtime and use its services.

Space Engineers mods cannot call each other's compiled code. Syntrix therefore hands each mod
a dictionary of functions through `SendModMessage`, the same idea Rich HUD Master and
WeaponCore use. A mod only needs one file: [`Api/SyntrixApi.cs`](Api/SyntrixApi.cs).

## Services (API version 1)

| Service | What it does |
|---|---|
| Network | one secure channel for all mods, named sub-channels, `byte[]` payloads, server checks the sender |
| Block settings | text values per block and mod, saved in the block (world and blueprint), changed by the server only, mirrored to all players, access check for player changes |
| Terminal controls | checkbox, slider, text box, button, label, toolbar action in one line each, bound to block settings, created lazily so vanilla controls stay intact |
| Chat commands | one chat hook for all mods, promote level checked on player and server, run on server or locally, `/syntrix` lists everything |
| Events | publish/subscribe between mods on the same machine |
| Notifications | server sends a text key, every player sees it in their own language |
| Helpers in the dock file | text lookup with fallback, log with mod prefix |

## Quick start

1. Subscribe to Syntrix in the Workshop and add it as a **dependency** of your mod.
2. Copy `Api/SyntrixApi.cs` into your mod's script folder (do not edit it).
3. Dock in your session component:

```csharp
readonly SyntrixApi _syx = new SyntrixApi();

public override void LoadData()
{
    _syx.Load("MyMod", OnReady);
    SyntrixControls<IMyDoor>.Checkbox(_syx, "MyMod_Auto", "MyMod_Auto", null, "MyMod", "auto", true);
}

void OnReady()
{
    _syx.ChatRegister("mymod", MyPromoteLevel.Admin, true, "admin tools", (sender, args) => "Hello");
}

protected override void UnloadData()
{
    SyntrixControls<IMyDoor>.Unload();
    _syx.Unload();
}
```

A complete example is in [`Examples/ExampleMod`](Examples/ExampleMod). The manual is in the
[wiki](../../wiki).

## Compatibility promise

- Endpoint names are never removed and never change meaning. A changed signature gets a new name.
- Syntrix accepts every dock file from its minimum version on; an older one is refused with a clear log line.
- A newer dock file on an older Syntrix binds what exists; missing services do nothing.
- Load order does not matter. If several copies of Syntrix are loaded, the newest one runs.
- Without Syntrix the dock file never throws: your mod logs one line, admins see one chat line.

## License and credits

MIT, see [LICENSE](LICENSE). Please name Syntrix in your Workshop description when you use it.
Credits for ideas and patterns: [CREDITS.md](CREDITS.md). Changes per version: [CHANGELOG.md](CHANGELOG.md).
