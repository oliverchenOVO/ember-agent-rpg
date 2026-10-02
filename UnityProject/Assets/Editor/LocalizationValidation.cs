using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Ember.Core;
using UnityEditor;
using UnityEngine;

namespace Ember.Editor
{
    public static class LocalizationValidation
    {
        static void Require(bool value,string message){if(!value)throw new Exception("Localization: "+message);}
        public static void Run()
        {
            string fontPath="Assets/Resources/Fonts/NotoSansCJKtc-Regular.otf";
            var importer=(TrueTypeFontImporter)AssetImporter.GetAtPath(fontPath);
            importer.fontTextureCase=FontTextureCase.Dynamic;importer.includeFontData=true;importer.fontNames=new[]{"Noto Sans CJK TC"};importer.SaveAndReimport();
            var font=Resources.Load<Font>("Fonts/NotoSansCJKtc-Regular");Require(font!=null,"bundled font missing");
            var en=JsonUtility.FromJson<StringTable>(Resources.Load<TextAsset>("Localization/en").text);
            var zh=JsonUtility.FromJson<StringTable>(Resources.Load<TextAsset>("Localization/zh-TW").text);
            var english=en.entries.ToDictionary(e=>e.key,e=>e.value);var chinese=zh.entries.ToDictionary(e=>e.key,e=>e.value);
            Require(english.Keys.OrderBy(k=>k).SequenceEqual(chinese.Keys.OrderBy(k=>k)),"locale keys differ");
            var glyphs=new HashSet<char>();
            foreach(var e in zh.entries)
            {
                var placeholders=Regex.Matches(e.value,"\\{[0-9]+\\}").Cast<Match>().Select(m=>m.Value).OrderBy(v=>v);
                Require(placeholders.SequenceEqual(Regex.Matches(english[e.key],"\\{[0-9]+\\}").Cast<Match>().Select(m=>m.Value).OrderBy(v=>v)),"placeholder mismatch: "+e.key);
                Require(!string.IsNullOrWhiteSpace(e.value),"empty translation: "+e.key);
                if(e.key.StartsWith("epitaph."))Require(e.value.Length<=80&&english[e.key].Length<=80,"epitaph exceeds word limit");
                foreach(char ch in e.value)if(!char.IsControl(ch))glyphs.Add(ch);
            }
            foreach(char ch in "KAELLYRAORINSERA0123456789 /:+×—†•.,→←")glyphs.Add(ch);
            var missing=glyphs.Where(ch=>!font.HasCharacter(ch)).ToArray();Require(missing.Length==0,"missing glyphs: "+new string(missing));
            Loc.SetLocale("zh-TW");
            Require(Loc.T("ui.pause")=="暫停"&&Loc.Profession(Profession.Warrior)=="戰士","default Taiwanese terminology");
            string token=Loc.Token("event.gift","KAEL",Loc.Ref("item","greatsword"),Loc.Ref("skill","heal"));
            string inZh=Loc.Render(token);Loc.SetLocale("en");Require(Loc.Render(token)!=inZh&&Loc.Render(token).Contains("Mend"),"persisted nested token switches language");Loc.SetLocale("zh-TW");
            Require(Loc.Render("Use Crescent slash; it fits the current opening.").Contains("月牙斬"),"legacy dynamic English skill message");
            Require(Loc.Render("My path ends here: the collapsing refuge.").Contains("避難層坍塌"),"legacy death cause");
            Require(Loc.Render("I choose Warrior. This life will be my own.").Contains("戰士"),"legacy enum interpolation");
            Require(Loc.Render("The tower took me. Watch the marked ground and the falling refuge.").Contains("高塔"),"legacy epitaph");
            Require(Loc.Render(token)==Loc.Render(JsonUtility.FromJson<TextArguments>(JsonUtility.ToJson(new TextArguments{values=new[]{token}})).values[0]),"tokens survive JSON escaping");
            var catalog=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("catalog").text);
            foreach(var sk in catalog.skills)Require(chinese.ContainsKey("skill."+sk.id),"skill name missing: "+sk.id);
            foreach(var it in catalog.items)Require(chinese.ContainsKey("item."+it.id),"item name missing: "+it.id);
            foreach(Profession cls in Enum.GetValues(typeof(Profession)))Require(chinese.ContainsKey("class."+cls),"class missing");
            foreach(ActionKind action in Enum.GetValues(typeof(ActionKind)))Require(chinese.ContainsKey("action."+action),"action missing");
            foreach(Outcome outcome in Enum.GetValues(typeof(Outcome)))Require(chinese.ContainsKey("outcome."+outcome),"outcome missing");
            var tower=Ember.Core.Phase2.TowerContent.Load();
            foreach(var f in tower.floors)Require(chinese.ContainsKey(f.nameKey),"floor name missing");
            foreach(var b in tower.bosses){Require(chinese.ContainsKey(b.nameKey),"boss name missing");foreach(var phase in b.phases)Require(chinese.ContainsKey(phase.nameKey),"phase name missing");}
            foreach(var ability in tower.abilities)Require(chinese.ContainsKey(ability.nameKey),"ability name missing");
            foreach(var rest in tower.rests)foreach(var site in rest.sites)Require(chinese.ContainsKey(site.nameKey),"rest site name missing");
            Require(Loc.MissingKeys.Count==0,"unresolved string keys");
            // The current project uses IMGUI. Enforce the actual state instead of claiming nonexistent TMP fallback coverage.
            bool tmp=Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro")!=null;
            File.WriteAllText(Path.GetFullPath("../Artifacts/localization-tests.txt"),"PASS: "+zh.entries.Length+" keys in each locale; placeholder parity; "+glyphs.Count+" distinct glyphs covered; nested language switching; legacy records; catalog/enum coverage.\nTextMeshPro installed: "+tmp+"; current UI: IMGUI; embedded dynamic Noto Sans CJK TC font.\n");
            Debug.Log("EMBER LOCALIZATION VALIDATION PASSED / "+zh.entries.Length+" keys / "+glyphs.Count+" glyphs");
        }
    }
}
