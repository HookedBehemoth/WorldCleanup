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
            var category = MelonPreferences.CreateCategory(Category);

            s_DisableLights = category.CreateEntry(DisableLights, false);
            s_DisablePostProcessing = category.CreateEntry(DisablePostProcessing, false);
            s_DisableMirrors = category.CreateEntry(DisableMirrors, false);
            s_EnableAudioOverride = category.CreateEntry(EnableAudioOverride, false);

            s_VoiceGain = category.CreateEntry(VoiceGain, 15.0f);
            s_VoiceFar = category.CreateEntry(VoiceFar, 25.0f);
            s_VoiceNear = category.CreateEntry(VoiceNear, 0.0f);
            s_VoiceVolRadius = category.CreateEntry(VoiceVolRadius, 0.0f);
            s_VoiceLowpass = category.CreateEntry(VoiceLowpass, true);

            s_UpdateInterval = category.CreateEntry(UpdateInterval, 1f);
        }

        public static MelonPreferences_Entry<bool> s_DisableLights;
        public static MelonPreferences_Entry<bool> s_DisablePostProcessing;
        public static MelonPreferences_Entry<bool> s_DisableMirrors;
        public static MelonPreferences_Entry<bool> s_EnableAudioOverride;
        public static MelonPreferences_Entry<float> s_VoiceGain;
        public static MelonPreferences_Entry<float> s_VoiceFar;
        public static MelonPreferences_Entry<float> s_VoiceNear;
        public static MelonPreferences_Entry<float> s_VoiceVolRadius;
        public static MelonPreferences_Entry<bool> s_VoiceLowpass;
        public static MelonPreferences_Entry<float> s_UpdateInterval;

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
