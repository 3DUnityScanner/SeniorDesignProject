@ECHO OFF
DEL SeniorDesignDoc.docx
pandoc -f markdown -t docx --reference-docx=Template.docx SeniorDesignDoc.md -o SeniorDesignDoc.docx
start SeniorDesignDoc.docx