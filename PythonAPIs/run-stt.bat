@echo off
echo ============================================
echo Whisper STT API Server (Port 9234)
echo ============================================
cd /d D:\lm\OpenVoice

REM Activate conda environment
call C:\Users\YOURNAME\miniconda3\condabin\conda.bat activate openvoice

REM Start STT API
python stt_api.py

pause
