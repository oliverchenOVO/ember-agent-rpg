using System;
using System.IO;
using System.Linq;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;
namespace Ember.Editor
{
    public static class PressureValidation
    {
        static void Check(bool ok,string reason){if(!ok)throw new Exception("Pressure: "+reason);}
        public static void Run()
        {
            var c=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("catalog").text);var data=TowerContent.Load();int checks=0;
            using(var s=new TowerSimulation(c,data,1729,true))
            {
                var g=s.State.groups[0];g.floor=8;g.boss=BossRuntime.Create(data.Boss(data.Floor(8).bossId));var b=g.boss;var a=s.State.world.agents[0];var node=b.pressureCores[0];
                Check(b.pressureCores.Count==3&&b.pressureCores.All(n=>n.stage==2),"initial sources active without warning");checks++;
                PressureField.Tick(b,3);Check(node.stage==0&&Mathf.Abs(node.left-3)<.001f&&node.activeThisTick==0,"initial warning missing");checks++;
                a.x=node.x;a.z=node.z;Check(PressureField.Rate(data,b,a.x,a.z)==0&&PressureField.Threat(b,a.x,a.z),"warning hurts");checks++;
                PressureField.Tick(b,3);Check(node.stage==1&&node.activeThisTick==0,"warning duration");checks++;
                PressureField.Tick(b,.5f);float raw=PressureField.DamageDuringTick(data,b,a);Check(raw>0&&Mathf.Abs(raw-data.Boss(b.definition).ambientPressure*.5f)<.001f,"pulse rate");checks++;
                a.x=0;a.z=0;Check(PressureField.DamageDuringTick(data,b,a)==0&&PressureField.Rate(data,b,a.x,a.z)==0,"outside still hurts");checks++;
                a.x=node.x;a.z=node.z;node.stage=0;node.left=2;b.elapsed=239.9f;PressureField.Tick(b,.2f);b.elapsed=240.1f;PressureField.Tick(b,.1f);Check(node.stage==0&&node.left>1.69f,"overtime skips warning");checks++;
                node.stage=1;node.left=.2f;PressureField.Tick(b,.5f);Check(node.stage==2&&Mathf.Abs(node.left-5.7f)<.001f&&Mathf.Abs(node.activeThisTick-.2f)<.001f,"overtime cooldown or integrated damage");checks++;
                node.hp=0;Check(PressureField.Forecast(data,b,a,30)==0&&!PressureField.Threat(b,a.x,a.z),"destroyed source forecast");checks++;
                node.hp=node.maxHp;node.stage=0;node.left=1;Check(PressureField.Forecast(data,b,a,3)>0&&node.left==1&&node.stage==0,"predictive healing mutation or missing pulse");checks++;
                for(int i=0;i<3;i++){b.pressureCores[i].stage=i;b.pressureCores[i].left=i+1;}b.pressureCores[2].hp=0;
                string saved=JsonUtility.ToJson(s.State);var restored=ExpeditionStore.Parse(saved,c,data);Check(restored.groups[0].boss.pressureCores.Select(n=>JsonUtility.ToJson(n)).SequenceEqual(b.pressureCores.Select(n=>JsonUtility.ToJson(n))),"source save/load");checks++;
                var copy=restored.groups[0].boss;for(int i=0;i<300;i++){PressureField.Tick(b,.1f);PressureField.Tick(copy,.1f);}Check(copy.pressureCores.Select(n=>JsonUtility.ToJson(n)).SequenceEqual(b.pressureCores.Select(n=>JsonUtility.ToJson(n))),"30-second timer replay");checks++;
                b.pressureCores=null;b.pressureVersion=0;var legacy=ExpeditionStore.Parse(JsonUtility.ToJson(s.State).Replace("\"pressureVersion\":0,","").Replace("\"pressureCores\":[],",""),c,data);var old=legacy.groups[0].boss;old.visible.timer=10000;old.Tick(data,legacy.Members(legacy.groups[0]).ToList(),legacy.world,.1f,(v,h,t)=>{});Check(old.pressureCores.Count==3&&old.pressureCores.All(n=>n.stage==2),"legacy initialization without warning");checks++;
                Check(!JsonUtility.FromJson<SkillPresentation>("{\"skill\":\"basic\",\"agent\":0,\"target\":-1}").pressureAttack,"legacy cast targets a core");checks++;
                b.pressureVersion=1;b.pressureCores=PressureField.Create(data.Boss(b.definition));b.pressureCores[0].stage=9;bool rejected=false;try{ExpeditionStore.Parse(JsonUtility.ToJson(s.State),c,data);}catch(InvalidDataException){rejected=true;}Check(rejected,"invalid source accepted");checks++;
            }
            using(var s=new TowerSimulation(c,data,1729,true))
            {
                var g=s.State.groups[0];g.floor=8;g.boss=BossRuntime.Create(data.Boss(data.Floor(8).bossId));var b=g.boss;var members=s.State.Members(g).ToList();var node=b.pressureCores[0];node.stage=1;node.left=5;
                foreach(var a in members){a.x=node.x;a.z=node.z;}members[1].alive=false;members[2].escaped=true;b.visible.timer=10000;int mask=0;
                b.Tick(data,members,s.State.world,.1f,(a,raw,t)=>mask|=1<<a.id);Check(mask==9,"dead or escaped agent hurt by pressure");checks++;
                foreach(var n in b.pressureCores)n.hp=0;b.elapsed=1000;b.visible.timer=10000;b.phase=data.Boss(b.definition).phases.Length-1;b.phaseTime=0;int hits=0;
                b.Tick(data,members,s.State.world,.1f,(a,raw,t)=>hits++);Check(hits==0,"hidden global overtime attrition remains");checks++;
            }
            foreach(uint seed in new uint[]{1729,1730})using(var s=new TowerSimulation(c,data,seed,true))
            {
                var g=s.State.groups[0];g.floor=8;g.boss=BossRuntime.Create(data.Boss(data.Floor(8).bossId));var b=g.boss;b.visible.timer=10000;float initial=b.pressureCores.Sum(n=>n.hp);
                bool seen=false;for(int i=0;i<400;i++){s.Step();seen|=b.skillEvents.Any(e=>e.pressureAttack);}
                Check(b.pressureCores.All(n=>n.hp==0),"AI did not disable all cores, seed "+seed+" / remaining="+b.pressureCores.Sum(n=>n.hp));
                Check(seen,"AI core attacks have no presentation");checks+=2;
                Debug.Log("PRESSURE AI FIXTURE / seed="+seed+" / 40 seconds / core HP "+initial+" -> "+b.pressureCores.Sum(n=>n.hp));
            }
            Debug.Log("PRESSURE VALIDATION PASSED / checks="+checks+" / localized damage, warning, overtime, prediction, AI, save replay, legacy, invalid saves");
        }
    }
}
