# -*- coding: utf-8 -*-
import io, re
from docx import Document
from docx.oxml.ns import qn
def slug(s):
    s = re.sub(r'[^0-9A-Za-zА-Яа-я]+','_', s).strip('_')
    return s[:40]
def extract(fn, prefix):
    d = Document(fn)
    body = list(d.element.body)
    from docx.text.paragraph import Paragraph
    log = []
    pending_img = []
    idx = 0
    for el in body:
        if el.tag == qn('w:p'):
            p = Paragraph(el, d)
            # find blips
            blips = el.findall('.//'+qn('a:blip'))
            for b in blips:
                rid = b.get(qn('r:embed'))
                if rid and rid in d.part.related_parts:
                    blob = d.part.related_parts[rid].blob
                    pending_img.append((rid, blob))
            txt = p.text.strip()
            if txt.startswith('Рисунок') and pending_img:
                rid, blob = pending_img.pop(0)
                idx += 1
                name = '_figs/%s_%02d_%s.png' % (prefix, idx, slug(txt))
                open(name,'wb').write(blob)
                log.append('%s  <=  %s' % (name, txt[:70]))
    io.open('_figmap_%s.txt'%prefix,'w',encoding='utf-8').write('\n'.join(log))
    print(prefix, 'figures:', idx)
extract('Диплом.docx','dmitriy')
extract('диплом_ксюша.docx','ksyusha')
