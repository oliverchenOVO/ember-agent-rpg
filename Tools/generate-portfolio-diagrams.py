"""Render implementation-grounded portfolio diagrams; no gameplay screenshots are edited.

Requires Pillow and matplotlib. Run from any directory:
    python Tools/generate-portfolio-diagrams.py --locale zh-TW
    python Tools/generate-portfolio-diagrams.py --locale en
The bundled Noto CJK font and its OFL remain in UnityProject/Assets/Resources/Fonts.
"""
import argparse
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json, math
source=Path(__file__).resolve().parents[1];target=source
parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--locale',choices=['zh-TW','en'],default='zh-TW');locale=parser.parse_args().locale
out=target/'Media/Diagrams'
if locale=='en':out=out/'en'
out.mkdir(parents=True,exist_ok=True)
regular=str(source/'UnityProject/Assets/Resources/Fonts/NotoSansCJKtc-Regular.otf');bold=regular
TRANSLATIONS = {'EMBER / ENGINEERING': 'EMBER / 工程設計',
 'Implementation grounded / Team Balance revision 43a2f70 / Oliver Chen': '依實際程式實作繪製 / 隊伍平衡版本 43a2f70 / '
                                                                          'Oliver Chen',
 'From state to observable action': '從世界狀態到可觀察的行動',
 'One authoritative simulation. Separate goals, tactics and presentation.': '由同一套模擬維護世界狀態，分開處理目標、戰術與畫面呈現。',
 'EXPEDITION STATE': '遠征狀態',
 'Agents / Groups / seeded RNG': '角色 / 分隊 / 固定種子亂數',
 'Memory / Bonds / Inventory': '記憶 / 關係 / 背包',
 'Independent Boss and Rest state': '各隊獨立的 Boss 與休息層狀態',
 'DECISION CONTEXT': '決策情境',
 'Legal options + resources + time': '合法選項 + 資源 + 剩餘時間',
 'Personality + sourced knowledge': '個性 + 註明來源的知識',
 'Relationship evidence': '關係與共同經歷',
 'HIGH-LEVEL REASONER': '高階決策器',
 'Rule / utility goals by default': '預設依規則與效用評分選擇目標',
 'Optional bounded LLM gateway': '可選用受限制的 LLM 決策介面',
 'Validated decision proposals': '提出經過合法性檢查的決策',
 'LOCAL EXECUTION': '行動執行',
 'Combat / exploration / social': '戰鬥 / 探索 / 社交',
 'Danger, legality and resources': '檢查危險、合法性與資源',
 'Team support and navigation': '隊伍支援與路徑移動',
 'SIMULATION UPDATE': '模擬更新',
 'Fixed-step authoritative state': '以固定時間步更新世界狀態',
 'Events / skill and damage results': '事件 / 技能效果與傷害結果',
 'Save/load + RNG continuation': '存讀檔 + 延續亂數狀態',
 'UNITY PRESENTATION': 'Unity 畫面呈現',
 'Models / VFX / camera / UI': '模型 / 特效 / 鏡頭 / 介面',
 'Mind, Book and skill inspection': '查看思緒、死者之書與技能',
 'zh-TW / English string tables': '繁體中文 / 英文文字表',
 'state feedback': '狀態回饋',
 'EVIDENCE & VALIDATION': '工程證據與驗證',
 'Telemetry, bounded fixtures, process status and build identity': '遙測資料、有界測試情境、程序狀態與成品版本識別',
 'An Agent chooses; the engine checks': 'Agent 選擇行動，引擎負責檢查',
 'Different update rates for goals, tactics and immediate safety.': '目標、戰術與即時安全檢查，各自以不同頻率更新。',
 '01 / OBSERVE & FILTER': '01 / 觀察與篩選',
 'HP / MP / remaining time / known facilities': '生命 / 魔力 / 剩餘時間 / 已知設施',
 'Generate only allowed high-level options': '只產生目前允許的高階行動選項',
 '02 / SCORE GOALS': '02 / 目標評分',
 'Personality + needs + time + relationships': '個性 + 需求 + 時間 + 關係',
 'Typical goal cadence: 1.5 simulated seconds': '一般目標更新間隔：1.5 秒模擬時間',
 '03 / LOCAL TACTICS': '03 / 即時戰術',
 'Attack / equipped skill / infusion / protection': '普攻 / 已裝備技能 / 灌注 / 保護',
 'Candidate refresh: about 0.6 seconds': '戰術候選更新間隔：約 0.6 秒',
 '04 / VALIDATE & EXECUTE': '04 / 檢查與執行',
 'Life, mana, cooldown, weapon, target and range': '生命、魔力、冷卻、武器、目標與距離',
 'Update the world and record observable events': '更新世界，記錄可供觀察的事件',
 'IMMEDIATE SAFETY': '即時安全檢查',
 'Each fixed tick (default 0.1 seconds)': '每個固定時間步檢查一次（預設 0.1 秒）',
 'Warned Boss areas / hazards / pressure cores': 'Boss 預警範圍 / 地面危險 / 壓力核心',
 'Move to reachable safe space before attacks': '攻擊前先移動到可達的安全位置',
 'TEAM COORDINATION': '隊伍協調',
 'Urgent rescue before avoidable damage': '優先搶救瀕死隊友，再考慮輸出',
 'Reserve pending healing; avoid duplicate shields': '預約治療目標，避免重複護盾',
 'Role-aware four-slot skill choices': '依隊伍角色分配四格技能',
 'CONTEXT CHANGES': '情境變化',
 'Death: stop acting and record witnessed moves': '死亡：停止行動，記錄親眼見過的招式',
 'Rest: search, discuss, work, route and escape': '休息層：搜尋、討論、製作、移動與撤離',
 'A goal can be overridden by safety or legality': '安全與合法性檢查可以改變原定目標',
 'Finite memories; explicit inheritance': '有限的記憶，需要主動閱讀才能傳承',
 'Observer access does not give Agents automatic knowledge.': '玩家能查看資訊，不代表 Agent 已自動取得這些知識。',
 'LIVING AGENT': '存活中的 Agent',
 'Personal experience / told information': '親身經歷 / 他人告知的資訊',
 'Bounded Knowledge + relationship events': '有限的知識容量 + 關係事件',
 'Source, confidence, age and importance': '來源、可信度、時間與重要性',
 'DEATH RECORD': '死亡紀錄',
 'Personality + actual situation': '個性 + 實際遭遇',
 'Only abilities personally witnessed': '只留下親眼見過的 Boss 招式',
 'Book of the Dead: bounded entries': '死者之書：保留有限筆紀錄',
 'NEXT LIFE': '下一輪生命',
 'Failure resets private run state': '挑戰失敗後重置個人輪迴狀態',
 'Book exists, but is not auto-learned': '遺書仍存在，但不會自動學會內容',
 'Read in a refuge to gain Book knowledge': '在休息層閱讀，才取得遺書知識',
 'PREPARATION': '提前準備',
 'Known threats influence counter skills': '已知威脅影響應對技能的選擇',
 'Unlocked four-slot loadout only': '限用已解鎖技能，維持四格配置',
 'Known moves get more dodge margin': '已知招式會預留更多閃避餘裕',
 'SUCCESS EXCEPTION': '通關後的保留例外',
 'Only living, genuinely completed tower survivors retain eligible memories and bonds.': '只有真正通關且仍存活的角色，才能保留符合條件的記憶與關係。',
 'Reading inherited advice is policy adaptation, not neural training.': '閱讀遺書會調整行動策略；目前不涉及神經網路訓練。',
 '4 Warriors': '四戰士',
 '4 Archers': '四弓箭手',
 '4 Mages': '四法師',
 '4 Healers': '四補師',
 'Mixed': '混合隊',
 'No healer': '無補師隊',
 'Before (pressure-core baseline)': '調整前（壓力核心基準版）',
 'Team Balance': '隊伍平衡調整後',
 'Mean party damage / simulated second': '全隊平均傷害 / 每秒模擬時間',
 'Controlled composition comparison': '受控條件下的隊伍編成比較',
 '36 matched pairs / 72 encounters / 2 seeds / floors 6, 8, 10 / not a full-progression Balance Gate': '36 '
                                                                                                       '組配對 '
                                                                                                       '/ 72 '
                                                                                                       '場戰鬥 '
                                                                                                       '/ 2 '
                                                                                                       '個種子 '
                                                                                                       '/ '
                                                                                                       '6、8、10 '
                                                                                                       '樓 / '
                                                                                                       '不代表完整爬塔平衡驗收'}
def tr(text):return TRANSLATIONS.get(text,text) if locale=='zh-TW' else text
BG='#0d171e';CARD='#182831';TEXT='#e7ede8';MUTED='#a1b7be';ORANGE='#ffa647';CYAN='#4cd5c3';PURPLE='#ae98ff'
def font(n,b=False):return ImageFont.truetype(bold if b else regular,n)
def canvas(title,subtitle,w=1600,h=1000):
    im=Image.new('RGB',(w,h),BG);d=ImageDraw.Draw(im)
    d.text((60,38),tr('EMBER / ENGINEERING'),font=font(20,True),fill=ORANGE)
    d.text((60,78),tr(title),font=font(46,True),fill=TEXT)
    d.text((60,143),tr(subtitle),font=font(22),fill=MUTED)
    d.line((60,190,w-60,190),fill='#304650',width=2)
    d.text((60,h-48),tr('Implementation grounded / Team Balance revision 43a2f70 / Oliver Chen'),font=font(18),fill=MUTED)
    return im,d
def box(d,r,title,lines,color=CYAN):
    x,y,xx,yy=r;d.rounded_rectangle(r,radius=15,fill=CARD,outline='#314952',width=2)
    d.rounded_rectangle((x,y,x+5,yy),radius=2,fill=color)
    d.text((x+24,y+20),tr(title),font=font(26,True),fill=TEXT)
    for i,line in enumerate(lines):
        text=tr(line);size=20
        while font(size).getlength(text)>xx-x-48 and size>16:size-=1
        assert font(size).getlength(text)<=xx-x-48,(title,text)
        d.text((x+24,y+62+i*29),text,font=font(size),fill=MUTED)
def arrow(d,a,b,color=CYAN):
    d.line([a,b],fill=color,width=3);theta=math.atan2(b[1]-a[1],b[0]-a[0])
    tip=[b,(b[0]-13*math.cos(theta-.5),b[1]-13*math.sin(theta-.5)),(b[0]-13*math.cos(theta+.5),b[1]-13*math.sin(theta+.5))];d.polygon(tip,fill=color)
def save(im,name):im.save(out/(name+'.png'),optimize=True)
im,d=canvas('From state to observable action','One authoritative simulation. Separate goals, tactics and presentation.')
box(d,(60,245,435,415),'EXPEDITION STATE',['Agents / Groups / seeded RNG','Memory / Bonds / Inventory','Independent Boss and Rest state'])
box(d,(560,245,1010,415),'DECISION CONTEXT',['Legal options + resources + time','Personality + sourced knowledge','Relationship evidence'])
box(d,(1135,245,1540,415),'HIGH-LEVEL REASONER',['Rule / utility goals by default','Optional bounded LLM gateway','Validated decision proposals'],PURPLE)
arrow(d,(435,330),(560,330));arrow(d,(1010,330),(1135,330))
box(d,(1135,520,1540,720),'LOCAL EXECUTION',['Combat / exploration / social','Danger, legality and resources','Team support and navigation'],ORANGE)
box(d,(560,520,1010,720),'SIMULATION UPDATE',['Fixed-step authoritative state','Events / skill and damage results','Save/load + RNG continuation'])
box(d,(60,520,435,720),'UNITY PRESENTATION',['Models / VFX / camera / UI','Mind, Book and skill inspection','zh-TW / English string tables'])
arrow(d,(1340,415),(1340,520),ORANGE);arrow(d,(1135,620),(1010,620));arrow(d,(560,620),(435,620));arrow(d,(785,520),(785,455));d.text((674,433),tr('state feedback'),font=font(20),fill=MUTED)
box(d,(560,800,1540,920),'EVIDENCE & VALIDATION',['Telemetry, bounded fixtures, process status and build identity'],PURPLE)
arrow(d,(1010,710),(1110,800),PURPLE)
save(im,'architecture')
im,d=canvas('An Agent chooses; the engine checks','Different update rates for goals, tactics and immediate safety.',h=1120)
box(d,(80,240,735,395),'01 / OBSERVE & FILTER',['HP / MP / remaining time / known facilities','Generate only allowed high-level options'])
box(d,(80,440,735,595),'02 / SCORE GOALS',['Personality + needs + time + relationships','Typical goal cadence: 1.5 simulated seconds'],PURPLE)
box(d,(80,640,735,795),'03 / LOCAL TACTICS',['Attack / equipped skill / infusion / protection','Candidate refresh: about 0.6 seconds'],ORANGE)
box(d,(80,840,735,995),'04 / VALIDATE & EXECUTE',['Life, mana, cooldown, weapon, target and range','Update the world and record observable events'])
for y in [395,595,795]:arrow(d,(400,y),(400,y+45))
box(d,(855,240,1520,440),'IMMEDIATE SAFETY',['Each fixed tick (default 0.1 seconds)','Warned Boss areas / hazards / pressure cores','Move to reachable safe space before attacks'],ORANGE)
box(d,(855,510,1520,710),'TEAM COORDINATION',['Urgent rescue before avoidable damage','Reserve pending healing; avoid duplicate shields','Role-aware four-slot skill choices'])
box(d,(855,780,1520,995),'CONTEXT CHANGES',['Death: stop acting and record witnessed moves','Rest: search, discuss, work, route and escape','A goal can be overridden by safety or legality'],PURPLE)
arrow(d,(855,345),(790,345),ORANGE);arrow(d,(790,345),(790,930),ORANGE);arrow(d,(790,930),(735,930),ORANGE)
arrow(d,(855,620),(790,620));arrow(d,(1520,870),(1550,870),PURPLE)
save(im,'decision-flow')
im,d=canvas('Finite memories; explicit inheritance','Observer access does not give Agents automatic knowledge.',h=980)
box(d,(60,255,505,450),'LIVING AGENT',['Personal experience / told information','Bounded Knowledge + relationship events','Source, confidence, age and importance'])
box(d,(575,255,1020,450),'DEATH RECORD',['Personality + actual situation','Only abilities personally witnessed','Book of the Dead: bounded entries'],ORANGE)
box(d,(1090,255,1540,450),'NEXT LIFE',['Failure resets private run state','Book exists, but is not auto-learned','Read in a refuge to gain Book knowledge'])
arrow(d,(505,350),(575,350),ORANGE);arrow(d,(1020,350),(1090,350))
box(d,(1090,585,1540,790),'PREPARATION',['Known threats influence counter skills','Unlocked four-slot loadout only','Known moves get more dodge margin'],PURPLE)
box(d,(60,585,1020,790),'SUCCESS EXCEPTION',['Only living, genuinely completed tower survivors retain eligible memories and bonds.','Reading inherited advice is policy adaptation, not neural training.'],PURPLE)
arrow(d,(1315,450),(1315,585),PURPLE)
save(im,'memory-lifecycle')
# Standard plotting tool for a scientific comparison, independent of screenshots.
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
data=json.loads((target/'Evidence/team-balance-summary.json').read_text())['composition_summary']
from matplotlib import font_manager
font_manager.fontManager.addfont(regular)
plt.rcParams.update({'font.family':font_manager.FontProperties(fname=regular).get_name(),'font.size':12})
fig,ax=plt.subplots(figsize=(12,6),dpi=150);fig.set_facecolor(BG);ax.set_facecolor(BG)
labels=[tr(s) for s in ['4 Warriors','4 Archers','4 Mages','4 Healers','Mixed','No healer']];x=list(range(6))
b=[r['baseline']['mean_dps'] for r in data];c=[r['candidate']['mean_dps'] for r in data]
ax.bar([v-.19 for v in x],b,.36,label=tr('Before (pressure-core baseline)'),color='#637986');ax.bar([v+.19 for v in x],c,.36,label=tr('Team Balance'),color=CYAN)
for i,r in enumerate(data):ax.text(i,max(b[i],c[i])+5,f"{r['dps_change_percent']:+.1f}%",ha='center',color=TEXT,fontsize=11)
ax.set_xticks(x,labels,color=TEXT);ax.tick_params(colors=MUTED);ax.set_ylabel(tr('Mean party damage / simulated second'),color=TEXT);ax.set_ylim(0,190);ax.grid(axis='y',alpha=.15,color=MUTED);ax.set_axisbelow(True)
for spine in ax.spines.values():spine.set_visible(False)
legend=ax.legend(loc='upper left',frameon=False,fontsize=10);plt.setp(legend.get_texts(),color=TEXT)
fig.suptitle(tr('Controlled composition comparison'),color=TEXT,fontsize=23,fontweight='bold',x=.07,ha='left')
fig.text(.07,.03,tr('36 matched pairs / 72 encounters / 2 seeds / floors 6, 8, 10 / not a full-progression Balance Gate'),color=MUTED,fontsize=10)
fig.subplots_adjust(top=.83,bottom=.15,left=.08,right=.98);fig.savefig(out/'balance-comparison.png',facecolor=BG);plt.close(fig)

print('Rendered four portfolio diagrams:',locale)
