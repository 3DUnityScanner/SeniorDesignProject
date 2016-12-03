@ECHO OFF
DEL SeniorDesignDoc.docx
pandoc -f markdown -t docx SeniorDesignDoc.md -o SeniorDesignDoc.docx
start SeniorDesignDoc.docx