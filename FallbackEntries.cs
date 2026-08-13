using System.Collections.Generic;

namespace SettingsAll;

public static class FallbackEntries
{
    public static readonly IReadOnlyList<SettingsEntry> Entries = new List<SettingsEntry>
    {
        new() { Uri = "ms-settings:display", FriendlyName = "Display", Category = "System", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:sound", FriendlyName = "Sound", Category = "System", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:notifications", FriendlyName = "Notifications", Category = "System", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:power", FriendlyName = "Power & Battery", Category = "System", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:storage", FriendlyName = "Storage", Category = "System", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:multitasking", FriendlyName = "Multitasking", Category = "System", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:about", FriendlyName = "About", Category = "System", IsFromSeedDictionary = true },
        
        new() { Uri = "ms-settings:bluetooth", FriendlyName = "Bluetooth", Category = "Bluetooth & Devices", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:connecteddevices", FriendlyName = "Devices", Category = "Bluetooth & Devices", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:printers", FriendlyName = "Printers & Scanners", Category = "Bluetooth & Devices", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:mousetouchpad", FriendlyName = "Mouse & Touchpad", Category = "Bluetooth & Devices", IsFromSeedDictionary = true },
        
        new() { Uri = "ms-settings:network-wifi", FriendlyName = "Wi-Fi", Category = "Network & Internet", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:network-ethernet", FriendlyName = "Ethernet", Category = "Network & Internet", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:network-mobilehotspot", FriendlyName = "Mobile Hotspot", Category = "Network & Internet", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:network-vpn", FriendlyName = "VPN", Category = "Network & Internet", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:network-proxy", FriendlyName = "Proxy", Category = "Network & Internet", IsFromSeedDictionary = true },
        
        new() { Uri = "ms-settings:personalization-background", FriendlyName = "Background", Category = "Personalization", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:colors", FriendlyName = "Colors", Category = "Personalization", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:lockscreen", FriendlyName = "Lock Screen", Category = "Personalization", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:themes", FriendlyName = "Themes", Category = "Personalization", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:fonts", FriendlyName = "Fonts", Category = "Personalization", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:taskbar", FriendlyName = "Taskbar", Category = "Personalization", IsFromSeedDictionary = true },
        
        new() { Uri = "ms-settings:appsfeatures", FriendlyName = "Installed Apps", Category = "Apps", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:defaultapps", FriendlyName = "Default Apps", Category = "Apps", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:optionalfeatures", FriendlyName = "Optional Features", Category = "Apps", IsFromSeedDictionary = true },
        
        new() { Uri = "ms-settings:yourinfo", FriendlyName = "Your Info", Category = "Accounts", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:emailandaccounts", FriendlyName = "Email & Accounts", Category = "Accounts", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:signinoptions", FriendlyName = "Sign-in Options", Category = "Accounts", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:otherusers", FriendlyName = "Other Users", Category = "Accounts", IsFromSeedDictionary = true },
        
        new() { Uri = "ms-settings:dateandtime", FriendlyName = "Date & Time", Category = "Time & Language", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:regionlanguage", FriendlyName = "Language & Region", Category = "Time & Language", IsFromSeedDictionary = true },
        
        new() { Uri = "ms-settings:gaming-gamebar", FriendlyName = "Game Bar", Category = "Gaming", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:gaming-gamemode", FriendlyName = "Game Mode", Category = "Gaming", IsFromSeedDictionary = true },
        
        new() { Uri = "ms-settings:privacy-location", FriendlyName = "Location", Category = "Privacy & Security", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:privacy-camera", FriendlyName = "Camera", Category = "Privacy & Security", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:privacy-microphone", FriendlyName = "Microphone", Category = "Privacy & Security", IsFromSeedDictionary = true },
        
        new() { Uri = "ms-settings:windowsupdate", FriendlyName = "Windows Update", Category = "Windows Update", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:windowsdefender", FriendlyName = "Windows Security", Category = "Windows Update", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:backup", FriendlyName = "Backup", Category = "Windows Update", IsFromSeedDictionary = true },
        new() { Uri = "ms-settings:recovery", FriendlyName = "Recovery", Category = "Windows Update", IsFromSeedDictionary = true }
    };
}
