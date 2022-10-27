@ECHO OFF
set modDir=D:\Games\Steam\steamapps\common\The Scroll Of Taiwu\Mod\IncreaseDifficulty
echo f| xcopy /y Config.lua "%modDir%\Config.lua"
echo f| xcopy /y Cover.jpg "%modDir%\Cover.jpg"
echo f| xcopy /y  .\IncreaseDifficultyBackend\bin\Debug\net5.0\IncreaseDifficultyBackend.dll "%modDir%\Plugins\IncreaseDifficultyBackend.dll"
pause