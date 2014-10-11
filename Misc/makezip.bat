@echo off
set name=C:\games\KSPtest\ProcFairings_3.10.zip
del %name%
7z a %name% readme.txt
cd ..
7z a %name% GameData
