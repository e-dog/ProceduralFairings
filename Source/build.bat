@echo off
%WINDIR%\Microsoft.NET\Framework64\v3.5\csc /nologo /t:library^
 /out:"C:\games\KSPtest\GameData\ProceduralFairings\ProceduralFairings.dll"^
 /r:"C:\games\KSPtest\GameData\ProceduralFairings\KSPAPIExtensions.dll"^
 /r:"C:\games\KSPtest\KSP_Data\Managed\Assembly-CSharp.dll"^
 /r:"C:\games\KSPtest\KSP_Data\Managed\UnityEngine.dll"^
 NodeNumberTweaker.cs Resizers.cs ^
 FairingBase.cs FairingDecoupler.cs FairingSide.cs PayloadScan.cs ProcAdapter.cs utils.cs
if errorlevel 1 goto exit
copy /y C:\games\KSPtest\GameData\ProceduralFairings\ProceduralFairings.dll "C:\Steam\SteamApps\common\Kerbal Space Program\GameData\ProceduralFairings\"
echo ======================================
:exit
