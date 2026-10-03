"""Author string tables and a complete inventory of runtime text, preserving technical IDs."""
import json, re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
entries={}
def add(key,en,zh):
    if key in entries: raise ValueError(key)
    entries[key]=(en,zh)
ui={
'brand':('E M B E R','E M B E R'), 'tagline':('THE WITNESS TOWER / Agent RPG','見證之塔 / Agent RPG'),
'window_title':('EMBER / The Witness Tower','EMBER / 見證之塔'),
'life':('CURRENT LIFE','目前輪迴'),'floor':('FLOOR','樓層'),'time':('RUN TIME','挑戰時間'),
'arena':('ROOTCROWN AMPHITHEATRE','根冠競技場'),'refuge':('ASHEN CROSSING','灰燼渡口'),'recorded':('LIFE RECORDED','生命已記錄'),
'edition':('GUARDIAN TRIAL · AUTONOMOUS ADVENTURE','守護者試煉 · 自主冒險'),
'pause':('PAUSE','暫停'),'resume':('RESUME','繼續'),'save':('SAVE','儲存'),'load':('LOAD','讀取'),'new_life':('NEW LIFE','開始新輪迴'),
'speed':('{0}×','{0} 倍速'),'witnesses':('THE FOUR WITNESSES','四名見證者'),'controls':('OBSERVATION CONTROLS','觀察控制'),
'controls_help':('SPACE  Pause / resume\n1 · 2 · 3  Playback speed\n← →  Orbit the arena','空白鍵　暫停 / 繼續\n1 · 2 · 3　播放速度\n← →　環繞觀看競技場'),
'showcase':('CLASS CHOICE / SHOWCASE','職業選擇 / 展示'),'autonomous':('CLASS CHOICE / AUTONOMOUS','職業選擇 / 自主'),
'next_classes':('Future lives choose classes freely.','未來的輪迴將由 Agent 自由選擇職業。'),
'boss':('THE ROOTCROWN','根冠之主'), 'boss_phase1':('PHASE I / STONE AND THORN','第一階段 / 岩石與荊棘'),
'boss_phase2':('PHASE II / THE ROOTS REMEMBER','第二階段 / 根系仍記得'),
'slam':('GROUND BREAK / {0}s','裂地重擊 / {0} 秒'),
'epilogue':('THE BOOK RECEIVES YOUR STORY','死者之書收錄了你的故事'),
'refuge_title':('REFUGE / THE ASHEN CROSSING','避難層 / 灰燼渡口'),
'restart':('{0} · New life in {1}s','{0} · {1} 秒後開始新輪迴'),
'collapse_active':('COLLAPSE ADVANCING / REACH THE EASTERN GATE','坍塌正在逼近 / 趕往東側出口'),
'collapse_timer':('COLLAPSE IN {0}s / THE GATE IS TO THE EAST','距離坍塌 {0} 秒 / 出口在東側'),
'mind':('INSIDE THE MIND','內心思緒'),'agent_class':('{0} / {1}','{0} / {1}'),
'traits1':('RISK {0}   CURIOSITY {1}','冒險傾向 {0}　好奇心 {1}'),
'traits2':('EMPATHY {0}   GREED {1}','同理心 {0}　貪欲 {1}'),
'intent':('INTENT / {0}','當前意圖 / {0}'),'build':('BUILD / LIFE {0}','配置 / 第 {0} 輪生命'),
'quality':('QUALITY {0} / INFUSION {1}','品質 {0} / 技能灌注 {1}'),
'stats':('STR {0}  DEX {1}  INT {2}  VIT {3}','力量 {0}　敏捷 {1}　智力 {2}　體質 {3}'),
'skills':('SKILLS / {0}','技能 / {0}'),'resources':('MATERIALS {0}  ITEMS {1}  MEMORIES {2}','材料 {0}　物品 {1}　記憶 {2}'),
'footer':('AUTONOMOUS LIVES. FINITE MEMORIES. / ONE GUARDIAN, THEN THE DANGEROUS REFUGE.','自主生命，有限記憶。 / 擊敗守護者，然後前往危險的避難層。'),
'none':('None','無'),'safe':('SAFE','已逃離'),'level':('LV {0}','等級 {0}'),'dead':('DEAD','死亡'),
'escaped':('Escaped','已逃離'),'working':('Working {0}s','製作中 · {0} 秒'),'vitals':('{0} HP / {1} MANA','生命 {0} / 魔力 {1}'),
'exploring':('Exploring {0}s','探索中 · {0} 秒'),
'chronicle':('LIVE CHRONICLE','即時紀事'),'book':('BOOK OF THE DEAD','死者之書'),'history':('RUN HISTORY','挑戰紀錄'),'relationships':('RELATIONSHIPS','關係'),
'observe':('OBSERVE · REMEMBER · REPEAT','觀察 · 記憶 · 再次輪迴'),'tower':('TOWER','高塔'),
'book_empty':('No former lives have written here yet. Death will leave a small piece of the story.','前世尚未留下文字。生命結束時，會有一小段故事留在這裡。'),
'book_author':('LIFE {0} / {1}','第 {0} 輪 / {1}'),
'history_empty':('The first life is still unfolding. Slice completion is recorded separately from a full 25-floor clear.','第一段生命仍在繼續。試煉完成與突破 25 層高塔的紀錄會分開保存。'),
'history_row':('LIFE {0}   {1}   {2}   SURVIVORS {3}   DAMAGE {4}   HEALING {5}','第 {0} 輪　{1}　{2}　存活 {3} 人　傷害 {4}　治療 {5}'),
'bond_row':('{0} → {1}   Trust {2}   Friendship {3}   Love {4}   Fear {5}   Resentment {6}','{0} → {1}　信任 {2}　友情 {3}　愛意 {4}　恐懼 {5}　怨懟 {6}'),
'language':('Language / English','語言 / 繁體中文'),
'dialogue_unknown':('This record cannot be displayed.','這段紀錄暫時無法顯示。'),
}
for k,v in ui.items():add('ui.'+k,*v)
for en,zh in [('Warrior','戰士'),('Archer','弓箭手'),('Mage','法師'),('Healer','補師')]:add('class.'+en,en,zh)
for en,zh in [('Attack','攻擊'),('Skill','技能'),('Protect','保護隊友'),('Rest','休息'),('Explore','探索'),('Craft','製作'),('Read','閱讀'),('Exit','逃離'),('GiveUp','放棄挑戰')]:add('action.'+en,en,zh)
for en,zh in [('None','進行中'),('Wipe','挑戰失敗'),('SliceComplete','試煉完成'),('TowerClear','高塔通關')]:add('outcome.'+en,en,zh)
for k,en,zh in [
 ('saved','Saved locally.','已儲存至本機。'),('loaded','Restored this life.','已讀取本次輪迴。'),('save_failed','Save failed. Please try again.','儲存失敗，請稍後再試。'),('load_failed','Could not load the save. Check that a valid save exists.','無法讀取存檔，請確認已有有效的存檔。')]:add('notice.'+k,en,zh)
for k,en,zh in [
 ('attack','Keep pressure on the guardian.','持續壓制守護者。'),('protect','Protect {0}; their survival outweighs my damage.','保護 {0}；比起多造成一點傷害，我更希望他活下來。'),
 ('give_up','The cost of continuing is too high.','繼續前進的代價太大了。'),('exit','Leave before the ruin takes us.','趁遺跡還沒吞沒我們，快離開。'),
 ('rest','Rest at the sanctuary before moving on.','先在聖所休息，再繼續前進。'),('explore','One more cache could change this run.','再找一處寶藏，也許就能扭轉這次挑戰。'),
 ('read','Read the warnings left by our former selves.','讀讀前世的我們留下了什麼警告。'),('craft','Forge a better weapon and bind a learned skill.','打造更好的武器，灌注已學會的技能。'),
 ('heal','Use {0} to keep a companion alive.','使用{0}，讓隊友撐下去。'),('skill','Use {0}; it fits the current opening.','現在正是施放{0}的好時機。')]:add('reason.'+k,en,zh)
for k,en,zh in [
 ('level','Level {0}. I chose a new path for my build.','升到 {0} 級。我為自己的配置選了一條新路。'),
 ('start','RUN {0} / Four lives enter the Witness Tower.','第 {0} 輪挑戰 / 四段生命踏入見證之塔。'),
 ('class','I choose {0}. This life will be my own.','我選擇成為{0}。這一生，由我自己決定。'),
 ('thorns','Thorns ripple across the arena.','荊棘正在競技場中蔓延。'),('slam','The Rootcrown shatters the marked ground.','根冠之主擊碎了標記的地面。'),
 ('heal','{0} → {1} +{2}','{0} → {1}，恢復 {2} 點生命'),('skill','{0}','施放{0}'),
 ('death','My path ends here: {0}.','我的旅程到此為止：{0}。'),
 ('gift','{0}, take my {1}. Its {2} is yours now.','{0}，拿著我打造的{1}。裡面的{2}也交給你了。'),
 ('hurry','{0}, the gate matters more than another cache.','{0}，先到出口，比再找一個寶箱更重要。'),
 ('forge_talk','{0}, give me a moment at the forge. This could help us both.','{0}，讓我在工坊待一下。這把武器也許能幫上我們。'),
 ('book_talk','Did our former selves leave truth, or only fear?','前世的我們留下的，是事實，還是恐懼？'),
 ('potion','Drink {0}','喝下{0}。'),('victory','Guardian defeated. The refuge will collapse in 26 seconds.','守護者已倒下。避難層將在 26 秒後開始坍塌。'),
 ('equip','Equip {0}; a different weapon may be worth the trade.','換上{0}。試試不同的武器，也許值得。'),
 ('salvage','This codex is not my path. Salvage it for the forge.','這本技能書不適合我，拆成工坊能用的材料吧。'),
 ('craft_start','Begin forge: {0} / {1}','開始打造{0} / 灌注{1}'),
 ('craft_done','Forged {0} · {1} / quality {2}','完成{0} · 灌注{1} / 品質 {2}'),
 ('cache','A hidden cache. Was the delay worth it?','找到隱藏的寶藏了。多花這些時間，值得嗎？'),
 ('escape','I made it through the gate.','我穿過出口了。'),
 ('blank','The pages are blank. We are the first witnesses.','書頁還是空白的。我們是最初的見證者。'),
 ('wipe','All four lights faded. A new life begins in 8 seconds.','四道光芒都熄滅了。8 秒後，新的輪迴將開始。'),
 ('complete','Slice complete. These are not yet the gates of floor 25.','試煉完成。真正的第 25 層，仍在前方。')]:add('event.'+k,en,zh)
for k,en,zh in [
 ('slam','rootcrown slam','根冠之主的重擊'),('thorns','thorn pulse','荊棘衝擊'),('give_up','gave up the climb','放棄繼續爬塔'),('collapse','the collapsing refuge','避難層坍塌'),('unknown','unknown cause','未知原因')]:add('cause.'+k,en,zh)
for k,en,zh in [
 ('death','I died to {0}.','我死於{0}。'),('ally_death','{0} died to {1}.','{0} 死於{1}。'),('victory','We defeated the Rootcrown.','我們擊敗了根冠之主。'),
 ('craft','I forged {0} with {1}.','我打造了灌注{1}的{0}。'),('read','Read a legacy: {0}','讀到前世的遺言：{0}'),('end','Run ended: {0}','挑戰結束：{0}')]:add('memory.'+k,en,zh)
for k,en,zh in [
 ('death','The tower took me. Watch the marked ground and the falling refuge.','高塔帶走了我。小心地面的標記，也別讓坍塌的避難層追上你。'),
 ('greed','The forge was worth it. The last cache almost was not.','花時間打造武器很值得。最後那個寶箱，差點就不值得了。'),
 ('care','Leave time to escape. A companion matters more than a perfect strike.','記得留時間逃離。比起完美的一擊，隊友更重要。')]:add('epitaph.'+k,en,zh)
catalog=json.loads((ROOT/'UnityProject/Assets/Resources/catalog.json').read_text(encoding='utf-8'))
skill_zh=['月牙斬','劍氣斬','血怒','鋼鐵守勢','挑釁','反擊','穿透射擊','毒箭','箭雨','纏足陷阱','破甲箭','星落狙擊','餘燼火球','寒霜冰槍','隕石','魔力護盾','火焰附魔','雷擊','聖光術','治療術','庇護聖域','神聖防護','淨化之光','復甦']
item_zh=['誓約巨劍','灰燼弓','餘燼法杖','朝聖者法杖','旅人長槍','符文胸甲','生命藥水','魔力藥水','冒險筆記','劍術技能書','獵人技能書','秘法技能書','祈禱技能書']
for s,zh in zip(catalog['skills'],skill_zh,strict=True):add('skill.'+s['id'],s['name'],zh)
for i,zh in zip(catalog['items'],item_zh,strict=True):add('item.'+i['id'],i['name'],zh)
add('boss.'+catalog['boss']['id'],catalog['boss']['name'],'根冠之主')

from phase2_strings import entries as phase2_entries
for key,(en,zh) in phase2_entries.items():add(key,en,zh)
from phase3_strings import entries as phase3_entries
for key,(en,zh) in phase3_entries.items():add(key,en,zh)
from phase31_strings import entries as phase31_entries
for key,(en,zh) in phase31_entries.items():add(key,en,zh)

if __name__=='__main__':
    import sys
    # Inventory comes first. --inventory never modifies game assets.
    lines=['# 玩家可見文字盤點 / zh-TW 字串表','',f'共 {len(entries)} 個語意鍵。英文與繁中各一份；保留全部技術識別字。','',
    '搜尋範圍：Assets 全部 C#、JSON、Unity Scene、ProjectSettings，以及會再產生內容的 Tools/write_catalog.py。',
    '可見來源：WitnessGame UI 與列舉顯示、PlayerWindow 視窗標題、UtilityBrain 意圖、Simulation 系統/對話/遺言、catalog 技能與物品名、舊存檔。',
    'WorldView GameObject/材質/動畫名稱、debug log、Editor 測試輸出、技術 ID、README 與設計文件不屬於遊戲玩家 UI。',
    '角色名 KAEL / LYRA / ORIN / SERA、品牌 EMBER，以及 Agent RPG 依需求保留。','',
    '| 字串鍵 | 英文來源或參數化版本 | 繁體中文 |','|---|---|---|']
    for k,(en,zh) in entries.items():lines.append(f'| `{k}` | {en.replace(chr(10),"<br>")} | {zh.replace(chr(10),"<br>")} |')
    (ROOT/'Docs/LOCALIZATION_INVENTORY.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
    print('Inventoried',len(entries),'semantic string keys')
    if '--inventory' not in sys.argv:
        dest=ROOT/'UnityProject/Assets/Resources/Localization';dest.mkdir(parents=True,exist_ok=True)
        for locale,col in [('en',0),('zh-TW',1)]:
            table=dict(locale=locale,entries=[dict(key=k,value=v[col]) for k,v in entries.items()])
            (dest/(locale+'.json')).write_text(json.dumps(table,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
