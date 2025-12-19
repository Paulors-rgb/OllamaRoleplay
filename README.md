# 🎭 Ollama Roleplay

Une application Windows de roleplay conversationnel avec des personnages IA, incluant le clonage vocal et la reconnaissance vocale.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![WPF](https://img.shields.io/badge/WPF-Windows-0078D6?logo=windows)
![License](https://img.shields.io/badge/License-MIT-green)

---

## ⚠️ Avertissement

**Cette application est destinée à un public adulte (18+).**

L'utilisateur est seul responsable de l'utilisation qu'il fait de ce logiciel et des contenus générés par les modèles IA. Les personnages créés doivent avoir au minimum 18 ans.

---

## 🌟 Fonctionnalités

- **🤖 Chat IA Local** - Conversations avec des modèles Ollama (Llama, Mistral, etc.)
- **👤 Personnages Personnalisables** - Créez des personnages avec personnalité, histoire, apparence
- **🎤 Clonage Vocal (TTS)** - Les personnages parlent avec une voix clonée via OpenVoice + MeloTTS
- **🗣️ Reconnaissance Vocale (STT)** - Parlez au lieu de taper grâce à Whisper
- **🌍 Multilingue** - Interface FR/EN, support vocal pour 6+ langues
- **🔒 Conversations Chiffrées** - Stockage local sécurisé (AES-256)
- **🌐 Accès Internet Optionnel** - Recherche web pour enrichir les réponses
- **🎨 Interface Moderne** - Design Material Dark avec WPF

## 📸 Aperçu

```
┌─────────────────────────────────────────────────────────────┐
│  🎭 Ollama Roleplay                              [FR/EN]    │
├─────────────┬───────────────────────────────────────────────┤
│ Status      │                                               │
│ ● Ollama    │   💬 Chat avec Luna                          │
│ ● TTS       │                                               │
│ ● STT       │   👤 Bonjour ! Comment vas-tu ?              │
│             │                                               │
│ Modèle LLM  │   🤖 Luna: Salut ! Je vais très bien,        │
│ [llama3.2] │       merci de demander ! 😊                  │
│             │                                               │
│ Personnages │                                    [🔊 Lire]  │
│ > Luna      │                                               │
│   Marcus    │   ┌─────────────────────────────────────┐    │
│   Sophie    │   │ Tapez votre message...    [🎤][📤] │    │
│             │   └─────────────────────────────────────┘    │
└─────────────┴───────────────────────────────────────────────┘
```

## 🛠️ Technologies & Dépendances

### Application Desktop (C# WPF)
| Composant | Version | Description |
|-----------|---------|-------------|
| .NET | 8.0 | Framework principal |
| WPF | - | Interface graphique Windows |
| NAudio | 2.2+ | Enregistrement audio |
| System.Text.Json | - | Sérialisation JSON |

### Backend IA (Python)
| Composant | Description |
|-----------|-------------|
| [Ollama](https://ollama.ai) | Serveur LLM local |
| [OpenVoice](https://github.com/myshell-ai/OpenVoice) | Clonage vocal |
| [MeloTTS](https://github.com/myshell-ai/MeloTTS) | Synthèse vocale multilingue |
| [faster-whisper](https://github.com/SYSTRAN/faster-whisper) | Reconnaissance vocale |
| FastAPI | API REST pour TTS/STT |

### Langues Supportées (Voix)
- 🇫🇷 Français
- 🇬🇧 English
- 🇪🇸 Español
- 🇨🇳 中文
- 🇯🇵 日本語
- 🇰🇷 한국어

## 📋 Prérequis

1. **Windows 10/11** (64-bit)
2. **[Ollama](https://ollama.ai)** installé avec au moins un modèle
3. **Python 3.9+** avec Conda (Miniconda/Anaconda)
4. **GPU NVIDIA** (recommandé) ou CPU

## 🚀 Installation

### 1. Cloner le repository
```bash
git clone https://github.com/VOTRE_USERNAME/OllamaRoleplay.git
cd OllamaRoleplay
```

### 2. Compiler l'application
```bash
dotnet build -c Release
```

### 3. Configurer l'environnement Python (pour TTS/STT)
```bash
conda create -n openvoice python=3.9
conda activate openvoice
pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu118
pip install openvoice-cli melo-tts faster-whisper fastapi uvicorn python-multipart
```

### 4. Télécharger les modèles OpenVoice
```bash
# Cloner OpenVoice (ou télécharger les checkpoints)
git clone https://github.com/myshell-ai/OpenVoice.git
# Les checkpoints seront téléchargés automatiquement au premier lancement
```

## 🎮 Utilisation

### Démarrer les services

**Terminal 1 - Ollama :**
```bash
ollama serve
```

**Terminal 2 - TTS (OpenVoice) :**
```bash
# Utiliser le script fourni ou manuellement :
conda activate openvoice
python PythonAPIs/tts_api.py
```

**Terminal 3 - STT (Whisper) :**
```bash
conda activate openvoice
python PythonAPIs/stt_api.py
```

**Terminal 4 - Application :**
```bash
dotnet run -c Release
```

### Ports par défaut
| Service | Port | URL |
|---------|------|-----|
| Ollama | 11434 | http://localhost:11434 |
| OpenVoice TTS | 9233 | http://127.0.0.1:9233 |
| Whisper STT | 9234 | http://127.0.0.1:9234 |

## 📁 Structure du Projet

```
OllamaRoleplay/
├── Models/
│   ├── Character.cs          # Modèle de personnage
│   ├── ChatMessage.cs        # Messages de conversation
│   └── AppSettings.cs        # Configuration
├── Services/
│   ├── OllamaService.cs      # Communication avec Ollama
│   ├── TTSService.cs         # Text-to-Speech (OpenVoice)
│   ├── STTService.cs         # Speech-to-Text (Whisper)
│   ├── ConversationService.cs # Gestion des conversations
│   └── CharacterService.cs   # Gestion des personnages
├── Views/
│   └── MainWindow.xaml(.cs)  # Interface principale
├── PythonAPIs/
│   ├── tts_api.py            # API OpenVoice TTS
│   └── stt_api.py            # API Whisper STT
└── App.xaml(.cs)             # Point d'entrée
```

## 🎭 Créer un Personnage

1. Cliquez sur **➕** dans la section Personnages
2. Remplissez les informations :
   - **Nom** : Le nom du personnage
   - **Âge** : Son âge (minimum 18 ans)
   - **Genre** : Male/Female
   - **Description** : Apparence physique
   - **Personnalité** : Traits de caractère
   - **Histoire** : Background du personnage
   - **Langue** : Langue de réponse
3. **(Optionnel)** Ajoutez un **échantillon vocal** (MP3/WAV) pour le clonage de voix
4. Cliquez **💾 Sauvegarder**

## 🗣️ Conversation Vocale

1. **Pour parler** : Cliquez 🎤, parlez, cliquez ⏹️ → Message envoyé automatiquement
2. **Pour écouter** : Clic droit sur un message → 🔊 Lire

## 👨‍💻 Développement

### Développé par
**PauloR Sl33pytech**

### Assisté par
**Claude (Anthropic)** - Assistant IA pour le développement, architecture et debugging

### Outils de développement
- Visual Studio 2022 / VS Code
- .NET 8 SDK
- Conda / Miniconda

## 📄 License

Ce projet est sous licence MIT - voir le fichier [LICENSE](LICENSE) pour plus de détails.

## 🙏 Remerciements

- [Ollama](https://ollama.ai) - Pour le serveur LLM local
- [MyShell.ai](https://github.com/myshell-ai) - Pour OpenVoice et MeloTTS
- [SYSTRAN](https://github.com/SYSTRAN) - Pour faster-whisper
- [Anthropic](https://anthropic.com) - Pour Claude, assistant IA

---

<p align="center">
  Fait avec ❤️ et 🤖
</p>
