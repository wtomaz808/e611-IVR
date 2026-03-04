# Simulator Audio & Speech Input Improvements - March 4, 2026

## Summary

Enhanced the PSTN Simulator with text-to-speech audio playback and improved speech input functionality.

## Issues Fixed

### 1. Speech Input Box Not Working ✅

**Problem:** The "Speech Input" text box was disabled and couldn't be used to respond to IVR prompts.

**Root Cause:** The input was only enabled when the IVR explicitly requested speech recognition mode (`RecognizeType = "speech"`). However, most IVR menus use DTMF by default, so the speech input was rarely available.

**Fix:** Changed the speech input to work as an alternative to DTMF buttons:
- Now enabled whenever the call is in `IvrRecognizing` state (regardless of DTMF or speech mode)
- Can be used alongside DTMF buttons
- Users can type what they would say and press Enter or click the mic button
- Added helpful text: "Type what you would say and press Enter or click the mic button"

**Changed File:** [simulator/src/PstnSimulator/Pages/Phone.razor](../simulator/src/PstnSimulator/Pages/Phone.razor)

### 2. No Audio from Phone System ✅

**Problem:** No audio playback when the IVR plays prompts - only text was displayed.

**Root Cause:** The simulator was text-only with no audio implementation.

**Fix:** Added text-to-speech audio using the Web Speech API:
- **Toggle Control:** Added "Enable Audio Prompts (Text-to-Speech)" checkbox
- **Automatic Playback:** When enabled, IVR prompts are automatically spoken using the browser's text-to-speech engine
- **Smart Voice Selection:** Prefers English female voices for a realistic IVR experience
- **No Repeat:** Prompts are only spoken once (won't repeat on UI updates)

**New Files:**
- [simulator/src/PstnSimulator/wwwroot/js/simulator.js](../simulator/src/PstnSimulator/wwwroot/js/simulator.js) - Speech synthesis JavaScript
- Updated [simulator/src/PstnSimulator/Pages/_Host.cshtml](../simulator/src/PstnSimulator/Pages/_Host.cshtml) - Added script reference

## How to Use

### Testing Locally (http://localhost:5200)

1. **Place a Call:**
   - Select a caller and DID
   - Click "Call"

2. **Enable Audio (Recommended):**
   - Check the "Enable Audio Prompts (Text-to-Speech)" checkbox
   - You will hear IVR prompts spoken through your speakers

3. **Respond to Prompts:**
   
   **Option A - DTMF (Number Buttons):**
   - Click the number buttons when prompted
   - Example: Press "1" for Fire, "2" for Medical, etc.

   **Option B - Speech Input:**
   - Type what you would say in the "Speech Input" textbox
   - Examples: "fire alarm", "medical emergency", "inspection"
   - Press Enter or click the microphone button
   - The IVR will process your speech input using Azure OpenAI

4. **Monitor Call Flow:**
   - Watch the "Call Event Trace" on the right
   - See the current prompt displayed in "Active Call" section
   - Hear prompts spoken automatically (if audio enabled)

### Testing on Azure (https://ivr-dev-simulator-4c5ax3aimbdsy.azurewebsites.us)

Same process as local testing above.

## Technical Details

### Speech Input Changed From:
```csharp
disabled="@(_activeCall == null || 
           _activeCall.State != CallState.IvrRecognizing || 
           _activeCall.RecognizeType != "speech")"
```

### Speech Input Changed To:
```csharp
disabled="@(_activeCall == null || 
           _activeCall.State != CallState.IvrRecognizing)"
```

This allows speech input to work with both DTMF and speech-enabled menus.

### Audio Implementation

**JavaScript Function (Web Speech API):**
```javascript
window.speakText = function (text) {
    if ('speechSynthesis' in window) {
        const utterance = new SpeechSynthesisUtterance(text);
        utterance.rate = 1.0;  // Normal speed
        utterance.pitch = 1.0; // Normal pitch
        utterance.volume = 1.0; // Full volume
        
        // Prefer English female voices
        const voices = window.speechSynthesis.getVoices();
        const preferredVoice = voices.find(v => 
            v.lang.startsWith('en') && v.name.includes('Female'));
        if (preferredVoice) {
            utterance.voice = preferredVoice;
        }
        
        window.speechSynthesis.speak(utterance);
    }
};
```

**C# Integration (Blazor):**
```csharp
private async Task SpeakPromptAsync(string text)
{
    await JS.InvokeVoidAsync("speakText", text);
}
```

**Automatic Playback:**
- Triggers when `CurrentPromptText` changes
- Only if audio is enabled
- Prevents repeating the same prompt

## Browser Compatibility

The Web Speech API is supported in:
- ✅ **Chrome/Edge:** Full support with multiple voices
- ✅ **Safari:** Full support with macOS/iOS voices
- ✅ **Firefox:** Supported (may have limited voices)
- ❌ **Internet Explorer:** Not supported

If speech synthesis is unavailable, the simulator will silently fail and continue working in text-only mode.

## Files Changed

### Modified
1. **[simulator/src/PstnSimulator/Pages/Phone.razor](../simulator/src/PstnSimulator/Pages/Phone.razor)**
   - Changed speech input condition to allow use with DTMF mode
   - Added audio enable/disable toggle
   - Added `_audioEnabled` and `_lastSpokenPrompt` state
   - Added `SpeakPromptAsync` method
   - Integrated audio playback in `HandleCallUpdated`
   - Injected `IJSRuntime`

2. **[simulator/src/PstnSimulator/Pages/_Host.cshtml](../simulator/src/PstnSimulator/Pages/_Host.cshtml)**
   - Added `<script src="js/simulator.js"></script>`

### Created
3. **[simulator/src/PstnSimulator/wwwroot/js/simulator.js](../simulator/src/PstnSimulator/wwwroot/js/simulator.js)** (New)
   - `speakText(text)` - Text-to-speech function
   - `stopSpeech()` - Cancel ongoing speech
   - Voice loading and selection logic

## Testing Checklist

- ✅ Hang up button works (can make multiple calls)
- ✅ Speech input box is enabled during IVR prompts
- ✅ Can type speech responses and submit with Enter or mic button
- ✅ DTMF buttons work as before
- ✅ Audio toggle checkbox appears
- ✅ Prompts are spoken when audio is enabled
- ✅ Prompts display as text in "Active Call" section
- ✅ Call event trace shows all interactions
- ✅ Local container rebuilt and running
- ✅ Azure deployment successful

## Example Workflow

1. Enable audio checkbox ✓
2. Place call ✓
3. **Hear:** "Welcome to the E911 IVR system..."
4. **See:** Prompt text in Active Call section
5. **Respond:** Either click "1" button OR type "fire alarm" and press Enter
6. **Hear:** Next prompt based on selection
7. Continue through call flow with audio feedback

## Benefits

1. **Better Testing Experience:** Hear prompts as callers would
2. **Accessibility:** Audio option for better UX
3. **Flexibility:** Can use either DTMF or speech input
4. **Realistic:** Simulates actual caller experience
5. **Educational:** Understand prompt phrasing and flow

## Notes

- Audio is **enabled by default** - uncheck to disable
- Speech input is now **always available** during IVR prompts
- Both DTMF and speech can be used interchangeably
- Text prompts are still shown even with audio disabled
- No actual phone calls are made (still using mock ACS)

---

**Update Completed**: March 4, 2026, 10:05 PM UTC  
**Performed By**: GitHub Copilot  
**Deployed To**: Local + Azure Government  
**Status**: ✅ All features operational
