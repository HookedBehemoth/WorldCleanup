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

using VRC.SDKBase;
using UIExpansionKit.API;
using System;
using MelonLoader;

namespace WorldCleanup {
    internal static class WorldAudio {

        private struct AudioConfig {
            public float voice_gain;
            public float voice_distance_far;
            public float voice_distance_near;
            public float voice_volumetric_radius;
            public bool voice_lowpass;
        };

        private enum Preset { Custom, Default, Quiet };

        private static readonly AudioConfig[] PresetAudioConfigs = new AudioConfig[] {
            new AudioConfig {
                voice_gain = 15,
                voice_distance_far = 25,
                voice_distance_near = 0,
                voice_volumetric_radius = 0,
                voice_lowpass = true,
            },
            new AudioConfig {
                voice_gain = 15,
                voice_distance_far = 10,
                voice_distance_near = 0,
                voice_volumetric_radius = 0,
                voice_lowpass = true,
            }
        };

        static bool s_Enabled;
        static AudioConfig s_AudioConfig;

        public static void LoadConfig() {
            s_Enabled = Settings.s_EnableAudioOverride.Value;

            s_AudioConfig = new AudioConfig {
                voice_gain = Settings.s_VoiceGain.Value,
                voice_distance_far = Settings.s_VoiceFar.Value,
                voice_distance_near = Settings.s_VoiceNear.Value,
                voice_volumetric_radius = Settings.s_VoiceVolRadius.Value,
                voice_lowpass = Settings.s_VoiceLowpass.Value,
            };
        }

        public static void FlushConfig() {
            Settings.s_EnableAudioOverride.Value = s_Enabled;
            Settings.s_VoiceGain.Value = s_AudioConfig.voice_gain;
            Settings.s_VoiceFar.Value = s_AudioConfig.voice_distance_far;
            Settings.s_VoiceNear.Value = s_AudioConfig.voice_distance_near;
            Settings.s_VoiceVolRadius.Value = s_AudioConfig.voice_volumetric_radius;
            Settings.s_VoiceLowpass.Value = s_AudioConfig.voice_lowpass;
        }

        public static void ApplySettings(VRCPlayerApi player) {
            if (!s_Enabled || player.isLocal)
                return;

            MelonLogger.Msg(ConsoleColor.Green, $"Applying sound settings to {player.displayName}");

            player.SetVoiceGain(s_AudioConfig.voice_gain);
            player.SetVoiceDistanceFar(s_AudioConfig.voice_distance_far);
            player.SetVoiceDistanceNear(s_AudioConfig.voice_distance_near);
            player.SetVoiceVolumetricRadius(s_AudioConfig.voice_volumetric_radius);
            player.SetVoiceLowpass(s_AudioConfig.voice_lowpass);
        }

        public static void ApplySettingsToAll() {
            VRCPlayerApi.AllPlayers?.ForEach((Action<VRCPlayerApi>)ApplySettings);
        }

        public static void RegisterSettings(ICustomShowableLayoutedMenu parent, Action on_exit) {
            parent.AddButtonToggleListItem("World Sound", "Settings", () => {
                var sound_menu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);

                sound_menu.AddHeader("World Audio Settings");
                sound_menu.AddDropdownListItem("Preset", typeof(Preset), (value) => {
                    if ((Preset)value == Preset.Custom)
                        return;
                    s_AudioConfig = PresetAudioConfigs[value - 1];
                    s_Enabled = true;
                    ApplySettingsToAll();
                    sound_menu.Hide();
                    on_exit();
                }, (int)Preset.Custom);

                sound_menu.AddCategoryHeader("Player voice");
                sound_menu.AddSliderListItem("Gain", (val) => { s_AudioConfig.voice_gain = val; }, () => s_AudioConfig.voice_gain, 0, 24);
                sound_menu.AddFloatDiffListItem("Far", (val) => { s_AudioConfig.voice_distance_far = val; }, () => s_AudioConfig.voice_distance_far);
                sound_menu.AddFloatDiffListItem("Near", (val) => { s_AudioConfig.voice_distance_near = val; }, () => s_AudioConfig.voice_distance_near);
                sound_menu.AddFloatDiffListItem("Volumetric Radius", (val) => { s_AudioConfig.voice_volumetric_radius = val; }, () => s_AudioConfig.voice_volumetric_radius);
                sound_menu.AddToggleListItem("Lowpass", (val) => { s_AudioConfig.voice_lowpass = val; }, () => s_AudioConfig.voice_lowpass, false);

                sound_menu.AddSimpleButton("Apply to all", ApplySettingsToAll);

                sound_menu.AddSimpleButton("Back", () => { sound_menu.Hide(); on_exit(); });
                sound_menu.Show();
            }, (enable) => {
                s_Enabled = enable;
            }, () => s_Enabled, false);
        }

        public static void OnPlayerJoined(VRCPlayerApi player) {
            /* Apply Sound modifiers */
            ApplySettings(player);
        }

        public static void OnPlayerLeft(VRCPlayerApi player) {
            /* Future Cleanup? */
        }
    }
}
