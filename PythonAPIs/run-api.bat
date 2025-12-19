@echo off
echo ============================================
echo OpenVoice + MeloTTS API Server
echo ============================================
cd /d D:\lm\OpenVoice

REM Activate conda environment
call conda activate openvoice

REM Start API
python tts_api.py

pause
