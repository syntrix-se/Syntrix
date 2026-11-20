# Syntrix AutoLCD

Mod script for Space Engineers that fills text screens on its own, **without a programmable block**: inventory, cargo, power, jump drives, block states, damage, gas, text, time, ship values and faction reputation. It runs on the server (or in single player) and writes the screens with `WriteText`, like a programmable block would, so in-game scripts can stay off.

Compatible with the operation of **Automatic LCDs 2 by MMaster** ([script](https://steamcommunity.com/sharedfiles/filedetails/?id=822950976), [guide](https://steamcommunity.com/sharedfiles/filedetails/?id=407158161)): same commands, variants, name filters, screen headers and name switches, so existing screens and blueprints keep working. **No code, texts or data of the original were used**; this is an independent implementation.

## Use

- Put `[LCD]` or `!LCD!` into the name of a text panel, cockpit or any block with screens.
- Write commands into its custom data, one per line (`Inventory *`, `Cargo *`, `Power *`, `Echo Hello` ...). Blocks with several screens: header line `@0 AutoLCD` or `#0 AutoLCD` per screen.
- Everything below a line `---` is read; the part above belongs to other scripts.
- Typing without AltGr: `"Name"` for `{Name}`, `&` for `\` at the line end, `#0` for `@0`, `'text'` in `DateTime`, `!LCD!` for `[LCD]`.
- Name switches of text panels: `!LINK:<group> <n>`, `!MARGIN:<n>`, `!NOSCROLL`.

## Old programmable blocks

Blocks holding the original script (or AutoLCD settings in their custom data) are read even with scripts off: `LCD_TAG`, `SlowMode`, `SCROLL_LINES`, `SKIP_CONTENT_TYPE`, `ENABLE_BOOT`, without recompiling. Their screens use the block as reference for `T:` and `C:`. "Run" with the arguments `scrollUp`, `scrollDown`, `clear`, `boot` is intercepted while scripts are off, so buttons and timers keep working. While the real script runs in a block (scripts on), its screens are left alone.

## Actions and chat

- Toolbar actions on every block with screens: `SyxAutoLcd.ScrollUp`, `SyxAutoLcd.ScrollDown`, `SyxAutoLcd.Clear`, `SyxAutoLcd.Boot`.
- Chat `/syxlcd help | scroll up|down "name" [lines] | stats | reload | pause | resume` (the last four for admins).
- The terminal info text of a screen shows its state and errors in its commands.

## Server load

A fixed time budget per tick for all screens of the world (`MaxMsPerTick`), measured with `Stopwatch`; work is split into small steps and constructs take turns. Screens without a player within `ViewDistance` rest, results of the same query are shared between screens, and text is only written when it changed.

## Settings

`Syntrix-AutoLcd.xml` in the world storage of Syntrix (written with defaults on first start): budget, view distance, screen language (`Language`, extra `LanguageFiles`), tags, defaults of the programmable block settings, bar characters, boot frames, default quotas per item type, item short names and quotas, size factors of modded screens, own texts.

## Other mods

Other mods add or replace commands through Syntrix events: publish `SyxAutoLcd.RegisterCommand` with `MyTuple<string, Func<IMyTerminalBlock, string[], List<MyTuple<string, string, float>>>>` (command name; function returning label, value and fill 0 to 1, negative = no bar; a null function removes the command). AutoLCD publishes `SyxAutoLcd.Ready` when it starts, so providers register on their own start and on that event. A provider command with the name of a built-in one replaces it. `SyxAutoLcd.RegisterCommandTimed` does the same with `MyTuple<string, double, Func<…>>`, the middle value being the seconds between two runs (1 to 60; the plain event uses 3).

## Requirements

Part of Syntrix since 1.5.0; it uses the Syntrix services for chat, network, settings and events directly and runs
in the active Syntrix copy only. Texts: `Data/Localization/MyTexts_AutoLcd*.resx` of any loaded mod, English
fallbacks in the code.

## License

MIT License, Copyright (c) 2026 Syntrix contributors.
