using MelonLoader;
using System.IO;

namespace WorldCleanup {
    internal static class Settings {
        private const string Category = "WorldCleanup";

        private const string DisableLights = "DisableLights";
        private const string DisablePostProcessing = "DisablePostProcessing";
        private const string DisableMirrors = "DisableMirrors";
        private const string EnableAudioOverride = "EnableAudioOverride";

        private const string VoiceGain = "VoiceGain";
        private const string VoiceFar = "VoiceFar";
        private const string VoiceNear = "VoiceNear";
        private const string VoiceVolRadius = "VoiceVolRadius";
        private const string VoiceLowpass = "VoiceLowpass";

        private const string UpdateInterval = "UpdateInterval";

        public static void OnPreferencesLoaded() {
            MelonPreferences.CreateCategory(Category);

            MelonPreferences.CreateEntry(Category, DisableLights, false);
            MelonPreferences.CreateEntry(Category, DisablePostProcessing, false);
            MelonPreferences.CreateEntry(Category, DisableMirrors, false);
            MelonPreferences.CreateEntry(Category, EnableAudioOverride, false);

            MelonPreferences.CreateEntry(Category, VoiceGain, 15.0f);
            MelonPreferences.CreateEntry(Category, VoiceFar, 25.0f);
            MelonPreferences.CreateEntry(Category, VoiceNear, 0.0f);
            MelonPreferences.CreateEntry(Category, VoiceVolRadius, 0.0f);
            MelonPreferences.CreateEntry(Category, VoiceLowpass, true);

            MelonPreferences.CreateEntry(Category, UpdateInterval, 1f);
        }

        public static void OnPreferencesSaved() {
            /* ... */
        }

        public static bool s_DisableLights {
            get => MelonPreferences.GetEntryValue<bool>(Category, DisableLights);
            set => MelonPreferences.SetEntryValue(Category, DisableLights, value);
        }

        public static bool s_DisablePostProcessing {
            get => MelonPreferences.GetEntryValue<bool>(Category, DisablePostProcessing);
            set => MelonPreferences.SetEntryValue(Category, DisablePostProcessing, value);
        }

        public static bool s_DisableMirrors {
            get => MelonPreferences.GetEntryValue<bool>(Category, DisableMirrors);
            set => MelonPreferences.SetEntryValue(Category, DisableMirrors, value);
        }

        public static bool s_EnableAudioOverride {
            get => MelonPreferences.GetEntryValue<bool>(Category, EnableAudioOverride);
            set => MelonPreferences.SetEntryValue(Category, EnableAudioOverride, value);
        }

        public static float s_VoiceGain {
            get => MelonPreferences.GetEntryValue<float>(Category, VoiceGain);
            set => MelonPreferences.SetEntryValue(Category, VoiceGain, value);
        }

        public static float s_VoiceFar {
            get => MelonPreferences.GetEntryValue<float>(Category, VoiceFar);
            set => MelonPreferences.SetEntryValue(Category, VoiceFar, value);
        }

        public static float s_VoiceNear {
            get => MelonPreferences.GetEntryValue<float>(Category, VoiceNear);
            set => MelonPreferences.SetEntryValue(Category, VoiceNear, value);
        }

        public static float s_VoiceVolRadius {
            get => MelonPreferences.GetEntryValue<float>(Category, VoiceVolRadius);
            set => MelonPreferences.SetEntryValue(Category, VoiceVolRadius, value);
        }

        public static bool s_VoiceLowpass {
            get => MelonPreferences.GetEntryValue<bool>(Category, VoiceLowpass);
            set => MelonPreferences.SetEntryValue(Category, VoiceLowpass, value);
        }

        public static float s_UpdateInterval {
            get => MelonPreferences.GetEntryValue<float>(Category, UpdateInterval);
            set => MelonPreferences.SetEntryValue(Category, UpdateInterval, value);
        }

        public static void StoreConfigFile(string file_name, string data) {
            var file_path = Path.Combine(MelonUtils.UserDataDirectory, file_name);
            File.WriteAllText(file_path, data);
        }

        public static string LoadConfigFile(string file_name) {
            var file_path = Path.Combine(MelonUtils.UserDataDirectory, file_name);
            return File.ReadAllText(file_path);
        }
    }
}
