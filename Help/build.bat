
set OPTS=-f markdown-auto_identifiers -t html5 --section-divs -c ../base.css

for %%i in (Contents_en\*.md) do pandoc -s %%i %OPTS% -o Contents_en\%%~ni.html

for %%i in (Contents_ja\*.md) do pandoc -s %%i %OPTS% -o Contents_ja\%%~ni.html