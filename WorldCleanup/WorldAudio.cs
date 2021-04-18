using VRC.SDKBase;
using UIExpansionKit.API;
using System;
using MelonLoader;

namespace WorldCleanup {
    class WorldAudio {

        private struct AudioConfig {
            public float voice_gain;
            public float voice_distance_far;
            public float voice_distance_near;
            public float voice_volumetric_radius;
            public bool voice_lowpass;
        };

        private enum Preset { Custom, Quiet };

        private static readonly AudioConfig[] PresetAudioConfigs = new AudioConfig[] {
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

        public static void Initialize() {
            s_Enabled = Settings.s_EnableAudioOverride;

            s_AudioConfig = new AudioConfig {
                voice_gain = Settings.s_VoiceGain,
                voice_distance_far = Settings.s_VoiceFar,
                voice_distance_near = Settings.s_VoiceNear,
                voice_volumetric_radius = Settings.s_VoiceVolRadius,
                voice_lowpass = Settings.s_VoiceLowpass,
            };
        }

        public static void OnPreferencesSaved() {
            Settings.s_EnableAudioOverride = s_Enabled;
            Settings.s_VoiceGain = s_AudioConfig.voice_gain;
            Settings.s_VoiceFar = s_AudioConfig.voice_distance_far;
            Settings.s_VoiceNear = s_AudioConfig.voice_distance_near;
            Settings.s_VoiceVolRadius = s_AudioConfig.voice_volumetric_radius;
            Settings.s_VoiceLowpass = s_AudioConfig.voice_lowpass;
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

        private static void ApplySettingsToAll() {
            VRCPlayerApi.AllPlayers.ForEach((Action<VRCPlayerApi>)ApplySettings);
        }

        public static void RegisterSettings(ICustomShowableLayoutedMenu parent, Action on_exit) {
            UiExpansion.AddButtonToggleListItem(parent, "World Sound", "Settings", () => {
                var sound_menu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);

                UiExpansion.AddDropdownListItem(sound_menu, "Preset", typeof(Preset), (value) => {
                    if ((Preset)value == Preset.Custom)
                        return;
                    s_AudioConfig = PresetAudioConfigs[value - 1];
                    s_Enabled = true;
                    ApplySettingsToAll();
                    sound_menu.Hide();
                    on_exit();
                }, (int)Preset.Custom);

                sound_menu.AddLabel("\n\n Player voice");
                UiExpansion.AddFloatListItem(sound_menu, "Gain", (val) => { s_AudioConfig.voice_gain = val; }, s_AudioConfig.voice_gain, 0, 24);
                UiExpansion.AddFloatDiffListItem(sound_menu, "Far", (val) => { s_AudioConfig.voice_distance_far = val; }, s_AudioConfig.voice_distance_far);
                UiExpansion.AddFloatDiffListItem(sound_menu, "Near", (val) => { s_AudioConfig.voice_distance_near = val; }, s_AudioConfig.voice_distance_near);
                UiExpansion.AddFloatDiffListItem(sound_menu, "Volumetric Radius", (val) => { s_AudioConfig.voice_volumetric_radius = val; }, s_AudioConfig.voice_volumetric_radius);
                UiExpansion.AddToggleListItem(sound_menu, "Lowpass", (val) => { s_AudioConfig.voice_lowpass = val; }, s_AudioConfig.voice_lowpass);

                sound_menu.AddSimpleButton("Apply to all", ApplySettingsToAll);

                sound_menu.AddSimpleButton("Back", () => { sound_menu.Hide(); on_exit(); });
                sound_menu.Show();
            }, (enable) => {
                s_Enabled = enable;
            }, s_Enabled);
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
