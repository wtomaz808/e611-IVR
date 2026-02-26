# Prompt Management Guide

This document covers creating, configuring, and managing IVR prompts — the audio or text-to-speech messages played to callers at each step of the call flow.

---

## Table of Contents

1. [Prompt Types](#1-prompt-types)
2. [Prompt Data Model](#2-prompt-data-model)
3. [Creating Prompts](#3-creating-prompts)
4. [Using Prompts in Menus](#4-using-prompts-in-menus)
5. [TTS Voice Options](#5-tts-voice-options)
6. [SSML Advanced Formatting](#6-ssml-advanced-formatting)
7. [Audio File Prompts](#7-audio-file-prompts)
8. [Prompt Resolution at Runtime](#8-prompt-resolution-at-runtime)
9. [Best Practices](#9-best-practices)

---

## 1. Prompt Types

The IVR system supports three prompt types:

| Type | Description | Use Case |
|---|---|---|
| **TTS** (Text-to-Speech) | Plain text converted to speech by Azure Cognitive Services | Quick setup, easy to edit, no recording needed |
| **Audio File** | Pre-recorded WAV/MP3 file stored in Blob Storage | Professional voice recordings, multilingual audio |
| **SSML** | Speech Synthesis Markup Language for advanced control | Fine-tuned pacing, emphasis, pauses, pronunciation |

---

## 2. Prompt Data Model

Each prompt is stored as an `IvrPrompt` document in the `Prompts` Cosmos DB container:

```json
{
  "id": "prompt-main-welcome",
  "name": "Main Menu Welcome",
  "description": "Welcome message played when callers reach the main menu",
  "type": "Tts",
  "ttsText": "Thank you for calling. For billing, press 1. For technical support, press 2. For sales, press 3. To repeat this menu, press 9.",
  "ttsVoice": "en-US-JennyNeural",
  "ttsStyle": null,
  "audioFileUrl": null,
  "audioBlobName": null,
  "language": "en-US",
  "durationSeconds": null,
  "category": "Main Menu",
  "tags": ["welcome", "main-menu", "dtmf"],
  "isActive": true,
  "version": 1,
  "createdBy": "admin@company.com",
  "createdAt": "2026-01-15T10:00:00Z",
  "updatedAt": "2026-02-10T14:30:00Z",
  "partitionKey": "prompt"
}
```

### Field Reference

| Field | Type | Required | Description |
|---|---|---|---|
| `id` | string | Auto | Unique ID (auto-generated GUID, can be overridden) |
| `name` | string | Yes | Human-readable name for admin portal display |
| `description` | string | No | Purpose/context for the prompt |
| `type` | enum | Yes | `Tts`, `AudioFile`, or `Ssml` |
| `ttsText` | string | For TTS/SSML | The text to convert to speech (plain text or SSML markup) |
| `ttsVoice` | string | For TTS | Azure neural voice name (default: `en-US-JennyNeural`) |
| `ttsStyle` | string | No | Voice style modifier (e.g., `cheerful`, `empathetic`) |
| `audioFileUrl` | string | For AudioFile | Direct URL to an audio file (if not using Blob Storage) |
| `audioBlobName` | string | For AudioFile | Blob Storage file name (generates a SAS URL at runtime) |
| `language` | string | Yes | BCP-47 language tag (default: `en-US`) |
| `durationSeconds` | double | No | Estimated duration (for admin reference) |
| `category` | string | No | Grouping label (e.g., "Main Menu", "After Hours", "Errors") |
| `tags` | string[] | No | Searchable tags for filtering in the admin portal |
| `isActive` | bool | Yes | Whether the prompt is available for use |
| `version` | int | Yes | Version number (incremented on edit) |
| `createdBy` | string | No | Admin user who created the prompt |

---

## 3. Creating Prompts

### Via Admin Portal

1. Navigate to **Prompts** in the sidebar.
2. Click **New Prompt**.
3. Fill in the form:
   - **Name**: Descriptive name (e.g., "After Hours Greeting")
   - **Type**: Select TTS, Audio File, or SSML
   - **TTS Text**: Enter the text to speak (for TTS/SSML)
   - **Voice**: Select a neural voice
   - **Language**: Select the language
   - **Category**: Optional grouping
   - **Tags**: Optional searchable tags
4. Click **Save**.

### Via Cosmos DB (Direct)

Insert a document into the `Prompts` container:

```json
{
  "id": "prompt-after-hours",
  "name": "After Hours Message",
  "type": "Tts",
  "ttsText": "Our office is currently closed. Our business hours are Monday through Friday, 8 AM to 5 PM Eastern. Please call back during business hours or leave a message after the tone.",
  "ttsVoice": "en-US-JennyNeural",
  "language": "en-US",
  "category": "After Hours",
  "tags": ["after-hours", "closed"],
  "isActive": true,
  "version": 1,
  "partitionKey": "prompt"
}
```

---

## 4. Using Prompts in Menus

Prompts are referenced by ID in menu configurations:

### Menu Prompt (played when entering the menu)
```json
{
  "id": "menu-main",
  "name": "Main Menu",
  "promptId": "prompt-main-welcome",
  ...
}
```

### Speech Routing Prompt (played before speech capture)
```json
{
  "speechRoutingPromptId": "prompt-describe-issue",
  ...
}
```

### Timeout Prompt (played when caller doesn't respond)
```json
{
  "timeoutPromptId": "prompt-no-input-received",
  ...
}
```

### Invalid Input Prompt (played when DTMF doesn't match any option)
```json
{
  "invalidInputPromptId": "prompt-invalid-selection",
  ...
}
```

### Action Prompts (played during or after an action)
```json
{
  "action": {
    "type": "Hangup",
    "promptId": "prompt-goodbye"
  }
}
```

### Team Routing Confirmation Prompt
```json
{
  "teamName": "Billing",
  "confirmationPromptId": "prompt-billing-confirm",
  ...
}
```

### External System Prompts
```json
{
  "successPromptId": "prompt-submission-success",
  "failurePromptId": "prompt-submission-error",
  ...
}
```

---

## 5. TTS Voice Options

The system uses Azure Cognitive Services neural voices. The default voice is `en-US-JennyNeural`.

### Popular English Voices

| Voice | Gender | Style | Best For |
|---|---|---|---|
| `en-US-JennyNeural` | Female | Friendly, conversational | General IVR prompts |
| `en-US-GuyNeural` | Male | Professional | Business IVR |
| `en-US-AriaNeural` | Female | Expressive, multiple styles | Dynamic prompts with emotion |
| `en-US-DavisNeural` | Male | Calm, authoritative | Emergency/important messages |
| `en-US-SaraNeural` | Female | Youthful, casual | Customer service |

### Voice Styles (for supported voices like Aria/Jenny)

| Style | Use Case |
|---|---|
| `cheerful` | Welcome messages, success confirmations |
| `empathetic` | Error handling, apology messages |
| `calm` | Hold messages, after-hours greetings |
| `customerservice` | Standard IVR prompts |
| `newscast` | Announcements |

### Multilingual Support

| Language | Voice Example | Language Code |
|---|---|---|
| English (US) | `en-US-JennyNeural` | `en-US` |
| English (UK) | `en-GB-SoniaNeural` | `en-GB` |
| Spanish (US) | `es-US-PalomaNeural` | `es-US` |
| French (CA) | `fr-CA-SylvieNeural` | `fr-CA` |
| Mandarin | `zh-CN-XiaoxiaoNeural` | `zh-CN` |

Set the `language` and `ttsVoice` fields to match.

---

## 6. SSML Advanced Formatting

For fine-grained control over speech output, use SSML prompts:

### Basic SSML Structure

```xml
<speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis"
    xmlns:mstts="https://www.w3.org/2001/mstts"
    xml:lang="en-US">
  <voice name="en-US-JennyNeural">
    Thank you for calling. <break time="500ms"/>
    For billing, press <say-as interpret-as="cardinal">1</say-as>.
    <break time="300ms"/>
    For support, press <say-as interpret-as="cardinal">2</say-as>.
  </voice>
</speak>
```

### Common SSML Tags

| Tag | Purpose | Example |
|---|---|---|
| `<break>` | Insert a pause | `<break time="500ms"/>` |
| `<say-as>` | Control how text is spoken | `<say-as interpret-as="telephone">5551234567</say-as>` |
| `<emphasis>` | Add emphasis | `<emphasis level="strong">important</emphasis>` |
| `<prosody>` | Control rate/pitch/volume | `<prosody rate="slow">Please listen carefully</prosody>` |
| `<mstts:express-as>` | Apply voice style | `<mstts:express-as style="cheerful">Welcome!</mstts:express-as>` |
| `<sub>` | Substitution | `<sub alias="A C S">ACS</sub>` |

### `say-as` Interpret Types

| Type | Input | Spoken As |
|---|---|---|
| `telephone` | `5551234567` | "five five five, one two three, four five six seven" |
| `cardinal` | `42` | "forty two" |
| `ordinal` | `3` | "third" |
| `date` | `2026-02-17` | "February seventeenth, twenty twenty-six" |
| `time` | `14:30` | "two thirty PM" |
| `spell-out` | `ABC` | "A B C" |
| `characters` | `PIN` | "P I N" |

### SSML Prompt Example

```json
{
  "id": "prompt-reference-number",
  "name": "Reference Number Readback",
  "type": "Ssml",
  "ttsText": "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:mstts=\"https://www.w3.org/2001/mstts\" xml:lang=\"en-US\"><voice name=\"en-US-JennyNeural\"><mstts:express-as style=\"cheerful\">Your request has been submitted successfully.</mstts:express-as><break time=\"500ms\"/>Your reference number is <say-as interpret-as=\"spell-out\">FA1234</say-as>. <break time=\"300ms\"/>Is there anything else I can help you with?</voice></speak>",
  "language": "en-US",
  "isActive": true
}
```

The `PromptService.BuildSsml()` helper can generate SSML programmatically:

```csharp
var ssml = _promptService.BuildSsml(
    "Welcome to our system!",
    voice: "en-US-AriaNeural",
    style: "cheerful");
```

---

## 7. Audio File Prompts

### Uploading Audio Files

Audio files are uploaded to Azure Blob Storage and referenced by blob name:

1. In the Admin Portal, create a new prompt with type **Audio File**.
2. Upload a WAV or MP3 file.
3. The file is stored in the `ivr-prompts` blob container.
4. The `audioBlobName` is saved in the prompt document.

### Supported Formats

| Format | Extension | Notes |
|---|---|---|
| WAV | .wav | Preferred — no transcoding needed |
| MP3 | .mp3 | Supported by ACS |
| OGG | .ogg | Supported by ACS |

### Audio File Prompt Example

```json
{
  "id": "prompt-hold-music",
  "name": "Hold Music",
  "type": "AudioFile",
  "audioBlobName": "hold-music-jazz.wav",
  "language": "en-US",
  "durationSeconds": 30.0,
  "category": "Hold",
  "isActive": true
}
```

### URL Generation

At runtime, the `PromptService` generates a time-limited SAS URL for the audio file:

```csharp
// BlobStorageService generates a read-only SAS URL valid for 1 hour
var url = await _blobStorage.GetAudioUrlAsync("hold-music-jazz.wav");
// Returns: https://ivrdevstor.blob.core.windows.net/ivr-prompts/hold-music-jazz.wav?sv=...&sig=...
```

---

## 8. Prompt Resolution at Runtime

When the IVR engine needs to play a prompt, the `PromptService.ResolvePromptAsync()` method:

1. **Loads** the `IvrPrompt` document from Cosmos DB by ID.
2. **Resolves** the playback content based on prompt type:
   - **TTS**: Returns the text and voice name → `TextSource`
   - **Audio File**: Generates a SAS URL from blob storage → `FileSource`
   - **SSML**: Returns the SSML content → `SsmlSource`
3. **Falls back** to a default TTS message if the prompt is not found:
   > "We're sorry, an error has occurred. Please try again later."

The resolved content is then passed to the ACS Call Automation SDK:

```csharp
// The CallbackHandler converts PromptContent to an ACS PlaySource:
PlaySource playSource = prompt.Type switch
{
    PromptType.Tts => new TextSource(prompt.Text) { VoiceName = prompt.Voice },
    PromptType.AudioFile => new FileSource(new Uri(prompt.AudioUrl)),
    PromptType.Ssml => new SsmlSource(prompt.SsmlContent),
};

await callConnection.GetCallMedia().PlayToAllAsync(playSource);
```

---

## 9. Best Practices

### Naming Convention
Use a consistent naming pattern for prompt IDs:
- `prompt-{menu}-{purpose}` (e.g., `prompt-main-welcome`, `prompt-billing-transfer`)
- `prompt-error-{type}` (e.g., `prompt-error-timeout`, `prompt-error-invalid`)
- `prompt-system-{purpose}` (e.g., `prompt-system-hold`, `prompt-system-goodbye`)

### Content Guidelines
- Keep prompts **under 30 seconds** to avoid caller frustration.
- Present DTMF options in **ascending order** (press 1 for X, press 2 for Y).
- Include a **repeat option** (typically `9` or `*`) in every menu.
- Provide a **transfer to agent** option as an escape hatch.
- Use natural pacing — add `<break>` tags between options.

### Organization
- Use **categories** to group related prompts (Main Menu, After Hours, Errors, Hold).
- Use **tags** for cross-cutting concerns (e.g., `spanish`, `emergency`, `seasonal`).
- **Version** prompts when making changes to maintain history.
- **Deactivate** rather than delete old prompts to preserve call log references.

### Testing
- Test TTS prompts by playing them in the admin portal preview.
- Verify audio file prompts are accessible (SAS URL generation works).
- Confirm SSML prompts parse correctly — malformed SSML will cause playback failures.
- Test prompts with different phone connections (PSTN audio quality differs from VoIP).

### Dynamic TTS Templates

For prompts that include dynamic data (e.g., confirmation messages), use `{{placeholder}}` syntax in `DataExtractionConfig.SuccessTtsTemplate` or `ExternalSystemEndpoint.ConfirmationMessageTemplate`:

```
"Done. Fire alarms at {{location}} have been placed in {{action}} mode. Your reference number is {{confirmationValue}}."
```

These templates are processed by `ExternalSystemIntegrationService.BuildConfirmationMessage()` at runtime and played as TTS `TextSource` prompts.
