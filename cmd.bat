@ECHO OFF
set modDir=D:\Games\Steam\steamapps\workshop\content\838350\2880390461
echo f| xcopy /y Config.lua "%modDir%\Config.lua"
echo f| xcopy /y Cover.jpg "%modDir%\Cover.jpg"
echo f| xcopy /y  .\IncreaseDifficultyBackend\bin\Debug\net5.0\IncreaseDifficultyBackend.dll "%modDir%\Plugins\IncreaseDifficultyBackend.dll"
echo f| xcopy /y  .\IncreaseDifficultyFrontend\bin\Debug\IncreaseDifficultyFrontend.dll "%modDir%\Plugins\IncreaseDifficultyFrontend.dll"
pause