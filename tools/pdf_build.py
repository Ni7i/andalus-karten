import json, re, sys
sys.path.insert(0,__import__('os').path.dirname(__file__))
from pdf_cells import logical
d=json.load(open('raw.json'))
AR=re.compile(r'[؀-ۿ]')
def isar(s): return bool(AR.search(s))
FONTFIX={'#':'\u0651','_':'\u0651','F':'\u0650\u0651','~':'\u0650\u0651','&':'\u0644\u0623','R':'\u0644\u0627','Ü':'\u0644\u0627','n':'\u0644\u0627','à':'\u064a\u0646','Z':'\u0644\u0644\u0647','\\':'\u064a\u0646'}
def arfix(s):
    s=s.replace(' Z ',' لِلَّهِ ').replace('مُعَ\\ٌّ','مُعَيَّنٌ')
    return ' '.join(''.join(FONTFIX.get(c,c) for c in tok) if re.search(r'[\u0600-\u06ff]',tok) and not re.search(r'[a-mo-zA-EG-QS-Y]',tok) else tok for tok in s.split(' '))
def clean(s): 
    s=(s or '').replace('\n',' ').strip()
    return '' if s in ('-','–','—') else re.sub(r'\s+',' ',s)
def key(s): return re.sub(r'[^ء-ي]','',s)
TITLEFIX={'#':'ّ','&':'لأ'}
def title_logical(s):
    s=logical(''.join(TITLEFIX.get(c,c) for c in s))
    return s
import unicodedata
def strip(s): return re.sub(r'[^\u0621-\u064a]','',s.replace('n','لا').replace('F','ت'))
TENS=[('عشرون',20),('ثلاثون',30),('ثلاثون',30),('اربعون',40),('أربعون',40),('ربعون',40),('خمسون',50),('ستون',60),('تتون',60),('سبعون',70),('عشر',10)]
UNITS=[('االول',1),('اول',1),('حادي',1),('ثاني',2),('ثالث',3),('رابع',4),('خامس',5),('خماس',5),('سادس',6),('سابع',7),('سبع',7),('ثامن',8),('ثمان',8),('تاسع',9),('عاشر',10)]
def lesson_no(line):
    t=logical(''.join(TITLEFIX.get(c,c) for c in line))
    t=strip(t).replace('لدرس','',1)
    t=t.replace('أ','ا').replace('إ','ا')
    for cand in (t, t[::-1]):
        tens=0
        for w,v in TENS:
            w=w.replace('أ','ا')
            if w in cand: tens=v; cand=cand.replace(w,'',1); break
        unit=0
        for w,v in UNITS:
            if w in cand or w[::-1] in cand: unit=v; break
        if tens==10 and unit==10: unit=0
        n=tens+unit if not (tens==10 and unit==0) else 10
        if n: return n
    return None
lessons=[]; cur=None; markers={}
pending_verbs=None
def new_lesson(mk):
    global cur
    cur={'n':len(lessons)+1,'title':None,'items':[],'verbs':[],'examples':{}}
    lessons.append(cur)
for i,p in enumerate(d):
    f=p['footer']
    mk=None
    for l in f:
        if 'دلا' in l and 'سْر' in l:
            mk=lesson_no(l)
    if mk and mk not in markers:
        markers[mk]=1; new_lesson(mk); cur['n']=mk
    if cur is None: continue
    if mk and cur['title'] is None and len(f)==2 and not re.search(r'[\ufb50-\ufeff]',f[0]) and 'سْر' not in f[0]:
        t=title_logical(f[0]).replace('\u06cc','\u064a').replace('\u06be','\u0647')
        if isar(t) and len(t)<60 and not re.search(r'[A-Za-z0-9]',t): cur['title']=t
    for t in p['tables']:
        if not t: continue
        h=' | '.join(t[0]); rows=[[clean(c) for c in r] for r in t[1:]]
        rows=[r for r in rows if any(r)]
        if h.startswith('المَعْنَى | الجَمْعُ الثَّانِي'):
            for m,p2,p1,s in rows:
                if s and m: cur['items'].append(dict(ar=s,de=m,note=' / '.join(x for x in (p1,p2) if x),pl=1,core=True))
        elif h.startswith('المُضَادُّ | المُرَادِفُ | المَعْنَى'):
            for a,sy,m,pl,s in rows:
                if s and m:
                    n=[]; 
                    if pl: n.append('Pl. '+pl)
                    if sy: n.append('≈ '+sy)
                    if a: n.append('↔ '+a)
                    cur['items'].append(dict(ar=s,de=m,note=' · '.join(n),core=True))
        elif h.startswith('الجُمْلَةُ | المَعْنَى | التَّعْبِيرُ'):
            for se,m,e in rows:
                if e and m: cur['items'].append(dict(ar=e,de=m,note=se,core=False))
        elif h.startswith('المَصْدَرُ | الأَمْرُ'):
            vs=[dict(madi=r[4],harf=r[3],mud=r[2],amr=r[1],masdar=r[0]) for r in rows if r[4]]
            # meanings on next page
            mean=[]
            if i+1<len(d):
                for t2 in d[i+1]['tables']:
                    if t2 and t2[0] and t2[0][0].startswith('الجُمْلَةُ/المَعْنَى'):
                        mean=[clean(r[0]) for r in t2[1:]]
            for k,v in enumerate(vs): v['de']=mean[k] if k<len(mean) else ''
            cur['verbs']+=vs
        elif h.startswith('اِسْمُ المَفْعُولِ | اِسْمُ الفَاعِلِ | المَصْدَرُ'):
            cur['verbs']+=[dict(madi=r[5],harf=r[4],mud=r[3],masdar=r[2],fail=r[1],maful=r[0],de='') for r in rows if r[5]]
        elif h.startswith('Examples'):
            for r in rows:
                if len(r)<4: continue
                en,arS=(r[0],r[1]) if not isar(r[0]) else (r[1],r[0])
                w,sing=r[2],r[3]
                if not sing or not en: continue
                if 'المَاضِي' in h: cur['examples'][key(sing)]=(en,arS)
                else: cur['items'].append(dict(ar=sing,de=en,note=arS+((' · Pl. '+w) if w else ''),core=False))
        elif h.startswith('English | Arabic | Number'):
            for r in rows:
                if len(r)>=2 and r[1] and r[0]: cur['items'].append(dict(ar=r[1],de=r[0],note=r[2] if len(r)>2 else '',core=True))
    pass
out_l=[]; out_e=[]; core=[]
stats=dict(words=0,verbs_nomean=0)
for L in lessons:
    words=[]
    for v in L['verbs']:
        ex=L['examples'].pop(key(v['madi']),None)
        de=v['de'] or (ex[0] if ex else '')
        if not de: stats['verbs_nomean']+=1; continue
        ar=v['madi']+((' '+v['harf']) if v['harf'] else '')
        note=' – '.join(x for x in (v['mud'],v['masdar']) if x)
        if ex and v['de']: note+=' · '+ex[1]
        elif ex: note+=' · '+ex[1]
        words.append(dict(ar=ar,de=de,note=note,core=bool(v['de'])))
    for k,(en,arS) in L['examples'].items():
        pass
    words+=L['items']
    seen=set(); W=[]
    for w in words:
        kk=(key(w['ar']),w['de'].lower())
        if kk in seen: continue
        seen.add(kk); W.append(w)
    if not W: continue
    lid=f"L{L['n']:02d}"
    ws=[]
    for j,w in enumerate(W,1):
        o={'id':f'{lid}-{j:03d}','ar':arfix(w['ar']),'de':w['de']}
        if w.get('note'): o['note']=arfix(w['note'])
        ws.append(o)
        if w['core']: core.append((L['n'],o))
    out_l.append({'id':lid,'number':L['n'],'title':', '.join(dict.fromkeys(w['de'].split('(')[0].strip() for w in W if w['core'] and len(w['de'])<25))[:0] or ', '.join(list(dict.fromkeys(w['de'].split('(')[0].strip() for w in W if w['core'] and len(w['de'])<25))[:3]) or f"Lektion {L['n']}",'words':ws})
    stats['words']+=len(ws)
# Essential: Kernwortschatz je 5 Lektionen
groups={}
for n,o in core: groups.setdefault((n-1)//5,[]).append(o)
for g,ws in sorted(groups.items()):
    a,b=g*5+1,g*5+5
    out_e.append({'id':f'E{g+1:02d}','number':g+1,'title':f'Kernwortschatz Lektionen {a}–{b}','essential':True,'words':ws})
json.dump({'lessons':out_l+out_e},open(sys.argv[1],'w'),ensure_ascii=False,indent=1)
print(len(out_l),'Lektionen',stats,len(out_e),'Essential',len(core),'Kernwörter')
for L in out_l[:60]: print(L['number'],L['title'],len(L['words']))
