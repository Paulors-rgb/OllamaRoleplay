# Whisper STT API - Speech-to-Text
# Port: 9234
# Supports: French, English, + 100 languages

import os
import sys
import torch
import tempfile
from pathlib import Path

from fastapi import FastAPI, File, UploadFile, Form, HTTPException
from fastapi.responses import JSONResponse
from fastapi.middleware.cors import CORSMiddleware
from typing import Optional

# ============================================
# Initialize Whisper Model
# ============================================
print("="*50)
print("🎤 Whisper STT API")
print("="*50)

# Force CPU to avoid cuDNN issues
# Change to "cuda" if you have cuDNN properly installed
device = "cpu"
compute_type = "int8"
print(f"Device: {device}, Compute: {compute_type}")

print("Loading Whisper model...")
from faster_whisper import WhisperModel

# Use "small" for good balance speed/accuracy, "medium" or "large-v3" for better accuracy
MODEL_SIZE = "small"
model = WhisperModel(MODEL_SIZE, device=device, compute_type=compute_type)
print(f"✓ Whisper {MODEL_SIZE} loaded!")

print("="*50)

# ============================================
# FastAPI App
# ============================================
app = FastAPI(title="Whisper STT API", description="Speech-to-Text with French support")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

OUTPUT_DIR = Path("D:/lm/OpenVoice/stt_temp")
OUTPUT_DIR.mkdir(exist_ok=True)


# ============================================
# API Endpoints
# ============================================

@app.get("/")
async def root():
    return {
        "service": "Whisper STT",
        "model": MODEL_SIZE,
        "device": device,
        "status": "ok"
    }

@app.get("/health")
async def health():
    return {"status": "ok", "model": f"whisper-{MODEL_SIZE}", "device": device}

@app.post("/transcribe")
async def transcribe(
    file: UploadFile = File(...),
    lang: str = Form(default="fr")
):
    """
    Transcribe audio to text
    
    - file: Audio file (wav, mp3, etc.)
    - lang: Language code (fr, en, auto, etc.)
    """
    try:
        # Save uploaded file
        suffix = Path(file.filename).suffix if file.filename else ".wav"
        tmp_path = OUTPUT_DIR / f"upload_{os.getpid()}{suffix}"
        
        with open(tmp_path, "wb") as f:
            content = await file.read()
            f.write(content)
        
        print(f"\n--- STT Request ---")
        print(f"File: {file.filename} ({len(content)} bytes)")
        print(f"Language: {lang}")
        
        # Transcribe
        language = None if lang == "auto" else lang
        segments, info = model.transcribe(
            str(tmp_path),
            language=language,
            beam_size=5,
            vad_filter=True,  # Filter silence
            vad_parameters=dict(
                min_silence_duration_ms=500,
                speech_pad_ms=400
            )
        )
        
        # Collect text
        text_parts = []
        for segment in segments:
            text_parts.append(segment.text.strip())
        
        full_text = " ".join(text_parts).strip()
        
        # Cleanup
        try:
            os.unlink(tmp_path)
        except:
            pass
        
        print(f"✓ Transcribed: {full_text[:50]}...")
        print(f"  Language: {info.language} ({info.language_probability:.0%})")
        
        return {
            "success": True,
            "text": full_text,
            "language": info.language,
            "language_probability": round(info.language_probability, 2),
            "emotion": "NEUTRAL",  # Whisper doesn't detect emotion
            "event": "Speech"
        }
        
    except Exception as e:
        print(f"STT Error: {e}")
        import traceback
        traceback.print_exc()
        return JSONResponse(
            {"success": False, "text": "", "error": str(e)},
            status_code=500
        )


@app.post("/stt")
async def stt_simple(
    audio: UploadFile = File(...),
    language: str = Form(default="fr")
):
    """Alternative endpoint with different parameter names"""
    return await transcribe(file=audio, lang=language)

# ============================================
# Main
# ============================================
if __name__ == "__main__":
    import uvicorn
    print("\n" + "="*50)
    print("🎤 Whisper STT API Starting...")
    print("="*50)
    print(f"URL: http://127.0.0.1:9234")
    print(f"Docs: http://127.0.0.1:9234/docs")
    print(f"Model: {MODEL_SIZE}")
    print(f"Languages: French, English, + 100 others")
    print("="*50)
    print("Endpoints:")
    print("  POST /transcribe  - Transcribe audio")
    print("  POST /stt         - Same as above")
    print("  GET  /health      - Health check")
    print("="*50 + "\n")
    uvicorn.run(app, host="127.0.0.1", port=9234)
