import sys, json, re
sys.path.insert(0,__import__('os').path.dirname(__file__))
from pdf_cells import logical, cell_text
import pdfplumber, pypdf
FIX={'ی':'ي','ھ':'ه','ک':'ك'}
def norm(s):
    s=''.join(FIX.get(c,c) for c in s)
    s=re.sub(r'[٠-٩]+', lambda m:m.group(0)[::-1], s)
    return s.strip()
out=[]
files=[('dec79e34-1780517390313-1-400.pdf',0),('b8a3d460-1780517390313-400-600.pdf',399),('a366b982-1780517390313-600-856.pdf',599)]
d=sys.argv[1] if len(sys.argv)>1 else './'
for f,off in files:
    pdf=pdfplumber.open(d+f); rd=pypdf.PdfReader(d+f)
    for i,p in enumerate(pdf.pages):
        pn=off+i+1
        if off and i==0: continue  # overlapping first page
        lines=[l for l in (rd.pages[i].extract_text() or '').splitlines() if l.strip()]
        tables=[]
        for t in p.find_tables():
            tables.append([[norm(logical(cell_text(p,c))) if c else '' for c in r.cells] for r in t.rows])
        out.append({'page':pn,'footer':lines[-2:] ,'tables':tables})
        p.flush_cache()
json.dump(out,open('raw.json','w'),ensure_ascii=False)
print(len(out))
