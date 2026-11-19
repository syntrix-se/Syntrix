# Syntrix

Shared services for Space Engineers mods. Instead of copying the same network, settings,
terminal and chat code into every mod, mods **dock** to Syntrix at runtime and use its services.

Space Engineers mods cannot call each other's compiled code. Syntrix therefore hands each mod
a dictionary of functions through `SendModMessage`, the same idea Rich HUD Master and
WeaponCore use. Syntrix needs no other mod.

## Files a mod copies

Copy the dock files into your mod's script folder and do not edit them:

| File | When |
|---|---|
| [`Api/SyntrixApi.cs`](Api/SyntrixApi.cs) | always |
| [`Api/SyntrixGui.cs`](Api/SyntrixGui.cs) | windows and screen overlays |

## Services

| Service | What it does |
|---|---|
| Network | one secure channel for all mods, named sub-channels, `byte[]` payloads, server checks the sender |
| Block settings | text values per block and mod, saved in the block (world and blueprint), changed by the server only, mirrored to all players, access check for player changes; bool and float helpers |
| Terminal controls | `SyntrixControls<TBlock>`: checkbox, on/off switch, slider (linear, logarithmic, dual logarithmic), text box, combo box, list box (multiselect, visible rows), colour picker, button, label, separator, toolbar actions (writer, icon, toolbar types, groups) and terminal properties for programmable blocks; each one bound to a block setting in one line, or with own getter and setter; created lazily so vanilla controls stay intact; `ListMode` gives one set of controls to several block types; `EnsureCreated` for dedicated servers |
| Chat commands | one chat hook for all mods, promote level checked on player and server, run on server or locally, `/syntrix` lists everything |
| Events | publish/subscribe between mods: locally, to the server, to all machines or to one player; string arguments over the network; `EventSender` tells who sent it |
| Settings | a text file in the world storage: the server reads it (or writes the default), every player gets the same text, `SettingsSave` changes it for everybody |
| Notifications | server sends a text key, every player sees it in their own language |
| Registration queue | network channels, block change handlers, chat commands, event subscriptions and settings may be registered right after `Load`; the dock file keeps them and hands them over when Syntrix is ready |
| Helpers in the dock file | `SyntrixFiles` (world and local storage, never throws), `SyntrixText` (text lookup with fallback), `SyntrixLog` (log with mod prefix, optional own log file) |
| Windows (`SyntrixGui.cs`, players only) | windows in the Vanilla style through the game's own screen API (Esc, pause in single player, cursor, close button): panels and borders, stacks, scroll areas, text with wrapping, markup and links, buttons, images and icons (materials or definition ids), text fields, lists with icons and columns, trees, tables, check boxes, sliders, progress bars, tabs, drop-downs, tooltips, a confirmation window, a drawing area for special cases, and overlays on the screen without a window; grid size symbols of the G menu as materials `SyxGui_GridLarge` and `SyxGui_GridSmall` |
| F action | a block gets its own F action (`UseRegister`) through a UseObjectsComponent with `UseObjectTypeOverride` `syntrix`; K still opens the Vanilla terminal |
| Wiki | in-game wiki (F11, `/syxwiki`): every loaded mod adds pages with a `Data/Syntrix-Wiki.xml`, no code needed; mods with code add generated pages (`WikiAddSource`) and fill automatic tables (`WikiAddTables`) |

## Quick start

1. Subscribe to Syntrix in the Workshop and add it as a **dependency** of your mod.
2. Copy `Api/SyntrixApi.cs` into your mod's script folder.
3. Dock in your session component:

```csharp
readonly SyntrixApi _syx = new SyntrixApi();

public override void LoadData()
{
    _syx.Load("MyMod", OnReady);
    SyntrixControls<IMyDoor>.Checkbox(_syx, "MyMod_Auto", "MyMod_Auto", null, "MyMod", "auto", true);
    SyntrixControls<IMyDoor>.PropertyBool(_syx, "MyMod.Auto", "MyMod", "auto", true);   // for scripts
    _syx.SettingsLoad("MyMod.ini", "speed=1\n", true, text => ParseSettings(text));
    _syx.Subscribe("MyMod.Alarm", payload => OnAlarm(payload as string[], _syx.EventSender));
}

void OnReady()
{
    _syx.ChatRegister("mymod", MyPromoteLevel.Admin, true, "admin tools", (sender, args) => "Hello");
    if (_syx.IsServer)
        _syx.SendEventToAll("MyMod.Alarm", "on");
}

protected override void UnloadData()
{
    SyntrixControls<IMyDoor>.Unload();
    _syx.Unload();
}
```

A complete example is in [`Examples/ExampleMod`](Examples/ExampleMod). The manual is in the
[wiki](../../wiki).

## Window service

Mods cannot create screen classes of another mod, so Syntrix builds the windows itself with the
game's `MyModScreen` and draws them in the Vanilla style. Your mod works with handles through the
typed wrappers in `Api/SyntrixGui.cs`:

```csharp
_gui = new SyntrixGui(_syx);               // in LoadData, after _syx.Load

void OpenWindow()
{
    if (!_gui.IsAvailable)                 // players only, Syntrix 1.2 or newer
        return;
    var window = _gui.Window("My window");   // Vanilla terminal size by default
    var root = window.AddStack(false, 10f);
    root.FillWidth = root.FillHeight = true;
    var text = root.AddLabel("Hello [color=#5AC8DC]world[/color], see [link=help]help[/link].");
    text.Link += target => MyAPIGateway.Utilities.ShowMessage("MyMod", target);
    var list = root.AddList();
    list.ItemAdd("plate", "Steel plate", "MyObjectBuilder_Component/SteelPlate");
    list.Selected += (index, key) => MyAPIGateway.Utilities.ShowMessage("MyMod", key);
    root.AddButton("Close", () => window.Close());
    window.Open();
}
// UnloadData: _gui.Release(); before _syx.Unload();
```

Events are queued and run after the input pass, so handlers may rebuild the window. Markup: `[b]`,
`[i]`, `[u]`, `[color=#RRGGBB]`, `[size=1.2]`, `[font=Name]`, `[align=center]`, `[link=target]`,
each closed with `[/tag]`; `[[` is a literal `[`. Every property without a typed wrapper is
reachable with `Set…`/`Get…` and the `GuiProp` numbers. The game allows four mod windows at the
same time.

### F action

```csharp
_syx.UseRegister("FunctionalBlock/MyTerminal", "MyMod_UseHint", (entityId, action) => OpenWindow());
```

The block definition needs a UseObjectsComponent with `UseObjectTypeOverride` `syntrix`; see
`Examples/ExampleMod/Data/EntityComponents_SyntrixExample.sbc`.

### Rich HUD

Syntrix 1.2 no longer ships the Rich HUD client. The `Ui.*` endpoints of API 2 stay registered and
report "not available" (`Ui.State` = -1). A mod that still wants Rich HUD includes it itself; the
module [Syntrix.RichHud](https://github.com/syntrix-se/Syntrix.RichHud) holds the former code as
source with instructions.

## Compatibility promise

- Endpoint names are never removed and never change meaning. A changed signature gets a new name.
- Syntrix accepts every dock file from its minimum version on; an older one is refused with a clear log line.
- A newer dock file on an older Syntrix binds what exists; missing services do nothing.
- Load order does not matter. If several copies of Syntrix are loaded, the newest one runs.
- Without Syntrix the dock file never throws: your mod logs one line, admins see one chat line.

## Code style

.NET naming (PascalCase for types, methods, properties and constants, camelCase for locals and
parameters, `_camelCase` for private fields, `I` for interfaces), one main type per file named
after it, formatting per [`.editorconfig`](.editorconfig), English comments and XML documentation
on everything public. Deliberate exceptions:

- The dock files (`Api/SyntrixApi.cs`, `Api/SyntrixGui.cs`) hold several types each, because a mod
  copies them as a whole.
- Public fields stay where changing them would break mods or serialisation: `SyntrixLog.Debug`
  (API 1), the ProtoBuf fields of `Packet`, the XML classes of the wiki, and plain data holders
  inside the framework (chat command entries, element properties of the window service, wiki pages).
- `MyTuple` instead of C# tuples (the game's script whitelist).

## License and credits

MIT, see [LICENSE](LICENSE). Please name Syntrix in your Workshop description when you use it.
Credits for ideas and patterns: [CREDITS.md](CREDITS.md). Changes per version: [CHANGELOG.md](CHANGELOG.md).
