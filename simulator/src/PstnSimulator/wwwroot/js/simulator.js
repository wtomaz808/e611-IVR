// ─────────────────────────────────────────────────────────────────
// PSTN Simulator — JavaScript Functions
// ─────────────────────────────────────────────────────────────────

/**
 * Speak text using the Web Speech API (Text-to-Speech)
 * @param {string} text - The text to speak
 */
window.speakText = function (text) {
    if ('speechSynthesis' in window) {
        // Cancel any ongoing speech
        window.speechSynthesis.cancel();

        // Create utterance
        const utterance = new SpeechSynthesisUtterance(text);
        
        // Configure voice parameters
        utterance.rate = 1.0;  // Normal speed
        utterance.pitch = 1.0; // Normal pitch
        utterance.volume = 1.0; // Full volume
        
        // Optionally select a specific voice (prefer female voices for IVR)
        const voices = window.speechSynthesis.getVoices();
        const preferredVoice = voices.find(v => v.lang.startsWith('en') && v.name.includes('Female')) 
                            || voices.find(v => v.lang.startsWith('en'));
        if (preferredVoice) {
            utterance.voice = preferredVoice;
        }

        // Handle errors
        utterance.onerror = function(event) {
            console.error('Speech synthesis error:', event);
        };

        // Speak the text
        window.speechSynthesis.speak(utterance);
    } else {
        console.warn('Speech synthesis not supported in this browser');
    }
};

/**
 * Play an audio file by URL (for Teams bot mode — IVR prompts are WAV files in Blob Storage)
 * Falls back to a visual-only indicator if the URL can't be played (CORS restriction).
 * @param {string} url - Audio file URL
 */
window.playAudioUrl = function (url) {
    // Stop any existing audio
    if (window._ivrAudio) {
        window._ivrAudio.pause();
        window._ivrAudio = null;
    }
    try {
        const audio = new Audio(url);
        audio.volume = 1.0;
        audio.onerror = function () {
            // CORS or network error — fall back to browser TTS placeholder
            window.speakText("I V R prompt playing");
            console.warn('Audio playback failed (CORS?), using TTS fallback. URL:', url);
        };
        audio.play().catch(function (err) {
            window.speakText("I V R prompt playing");
            console.warn('Audio play() rejected:', err.message);
        });
        window._ivrAudio = audio;
    } catch (e) {
        console.error('playAudioUrl error:', e);
    }
};

/**
 * Stop any ongoing speech
 */
window.stopSpeech = function () {
    if ('speechSynthesis' in window) {
        window.speechSynthesis.cancel();
    }
};

// Load voices when available (some browsers load them asynchronously)
if ('speechSynthesis' in window) {
    window.speechSynthesis.onvoiceschanged = function() {
        const voices = window.speechSynthesis.getVoices();
        console.log('Speech synthesis voices loaded:', voices.length);
    };
}
