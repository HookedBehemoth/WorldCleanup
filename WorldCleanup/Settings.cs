/*
 * Copyright (c) 2021 HookedBehemoth
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms and conditions of the GNU General Public License,
 * version 3, as published by the Free Software Foundation.
 *
 * This program is distributed in the hope it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for
 * more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

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

        public static void RegisterConfig() {
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

        public static void LoadConfig() {
            s_DisableLights = MelonPreferences.GetEntryValue<bool>(Category, DisableLights);
            s_DisablePostProcessing = MelonPreferences.GetEntryValue<bool>(Category, DisablePostProcessing);
            s_DisableMirrors = MelonPreferences.GetEntryValue<bool>(Category, DisableMirrors);
            s_EnableAudioOverride = MelonPreferences.GetEntryValue<bool>(Category, EnableAudioOverride);
            s_VoiceGain = MelonPreferences.GetEntryValue<float>(Category, VoiceGain);
            s_VoiceFar = MelonPreferences.GetEntryValue<float>(Category, VoiceFar);
            s_VoiceNear = MelonPreferences.GetEntryValue<float>(Category, VoiceNear);
            s_VoiceVolRadius = MelonPreferences.GetEntryValue<float>(Category, VoiceVolRadius);
            s_VoiceLowpass = MelonPreferences.GetEntryValue<bool>(Category, VoiceLowpass);
            s_UpdateInterval = MelonPreferences.GetEntryValue<float>(Category, UpdateInterval);
        }

        public static void FlushConfig() {
            MelonPreferences.SetEntryValue(Category, DisableLights, s_DisableLights);
            MelonPreferences.SetEntryValue(Category, DisablePostProcessing, s_DisablePostProcessing);
            MelonPreferences.SetEntryValue(Category, DisableMirrors, s_DisableMirrors);
            MelonPreferences.SetEntryValue(Category, EnableAudioOverride, s_EnableAudioOverride);
            MelonPreferences.SetEntryValue(Category, VoiceGain, s_VoiceGain);
            MelonPreferences.SetEntryValue(Category, VoiceFar, s_VoiceFar);
            MelonPreferences.SetEntryValue(Category, VoiceNear, s_VoiceNear);
            MelonPreferences.SetEntryValue(Category, VoiceVolRadius, s_VoiceVolRadius);
            MelonPreferences.SetEntryValue(Category, VoiceLowpass, s_VoiceLowpass);
            MelonPreferences.SetEntryValue(Category, UpdateInterval, s_UpdateInterval);
        }

        public static bool s_DisableLights;
        public static bool s_DisablePostProcessing;
        public static bool s_DisableMirrors;
        public static bool s_EnableAudioOverride;
        public static float s_VoiceGain;
        public static float s_VoiceFar;
        public static float s_VoiceNear;
        public static float s_VoiceVolRadius;
        public static bool s_VoiceLowpass;
        public static float s_UpdateInterval;

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
