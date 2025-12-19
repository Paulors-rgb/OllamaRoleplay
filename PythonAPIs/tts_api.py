# OpenVoice + MeloTTS Local API Server
# Port: 9233
# 100% Compatible with Segmind API format

import os
import sys
import torch
import tempfile
import base64
import hashlib
from pathlib import Path

# Add OpenVoice to path
OPENVOICE_DIR = Path(r"D:\lm\OpenVoice")
sys.path.insert(0, str(OPENVOICE_DIR))
os.chdir(OPENVOICE_DIR)

from fastapi import FastAPI, File, UploadFile, Form, HTTPException
from fastapi.responses import Response, JSONResponse
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import Optional
import soundfile as sf
import numpy as np
import requests as http_requests

# ============================================
# Initialize Models
# ============================================
print("="*50)
print("🎤 OpenVoice + MeloTTS Local API")
print("="*50)

device = "cuda:0" if torch.cuda.is_available() else "cpu"
print(f"Device: {device}")

# Load OpenVoice V2 Tone Color Converter
print("Loading OpenVoice V2 converter...")
from openvoice import se_extractor
from openvoice.api import ToneColorConverter

ckpt_converter = OPENVOICE_DIR / "checkpoints_v2" / "converter"
tone_color_converter = ToneColorConverter(
    str(ckpt_converter / "config.json"), 
    device=device
)
tone_color_converter.load_ckpt(str(ckpt_converter / "checkpoint.pth"))
print("✓ OpenVoice V2 loaded")

# Load MeloTTS models (lazy loading)
print("Preparing MeloTTS...")
from melo.api import TTS as MeloTTS

melo_models = {}

# Language mapping
LANG_TO_MELO = {
    'EN_NEWEST': 'EN_NEWEST',
    'EN': 'EN',
    'EN_US': 'EN',
    'EN_BR': 'EN',
    'EN_AU': 'EN',
    'EN_INDIA': 'EN',
    'ES': 'ES',
    'FR': 'FR',
    'ZH': 'ZH',
    'JP': 'JP',
    'KR': 'KR',
}

# Speaker embedding mapping
LANG_TO_SPEAKER = {
    'EN_NEWEST': 'en-newest',
    'EN': 'en-default',
    'EN_US': 'en-us',
    'EN_BR': 'en-br',
    'EN_AU': 'en-au',
    'EN_INDIA': 'en-india',
    'ES': 'es',
    'FR': 'fr',
    'ZH': 'zh',
    'JP': 'jp',
    'KR': 'kr',
}

def get_melo_model(language: str):
    """Lazy load MeloTTS model"""
    lang = LANG_TO_MELO.get(language.upper(), 'EN_NEWEST')
    if lang not in melo_models:
        print(f"Loading MeloTTS {lang}...")
        melo_models[lang] = MeloTTS(language=lang, device=device)
        print(f"✓ MeloTTS {lang} loaded")
    return melo_models[lang]

# Load base speaker embeddings
print("Loading speaker embeddings...")
base_speakers_dir = OPENVOICE_DIR / "checkpoints_v2" / "base_speakers" / "ses"
source_embeddings = {}
for pth_file in base_speakers_dir.glob("*.pth"):
    name = pth_file.stem
    source_embeddings[name] = torch.load(str(pth_file), map_location=device)
print(f"✓ Loaded {len(source_embeddings)} speakers: {list(source_embeddings.keys())}")

# Voice embedding cache
voice_cache = {}

print("="*50)

# ============================================
# FastAPI App
# ============================================
app = FastAPI(title="OpenVoice API", description="Local TTS with voice cloning")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

OUTPUT_DIR = OPENVOICE_DIR / "outputs"
OUTPUT_DIR.mkdir(exist_ok=True)


# ============================================
# Request Model (Segmind compatible)
# ============================================
class OpenVoiceRequest(BaseModel):
    input_audio: str  # URL or base64
    text: str
    language: str = "EN_NEWEST"
    speed: float = 1.0

# ============================================
# Helper Functions  
# ============================================
def download_audio(url: str, output_path: str) -> str:
    """Download audio from URL"""
    response = http_requests.get(url, timeout=30)
    response.raise_for_status()
    with open(output_path, 'wb') as f:
        f.write(response.content)
    return output_path

def get_voice_embedding(audio_path: str) -> torch.Tensor:
    """Extract voice embedding with caching"""
    with open(audio_path, 'rb') as f:
        file_hash = hashlib.md5(f.read()).hexdigest()[:16]
    
    if file_hash in voice_cache:
        print(f"Using cached voice embedding: {file_hash}")
        return voice_cache[file_hash]
    
    print(f"Extracting voice embedding...")
    target_se, _ = se_extractor.get_se(audio_path, tone_color_converter, vad=True)
    voice_cache[file_hash] = target_se
    return target_se

# ============================================
# API Endpoints
# ============================================

@app.get("/")
async def root():
    return {"service": "OpenVoice + MeloTTS", "status": "ok", "device": device}

@app.get("/health")
async def health():
    return {"status": "ok", "device": device, "cached_voices": len(voice_cache)}

@app.get("/v1/openvoice")
async def info():
    return {
        "languages": list(LANG_TO_MELO.keys()),
        "speakers": list(source_embeddings.keys())
    }

@app.post("/v1/openvoice")
@app.post("/tts")
async def openvoice_tts(request: OpenVoiceRequest):
    """
    Main TTS endpoint - Segmind API compatible
    Returns: audio/wav binary
    """
    try:
        print(f"\n--- TTS Request ---")
        print(f"Text: {request.text[:50]}...")
        print(f"Language: {request.language}")
        print(f"Speed: {request.speed}")
        
        # 1. Download reference audio
        ref_path = str(OUTPUT_DIR / "reference.mp3")
        if request.input_audio.startswith("http"):
            download_audio(request.input_audio, ref_path)
        else:
            # Base64 or local path
            if os.path.exists(request.input_audio):
                ref_path = request.input_audio
            else:
                # Assume base64
                audio_data = base64.b64decode(request.input_audio)
                with open(ref_path, 'wb') as f:
                    f.write(audio_data)
        
        # 2. Generate base TTS with MeloTTS
        model = get_melo_model(request.language)
        speaker_ids = model.hps.data.spk2id
        speaker_id = list(speaker_ids.values())[0]
        
        tmp_tts = str(OUTPUT_DIR / "tmp_base.wav")
        print(f"Generating TTS...")
        model.tts_to_file(request.text, speaker_id, tmp_tts, speed=request.speed)
        
        # 3. Get speaker embeddings
        speaker_key = LANG_TO_SPEAKER.get(request.language.upper(), 'en-newest')
        source_se = source_embeddings.get(speaker_key, source_embeddings['en-newest'])
        target_se = get_voice_embedding(ref_path)
        
        # 4. Convert voice (clone)
        output_path = str(OUTPUT_DIR / "output_final.wav")
        print(f"Cloning voice...")
        tone_color_converter.convert(
            audio_src_path=tmp_tts,
            src_se=source_se,
            tgt_se=target_se,
            output_path=output_path,
            message="@OpenVoice"
        )
        
        # 5. Return audio
        with open(output_path, 'rb') as f:
            audio_bytes = f.read()
        
        print(f"✓ Done! Audio size: {len(audio_bytes)} bytes")
        
        return Response(
            content=audio_bytes,
            media_type="audio/wav",
            headers={"Content-Disposition": "attachment; filename=output.wav"}
        )
        
    except Exception as e:
        print(f"Error: {e}")
        import traceback
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/simple-tts")
async def simple_tts(text: str = Form(...), voice: str = Form(default="fr"), speed: float = Form(default=1.0)):
    """
    Simple TTS without voice cloning (faster)
    voice: fr, en, es, zh, jp, kr
    """
    try:
        # Map voice to language
        voice_map = {
            'fr': 'FR', 'french': 'FR',
            'en': 'EN_NEWEST', 'english': 'EN_NEWEST',
            'es': 'ES', 'spanish': 'ES',
            'zh': 'ZH', 'chinese': 'ZH',
            'jp': 'JP', 'ja': 'JP', 'japanese': 'JP',
            'kr': 'KR', 'ko': 'KR', 'korean': 'KR',
        }
        lang = voice_map.get(voice.lower(), 'FR')
        
        model = get_melo_model(lang)
        speaker_ids = model.hps.data.spk2id
        speaker_id = list(speaker_ids.values())[0]
        
        output_path = str(OUTPUT_DIR / "output_simple.wav")
        model.tts_to_file(text, speaker_id, output_path, speed=speed)
        
        with open(output_path, 'rb') as f:
            audio_bytes = f.read()
        
        return Response(content=audio_bytes, media_type="audio/wav")
        
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/clone")
async def clone_voice(
    audio: UploadFile = File(...),
    text: str = Form(...),
    language: str = Form(default="FR"),
    speed: float = Form(default=1.0)
):
    """
    Upload audio file for voice cloning
    """
    try:
        # Save uploaded audio
        ref_path = str(OUTPUT_DIR / f"upload_{audio.filename}")
        with open(ref_path, 'wb') as f:
            content = await audio.read()
            f.write(content)
        
        # Generate TTS
        model = get_melo_model(language)
        speaker_ids = model.hps.data.spk2id
        speaker_id = list(speaker_ids.values())[0]
        
        tmp_tts = str(OUTPUT_DIR / "tmp_clone.wav")
        model.tts_to_file(text, speaker_id, tmp_tts, speed=speed)
        
        # Clone voice
        speaker_key = LANG_TO_SPEAKER.get(language.upper(), 'fr')
        source_se = source_embeddings.get(speaker_key, source_embeddings['fr'])
        target_se = get_voice_embedding(ref_path)
        
        output_path = str(OUTPUT_DIR / "output_clone.wav")
        tone_color_converter.convert(
            audio_src_path=tmp_tts,
            src_se=source_se,
            tgt_se=target_se,
            output_path=output_path,
            message="@OpenVoice"
        )
        
        with open(output_path, 'rb') as f:
            audio_bytes = f.read()
        
        return Response(content=audio_bytes, media_type="audio/wav")
        
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

# ============================================
# Main
# ============================================
if __name__ == "__main__":
    import uvicorn
    print("\n" + "="*50)
    print("🎤 OpenVoice + MeloTTS API")
    print("="*50)
    print(f"URL: http://127.0.0.1:9233")
    print(f"Docs: http://127.0.0.1:9233/docs")
    print("="*50)
    print("Endpoints:")
    print("  POST /v1/openvoice  - Segmind compatible")
    print("  POST /tts           - Same as above")
    print("  POST /simple-tts    - Fast TTS (no clone)")
    print("  POST /clone         - Upload audio + clone")
    print("="*50 + "\n")
    uvicorn.run(app, host="127.0.0.1", port=9233)
