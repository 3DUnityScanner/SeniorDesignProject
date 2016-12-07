@ECHO OFF
DEL SeniorDesignDoc.tex
DEL SeniorDesignDoc.pdf
pandoc -N --template=master.tex --biblatex SeniorDesignDoc.md --latex-engine=xelatex -o SeniorDesignDoc.tex
xelatex SeniorDesignDoc.tex
bibtex SeniorDesignDoc
xelatex SeniorDesignDoc.tex
DEL SeniorDesignDoc.aux
DEL SeniorDesignDoc.log
DEL SeniorDesignDoc.out
DEL SeniorDesignDoc.run.xml
DEL SeniorDesignDoc.toc
DEL SeniorDesignDoc-blx.bib
DEL SeniorDesignDoc.bbl
DEL SeniorDesignDoc.blg
DEL SeniorDesignDoc.tex
start SeniorDesignDoc.pdf