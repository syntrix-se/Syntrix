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
| Helpers in the dock file | `SyntrixFiles` (world and local storage, never throws), `SyntrixText` (text lookup with fallback), `SyntrixLog` (log with mod prefix, optional own log file), `NotifyNear` (notification to the players near a place), `PlayersNear` and `PlayersIn` (players near a place or inside a box), drop-down lists whose entries depend on the block |
| Windows (`SyntrixGui.cs`, players only) | windows in the Vanilla style through the game's own screen API (Esc, pause in single player, cursor, close button): panels and borders, stacks, scroll areas, text with wrapping, markup and links, buttons, images and icons (materials or definition ids), text fields, lists with icons and columns, trees, tables, check boxes, sliders, progress bars, tabs, drop-downs, tooltips, a confirmation window, a drawing area for special cases, and overlays on the screen without a window; grid size symbols of the G menu as materials `SyxGui_GridLarge` and `SyxGui_GridSmall` |
| F action | a block gets its own F action (`UseRegister`) through the UseObjectsComponent `SyntrixUse` that Syntrix ships; without a registered action F opens the terminal; K still opens the Vanilla terminal; both stay closed for players a faction with reputation rules does not trade with (1.4.1) |
| Grid storage (server) | a grid group becomes a text (blueprint with block storage, relative to a frame, with its outline) and is spawned back after a space check, keeping the ids when they are free; plus the grid groups near a point (`GridsNearby`) |
| Fixed screens (players) | a mod hands over lines for a block's screen; Syntrix draws them in one board style within 150 m, only on change, and takes the screen settings out of the K menu |
| Admin only blocks | block types only admins see in the G menu; the server razes such blocks built by other players |
| Price requests | one mod gives prices for a service name, others ask with a description and get an amount or "no price" |
| Block lights | the server sets colour, brightness and blinking of a block's status lights (emissive parts); Syntrix sends it to every player, also to later ones, and paints it again when the game repaints the block |
| Short sounds | the server plays a short sound at a block or a place for the players within a radius |
| Reputation | one faction mod defines scale, tiers with trade percent and goods level, start values, limits, links between factions and slow recovery; Syntrix gives the start values once, keeps every value in its limits (also against the game), passes shares on, sends the values to the player's machine, shows the change and raises `Syntrix.Rep.Changed`; `/syxrep` for admins |
| Accounts (API 6) | player and faction money with one check per frame, `Syntrix.Account.Changed` for every change |
| Reputation gate and price (API 6) | lowest goods level per block type or block (also Vanilla blocks such as stores), price after the player's trade percent |
| World clock (API 6) | one world date and local time of day for all mods, settings in the world storage, planet calendars, event controller events, script properties, AutoLCD commands, `/syxtime` |
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

The block's entity container takes the use object of Syntrix (API 4):
`<Component BuilderType="MyObjectBuilder_UseObjectsComponent" SubtypeId="SyntrixUse" ForceCreate="true" />`;
see `Examples/ExampleMod/Data/EntityComponents_SyntrixExample.sbc`. Without a registered action F opens
the terminal of the block.

### Grid storage, fixed screens, admin blocks, prices (API 4)

```csharp
// server: store the ship on a pad and spawn it again later
string text = _syx.GridsCapture(gridId, connector.WorldMatrix);   // save it, then close the grids
int result = _syx.GridsSpawn(text, connector.WorldMatrix, connector.CubeGrid.EntityId);

// players: the screen of every such block, redrawn only on change
_syx.ScreenRegister("FunctionalBlock/MyTerminal", 0, id =>
    SyntrixApi.ScreenTitle("MySymbol", "Hangar") + SyntrixApi.ScreenLine('V', "2 ships") + SyntrixApi.ScreenLine('F', "[{F}] open"));

// all machines: only admins may build it
_syx.AdminBlockRegister("FunctionalBlock/MyNpcTerminal", "MyMod_AdminOnly", "Only admins may build this.");

// server: a price from the economy mod, or the own one
long price = _syx.PriceAsk("MyMod.Repair", SyntrixApi.PriceLine("blocks", "12"));
if (price < 0)
    price = 12 * 100;

// server: the status lights of a door blink yellow, then a short sound for everyone within 15 m
_syx.LightsSet(door.EntityId, null, Color.Yellow, 1f, 0.8f);
_syx.SoundPlay(door.EntityId, "HudUnable", 15);
_syx.LightsClear(door.EntityId);   // back to the game's own colours
```

### Reputation (API 5)

```csharp
// server, faction mod only: scale, tiers and a faction
_syx.RepDefineScale(0, 30000);
_syx.RepDefineTiers(new List<MyTuple<string, int, float, int>>
{
    new MyTuple<string, int, float, int>("MyMod_TierHostile", 0, 0f, 0),
    new MyTuple<string, int, float, int>("MyMod_TierNeutral", 12001, 0f, 1),
    new MyTuple<string, int, float, int>("MyMod_TierFriendly", 18001, 5f, 1),
});
_syx.RepDefineFaction("ABC", 15000, 12001, 30000);   // start, never below neutral
_syx.RepDefineLink("ABC", "XYZ", -0.02f);            // 2 % the other way

// server, any mod: change it (inside the limits, linked factions follow)
_syx.RepChange(identityId, "ABC", -300, "MyMod.BaseCore");

// everywhere, also on the player's machine: value, tier, percent, goods level
var info = _syx.RepInfo(identityId, "ABC");
```

### Accounts, reputation gate, world clock (API 6)

```csharp
// all machines: Vanilla stores refuse players below goods level 1
_syx.RepGateType("MyObjectBuilder_StoreBlock", 1);

// server: charge the price after the player's trade percent
long price = _syx.RepPrice(identityId, "ABC", 500, true);
if (!_syx.AccountCharge(identityId, price, "MyMod.Repair"))
    return;   // not enough money or player offline

// everywhere: world date and the local time of day at a place
string date = _syx.TimeWorldText();
string local = _syx.TimeLocalText(position);
_syx.Subscribe(SyntrixApi.TimeNewDayEvent, payload => OnNewDay());
```

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
