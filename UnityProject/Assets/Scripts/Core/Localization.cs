using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Ember.Core
{
    [Serializable] public class StringEntry { public string key, value; }
    [Serializable] public class StringTable { public string locale; public StringEntry[] entries; }
    [Serializable] public class TextArguments { public string[] values; }

    // Tokens are serialized in existing text fields: IDs and save schema remain stable.
    // They contain a semantic key + arguments, not a frozen translation.
    public static class Loc
    {
        public static string Locale {get; private set;}="zh-TW";
        public static event Action Changed;
        static Dictionary<string,string> active, english;
        static readonly Dictionary<string,string> legacyExact=new Dictionary<string,string>();
        static readonly List<(Regex pattern,string key)> legacyTemplates=new List<(Regex,string)>();
        public static readonly HashSet<string> MissingKeys=new HashSet<string>();
        public static void SetLocale(string locale)
        {
            if(locale!="zh-TW"&&locale!="en")throw new ArgumentException("Unsupported locale: "+locale);
            EnsureEnglish();active=Read(locale);Locale=locale;Changed?.Invoke();
        }
        static Dictionary<string,string> Read(string locale)
        {
            var asset=Resources.Load<TextAsset>("Localization/"+locale);if(asset==null)throw new InvalidOperationException("Missing locale table: "+locale);
            var table=JsonUtility.FromJson<StringTable>(asset.text);var result=new Dictionary<string,string>();
            foreach(var e in table.entries)result.Add(e.key,e.value);return result;
        }
        static void EnsureEnglish()
        {
            if(english!=null)return;english=Read("en");
            // Import old schema-1 English records at display time; never discard old save data.
            foreach(var pair in english)
            {
                if(!pair.Value.Contains("{0}")) {if(!legacyExact.ContainsKey(pair.Value))legacyExact.Add(pair.Value,pair.Key);continue;}
                if(pair.Value=="{0}"||(!pair.Key.StartsWith("event.")&&!pair.Key.StartsWith("reason.")&&!pair.Key.StartsWith("memory.")))continue;
                string regex=Regex.Escape(pair.Value);
                for(int i=0;i<10;i++)regex=regex.Replace(Regex.Escape("{"+i+"}"),"(.*?)");
                legacyTemplates.Add((new Regex("^"+regex+"$",RegexOptions.CultureInvariant),pair.Key));
            }
            foreach(var e in english) if(e.Key.StartsWith("skill.")||e.Key.StartsWith("item."))
            {
                string id=e.Key.Substring(e.Key.IndexOf('.')+1);if(!legacyExact.ContainsKey(id))legacyExact.Add(id,e.Key);
            }
        }
        static string Arg(object value) => value==null?"":Convert.ToString(value,CultureInfo.InvariantCulture);
        public static string Token(string key,params object[] args)
        {
            var values=new string[args.Length];for(int i=0;i<values.Length;i++)values[i]=Arg(args[i]);
            return "@"+key+":"+JsonUtility.ToJson(new TextArguments{values=values});
        }
        public static string Ref(string family,object id) => Token(family+"."+Arg(id));
        public static string T(string key,params object[] args)
        {
            EnsureEnglish();if(active==null)active=Read(Locale);
            if(!active.TryGetValue(key,out string format)) {MissingKeys.Add(key);if(!english.TryGetValue(key,out format))return active["ui.dialogue_unknown"];}
            var values=new object[args.Length];for(int i=0;i<args.Length;i++) {string text=Arg(args[i]);values[i]=text.StartsWith("@")?Render(text):text;}
            return string.Format(CultureInfo.InvariantCulture,format,values);
        }
        public static string Render(string value)
        {
            if(string.IsNullOrEmpty(value))return "";
            if(value.StartsWith("@"))
            {
                int split=value.IndexOf(':');if(split>1)
                {
                    var data=JsonUtility.FromJson<TextArguments>(value.Substring(split+1));
                    return T(value.Substring(1,split-1),data.values??Array.Empty<string>());
                }
            }
            return RenderLegacy(value);
        }
        public static string MigrateLegacy(string value)
        {
            EnsureEnglish();if(string.IsNullOrEmpty(value)||value.StartsWith("@"))return value;
            if(legacyExact.TryGetValue(value,out string key))return Token(key);
            foreach(var rule in legacyTemplates)
            {
                var match=rule.pattern.Match(value);if(!match.Success)continue;
                var args=new object[match.Groups.Count-1];for(int i=1;i<match.Groups.Count;i++)args[i-1]=MigrateLegacy(match.Groups[i].Value);
                return Token(rule.key,args);
            }
            return value;
        }
        static string RenderLegacy(string value)
        {
            string token=MigrateLegacy(value);if(token!=value)return Render(token);
            // Existing free-form Chinese prose and allowed proper names remain readable.
            if(Locale=="en"||Regex.IsMatch(value,"[\\u3400-\\u9fff]")||value=="KAEL"||value=="LYRA"||value=="ORIN"||value=="SERA"||Regex.IsMatch(value,"^[0-9., /:+×—-]+$"))return value;
            return T("ui.dialogue_unknown");
        }
        public static string Profession(Profession p)=>T("class."+p);
        public static string Action(ActionKind action)=>T("action."+action);
        public static string Outcome(Outcome outcome)=>T("outcome."+outcome);
        public static string Skill(string id)=>string.IsNullOrEmpty(id)?T("ui.none"):T("skill."+id);
        public static string Item(string id)=>T("item."+id);
    }
}
