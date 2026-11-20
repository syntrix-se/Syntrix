# Changelog

## 1.4.1 (API version 5)

No new endpoints; dock files stay valid.

- F and K at blocks with the use object `SyntrixUse` stay closed for a player whose reputation with the block's
  faction does not trade (goods level 0, see `RepDefineTiers` and `tradeFrom`). The player sees one notice
  (`Syntrix_Rep_Refused`) instead. Members of the faction and blocks of factions without a reputation definition
  are not affected. Mods with an F action need no code of their own for it.

## 1.4.0 (API version 5)

Dock files of API versions 1 to 4 keep working; no endpoint was removed. Copy the new `Api/SyntrixApi.cs`
to use the reputation service.

### Reputation of players with factions

- One mod (the faction mod) defines on the server the scale, the tiers (text key, lowest value, trade
  percent, goods level) and per faction the start value, the lowest and highest value, the lowest tier
  that trades, a slow recovery while online and links to other factions. Players get the definitions from
  the server, also when they join later.
- The start value is given once per player and faction; only these marks are saved (world storage,
  `Syntrix-Reputation-Start.txt`). The values themselves stay in the game.
- Every value stays inside its faction's limits, also when the game or another mod changes it (checked
  in the next frame, because the game reports a change before it stores it).
- A change with the propagate flag passes a share on to linked factions (one step, inside their limits).
- Every value is sent to the player's machine, so `RepGet` and `RepInfo` work there too (the game does
  not send values set by scripts). Faction to faction values (`RepSetBetween`) go to every player.
- The player sees the amount of a change (summed up over two seconds) and every tier change, in their own
  language (`Syntrix_Rep_Change`, `Syntrix_Rep_TierUp`, `Syntrix_Rep_TierDown`).
- Event `Syntrix.Rep.Changed` (identity, tag, old, new, reason) on the server and on the player's machine.
- Admin chat command `/syxrep list|set|add|reset`.
- Endpoints `Rep.DefineScale`, `Rep.DefineTiers`, `Rep.DefineFaction`, `Rep.DefineRecovery`,
  `Rep.DefineLink`, `Rep.ClearDefinitions`, `Rep.SetBetween`, `Rep.Get`, `Rep.Info`, `Rep.TierOf`,
  `Rep.TierCount`, `Rep.Tier`, `Rep.Scale`, `Rep.Limits`, `Rep.Factions`, `Rep.Change`, `Rep.Set`,
  `Rep.ResetStart`; in the dock file the methods `Rep*`, `RepOnChanged` and the constants
  `RepChangedEvent`, `RepPropagate`, `RepQuiet`.

## 1.3.0 (API version 4)

Dock files of API versions 1 to 3 keep working; no endpoint was removed. Copy the new `Api/SyntrixApi.cs`
to use the new services. Contains the changes of 1.2.2.

### Shared use object

- UseObjectsComponent `SyntrixUse` (in `Data/EntityComponents_Syntrix.sbc`, `UseObjectTypeOverride`
  `syntrix`): a mod puts it into the entity container of its block and needs no own definition. Without a
  registered F action, F opens the terminal of the block.

### Grid storage (server)

- A mechanical grid group becomes a text (blueprint with the block storage, relative to a reference
  frame, plus its outline) and back, with a space check. Ids are kept when all are free, otherwise the
  whole collection gets new ones. The mod keeps the text itself.
- Endpoints `Grids.Nearby`, `Grids.Capture`, `Grids.Spawn`, `Grids.Fits`; in the dock file
  `GridsNearby`, `GridsCapture`, `GridsSpawn`, `GridsFits` and the constants `Grid*`.

### Fixed screens (players)

- A mod hands over the content of a block's screen as lines (title with symbol, alarm bar, name,
  small, value, hint, foot, gap); Syntrix draws it in one board style within 150 m, only when it
  changed, and removes the screen settings of the block from the K menu.
- Endpoints `Screen.Register`, `Screen.Refresh`; in the dock file `ScreenRegister`, `ScreenRefresh`,
  `ScreenLine`, `ScreenTitle`.

### Admin only blocks

- Registered block types are in the G menu only for admins; the server razes such a block again when a
  player without admin rank starts to build it or welds it from a projection, with the mod's notice.
- Endpoints `Admin.RegisterBlock`, `Admin.UnregisterBlock`; in the dock file `AdminBlockRegister`,
  `AdminBlockUnregister`.

### Price requests

- A mod registers as price giver for a service name; other mods ask with a description ("key=value"
  lines) and get an amount, or -1 for "no price" and use their own.
- Endpoints `Price.Register`, `Price.Has`, `Price.Ask`; in the dock file `PriceRegister`, `PriceHas`,
  `PriceAsk`, `PriceLine`.

### Block lights

- The server sets colour, brightness and blinking of an emissive part of a block (status lights). Syntrix
  keeps the state, sends it to every player (also to players who join later) and paints it again within
  300 m when the game repaints the block. Not saved. Clearing gives the part back to the game.
- Endpoints `Lights.Set`, `Lights.Clear`; in the dock file `LightsSet`, `LightsClear`.

### Short sounds

- The server plays a short sound at a block or a place for the players within a radius (up to 200 m).
- Endpoints `Sound.Play`, `Sound.PlayAt`; in the dock file `SoundPlay`, `SoundPlayAt`.

### Dock file helpers

- `NotifyNear` (notification to the players near a place), `PlayersNear`, `PlayersIn` (players near a
  place or inside a box), `SyntrixControls<T>.Combo` with entries per block and
  `SyntrixControls<T>.CurrentBlock`.

## 1.2.2 (API version 3)

- Tabs (`GuiTabs`) use the Vanilla button textures like the tabs of the terminal: normal, hovered and
  pressed for the selected tab. Before, they used the striped toolbar tabs of the G menu, which glowed
  light blue and looked the same for the selected and the hovered tab.
- Confirmation window (`Gui.Confirm`): text and buttons centred, background a little darker.

## 1.2.1 (API version 3)

- Materials `SyxGui_GridLarge` and `SyxGui_GridSmall`: the grid size symbols of the G menu, for icons
  in lists of grids.

## 1.2.0 (API version 3)

Dock files of API versions 1 and 2 keep working. No endpoint was removed; the `Ui.*` endpoints of
API 2 stay registered and report "not available".

### Rich HUD removed

- Syntrix no longer ships the Rich HUD client and no longer needs Rich HUD Master.
- `Ui.*` (API 2): every endpoint is still there with its old signature; `Ui.State` returns -1, all
  others do nothing. The game log tells once per session which mod still uses them.
- The dock files `Api/SyntrixUi.cs`, `Api/SyntrixUi.Controls.cs` and `Api/SyntrixUi.Terminal.cs`
  are gone. Their code and the Rich HUD client are kept as source in the module
  [Syntrix.RichHud](https://github.com/syntrix-se/Syntrix.RichHud) for mods that include Rich HUD
  themselves.

### Window service (players only)

- New optional dock file `Api/SyntrixGui.cs`.
- Windows through the game's `MyModScreen` in the Vanilla style (Vanilla terminal size, title band,
  close button, Esc, pause in single player), overlays on the screen without a window (layer below
  or above the HUD, hidden with the HUD and in menus unless `ShowInMenus`).
- Elements: panel, stack, scroll area, label (wrapping, markup, links), button, image, text field
  (one or more lines), list (icons, columns, second text, badge), tree, table (header, relative
  widths, markup and links in cells), check box, slider, progress bar, tabs, drop-down, drawing
  area; tooltips; confirmation window.
- Endpoints: `Gui.State`, `Gui.Events`, `Gui.Release`, `Gui.Create`, `Gui.Remove`, `Gui.Clear`,
  `Gui.SetParent`, `Gui.SetBool`, `Gui.SetInt`, `Gui.SetFloat`, `Gui.SetVector`, `Gui.SetColor`,
  `Gui.SetText`, `Gui.GetBool`, `Gui.GetInt`, `Gui.GetFloat`, `Gui.GetVector`, `Gui.GetColor`,
  `Gui.GetText`, `Gui.ItemAdd`, `Gui.ItemSet`, `Gui.ItemRemove`, `Gui.ItemClear`, `Gui.ItemCount`,
  `Gui.ItemFind`, `Gui.Expand`, `Gui.ScrollTo`, `Gui.Open`, `Gui.Close`, `Gui.Focus`, `Gui.SetDraw`,
  `Gui.Measure`, `Gui.Screen`, `Gui.Scale`, `Gui.Cursor`, `Gui.Confirm`, `Gui.WindowCount`.

### F action

- Use object `syntrix` (UseObjectsComponent with `UseObjectTypeOverride`): F runs the action a mod
  registered for the block type, K opens the Vanilla terminal.
- Endpoints `Use.Register`, `Use.Unregister`; in the dock file `UseRegister`, `UseUnregister`.

### Wiki

- The in-game wiki moved into Syntrix (before: part of a content mod): F11 and `/syxwiki`
  (`/syxwiki <page>`, `/syxwiki reload`), page tree with search, back and forward, path, language
  switch, "new" and "important" marks, error page for authors.
- Every loaded mod adds pages with `Data/Syntrix-Wiki.xml`; new attributes for generating mods
  (`Icons`, `Source`, `ExtraDe`, `ExtraEn`, `<Profile>`, `<Error>`).
- Pages and tables from the game definitions, for Vanilla and every mod: ore, component, system and
  planet pages; tables `Ores`, `Components`, `Planets`, `OresPerPlanet`, `PlanetData`, `Recipes`;
  lists `Ores`, `Components`, `OresPerPlanet`, `Ingredients`, `Meals`, `Spoiled`, `Seeds`. Public
  planets that no `<System>` names go to the page "planets". Icons come from the definition ids.
- Generated lists show public definitions only; `<Hide Id="Type/Subtype" />` and
  `<Show Id="Type/Subtype" />` in a wiki file change that. A hand-written page with the id of a
  generated one stays and gets the generated blocks appended.
- Endpoints `Wiki.Open`, `Wiki.Close`, `Wiki.IsOpen`, `Wiki.OnClosed`, `Wiki.AddSource`,
  `Wiki.AddTables`, `Wiki.Context`, `Wiki.Reload`, `Wiki.Planet`, `Wiki.Title`, `Wiki.Text`; in the
  dock file `WikiOpen`, `WikiClose`, `WikiIsOpen`, `WikiOnClosed`, `WikiAddSource`, `WikiAddTables`,
  `WikiContext`, `WikiReload`, `WikiPlanet`, `WikiTitle`, `WikiText`.

## 1.1.0 (API version 2)

Dock files of API version 1 keep working; no endpoint or dock file method of 1.0.0 changed.

### Dock files

- `Api/SyntrixApi.cs` stays the only file a mod needs. The UI service comes in three optional files
  that are always copied together: `Api/SyntrixUi.cs` (service, base elements), `Api/SyntrixUi.Controls.cs`
  (Rich HUD controls, HUD spaces, shapes) and `Api/SyntrixUi.Terminal.cs` (Rich HUD terminal pages).
- Registrations made before docking (network channels, block change handlers, chat commands, event
  subscriptions, settings) are kept and handed over when Syntrix is ready.
- `SyntrixFiles`: text files in world and local storage, never throws.
- `SyntrixLog`: optional own log file in local storage (`SyntrixLog(modName, fileName)`, `Close`).
- XML documentation on every public member.

### Services

- Chat: `Chat.Unregister`.
- Events between machines: `Events.SendToServer`, `Events.SendToAll`, `Events.SendToPlayer` (string
  arguments) and `Events.Sender` (Steam id of the sender of the event being handled).
- Settings: `Settings.Publish`, `Settings.Subscribe`; in the dock file `SettingsLoad`, `SettingsSave`,
  `SettingsText` (a text file in the world storage, the same text on every player).

### Terminal controls (`SyntrixControls<TBlock>`)

- New controls: on/off switch, colour picker, list box (multiselect, visible rows), sliders with
  `SyntrixSliderScale` (linear, logarithmic, dual logarithmic) and limits from functions, check box and
  on/off switch with own on/off texts. Every control comes bound to a block setting and with own getter
  and setter.
- Terminal properties for programmable blocks: `PropertyBool`, `PropertyFloat`, `PropertyText`,
  `PropertyColor` (bound) and `Property<TValue>` (own getter and setter), plus `AddProperty`.
- Toolbar actions: invalid toolbar types and "valid for groups".
- `ListMode` (one set of controls for several block types), `EnsureCreated` (dedicated servers),
  `Created`; all new controls and properties work with both.

### UI service (players only, needs Rich HUD Master, Workshop 1965654081)

- Endpoints: `Ui.State`, `Ui.Events`, `Ui.Release`, `Ui.Create`, `Ui.Remove`, `Ui.Clear`,
  `Ui.SetParent`, `Ui.Part`, `Ui.Listen`, `Ui.SetBool`, `Ui.SetInt`, `Ui.SetFloat`, `Ui.SetVector`,
  `Ui.SetColor`, `Ui.SetText`, `Ui.GetBool`, `Ui.GetInt`, `Ui.GetFloat`, `Ui.GetVector`, `Ui.GetColor`,
  `Ui.GetText`, `Ui.SetVector3`, `Ui.GetVector3`, `Ui.SetMatrix`, `Ui.SetPoints`, `Ui.ItemInsert`,
  `Ui.ItemRemove`, `Ui.ItemText`, `Ui.ItemEnabled`, `Ui.ItemKey`, `Ui.ItemFind`, `Ui.Focus`,
  `Ui.Measure`, `Ui.Screen`, `Ui.ScreenInfo`, `Ui.ResScale`, `Ui.CursorPos`, `Ui.CursorWorld`,
  `Ui.Cursor`, `Ui.Clipboard`, `Ui.SetClipboard`, `Ui.Terminal`, `Ui.TerminalOpen`, `Ui.Bind`,
  `Ui.BindKeys`, `Ui.BindPage`. Element kinds, properties and events are fixed numbers (`UiKind`,
  `UiProp`, `UiEvent`).
- Elements: window, panel, label, label box, button, text field, box (colour or texture), border,
  scroll box, chain, empty, click area, check box, slider; multi-line text box, drop-down, list box,
  tree list, tree box, colour picker (RGB and HSV), on/off button, named check box, named on/off
  button, named slider, scroll bar, slider bar, icon button, label button, bordered button, double
  label box, radial selection; HUD spaces (camera, world matrix, scaled); shapes (line, polygon, ring,
  triangle mesh).
- Rich HUD terminal: page categories, control pages with control categories, tiles and controls
  (button, check box, on/off, colour picker, drag box, drop-down, label, list, slider, text field),
  text pages; opening the terminal on a page.
- Tooltips with markup and own background colour, clipboard, screen values (aspect ratio, field of
  view, input mode, cursor state), key binds with a rebind page.
- Text markup: `[b]`, `[i]`, `[u]`, `[color=..]`, `[size=..]`, `[font=..]`, `[align=..]`, `[link=..]`,
  closed with `[/tag]`; `[[` is a literal `[`.
- The Rich HUD Framework client (MIT, Zach Hembree) is now part of Syntrix; Rich HUD Master is a
  dependency.

## 1.0.0 (API version 1)

First version.

- Docking: `syx.register` / `syx.hello` / `syx.bye` on mod message channel 8346120579001.
- Endpoints: `Info.IsServer`, `Info.IsPlayer`, `Info.PromoteLevel`, `Net.Register`, `Net.Unregister`,
  `Net.SendToServer`, `Net.SendToPlayer`, `Net.SendToAll`, `Block.Get`, `Block.Set`, `Block.OnChanged`,
  `Block.RemoveOnChanged`, `Block.RequestSync`, `Chat.Register`, `Events.Subscribe`,
  `Events.Unsubscribe`, `Events.Publish`, `Notify.Player`.
- Secure network channel 52741, block storage GUID 583FC123-E62D-4269-A84C-14B32976D720.
- Dock file helpers: `SyntrixText`, `SyntrixLog`, `SyntrixControls<TBlock>`.
