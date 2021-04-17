using VRC.SDKBase;
using UIExpansionKit.API;
using System;
using MelonLoader;

namespace WorldCleanup {
    class WorldAudio {

        struct Settings {
            public bool enabled;

            public float voice_gain;
            public float voice_distance_far;
            public float voice_distance_near;
            public float voice_volumetric_radius;
            public bool voice_lowpass;

            public float avatar_gain;
            public float avatar_far_radius;
            public float avatar_near_radius;
            public float avatar_volumetric_radius;
            public bool avatar_force_spatial;
            public bool avatar_custom_curve;
        };

        static Settings s_Settings;

        public static void Initialize() {
            s_Settings = new Settings {
                enabled = false,

                voice_gain = 15,
                voice_distance_far = 25,
                voice_distance_near = 0,
                voice_volumetric_radius = 0,
                voice_lowpass = false,

                avatar_gain = 10,
                avatar_far_radius = 10,
                avatar_near_radius = 10,
                avatar_volumetric_radius = 10,
                avatar_force_spatial = false,
                avatar_custom_curve = true,
            };
        }

        public static void ApplySettings(VRCPlayerApi player) {
            if (!s_Settings.enabled || player.isLocal)
                return;

            MelonLogger.Msg(ConsoleColor.Green, $"Applying sound settings to {player.displayName}");

            player.SetVoiceGain(s_Settings.voice_gain);
            player.SetVoiceDistanceFar(s_Settings.voice_distance_far);
            player.SetVoiceDistanceNear(s_Settings.voice_gain);
            player.SetVoiceVolumetricRadius(s_Settings.voice_distance_near);
            player.SetVoiceLowpass(s_Settings.voice_lowpass);

            player.SetAvatarAudioGain(s_Settings.avatar_gain);
            player.SetAvatarAudioFarRadius(s_Settings.avatar_far_radius);
            player.SetAvatarAudioNearRadius(s_Settings.avatar_near_radius);
            player.SetAvatarAudioVolumetricRadius(s_Settings.avatar_volumetric_radius);
            player.SetAvatarAudioForceSpatial(s_Settings.avatar_force_spatial);
            player.SetAvatarAudioCustomCurve(s_Settings.avatar_custom_curve);
        }

        public static void AddSettingList(ICustomShowableLayoutedMenu sound_menu) {
            UiExpansion.AddToggleListItem(sound_menu, "Enable Audio override", (val) => { s_Settings.enabled = val; }, s_Settings.enabled);

            sound_menu.AddLabel("\n\n Player voice");
            UiExpansion.AddFloatListItem(sound_menu, "Gain", (val) => { s_Settings.voice_gain = val; }, s_Settings.voice_gain, 0, 24);
            UiExpansion.AddFloatDiffListItem(sound_menu, "Far", (val) => { s_Settings.voice_distance_far = val; }, s_Settings.voice_distance_far);
            UiExpansion.AddFloatDiffListItem(sound_menu, "Near", (val) => { s_Settings.voice_gain = val; }, s_Settings.voice_gain);
            UiExpansion.AddFloatDiffListItem(sound_menu, "Volumetric Radius", (val) => { s_Settings.voice_distance_near = val; }, s_Settings.voice_distance_near);
            UiExpansion.AddToggleListItem(sound_menu, "Lowpass", (val) => { s_Settings.voice_lowpass = val; }, s_Settings.voice_lowpass);

            sound_menu.AddLabel("\n\n Avatar audio");
            UiExpansion.AddFloatListItem(sound_menu, "Gain", (val) => { s_Settings.avatar_gain = val; }, s_Settings.avatar_gain, 0, 10);
            UiExpansion.AddFloatDiffListItem(sound_menu, "Far", (val) => { s_Settings.avatar_far_radius = val; }, s_Settings.avatar_far_radius);
            UiExpansion.AddFloatDiffListItem(sound_menu, "Near", (val) => { s_Settings.avatar_near_radius = val; }, s_Settings.avatar_near_radius);
            UiExpansion.AddFloatDiffListItem(sound_menu, "Volumetric Radius", (val) => { s_Settings.avatar_volumetric_radius = val; }, s_Settings.avatar_volumetric_radius);
            UiExpansion.AddToggleListItem(sound_menu, "Force Spatial", (val) => { s_Settings.avatar_force_spatial = val; }, s_Settings.avatar_force_spatial);
            UiExpansion.AddToggleListItem(sound_menu, "Allow Custom Curve", (val) => { s_Settings.avatar_custom_curve = val; }, s_Settings.avatar_custom_curve);

            sound_menu.AddSimpleButton("Apply to all", ApplySettingsToAll);
        }

        private static void ApplySettingsToAll() {
            VRCPlayerApi.AllPlayers.ForEach((Action<VRCPlayerApi>)ApplySettings);
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
