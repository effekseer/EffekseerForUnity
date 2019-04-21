
set OPTS=-f markdown-auto_identifiers -t html5 --section-divs -c ../base.css

for %%i in (Contents_en\*.md) do call pandoc -s %%i %OPTS% -o Contents_en\%%~ni.html

for %%i in (Contents_ja\*.md) do call pandoc -s %%i %OPTS% -o Contents_ja\%%~ni.html

echo doxygen(exe or bat)
call doxygen.bat Doxyfile
call doxygen.exe Doxyfile