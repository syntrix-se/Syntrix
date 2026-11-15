# Syntrix

Shared services for Space Engineers mods. Instead of copying the same network, settings,
terminal and chat code into every mod, mods **dock** to Syntrix at runtime and use its services.

Space Engineers mods cannot call each other's compiled code. Syntrix therefore hands each mod
a dictionary of functions through `SendModMessage`, the same idea Rich HUD Master and
WeaponCore use. A mod only needs one file: [`Api/SyntrixApi.cs`](Api/SyntrixApi.cs). Mods with windows or HUD
elements add a second one: [`Api/SyntrixUi.cs`](Api/SyntrixUi.cs).

## Services

| Service | What it does |
|---|---|
| Network | one secure channel for all mods, named sub-channels, `byte[]` payloads, server checks the sender |
| Block settings | text values per block and mod, saved in the block (world and blueprint), changed by the server only, mirrored to all players, access check for player changes |
| Terminal controls | checkbox, slider, text box, button, label, toolbar action in one line each, bound to block settings, created lazily so vanilla controls stay intact |
| Chat commands | one chat hook for all mods, promote level checked on player and server, run on server or locally, `/syntrix` lists everything |
| Events | publish/subscribe between mods on the same machine |
| Notifications | server sends a text key, every player sees it in their own language |
| Helpers in the dock file | text lookup with fallback, log with mod prefix |
| UI (API 2, `SyntrixUi.cs`) | windows, lists, buttons, text fields, images and HUD overlays through Rich HUD: elements by handle, text markup with links, key binds with a rebind page, cursor and view lock, nothing to copy from Rich HUD |

## Quick start

1. Subscribe to Syntrix in the Workshop and add it as a **dependency** of your mod.
   Syntrix itself depends on **Rich HUD Master** (Workshop 1965654081), which the UI service needs.
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

## UI service (API 2)

Space Engineers mods cannot use Rich HUD classes of another mod, so Syntrix carries the Rich HUD
client and creates the real elements. Your mod works with handles through typed wrappers in
`Api/SyntrixUi.cs` (copy it next to `SyntrixApi.cs`; it needs no other file):

```csharp
_ui = new SyntrixUi(_syx);                 // in LoadData, after _syx.Load
_ui.Load(BuildWindow);                     // runs when Rich HUD is ready (again after a restart)
_ui.Bind("Toggle", "F7", () => _window.Toggle());
_ui.BindPage("My Mod");                    // rebind page in the Rich HUD terminal (F2)

void BuildWindow()
{
    _window = _ui.Window("[b]My window[/b]");
    _window.Size = new Vector2(560f, 420f);
    var text = _ui.Label(_window, "Hello [color=#2FD1C5]world[/color], see [link=help]help[/link].");
    text.ParentAlign = UiAlign.InnerTopLeft;
    text.Offset = new Vector2(16f, -12f);
    text.WrapToParent(32f);
    text.OnLink(target => MyAPIGateway.Utilities.ShowMessage("MyMod", target));
    var ok = _ui.Button(_window, "OK");
    ok.ParentAlign = UiAlign.InnerBottomRight;
    ok.Offset = new Vector2(-16f, 16f);
    ok.OnClick(() => _window.Close());
}
// UnloadData: _ui.Unload(); before _syx.Unload();
```

Elements: window, panel, label, label box, button, text field, box (colour or texture), border,
scroll box, chain, empty, click area, check box, slider. Events arrive outside of Rich HUD's own
update, so handlers may rebuild the UI. Markup: `[b]`, `[i]`, `[u]`, `[color=#RRGGBB]`,
`[size=1.2]`, `[font=Name]`, `[align=center]`, `[link=target]`, each closed with `[/tag]`; `[[` is a
literal `[`. The full description is in the wiki; [`Examples/ExampleMod`](Examples/ExampleMod)
shows a small window.
## Compatibility promise

- Endpoint names are never removed and never change meaning. A changed signature gets a new name.
- Syntrix accepts every dock file from its minimum version on; an older one is refused with a clear log line.
- A newer dock file on an older Syntrix binds what exists; missing services do nothing.
- Load order does not matter. If several copies of Syntrix are loaded, the newest one runs.
- Without Syntrix the dock file never throws: your mod logs one line, admins see one chat line.

## License and credits

MIT, see [LICENSE](LICENSE). Please name Syntrix in your Workshop description when you use it.
Syntrix ships the Rich HUD Framework client by Zach Hembree (MIT, licence text in
`Data/Scripts/Syntrix/RichHudFramework/LICENSE.txt`).
Credits for ideas and patterns: [CREDITS.md](CREDITS.md). Changes per version: [CHANGELOG.md](CHANGELOG.md).
