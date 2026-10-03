using System;
using System.Linq;
using UnityEngine;

namespace Ember.Core.Phase2
{
    public enum Mechanic { Slam, Projectile, Charge, Summon, Storm, Drain, Shield, Survival, Doom }
    public enum TargetRule { Random, LowestHealth, Farthest, HighestDamage }
    public enum CueShape { Circle, Lane, Cross, Annulus }
    [Serializable] public class FloorDefinition
    {
        public int floor, difficultyTier; public string id, nameKey, theme, bossId, hazard, music, ambience, lighting, vfx;
        public string[] arenaRules, lootTables, restPool, threatTags, intelTags;
        public bool placeholder;
        public string arenaLayout,introKey,deathKey,intelKey,lootIdentity;
        public DifficultyVector difficulty;
    }
    [Serializable] public class DifficultyVector { public float effectiveHp,incomingDamage,attackFrequency,aoePressure,positioningPressure,reactionWindow,mechanicComplexity,resourceAttrition,buildDependency,coordinationDependency; }
    [Serializable] public class AbilityDefinition
    {
        public string id, nameKey, element, status; public Mechanic mechanic; public TargetRule target;
        public float damage, interval, windup, radius, duration, interruptWindow;
        public bool interruptible;
        public CueShape shape;public float length=16,innerRadius,push,resourceDrain;
    }
    [Serializable] public class PhaseDefinition
    {
        public string nameKey; public float hpBelow=1, afterSeconds, enrageAfter=90, dpsDeadline, requiredDamage;
        public string[] abilities; public float resistance, weakness;
    }
    [Serializable] public class BossDefinition
    {
        public string id, nameKey, archetype, weaknessElement, resistElement, deathMechanic;
        public float hp, armor; public PhaseDefinition[] phases; public bool placeholder;
        public string movement="",presentation="";public float ambientPressure;
    }
    [Serializable] public class LootTable { public string id,affix,infusion; public string[] items; public int rolls, materials; }
    [Serializable] public class RestSite { public string id, nameKey, effect; public float x,z,seconds,risk,reward,travelSeconds,escapeSeconds; public int materialCost; }
    [Serializable] public class RestDefinition
    {
        public string id;public float collapseAfter,collapseSpeed;
        public float emptyChance=.15f,facilityChance=.65f;public RestSite[] sites;
        public bool HasSite(GroupState g,string site)=>!g.restSitesRolled||g.availableRestSites.Contains(site);
        public int AvailableCount(GroupState g)=>g.restSitesRolled?g.availableRestSites.Count:sites.Length;
        public RestSite[] AvailableSites(GroupState g)=>sites.Where(s=>HasSite(g,s.id)).ToArray();
    }
    [Serializable] public class EnvironmentProfile { public string id; public float r,g,b, fog; public string lighting,vfx,music,ambience; }
    [Serializable] public class TowerContent
    {
        public int version; public FloorDefinition[] floors; public BossDefinition[] bosses; public AbilityDefinition[] abilities;
        public LootTable[] loot; public RestDefinition[] rests; public EnvironmentProfile[] environments;
        public FloorDefinition Floor(int n)=>Array.Find(floors,f=>f.floor==n);
        public BossDefinition Boss(string id)=>Array.Find(bosses,b=>b.id==id);
        public AbilityDefinition Ability(string id)=>Array.Find(abilities,a=>a.id==id);
        public RestDefinition Rest(string id)=>Array.Find(rests,r=>r.id==id);
        public static TowerContent Load()=>JsonUtility.FromJson<TowerContent>(Resources.Load<TextAsset>("tower").text);
        public void Validate(Catalog catalog)
        {
            if(version!=2||floors.Length!=25||floors.Select(f=>f.floor).Distinct().Count()!=25)throw new InvalidOperationException("Tower must define floors 1-25");
            foreach(var f in floors)
            {
                if(f.floor<1||f.floor>25||Boss(f.bossId)==null||!environments.Any(e=>e.id==f.theme)||f.restPool.Length==0||f.lootTables.Length==0)throw new InvalidOperationException("Invalid floor "+f.id);
                foreach(var id in f.restPool)if(Rest(id)==null)throw new InvalidOperationException("Invalid rest reference");
                foreach(var id in f.lootTables)if(!loot.Any(l=>l.id==id))throw new InvalidOperationException("Invalid loot reference");
            }
            foreach(var b in bosses)foreach(var p in b.phases)foreach(var id in p.abilities)if(Ability(id)==null)throw new InvalidOperationException("Invalid ability reference "+id);
            foreach(var l in loot)foreach(var id in l.items)if(catalog.Item(id)==null)throw new InvalidOperationException("Invalid item reference "+id);
            if(bosses.Select(b=>b.id).Distinct().Count()!=bosses.Length||abilities.Select(a=>a.id).Distinct().Count()!=abilities.Length)throw new InvalidOperationException("Duplicate content ID");
        }
    }
}
