# Syntrix

Shared services for Space Engineers mods. Instead of copying the same network, settings,
terminal and chat code into every mod, mods **dock** to Syntrix at runtime and use its services.

Space Engineers mods cannot call each other's compiled code. Syntrix therefore hands each mod
a dictionary of functions through `SendModMessage`, the same idea Rich HUD Master and
WeaponCore use.

## Files a mod copies

Copy the dock files into your mod's script folder and do not edit them:

| File | When |
|---|---|
| [`Api/SyntrixApi.cs`](Api/SyntrixApi.cs) | always |
| [`Api/SyntrixUi.cs`](Api/SyntrixUi.cs), [`Api/SyntrixUi.Controls.cs`](Api/SyntrixUi.Controls.cs), [`Api/SyntrixUi.Terminal.cs`](Api/SyntrixUi.Terminal.cs) | windows, HUD elements or Rich HUD terminal pages; always the three together |

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
| UI (`SyntrixUi*.cs`) | everything the Rich HUD client offers, through handles: windows, panels, labels with markup and links, buttons, text fields and multi-line text boxes, drop-downs, list boxes, tree lists and tree boxes, colour pickers (RGB, HSV), on/off buttons, named check boxes, sliders and switches, scroll bars, slider bars, icon and label buttons, radial selection, HUD spaces (camera, world matrix, scaled), custom drawing (lines, polygons, rings, meshes), tooltips with own colour, clipboard, screen values, key binds with a rebind page, and Rich HUD terminal pages (categories, control pages with controls, text pages) |

## Quick start

1. Subscribe to Syntrix in the Workshop and add it as a **dependency** of your mod.
   Syntrix itself depends on **Rich HUD Master** (Workshop 1965654081), which the UI service needs.
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

## UI service

Space Engineers mods cannot use Rich HUD classes of another mod, so Syntrix carries the Rich HUD
client and creates the real elements. Your mod works with handles through typed wrappers in the
three `SyntrixUi*.cs` dock files:

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
    var pick = _ui.Dropdown(_window);
    pick.Add("a", "First");
    pick.Add("b", "[i]Second[/i]");
    pick.OnSelected(key => MyAPIGateway.Utilities.ShowMessage("MyMod", key));
    var ok = _ui.Button(_window, "OK");
    ok.ParentAlign = UiAlign.InnerBottomRight;
    ok.Offset = new Vector2(-16f, 16f);
    ok.OnClick(() => _window.Close());

    var page = _ui.TerminalCategory("My Mod").ControlPage("Options");
    var tile = page.Category("General").Tile();
    tile.CheckBox("Show window").OnChanged(on => _window.Visible = on);
}
// UnloadData: _ui.Unload(); before _syx.Unload();
```

Events arrive outside of Rich HUD's own update, so handlers may rebuild the UI. Markup: `[b]`,
`[i]`, `[u]`, `[color=#RRGGBB]`, `[size=1.2]`, `[font=Name]`, `[align=center]`, `[link=target]`,
each closed with `[/tag]`; `[[` is a literal `[`. Every property without a typed wrapper is
reachable with `Set`/`Get` and the `UiProp` numbers. The full description is in the wiki.

Limits that come from Rich HUD itself: all pages of all mods appear under the one terminal entry
"Syntrix" (group them with `TerminalCategory`); terminal members cannot be removed, only disabled;
tree box entries must be text elements; the radial selection raises no events (read
`HighlightIndex`).

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

- The dock files (`Api/SyntrixApi.cs`, `Api/SyntrixUi*.cs`) hold several types each, because a mod
  copies them as a whole.
- Public fields stay where changing them would break mods or serialisation: `SyntrixLog.Debug`
  (API 1), `UiElement.Ui` and `UiElement.Handle` (read-only), the ProtoBuf fields of `Packet`, and
  plain data holders inside the framework (`UiMarkup.Style`, chat command entries, UI event queue,
  typed views of `UiNode`).
- `MyTuple` instead of C# tuples (the game's script whitelist).
- `Data/Scripts/Syntrix/RichHudFramework/` is the unchanged Rich HUD client and keeps its own style.

## License and credits

MIT, see [LICENSE](LICENSE). Please name Syntrix in your Workshop description when you use it.
Syntrix ships the Rich HUD Framework client by Zach Hembree (MIT, licence text in
`Data/Scripts/Syntrix/RichHudFramework/LICENSE.txt`).
Credits for ideas and patterns: [CREDITS.md](CREDITS.md). Changes per version: [CHANGELOG.md](CHANGELOG.md).
