@echo off
set KSP=C:\games\KSPtest
%WINDIR%\Microsoft.NET\Framework64\v3.5\csc /nologo /t:library^
 /out:"%KSP%\GameData\ProceduralFairings\ProceduralFairings.dll"^
 /r:"%KSP%\GameData\ProceduralFairings\KSPAPIExtensions.dll"^
 /r:"%KSP%\KSP_Data\Managed\Assembly-CSharp.dll"^
 /r:"%KSP%\KSP_Data\Managed\UnityEngine.dll"^
 NodeNumberTweaker.cs Resizers.cs ^
 FairingBase.cs FairingDecoupler.cs FairingSide.cs PayloadScan.cs ProcAdapter.cs utils.cs
if errorlevel 1 goto exit
rem copy /y %KSP%\GameData\ProceduralFairings\ProceduralFairings.dll "C:\Steam\SteamApps\common\Kerbal Space Program\GameData\ProceduralFairings\"
echo ======================================
:exit
