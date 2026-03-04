using IVR.Core.Models;

namespace IVR.DataSeeder;

/// <summary>
/// Generates test prompts for E911 IVR system.
/// </summary>
public static class PromptSeeder
{
    public static List<IvrPrompt> GenerateTestPrompts()
    {
        var prompts = new List<IvrPrompt>();

        // ===== WELCOME / GREETING PROMPTS =====
        prompts.Add(new IvrPrompt
        {
            Id = "welcome-main",
            Name = "Main Welcome Message",
            Description = "Primary greeting when caller first connects",
            Type = PromptType.Tts,
            TtsText = "Thank you for calling Emergency Services. How may we help you today?",
            TtsVoice = "en-US-JennyNeural",
            Category = "Welcome",
            Tags = new List<string> { "greeting", "main", "entry" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "welcome-business-hours",
            Name = "Business Hours Welcome",
            Description = "Greeting during normal business hours",
            Type = PromptType.Tts,
            TtsText = "Thank you for calling Emergency Services. Our office is open Monday through Friday, 8 AM to 5 PM. If this is an emergency, please stay on the line.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Welcome",
            Tags = new List<string> { "greeting", "business-hours" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "welcome-after-hours",
            Name = "After Hours Welcome",
            Description = "Greeting outside business hours",
            Type = PromptType.Tts,
            TtsText = "Thank you for calling Emergency Services. You have reached us outside of normal business hours. For emergencies, please stay on the line. For non-emergency matters, please call back during business hours.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Welcome",
            Tags = new List<string> { "greeting", "after-hours" },
            CreatedBy = "System Seeder"
        });

        // ===== MAIN MENU PROMPTS =====
        prompts.Add(new IvrPrompt
        {
            Id = "menu-main-dtmf",
            Name = "Main Menu - DTMF Options",
            Description = "Main menu with keypad options",
            Type = PromptType.Tts,
            TtsText = "For fire emergency, press 1. For police emergency, press 2. For medical emergency, press 3. For non-emergency services, press 4. To repeat this menu, press star.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Menu",
            Tags = new List<string> { "menu", "main", "dtmf" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "menu-main-speech",
            Name = "Main Menu - Speech Recognition",
            Description = "Main menu prompting for voice input",
            Type = PromptType.Tts,
            TtsText = "Please tell us the nature of your emergency. You can say fire, police, medical, or non-emergency.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Menu",
            Tags = new List<string> { "menu", "main", "speech", "ai" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "menu-fire-services",
            Name = "Fire Services Submenu",
            Description = "Submenu for fire-related services",
            Type = PromptType.Tts,
            TtsText = "Fire Services. Press 1 for active fire emergency. Press 2 for smoke or gas leak. Press 3 for fire safety inspection. Press 9 to return to the main menu.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Menu",
            Tags = new List<string> { "menu", "fire", "submenu" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "menu-police-services",
            Name = "Police Services Submenu",
            Description = "Submenu for police-related services",
            Type = PromptType.Tts,
            TtsText = "Police Services. Press 1 for active emergency. Press 2 to report a crime. Press 3 for traffic incident. Press 9 to return to the main menu.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Menu",
            Tags = new List<string> { "menu", "police", "submenu" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "menu-medical-services",
            Name = "Medical Services Submenu",
            Description = "Submenu for medical emergencies",
            Type = PromptType.Tts,
            TtsText = "Medical Emergency Services. Press 1 for life-threatening emergency. Press 2 for urgent medical assistance. Press 3 for poison control. Press 9 to return to the main menu.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Menu",
            Tags = new List<string> { "menu", "medical", "submenu" },
            CreatedBy = "System Seeder"
        });

        // ===== CONFIRMATION PROMPTS (for AI routing) =====
        prompts.Add(new IvrPrompt
        {
            Id = "confirm-fire-dept",
            Name = "Confirm Fire Department",
            Description = "Confirmation for fire department routing",
            Type = PromptType.Tts,
            TtsText = "I understood you need the Fire Department. Is that correct? Say yes or no.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Confirmation",
            Tags = new List<string> { "confirmation", "fire", "ai-routing" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "confirm-police-dept",
            Name = "Confirm Police Department",
            Description = "Confirmation for police department routing",
            Type = PromptType.Tts,
            TtsText = "I understood you need the Police Department. Is that correct? Say yes or no.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Confirmation",
            Tags = new List<string> { "confirmation", "police", "ai-routing" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "confirm-medical-ems",
            Name = "Confirm Medical/EMS",
            Description = "Confirmation for medical emergency routing",
            Type = PromptType.Tts,
            TtsText = "I understood you need Medical Emergency Services. Is that correct? Say yes or no.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Confirmation",
            Tags = new List<string> { "confirmation", "medical", "ai-routing" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "confirm-non-emergency",
            Name = "Confirm Non-Emergency",
            Description = "Confirmation for non-emergency routing",
            Type = PromptType.Tts,
            TtsText = "I understood you need non-emergency services. Is that correct? Say yes or no.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Confirmation",
            Tags = new List<string> { "confirmation", "non-emergency", "ai-routing" },
            CreatedBy = "System Seeder"
        });

        // ===== TRANSFER PROMPTS =====
        prompts.Add(new IvrPrompt
        {
            Id = "transfer-fire",
            Name = "Transferring to Fire Department",
            Description = "Message played when transferring to fire services",
            Type = PromptType.Tts,
            TtsText = "Please stay on the line. Transferring you to the Fire Department now.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Transfer",
            Tags = new List<string> { "transfer", "fire" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "transfer-police",
            Name = "Transferring to Police Department",
            Description = "Message played when transferring to police services",
            Type = PromptType.Tts,
            TtsText = "Please stay on the line. Transferring you to the Police Department now.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Transfer",
            Tags = new List<string> { "transfer", "police" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "transfer-medical",
            Name = "Transferring to Medical/EMS",
            Description = "Message played when transferring to medical services",
            Type = PromptType.Tts,
            TtsText = "Please stay on the line. Transferring you to Emergency Medical Services now.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Transfer",
            Tags = new List<string> { "transfer", "medical" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "transfer-nonemergency",
            Name = "Transferring to Non-Emergency",
            Description = "Message played when transferring to non-emergency line",
            Type = PromptType.Tts,
            TtsText = "Transferring you to our non-emergency services line. Please hold.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Transfer",
            Tags = new List<string> { "transfer", "non-emergency" },
            CreatedBy = "System Seeder"
        });

        // ===== ERROR / FALLBACK PROMPTS =====
        prompts.Add(new IvrPrompt
        {
            Id = "error-invalid-input",
            Name = "Invalid Input Error",
            Description = "Message when invalid input is received",
            Type = PromptType.Tts,
            TtsText = "I'm sorry, I didn't understand that. Please try again.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Error",
            Tags = new List<string> { "error", "invalid-input" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "error-no-input",
            Name = "No Input Error",
            Description = "Message when no input is detected",
            Type = PromptType.Tts,
            TtsText = "I didn't hear anything. Please tell us how we can help you.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Error",
            Tags = new List<string> { "error", "no-input", "timeout" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "error-max-retries",
            Name = "Maximum Retries Reached",
            Description = "Message after too many failed attempts",
            Type = PromptType.Tts,
            TtsText = "I'm having trouble understanding you. Let me transfer you to an operator who can help.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Error",
            Tags = new List<string> { "error", "max-retries", "escalation" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "error-system-error",
            Name = "System Error",
            Description = "Generic system error message",
            Type = PromptType.Tts,
            TtsText = "We're experiencing technical difficulties. Please hold while we connect you to an operator.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Error",
            Tags = new List<string> { "error", "system", "technical" },
            CreatedBy = "System Seeder"
        });

        // ===== CONFIRMATION RESPONSES =====
        prompts.Add(new IvrPrompt
        {
            Id = "confirm-yes-response",
            Name = "Confirmation Yes Response",
            Description = "Response when user confirms routing",
            Type = PromptType.Tts,
            TtsText = "Great, connecting you now.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Confirmation",
            Tags = new List<string> { "confirmation", "yes", "positive" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "confirm-no-response",
            Name = "Confirmation No Response",
            Description = "Response when user rejects routing suggestion",
            Type = PromptType.Tts,
            TtsText = "I apologize for the confusion. Let me try again. Please tell me how we can help you.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Confirmation",
            Tags = new List<string> { "confirmation", "no", "negative", "retry" },
            CreatedBy = "System Seeder"
        });

        // ===== HOLD / WAIT MESSAGES =====
        prompts.Add(new IvrPrompt
        {
            Id = "hold-music",
            Name = "Hold Music Message",
            Description = "Message before playing hold music",
            Type = PromptType.Tts,
            TtsText = "Please hold while we connect your call. Your call is important to us.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Hold",
            Tags = new List<string> { "hold", "wait" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "hold-position",
            Name = "Hold Position Update",
            Description = "Update caller on queue position",
            Type = PromptType.Tts,
            TtsText = "All operators are currently assisting other callers. You are number one in the queue. Please continue to hold.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Hold",
            Tags = new List<string> { "hold", "queue", "position" },
            CreatedBy = "System Seeder"
        });

        // ===== CALLBACK PROMPTS =====
        prompts.Add(new IvrPrompt
        {
            Id = "callback-offer",
            Name = "Callback Offer",
            Description = "Offer callback option to caller",
            Type = PromptType.Tts,
            TtsText = "Would you like us to call you back when an operator becomes available? Say yes or no.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Callback",
            Tags = new List<string> { "callback", "queue", "option" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "callback-confirmed",
            Name = "Callback Confirmed",
            Description = "Confirmation that callback is scheduled",
            Type = PromptType.Tts,
            TtsText = "Thank you. We will call you back at the number you're calling from as soon as an operator is available. Goodbye.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Callback",
            Tags = new List<string> { "callback", "confirmation" },
            CreatedBy = "System Seeder"
        });

        // ===== CLOSING / GOODBYE PROMPTS =====
        prompts.Add(new IvrPrompt
        {
            Id = "goodbye-standard",
            Name = "Standard Goodbye",
            Description = "Standard closing message",
            Type = PromptType.Tts,
            TtsText = "Thank you for calling Emergency Services. Goodbye.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Closing",
            Tags = new List<string> { "goodbye", "closing" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "goodbye-callback",
            Name = "Goodbye with Callback",
            Description = "Closing message mentioning callback",
            Type = PromptType.Tts,
            TtsText = "We will call you back shortly. Thank you and goodbye.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Closing",
            Tags = new List<string> { "goodbye", "callback" },
            CreatedBy = "System Seeder"
        });

        // ===== SPECIAL SCENARIOS =====
        prompts.Add(new IvrPrompt
        {
            Id = "emergency-priority",
            Name = "Emergency Priority Routing",
            Description = "Immediate routing for critical emergencies",
            Type = PromptType.Tts,
            TtsText = "This is a life-threatening emergency. Connecting you to an operator immediately. Stay on the line.",
            TtsVoice = "en-US-JennyNeural",
            TtsStyle = "urgent",
            Category = "Emergency",
            Tags = new List<string> { "emergency", "priority", "critical" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "caller-location",
            Name = "Caller Location Prompt",
            Description = "Request caller location information",
            Type = PromptType.Tts,
            TtsText = "Please state your location, including street address and city.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Information",
            Tags = new List<string> { "location", "address", "information" },
            CreatedBy = "System Seeder"
        });

        prompts.Add(new IvrPrompt
        {
            Id = "caller-name",
            Name = "Caller Name Prompt",
            Description = "Request caller name",
            Type = PromptType.Tts,
            TtsText = "Please state your name.",
            TtsVoice = "en-US-JennyNeural",
            Category = "Information",
            Tags = new List<string> { "name", "information" },
            CreatedBy = "System Seeder"
        });

        return prompts;
    }
}
