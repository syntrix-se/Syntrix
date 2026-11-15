# UI service (API 2)

Windows, lists, buttons, text fields, images and HUD overlays for your mod, drawn by the Rich HUD
Framework. Space Engineers mods cannot use classes of another mod, so Syntrix carries the Rich HUD
client, creates the real elements and hands your mod **handles**. The dock file
`Api/SyntrixUi.cs` wraps the handles in typed classes.

Needs **Rich HUD Master** (Workshop 1965654081) in the world; it is a dependency of Syntrix.
Players only: on a dedicated server the service is off (`State == -1`) and every call does nothing.

## Setup

```csharp
readonly SyntrixApi _syx = new SyntrixApi();
SyntrixUi _ui;

public override void LoadData()
{
    _syx.Load("MyMod");
    _ui = new SyntrixUi(_syx);
    _ui.Load(Build, onLost: null, onUnavailable: () => { /* tell the player, e.g. in your own language */ });
    _ui.Bind("OpenWindow", "F7", () => { if (_window != null) _window.Toggle(); });
    _ui.BindPage("My Mod");
}

protected override void UnloadData()
{
    _ui.Unload();
    _syx.Unload();
}
```

- `onReady` (`Build` above) runs when Rich HUD is ready. Build all elements there. If Rich HUD
  restarts, all elements are lost: `onLost` runs, then `onReady` again.
- `Unload` removes all elements of the mod and saves its key binds.
- Without Syntrix or Rich HUD every factory returns an element with `Handle == 0`; all calls on it
  are ignored, nothing throws.

## Elements

| Factory | Rich HUD element | Use |
|---|---|---|
| `Window(title)` | window | header, body, drag, resize; starts hidden, `Open`/`Close`/`Toggle` |
| `Panel(parent)` | background + border + mouse input | rows, cards, tabs, custom buttons; `HighlightColor` under the cursor |
| `Label(parent, text)` | label | markup text, wrapping, links (`OnLink`) |
| `LabelBox(parent, text)` | label box | text on a coloured background (badges) |
| `Button(parent, text)` | label box button | text button with highlight and optional border |
| `TextField(parent)` | text field | one line entry, placeholder, filter, `OnTextChanged`, `OnSubmit` |
| `Box(parent, texture)` | textured box | colour or image (transparent material), atlas regions |
| `Border(parent)` | border box | frame line |
| `ScrollBox(parent, vertical)` | scroll box | lists; children become entries, hidden children leave the list |
| `Chain(parent, vertical)` | chain | stacks that size themselves |
| `Empty(parent)` | empty element | anchor, spacer, group |
| `ClickArea(parent)` | mouse input | invisible click zone |
| `CheckBox(parent)`, `Slider(parent)` | check box, slider box | values with `OnChanged` |

Parents: another element, `null`/`ui.Root` (scaled UI units) or `ui.PixelRoot` (screen pixels, for
HUD overlays sized with `ui.ScreenSize`). Children of a window go into its body; `window.Header`
and `window.Body` are the fixed parts. Children of a scroll box are list entries; for an overlay
(e.g. "list is empty") use `scrollBox.Background` as parent.

Layout: `ParentAlign` (`UiAlign` flags, e.g. `InnerTopLeft`), `Offset` (y points up), `Size`,
`DimAlign` (`UiDim`: take the parent's width, height or size), `Padding`, `ZOffset`, `Masking`.
`window.OnResized` and `panel.OnResized` tell you when to lay out again. `label.WrapToParent(margin, max)`
wraps at the parent's width and follows resizing. `ui.MeasureText(markup, wrapWidth, styleFrom)`
gives the size of a text before you place it (tables, shortening with "…").

Every property is also reachable as `element.Set(UiProp.X, value)` / `Get…(UiProp.X)`.

## Events

`OnClick`, `OnRightClick`, `OnCursorEnter`, `OnCursorExit` (panels, buttons, click areas, check
boxes), `OnLink` (labels), `OnTextChanged`, `OnSubmit` (text fields), `OnChanged` (check box,
slider), `OnResized`, `OnClosed` (windows: Escape, `Close`, `Visible = false`).
Events are queued and run outside of Rich HUD's own update, so a handler may remove or rebuild
any element. One handler per element and event; setting another replaces it.

## Text markup

| Tag | Effect |
|---|---|
| `[b]…[/b]`, `[i]…[/i]`, `[u]…[/u]` | bold, italic, underline (if the font has the style) |
| `[color=#RRGGBB]…[/color]` | colour, also `#RRGGBBAA` or a name (white, black, gray, red, green, blue, yellow, cyan, orange, magenta) |
| `[size=1.3]…[/size]` | text size, 1 = normal |
| `[font=BitstreamVeraSans]…[/font]` | Rich HUD font |
| `[align=center]…[/align]` | left, center, right |
| `[link=target]…[/link]` | clickable; `OnLink` gets `target`; shown in `LinkColor` |
| `[[` | a literal `[` (`SyntrixUi.Escape(text)` does this for player text) |

Closing a tag also closes tags opened after it. Unknown tags stay as text. The base format comes
from the element: `TextColor`, `FontSize`, `Font`, `FontStyle`, `TextAlign`. `PlainText` sets text
without markup.

## Windows and input

While a window with `Modal` (default) is open, Syntrix shows the cursor and keeps mouse and camera
from the game; `LockView` (default) also keeps the view still in seats; `HideHud` hides the game
HUD and restores it on closing. Escape closes the focused window (`CloseOnEscape`). A text field
takes typing after a click into it; while typing no key reaches the game and key binds do not fire.
`ui.ShowCursor(true)` shows the cursor without a window.

## Key binds

`ui.Bind(name, "F11", action)` or `"Control+F1"` (up to three keys, Rich HUD control names).
`ui.BindPage(title)` adds a rebind page for the mod's binds to the Rich HUD terminal (F2, under
"Syntrix"); call it after the binds. Syntrix saves the player's choice per mod in its local storage.
`ui.BindKeys(name)` returns the current keys for help texts.

## HUD overlays and performance

HUD elements without a window (compass, markers) are made on `ui.PixelRoot` and moved in your own
`Draw` every frame. `Offset`, `Size`, `Visible` and the other number, bool and colour properties do
not allocate; set texts only when they change.

## Limits

Not offered: multi-line text boxes, drop-downs, tree lists, colour pickers, Rich HUD terminal pages
other than the rebind page, world-space (3D) HUD nodes, own subclasses with custom drawing.
There is no access to single characters of a text; use links instead.

## Endpoints

`Ui.State`, `Ui.Events`, `Ui.Release`, `Ui.Create`, `Ui.Remove`, `Ui.Clear`, `Ui.SetParent`,
`Ui.Part`, `Ui.Listen`, `Ui.SetBool|Int|Float|Vector|Color|Text`, `Ui.GetBool|Int|Float|Vector|Color|Text`,
`Ui.Focus`, `Ui.Measure`, `Ui.Screen`, `Ui.ResScale`, `Ui.CursorPos`, `Ui.Cursor`, `Ui.Bind`,
`Ui.BindKeys`, `Ui.BindPage`. Kinds, properties and events are fixed numbers in `UiKind`, `UiProp`
and `UiEvent`; they never change meaning.
