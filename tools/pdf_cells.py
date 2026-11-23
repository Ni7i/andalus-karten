import sys, unicodedata, re, pdfplumber

LIG = {'ﱞ':'ٌّ','ﱟ':'ٍّ','ﱠ':'َّ','ﱡ':'ُّ','ﱢ':'ِّ','ﱣ':'ّٰ'}
def is_ar(c): return '؀'<=c<='ۿ' or 'ﭐ'<=c<='﻿' or 'ݐ'<=c<='ݿ'

def fix_ar(s):
    # input in visual (reversed) order; LIG are reversed-order replacements
    s=''.join(LIG.get(c,c) for c in s)
    s=s[::-1]
    s=unicodedata.normalize('NFKC',s)
    return s

def logical(text):
    """Visual-order line → logical order. Reverse whole line if mostly arabic; keep latin runs."""
    text=text.strip()
    if not any(is_ar(c) for c in text): return text
    # split into runs of latin vs arabic
    runs=re.findall(r'[A-Za-z0-9][A-Za-z0-9 ,.\'()\-?!/;:’]*[A-Za-z0-9.)?!]|[A-Za-z0-9]', text)
    out=fix_ar(text)
    # latin runs got reversed: restore
    for r in runs:
        out=out.replace(r[::-1], r)
    return re.sub(r'\s+',' ',out).strip()

def cell_text(page, bbox):
    x0,top,x1,bottom=bbox
    cs=[c for c in page.chars if c['x0']>=x0-1 and c['x1']<=x1+1 and c['top']>=top-1 and c['bottom']<=bottom+1]
    # stream order is list order; add spaces at gaps between consecutive chars on same line
    return ''.join(c['text'] for c in cs)

if __name__=='__main__':
    pdf=pdfplumber.open(sys.argv[1])
    for pn in map(int,sys.argv[2:]):
        p=pdf.pages[pn-1]
        for t in p.find_tables():
            for row in t.rows[:6]:
                print([logical(cell_text(p,c)) if c else None for c in row.cells])
