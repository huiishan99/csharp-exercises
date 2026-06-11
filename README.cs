@echo off
setlocal enabledelayedexpansion

REM 输出文件夹
set "OUTPUT_DIR=rotated"

REM 如果输出文件夹不存在，就创建
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

REM 遍历当前文件夹下所有 mp4 文件
for %%F in (*.mp4) do (
    echo Processing: %%F

    REM 输出文件名：rotated_原文件名
    ffmpeg -y -i "%%F" -vf "hflip,vflip" -c:v libx264 -crf 18 -preset medium -c:a copy "%OUTPUT_DIR%\rotated_%%F"
)

echo Done.
pause
