@echo off
title Mod Uploader - Publish
dotnet clean -nologo -v q
rd /s /q "%localappdata%\Colossal Order\Cities_Skylines\Addons\Mods\ModUploader"
dotnet publish ModUploader.App/ModUploader.App.csproj  /p:Configuration=Release -nologo -v q
call "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat"
msbuild ModUploader.Mod/ModUploader.Mod.csproj /p:Configuration=Release -nologo -v:q