# -*- coding: utf-8 -*-
import io
from docx import Document
def dump(fn, out):
    d = Document(fn)
    buf = []
    for blk in d.element.body:
        tag = blk.tag.split('}')[-1]
        if tag == 'p':
            from docx.text.paragraph import Paragraph
            p = Paragraph(blk, d)
            buf.append(p.text)
        elif tag == 'tbl':
            buf.append("[ТАБЛИЦА]")
    io.open(out,'w',encoding='utf-8').write("\n".join(buf))
    print(out, len(buf), "blocks")
dump("Диплом.docx","_x_diplom_dmitriy.txt")
dump("диплом_ксюша.docx","_x_diplom_ksyusha.txt")
