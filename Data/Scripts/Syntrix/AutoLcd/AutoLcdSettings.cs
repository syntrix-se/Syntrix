// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Sandbox.ModAPI;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// Server settings of AutoLCD, file Syntrix-AutoLcd.xml in the world storage of Syntrix (written with the
    /// defaults when missing). Values of an AutoLCD programmable block on a construct (LCD_TAG, SlowMode ...)
    /// override the matching values for the screens of that block.
    /// </summary>
    [XmlRoot("AutoLcdSettings")]
    public class AutoLcdSettings
    {
        /// <summary>Default file text with comments for admins.</summary>
        public const string DefaultText =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<!-- Syntrix AutoLCD server settings. Reload with /syxlcd reload (admin). -->
<AutoLcdSettings>
  <!-- Time budget for all screens of the world per game tick in milliseconds. More screens only update slower. -->
  <MaxMsPerTick>0.5</MaxMsPerTick>
  <!-- Screens without a player within this distance (m) rest. -->
  <ViewDistance>200</ViewDistance>
  <!-- Language of the screen texts: empty = language of the server, or a culture like en, de. -->
  <Language></Language>
  <!-- Extra text files read for that language from every loaded mod (Data/Localization/<File>.<lang>.resx). -->
  <LanguageFiles>
    <File>MyTexts_AutoLcd</File>
  </LanguageFiles>
  <!-- Name tags of managed screens (case sensitive). -->
  <Tags>
    <Tag>[LCD]</Tag>
    <Tag>!LCD!</Tag>
  </Tags>
  <!-- Defaults for screens without an AutoLCD programmable block (same meaning as in the block). -->
  <SlowMode>false</SlowMode>
  <ScrollLines>1</ScrollLines>
  <SkipContentType>false</SkipContentType>
  <EnableBoot>true</EnableBoot>
  <!-- Seconds between two automatic scroll steps. -->
  <ScrollSeconds>2</ScrollSeconds>
  <!-- Read the settings of AutoLCD programmable blocks (also with scripts off). -->
  <ReadProgrammableBlocks>true</ReadProgrammableBlocks>
  <!-- Limits. -->
  <MaxScreensPerConstruct>100</MaxScreensPerConstruct>
  <MaxLinesPerScreen>400</MaxLinesPerScreen>
  <!-- Characters of the text bars. -->
  <BarStart>[</BarStart>
  <BarEnd>]</BarEnd>
  <BarFull>|</BarFull>
  <BarEmpty>&#183;</BarEmpty>
  <!-- Boot frames (one line block per frame, shown one second each); empty = built-in frames. -->
  <BootFrames />
  <!-- Default bar maximum (Inventory) per item type. -->
  <TypeQuotas>
    <Quota Type=""ore"" Value=""100000"" />
    <Quota Type=""ingot"" Value=""50000"" />
    <Quota Type=""component"" Value=""2000"" />
    <Quota Type=""ammo"" Value=""200"" />
    <Quota Type=""tool"" Value=""4"" />
    <Quota Type=""oxygen"" Value=""10"" />
    <Quota Type=""gas"" Value=""10"" />
    <Quota Type=""seed"" Value=""50"" />
    <Quota Type=""consumable"" Value=""50"" />
    <Quota Type=""object"" Value=""100"" />
  </TypeQuotas>
  <!-- List items of mods with amount 0 too (like the items of the game); false = mod items only from 1. -->
  <ShowZeroModItems>false</ShowZeroModItems>
  <!-- Items: extra short names, own quotas, display names, ShowZero (true = also listed with 0, false = only from 1).
       Example: <Item Type=""Component"" Subtype=""AzimuthSupercharger"" Quota=""1600"" Short=""supercharger"" ShowZero=""true"" /> -->
  <Items />
  <!-- Size factors for modded screens with wrong text area. Example: <Screen Subtype=""MyWideLcd"" Width=""1"" Height=""0.5"" /> -->
  <Screens />
  <!-- Own screen texts, override the language files. Example: <Text Key=""SyxAutoLcd_CargoUsed"" Value=""Hold:"" /> -->
  <Texts />
</AutoLcdSettings>
";

        public float MaxMsPerTick = 0.5f;

        public float ViewDistance = 200f;

        public string Language = "";

        [XmlArrayItem("File")]
        public List<string> LanguageFiles = new List<string>();

        [XmlArrayItem("Tag")]
        public List<string> Tags = new List<string>();

        public bool SlowMode;

        public int ScrollLines = 1;

        public bool SkipContentType;

        public bool EnableBoot = true;

        public float ScrollSeconds = 2f;

        public bool ReadProgrammableBlocks = true;

        public int MaxScreensPerConstruct = 100;

        public int MaxLinesPerScreen = 400;

        public bool ShowZeroModItems;

        public string BarStart = "[";

        public string BarEnd = "]";

        public string BarFull = "|";

        public string BarEmpty = "·";

        [XmlArrayItem("Frame")]
        public List<string> BootFrames = new List<string>();

        [XmlArrayItem("Quota")]
        public List<TypeQuota> TypeQuotas = new List<TypeQuota>();

        [XmlArrayItem("Item")]
        public List<ItemSetting> Items = new List<ItemSetting>();

        [XmlArrayItem("Screen")]
        public List<ScreenSetting> Screens = new List<ScreenSetting>();

        [XmlArrayItem("Text")]
        public List<TextSetting> Texts = new List<TextSetting>();

        /// <summary>Default bar maximum of one item type (lower case main type name).</summary>
        public class TypeQuota
        {
            [XmlAttribute]
            public string Type = "";

            [XmlAttribute]
            public double Value;
        }

        /// <summary>Settings of one item.</summary>
        public class ItemSetting
        {
            [XmlAttribute]
            public string Type = "";

            [XmlAttribute]
            public string Subtype = "";

            [XmlAttribute]
            public string Quota = "";

            [XmlAttribute]
            public string Name = "";

            [XmlAttribute]
            public string Short = "";

            [XmlAttribute]
            public string Short2 = "";

            [XmlAttribute]
            public string ShowZero = "";
        }

        /// <summary>Size factors of one screen block subtype.</summary>
        public class ScreenSetting
        {
            [XmlAttribute]
            public string Subtype = "";

            [XmlAttribute]
            public float Width = 1f;

            [XmlAttribute]
            public float Height = 1f;
        }

        /// <summary>Own text for one text key.</summary>
        public class TextSetting
        {
            [XmlAttribute]
            public string Key = "";

            [XmlAttribute]
            public string Value = "";
        }

        /// <summary>Settings with the built-in defaults (used without a file and before it is read).</summary>
        public static AutoLcdSettings CreateDefault()
        {
            var settings = new AutoLcdSettings();
            settings.Complete();
            return settings;
        }

        /// <summary>Parses the file text; falls back to the defaults on errors.</summary>
        public static AutoLcdSettings Parse(string text)
        {
            AutoLcdSettings settings = null;
            if (!string.IsNullOrWhiteSpace(text))
            {
                try
                {
                    settings = MyAPIGateway.Utilities.SerializeFromXML<AutoLcdSettings>(text);
                }
                catch (Exception e)
                {
                    AutoLcdCore.Log.Warn("settings file unreadable, using defaults: " + e.Message);
                }
            }
            if (settings == null)
                settings = new AutoLcdSettings();
            settings.Complete();
            return settings;
        }

        /// <summary>Fills empty lists with the defaults and keeps numbers in sane ranges.</summary>
        private void Complete()
        {
            if (Tags.Count == 0)
            {
                Tags.Add("[LCD]");
                Tags.Add("!LCD!");
            }
            if (!LanguageFiles.Contains("MyTexts_AutoLcd"))
                LanguageFiles.Add("MyTexts_AutoLcd");
            if (TypeQuotas.Count == 0)
            {
                AddQuota("ore", 100000);
                AddQuota("ingot", 50000);
                AddQuota("component", 2000);
                AddQuota("ammo", 200);
                AddQuota("tool", 4);
                AddQuota("oxygen", 10);
                AddQuota("gas", 10);
                AddQuota("seed", 50);
                AddQuota("consumable", 50);
                AddQuota("object", 100);
            }
            MaxMsPerTick = Math.Max(0.05f, Math.Min(MaxMsPerTick, 20f));
            ViewDistance = Math.Max(10f, ViewDistance);
            ScrollLines = Math.Max(1, ScrollLines);
            ScrollSeconds = Math.Max(0.5f, ScrollSeconds);
            MaxScreensPerConstruct = Math.Max(1, MaxScreensPerConstruct);
            MaxLinesPerScreen = Math.Max(10, MaxLinesPerScreen);
            if (string.IsNullOrEmpty(BarFull))
                BarFull = "|";
            if (string.IsNullOrEmpty(BarEmpty))
                BarEmpty = "·";
            if (BarStart == null)
                BarStart = "";
            if (BarEnd == null)
                BarEnd = "";
            if (Language == null)
                Language = "";
        }

        private void AddQuota(string type, double value)
        {
            TypeQuotas.Add(new TypeQuota { Type = type, Value = value });
        }

        /// <summary>Default quota of a lower case main type name, 0 if none.</summary>
        public double QuotaOf(string mainType)
        {
            foreach (var quota in TypeQuotas)
                if (string.Equals(quota.Type, mainType, StringComparison.OrdinalIgnoreCase))
                    return quota.Value;
            return 0;
        }

        /// <summary>Size factors of a screen block subtype (1, 1 if none).</summary>
        public ScreenSetting ScreenOf(string subtype)
        {
            foreach (var screen in Screens)
                if (screen.Subtype == subtype)
                    return screen;
            return null;
        }
    }
}
