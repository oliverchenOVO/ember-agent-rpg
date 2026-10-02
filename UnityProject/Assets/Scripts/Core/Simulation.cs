using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ember.Core
{
    public sealed class Simulation
    {
        public const float StepSeconds=.1f, RestLimit=26, MoveSpeed=3.8f;
        public World State {get; private set;}
        public Catalog Catalog {get; private set;}
        readonly IBrain brain;
        public Simulation(Catalog catalog,uint seed=1729,bool showcase=false,IBrain brain=null)
        {
            Catalog=catalog; this.brain=brain??new UtilityBrain(); State=new World {rng=seed==0?1729:seed,showcase=showcase}; Begin();
        }
        public void Restore(World state) { State=state; }
        public static float Distance(float x,float z,float bx,float bz) => Mathf.Sqrt((x-bx)*(x-bx)+(z-bz)*(z-bz));
        public static float Damage(float raw,float armor,float guard) => Mathf.Max(1,raw*100/(100+Mathf.Max(0,armor)*8)*(1-Mathf.Clamp(guard,0,.8f)));
        public static float Proficiency(Agent a,Catalog c)
        {
            string w=c.Item(a.weapon.id).weapon;
            return (a.profession==Profession.Warrior&&(w=="Sword"||w=="Greatsword"))||(a.profession==Profession.Archer&&w=="Bow")||((a.profession==Profession.Mage||a.profession==Profession.Healer)&&w=="Staff")?1.2f:1;
        }
        public static bool CanCast(Agent a,Catalog c,string id,bool weapon=false)
        {
            var s=c.Skill(id); if(s==null||!a.alive||a.escaped||a.mp<s.mana) return false;
            if(weapon) {if(a.weapon.infusion!=id&&c.Item(a.weapon.id).skill!=id) return false;}
            else if(!a.unlocked.Contains(id)||!a.equipped.Contains(id)||s.profession!=a.profession) return false;
            if(a.cooldowns.Exists(x=>x.id==id&&x.left>0)) return false;
            string type=c.Item(a.weapon.id).weapon;
            // Infused skills retain their physical weapon requirements.
            return string.IsNullOrEmpty(s.weapon)||s.weapon==type||(s.weapon=="Sword"&&type=="Greatsword");
        }
        public bool Unlock(Agent a,string id)
        {
            var s=Catalog.Skill(id);
            if(s==null||s.profession!=a.profession||a.unlocked.Contains(id)||a.skillPoints<s.cost||(!string.IsNullOrEmpty(s.prerequisite)&&!a.unlocked.Contains(s.prerequisite))) return false;
            a.skillPoints-=s.cost; a.unlocked.Add(id); if(a.equipped.Count<4) a.equipped.Add(id); return true;
        }
        public void LevelUp(Agent a)
        {
            a.level++; a.skillPoints++;
            for(int j=0;j<3;j++) Allocate(a);
            var available=new List<SkillDef>(); foreach(var s in Catalog.skills) if(s.profession==a.profession&&!a.unlocked.Contains(s.id)&&(string.IsNullOrEmpty(s.prerequisite)||a.unlocked.Contains(s.prerequisite))) available.Add(s);
            if(available.Count>0) Unlock(a,available[State.Pick(available.Count)].id);
            State.Say(a.id,"Level "+a.level+". I chose a new path for my build.","build");
        }
        void Allocate(Agent a)
        {
            // Bias toward class strengths without excluding any stat.
            int preferred=a.profession==Profession.Warrior?0:a.profession==Profession.Archer?1:a.profession==Profession.Mage?2:4;
            a.stats.Add(State.Roll()<.58f?preferred:State.Pick(7),1);
        }
        public void Begin()
        {
            var w=State; var old=w.agents; w.agents=new List<Agent>();
            bool fullClear=w.outcome==Outcome.TowerClear;
            string[] names={"KAEL","LYRA","ORIN","SERA"};
            for(int i=0;i<4;i++)
            {
                var prior=old.Count==4?old[i]:null;
                var p=prior?.personality??new Personality {risk=.25f+w.Roll()*.65f,greed=.2f+w.Roll()*.7f,curiosity=.2f+w.Roll()*.7f,empathy=.2f+w.Roll()*.7f,loyalty=.3f+w.Roll()*.6f,aggression=.25f+w.Roll()*.7f};
                // Weighted choice allows every class and repeated composition.
                float[] weights={.3f+p.aggression,.3f+p.risk,.3f+p.curiosity,.3f+p.empathy};
                float roll=w.Roll()*(weights[0]+weights[1]+weights[2]+weights[3]); int cls=0;
                while(cls<3&&roll>weights[cls]) {roll-=weights[cls];cls++;}
                if(w.showcase&&w.run==1) cls=i;
                var d=Catalog.Class((Profession)cls);
                var a=new Agent {id=i,name=names[i],personality=p,profession=(Profession)cls,stats=JsonUtility.FromJson<Stats>(JsonUtility.ToJson(d.stats)),x=-6+i*3.5f,z=-5,weapon=null};
                for(int j=0;j<5;j++) Allocate(a);
                a.hp=a.MaxHp; a.mp=a.MaxMp; a.weapon=a.Make(d.weapon); a.unlocked.Add(d.starter);a.equipped.Add(d.starter);
                if(cls==3) {a.unlocked.Add("heal");a.equipped.Add("heal");}
                a.inventory.Add(a.Make("hp"));a.inventory.Add(a.Make("mp"));
                if(fullClear&&prior!=null&&prior.alive) {a.memory=new List<string>(prior.memory);a.bonds=JsonUtility.FromJson<Agent>(JsonUtility.ToJson(prior)).bonds;}
                else for(int j=0;j<4;j++) if(j!=i) a.bonds.Add(new Bond {target=j});
                w.agents.Add(a);
            }
            w.clock=0;w.phaseClock=0;w.phase=Phase.Battle;w.outcome=Outcome.None;w.restartTimer=0;w.floor=1;
            w.boss=new BossState {hp=Catalog.boss.hp,maxHp=Catalog.boss.hp}; w.messages.Clear();
            w.Say(-1,"RUN "+w.run+" / Four lives enter the Witness Tower.","run");
            foreach(var a in w.agents) w.Say(a.id,"I choose "+a.profession+". This life will be my own.","build");
        }
        public void Step(float dt=StepSeconds)
        {
            var w=State;
            if(w.phase==Phase.Ended) {w.restartTimer+=dt;if(w.restartTimer>=8) {w.run++;Begin();} return;}
            w.clock+=dt;w.phaseClock+=dt;
            if(w.phase==Phase.Battle) UpdateBoss(dt);
            foreach(var a in w.agents)
            {
                if(!a.alive||a.escaped) continue;
                a.guard=Mathf.Max(0,a.guard-dt*.055f); a.enchant=Mathf.Max(0,a.enchant-dt);
                a.mp=Mathf.Min(a.MaxMp,a.mp+dt*(1.4f+a.stats.wis*.05f));
                foreach(var cd in a.cooldowns) cd.left=Mathf.Max(0,cd.left-dt);
                a.attackTimer=Mathf.Max(0,a.attackTimer-dt); a.decisionTimer-=dt;
                if(a.decisionTimer<=0&&a.taskTimer<=0)
                {
                    var next=brain.Decide(w,a,Catalog);
                    if(a.intent.kind!=next.kind||a.intent.skill!=next.skill||a.intent.target!=next.target) w.Say(a.id,next.reason);
                    if(w.phase==Phase.Rest&&a.intent.kind!=next.kind) Discuss(a,next);
                    a.intent=next;a.decisionTimer=1.5f;
                }
                if(w.phase==Phase.Battle) Battle(a,dt); else Rest(a,dt);
            }
            if(w.agents.TrueForAll(a=>!a.alive)) Finish(Outcome.Wipe);
            else if(w.phase==Phase.Battle&&w.boss.hp<=0) EnterRest();
            else if(w.phase==Phase.Rest&&w.agents.TrueForAll(a=>!a.alive||a.escaped)) Finish(Outcome.SliceComplete);
        }
        void Move(Agent a,float x,float z,float dt,float multiplier=1)
        {
            var p=Vector2.MoveTowards(new Vector2(a.x,a.z),new Vector2(x,z),MoveSpeed*dt*multiplier); a.x=p.x;a.z=p.y;
        }
        void UpdateBoss(float dt)
        {
            var b=State.boss; var d=Catalog.boss; if(b.hp<=0)return;
            b.enraged=b.hp<b.maxHp*.5f;
            if(b.telegraph)
            {
                b.windup-=dt;
                if(b.windup<=0)
                {
                    b.telegraph=false;b.timer=d.interval*(b.enraged?.72f:1); b.hits++;
                    foreach(var a in State.agents) if(a.alive)
                    {
                        float radius=d.radius*(b.enraged?1.25f:1);
                        if(Distance(a.x,a.z,b.targetX,b.targetZ)<radius) Hurt(a,d.damage*(b.enraged?1.3f:1),"rootcrown slam");
                        else if(b.hits%3==0) Hurt(a,d.damage*.36f,"thorn pulse");
                    }
                    State.Say(-1,b.hits%3==0?"Thorns ripple across the arena.":"The Rootcrown shatters the marked ground.","boss");
                }
            }
            else
            {
                b.timer-=dt;
                if(b.timer<=0)
                {
                    var living=State.agents.FindAll(a=>a.alive);if(living.Count==0)return;
                    var target=living[State.Pick(living.Count)];
                    foreach(var a in living) if(a.intent.kind==ActionKind.Protect) {target=a;break;}
                    b.targetX=target.x;b.targetZ=target.z;b.windup=d.windup;b.telegraph=true;
                }
            }
        }
        void Battle(Agent a,float dt)
        {
            var b=State.boss;
            if(a.intent.kind==ActionKind.GiveUp) {Die(a,"gave up the climb");return;}
            float radius=Catalog.boss.radius*(b.enraged?1.25f:1);
            if(b.telegraph&&Distance(a.x,a.z,b.targetX,b.targetZ)<radius+.6f)
            {
                float dx=a.x-b.targetX,dz=a.z-b.targetZ;
                if(Mathf.Abs(dx)+Mathf.Abs(dz)<.1f) {dx=a.id%2==0?1:-1;dz=-.5f;}
                float len=Mathf.Sqrt(dx*dx+dz*dz);
                Move(a,Mathf.Clamp(b.targetX+dx/len*(radius+1.2f),-9,9),Mathf.Clamp(b.targetZ+dz/len*(radius+1.2f),-8,8),dt,.75f+a.personality.risk*.65f);return;
            }
            if(a.intent.kind==ActionKind.Protect&&a.intent.target>=0)
            {
                var ally=State.agents[a.intent.target];Move(a,ally.x,ally.z-1,dt);a.guard=Mathf.Max(a.guard,.35f);
                if(a.attackTimer<=0&&ally.alive&&Distance(a.x,a.z,ally.x,ally.z)<2) {ally.guard=Mathf.Max(ally.guard,.32f);ally.Bond(a.id).Help(.025f);a.attackTimer=2;}
                return;
            }
            if(a.intent.kind==ActionKind.Skill)
            {
                var s=Catalog.Skill(a.intent.skill);bool weapon=a.weapon.infusion==s.id;
                float tx=s.effect=="Heal"&&a.intent.target>=0?State.agents[a.intent.target].x:0;
                float tz=s.effect=="Heal"&&a.intent.target>=0?State.agents[a.intent.target].z:1;
                if(s.range>0&&Distance(a.x,a.z,tx,tz)>s.range) Move(a,tx,tz,dt);
                else Cast(a,s.id,a.intent.target,weapon);
            }
            else
            {
                var item=Catalog.Item(a.weapon.id);float range=item.weapon=="Bow"||item.weapon=="Staff"?9:2.8f;
                if(Distance(a.x,a.z,0,1)>range) Move(a,0,1,dt);
                else if(a.attackTimer<=0)
                {
                    float scale=item.weapon=="Bow"?a.stats.dex:item.weapon=="Staff"?a.stats.intel:a.stats.str;
                    float damage=(item.power*a.weapon.quality+a.weapon.upgrade*3+scale*.9f+(a.enchant>0?12:0))*Proficiency(a,Catalog);
                    HitBoss(a,damage);a.attackTimer=Mathf.Max(.65f,1.7f-a.stats.dex*.025f);
                }
            }
            UsePotion(a);
        }
        public bool Cast(Agent a,string id,int target=-1,bool weapon=false)
        {
            if(State.phase!=Phase.Battle||!CanCast(a,Catalog,id,weapon)) return false;
            var s=Catalog.Skill(id);
            if(s.effect=="Heal"&&(target<0||target>=State.agents.Count||!State.agents[target].alive)) return false;
            float tx=s.effect=="Heal"?State.agents[target].x:0,tz=s.effect=="Heal"?State.agents[target].z:1;
            if(s.range>0&&Distance(a.x,a.z,tx,tz)>s.range) return false;
            a.mp-=s.mana;var cd=a.cooldowns.Find(x=>x.id==id);if(cd==null){cd=new Cooldown{id=id};a.cooldowns.Add(cd);}cd.left=s.cooldown;
            if(s.effect=="Heal")
            {
                var ally=State.agents[target];float amount=Mathf.Min(ally.MaxHp-ally.hp,s.power+a.stats.wis*1.4f);ally.hp+=amount;a.healing+=amount;
                if(a.id!=ally.id) {ally.Bond(a.id).Help(.04f);a.Bond(ally.id).Help(.01f);}
                State.Say(a.id,s.name+" → "+ally.name+" +"+(int)amount,"heal");
            }
            else if(s.effect=="Guard") a.guard=Mathf.Max(a.guard,s.power);
            else if(s.effect=="Enchant") a.enchant=12;
            else if(s.effect=="Taunt") {State.boss.targetX=a.x;State.boss.targetZ=a.z;a.guard=.45f;}
            else {HitBoss(a,(s.power+a.stats.intel*.7f+a.stats.str*.3f)*Proficiency(a,Catalog));State.Say(a.id,s.name,"skill");}
            return true;
        }
        void HitBoss(Agent a,float damage) {float dealt=Mathf.Min(State.boss.hp,damage);State.boss.hp-=dealt;a.damage+=dealt;}
        public void Hurt(Agent a,float amount,string cause) {if(!a.alive)return;a.hp=Mathf.Max(0,a.hp-Damage(amount,a.armor,a.guard));if(a.hp<=0) Die(a,cause);}
        public void Die(Agent a,string cause)
        {
            if(!a.alive)return;a.hp=0;a.alive=false;a.taskTimer=0;a.task="";a.Remember("I died to "+cause+".");State.Say(a.id,"My path ends here: "+cause+".","death");
            foreach(var ally in State.agents) if(ally.alive) {var bond=ally.Bond(a.id);bond.fear=Mathf.Clamp01(bond.fear+.2f);ally.Remember(a.name+" died to "+cause+".");}
        }
        public void AddItem(Agent a,Item item) {if(a.inventory.Count>=24)a.materials++;else a.inventory.Add(item);}
        public bool GiveWeapon(Agent from,Agent to,Item item)
        {
            if(from==to||!from.alive||!to.alive||from.escaped||to.escaped||State.phase!=Phase.Rest||!from.inventory.Contains(item)||Catalog.Item(item.id).kind!="Weapon")return false;
            from.inventory.Remove(item);AddItem(to,to.weapon);to.weapon=item;to.Bond(from.id).Help(.12f);from.Bond(to.id).Help(.03f);
            State.Say(from.id,to.name+", take my "+Catalog.Item(item.id).name+". Its "+item.infusion+" is yours now.","social");return true;
        }
        void Discuss(Agent speaker,Intent next)
        {
            var other=State.agents.Find(a=>a.id!=speaker.id&&a.alive&&!a.escaped);if(other==null)return;
            var bond=other.Bond(speaker.id);
            if(next.kind==ActionKind.Explore&&other.intent.kind==ActionKind.Exit)
            {
                float disagreement=Mathf.Abs(speaker.personality.risk-other.personality.risk)*.05f;
                bond.resentment=Mathf.Clamp01(bond.resentment+disagreement);bond.rivalry=Mathf.Clamp01(bond.rivalry+disagreement*.5f);
                State.Say(other.id,speaker.name+", the gate matters more than another cache.","social");
            }
            else if(next.kind==ActionKind.Craft)State.Say(speaker.id,other.name+", give me a moment at the forge. This could help us both.","social");
            else if(next.kind==ActionKind.Read)State.Say(speaker.id,"Did our former selves leave truth, or only fear?","social");
        }
        void UsePotion(Agent a)
        {
            string id=a.hp<a.MaxHp*.35f?"hp":a.mp<a.MaxMp*.2f?"mp":"";var item=a.inventory.Find(i=>i.id==id);if(item==null)return;
            if(id=="hp")a.hp=Mathf.Min(a.MaxHp,a.hp+Catalog.Item(id).power);else a.mp=Mathf.Min(a.MaxMp,a.mp+Catalog.Item(id).power);
            a.inventory.Remove(item);State.Say(a.id,"Drink "+Catalog.Item(id).name,"item");
        }
        public void EnterRest()
        {
            State.phase=Phase.Rest;State.phaseClock=0;State.boss.telegraph=false;
            State.Say(-1,"Guardian defeated. The refuge will collapse in 26 seconds.","run");
            foreach(var a in State.agents) if(a.alive)
            {
                a.kills++;a.Remember("We defeated the Rootcrown.");LevelUp(a);a.materials+=3+State.Pick(3);
                for(int i=0;i<2;i++) AddItem(a,a.Make(Catalog.items[State.Pick(Catalog.items.Length)].id));
                ResolveInventory(a);a.x=-8;a.z=-3+a.id*2;a.decisionTimer=0;a.intent=new Intent();a.taskTimer=0;
            }
        }
        public void ResolveInventory(Agent a)
        {
            foreach(var item in new List<Item>(a.inventory))
            {
                var d=Catalog.Item(item.id);
                if(d.kind=="Weapon")
                {
                    float old=Catalog.Item(a.weapon.id).power*a.weapon.quality;
                    float score=d.power*item.quality+(d.weapon==Catalog.Item(Catalog.Class(a.profession).weapon).weapon?3:0);
                    if(score>old+2) {var prior=a.weapon;a.weapon=item;a.inventory.Remove(item);AddItem(a,prior);State.Say(a.id,"Equip "+d.name+"; a different weapon may be worth the trade.","loot");}
                }
                else if(d.kind=="Armor") {a.armor+=d.power;a.inventory.Remove(item);}
                else if(d.kind=="Experience") {LevelUp(a);a.inventory.Remove(item);}
                else if(d.kind=="SkillBook")
                {
                    if(d.profession==a.profession) {a.skillPoints++;foreach(var s in Catalog.skills) if(Unlock(a,s.id))break;}
                    else {a.materials+=2;State.Say(a.id,"This codex is not my path. Salvage it for the forge.","loot");}
                    a.inventory.Remove(item);
                }
            }
        }
        public bool StartCraft(Agent a,string recipe,string infusion)
        {
            var r=Catalog.Recipe(recipe);var s=Catalog.Skill(infusion);
            if(State.phase!=Phase.Rest||!a.alive||a.escaped||a.taskTimer>0||r==null||a.materials<r.materials||Distance(a.x,a.z,-1,2)>1.5f) return false;
            if(!string.IsNullOrEmpty(infusion)&&(s==null||!a.unlocked.Contains(infusion))) return false;
            a.materials-=r.materials;a.task=recipe+"|"+infusion;a.taskTimer=r.seconds;State.Say(a.id,"Begin forge: "+Catalog.Item(r.item).name+" / "+infusion,"craft");return true;
        }
        void Rest(Agent a,float dt)
        {
            float collapse=-10+Mathf.Max(0,State.phaseClock-RestLimit)*2.3f;
            if(State.phaseClock>=RestLimit&&a.x<collapse) {Die(a,"the collapsing refuge");return;}
            if(a.taskTimer>0)
            {
                a.taskTimer-=dt;
                if(a.taskTimer<=0)
                {
                    if(a.task.StartsWith("forge"))
                    {
                        var parts=a.task.Split('|');var recipe=Catalog.Recipe(parts[0]);var item=a.Make(recipe.item,recipe.quality+.25f+a.stats.wis*.01f,parts[1]);
                        if(a.taskRecipient>=0&&State.agents[a.taskRecipient].alive&&!State.agents[a.taskRecipient].escaped) {AddItem(a,item);GiveWeapon(a,State.agents[a.taskRecipient],item);}
                        else {AddItem(a,a.weapon);a.weapon=item;}
                        a.taskRecipient=-1;a.Remember("I forged "+Catalog.Item(item.id).name+" with "+item.infusion+".");State.Say(a.id,"Forged "+Catalog.Item(item.id).name+" · "+item.infusion+" / quality "+item.quality.ToString("F2"),"craft");
                    }
                    else if(a.task=="cache") {a.materials+=1+State.Pick(3);AddItem(a,a.Make(Catalog.items[State.Pick(Catalog.items.Length)].id));ResolveInventory(a);State.Say(a.id,"A hidden cache. Was the delay worth it?","loot");}
                    a.task="";a.decisionTimer=0;
                }
                return;
            }
            switch(a.intent.kind)
            {
                case ActionKind.Exit: Move(a,10,a.z,dt);if(a.x>=9.5f) {a.escaped=true;State.Say(a.id,"I made it through the gate.","exit");}break;
                case ActionKind.Rest: Move(a,-4+(a.id%2==0?-.5f:.5f),-2+(a.id<2?-.5f:.5f),dt);if(Distance(a.x,a.z,-4,-2)<1) {a.hp=Mathf.Min(a.MaxHp,a.hp+dt*15);a.mp=Mathf.Min(a.MaxMp,a.mp+dt*12);}break;
                case ActionKind.Read:
                    Move(a,-4+(a.id%2==0?-.5f:.5f),3+(a.id<2?-.5f:.5f),dt);
                    if(Distance(a.x,a.z,-4,3)<1)
                    {
                        a.readBook=true;var entries=State.book.FindAll(b=>b.author==a.id);string text=entries.Count>0?entries[entries.Count-1].text:"The pages are blank. We are the first witnesses.";
                        a.Remember("Read a legacy: "+text);State.Say(a.id,text,"legacy");a.decisionTimer=0;
                    }break;
                case ActionKind.Craft:
                    Move(a,-1+(a.id%2==0?-.5f:.5f),2+(a.id<2?-.5f:.5f),dt);
                    if(Distance(a.x,a.z,-1,2)<1)
                    {
                        string recipe="forge"+(int)a.profession;string infusion=a.unlocked[State.Pick(a.unlocked.Count)];int recipient=-1;
                        var warrior=State.agents.Find(x=>x.id!=a.id&&x.alive&&!x.escaped&&x.profession==Profession.Warrior);
                        if(a.profession==Profession.Healer&&a.unlocked.Contains("heal")&&warrior!=null&&a.personality.empathy+a.Bond(warrior.id).Attachment>.4f)
                        {recipe="forge0";infusion="heal";recipient=warrior.id;}
                        else if(a.personality.risk>.65f&&State.Roll()<.3f)recipe="forge"+State.Pick(5);
                        if(StartCraft(a,recipe,infusion))a.taskRecipient=recipient;
                    }break;
                default:Move(a,3,-3,dt);if(Distance(a.x,a.z,3,-3)<1) {a.task="cache";a.taskTimer=2.7f;}break;
            }
            UsePotion(a);
        }
        public void Finish(Outcome outcome)
        {
            if(State.phase==Phase.Ended)return;
            State.outcome=outcome;State.phase=Phase.Ended;State.restartTimer=0;
            var record=new RunRecord{run=State.run,floor=outcome==Outcome.TowerClear?25:1,seconds=State.clock,outcome=outcome};
            foreach(var a in State.agents)
            {
                record.composition.Add(a.name+" / "+a.profession);if(a.alive) record.survivors.Add(a.name);else record.deaths++;
                record.damage+=a.damage;record.healing+=a.healing;record.builds.Add(a.name+": "+a.weapon.id+" ["+a.weapon.infusion+"] / "+string.Join(",",a.equipped));
                string words=!a.alive?"The tower took me. Watch the marked ground and the falling refuge.":a.personality.greed>.55f?"The forge was worth it. The last cache almost was not.":"Leave time to escape. A companion matters more than a perfect strike.";
                a.lastWords=words.Substring(0,Math.Min(80,words.Length));State.book.Add(new Epitaph {run=State.run,author=a.id,text=a.lastWords});
                a.Remember("Run ended: "+outcome);State.Say(a.id,a.lastWords,"legacy");
            }
            while(State.book.Count>64)State.book.RemoveAt(0);
            State.history.Add(record);if(State.history.Count>100)State.history.RemoveAt(0);
            State.Say(-1,outcome==Outcome.Wipe?"All four lights faded. A new life begins in 8 seconds.":"Slice complete. These are not yet the gates of floor 25.","run");
        }
    }
}
