using MelonLoader;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using VRC.Core;
using VRC.Playables;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace WorldCleanup {
    internal static class Parameters {
        public static readonly string[] DefaultParameterNames = new string[] {
            "Viseme",
            "GestureLeft",
            "GestureLeftWeight",
            "GestureRight",
            "GestureRightWeight",
            "TrackingType",
            "VRMode",
            "MuteSelf",
            "Grounded",
            "AngularY",
            "Upright",
            "AFK",
            "Seated",
            "InStation",
            "VelocityX",
            "VelocityY",
            "VelocityZ",
            "IsLocal",
            "AvatarVersion",
            "VRCEmote",
            "VRCFaceBlendH",
            "VRCFaceBlendV",
        };

        public static List<AvatarParameter> FilterParameters(Il2CppSystem.Collections.Generic.Dictionary<int, AvatarParameter>.ValueCollection src) {
            /* Note: IL2CPP Dictionary misses "Which" */
            var parameters = new List<AvatarParameter>();
            foreach (var param in src)
                if (!DefaultParameterNames.Contains(param.field_Private_String_0))
                    parameters.Add(param);
            return parameters;
        }

        class Parameter {
            public void Source(AvatarParameter src) {
                type = src.field_Private_EnumNPublicSealedvaUnBoInFl5vUnique_0;
                switch (type) {
                    case AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique.Bool:
                        val_bool = src.prop_Boolean_0;
                        break;

                    case AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique.Int:
                        val_int = src.prop_Int32_1;
                        break;

                    case AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique.Float:
                        val_float = src.prop_Single_0;
                        break;
                }
            }
            public void Apply(AvatarParameter dst) {
                switch (type) {
                    case AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique.Bool:
                        dst.prop_Boolean_0 = val_bool;
                        break;

                    case AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique.Int:
                        dst.prop_Int32_1 = val_int;
                        break;

                    case AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique.Float:
                        dst.prop_Single_0 = val_float;
                        break;
                }
            }
            public AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique type;
            public int val_int = 0;
            public float val_float = 0.0f;
            public bool val_bool = false;
        };

        class AvatarSettings {
            public string name;
            public int version;
            public Dictionary<string, Parameter> parameters;
        }

        static private Dictionary<string, AvatarSettings> settings;

        public static void ApplyParameters(ApiAvatar api_avatar, List<AvatarParameter> parameters) {
            /* Look up store */
            var key = api_avatar.id;
            if (!settings.ContainsKey(key))
                return;
            var config = settings[key];

            /* Check version */
            if (config.version != api_avatar.version) {
                MelonLogger.Msg($"Avatar {api_avatar.name} version missmatch ({config.version} != {api_avatar.version}). Removing");
                settings.Remove(key);
                return;
            }

            MelonLogger.Msg($"Applying {config.parameters.Count} parameters to {api_avatar.name}");

            /* Apply parameters */
            foreach (var parameter in parameters) {
                config.parameters[parameter.field_Private_String_0].Apply(parameter);
            }
        }

        public static void StoreParameters(ApiAvatar api_avatar, List<AvatarParameter> parameters) {
            MelonLogger.Msg($"Storing {parameters.Count} parameters for {api_avatar.name}");

            var key = api_avatar.id;

            var config = new AvatarSettings {
                name = api_avatar.name,
                version = api_avatar.version,
                parameters = parameters.ToDictionary(o => o.field_Private_String_0, o => { var param = new Parameter(); param.Source(o); return param; }),
            };

            if (settings.ContainsKey(key)) {
                settings[key] = config;
            } else {
                settings.Add(key, config);
            }
        }

        public static void ResetParameters(ApiAvatar api_avatar, List<AvatarParameter> parameters, VRCExpressionParameters defaults) {
            var key = api_avatar.id;

            if (settings.ContainsKey(key))
                settings.Remove(key);

            foreach (var parameter in parameters) {
                var value = defaults.FindParameter(parameter.field_Private_String_0).defaultValue;
                switch (parameter.field_Private_EnumNPublicSealedvaUnBoInFl5vUnique_0) {
                    case AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique.Bool:
                        parameter.prop_Boolean_0 = value != 0.0f;
                        break;

                    case AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique.Int:
                        parameter.prop_Int32_1 = (int)value;
                        break;

                    case AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique.Float:
                        parameter.prop_Single_0 = value;
                        break;
                }
            }
        }

        private static readonly string ConfigFileName = "AvatarParameterConfig.json";

        public static void OnPreferencesLoaded() {
            var config = Settings.LoadConfigFile(ConfigFileName);
            settings = config != null
                ? JsonConvert.DeserializeObject<Dictionary<string, AvatarSettings>>(config)
                : new Dictionary<string, AvatarSettings>();
        }

        public static void OnPreferencesSaved() {
            var serialized = JsonConvert.SerializeObject(settings);
            Settings.StoreConfigFile(ConfigFileName, serialized);
        }
    }
}
