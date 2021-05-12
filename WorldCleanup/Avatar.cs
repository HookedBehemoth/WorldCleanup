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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.Core;
using VRC.Playables;

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

        public static List<AvatarParameter> FilterDefaultParameters(Il2CppSystem.Collections.Generic.Dictionary<int, AvatarParameter>.ValueCollection src) {
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
                        dst.SetBoolProperty(val_bool);
                        break;

                    case AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique.Int:
                        dst.SetIntProperty(val_int);
                        break;

                    case AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique.Float:
                        dst.SetFloatProperty(val_float);
                        break;
                }

                dst.Lock();
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
            public List<bool> renderers;
        }

        static private Dictionary<string, AvatarSettings> settings;

        public static IEnumerable<AvatarParameter> GetAvatarParameters(this VRCAvatarManager manager) {
            var parameters = manager.field_Private_AvatarPlayableController_0?
                                       .field_Private_Dictionary_2_Int32_AvatarParameter_0
                                       .Values;

            return parameters != null ? FilterDefaultParameters(parameters) : Enumerable.Empty<AvatarParameter>();
        }

        public static bool HasCustomExpressions(this VRCAvatarManager manager) {
            return manager.field_Private_AvatarPlayableController_0 &&
                   manager.prop_VRCAvatarDescriptor_0.customExpressions &&
                   /* Fuck you */
                   manager.prop_VRCAvatarDescriptor_0.expressionParameters != null &&
                   manager.prop_VRCAvatarDescriptor_0.expressionsMenu != null &&
                   /* This isn't funny */
                   manager.prop_VRCAvatarDescriptor_0.expressionsMenu.controls != null &&
                   manager.prop_VRCAvatarDescriptor_0.expressionsMenu.controls.Count > 0;
        }

        public static IEnumerable<Renderer> GetAvatarRenderers(this VRCAvatarManager manager) {
            return manager.field_Private_ArrayOf_Renderer_0;
        }

        public static void ApplyParameters(VRCAvatarManager manager) {
            var api_avatar = manager.field_Private_ApiAvatar_1;

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

            MelonLogger.Msg($"Applying avatar state to {api_avatar.name}");

            /* Apply parameters */
            if (config.parameters != null)
                foreach (var parameter in manager.GetAvatarParameters())
                    config.parameters[parameter.field_Private_String_0].Apply(parameter);

            /* Apply Meshes */
            if (config.renderers != null)
                foreach (var element in Enumerable.Zip(config.renderers, manager.GetAvatarRenderers(), (state, renderer) => new { state, renderer }))
                    element.renderer.gameObject.active = element.renderer.enabled = element.state;
        }

        public static void StoreParameters(VRCAvatarManager manager) {
            var api_avatar = manager.field_Private_ApiAvatar_1;
            MelonLogger.Msg($"Storing avatar state for {api_avatar.name}");

            var key = api_avatar.id;

            var config = new AvatarSettings {
                name = api_avatar.name,
                version = api_avatar.version,
                parameters = manager.GetAvatarParameters()?.ToDictionary(o => o.field_Private_String_0, o => { var param = new Parameter(); param.Source(o); return param; }),
                renderers = manager.GetAvatarRenderers().Select(o => o.gameObject.active && o.enabled).ToList(),
            };

            if (settings.ContainsKey(key)) {
                settings[key] = config;
            } else {
                settings.Add(key, config);
            }

            /* Prevent override of changed parameters */
            foreach (var parameter in manager.GetAvatarParameters())
                parameter.Lock();
        }

        private static readonly HashSet<IntPtr> s_ParameterOverrideList = new();

        public static void SetValue(this AvatarParameter parameter, float value) {
            /* Call original delegate to avoid self MITM */
            switch (parameter.field_Private_EnumNPublicSealedvaUnBoInFl5vUnique_0) {
                case AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique.Bool:
                    _boolPropertySetterDelegate(parameter.Pointer, value != 0.0f);
                    break;

                case AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique.Int:
                    _intPropertySetterDelegate(parameter.Pointer, (int)value);
                    break;

                case AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique.Float:
                    _floatPropertySetterDelegate(parameter.Pointer, value);
                    break;
            }
        }

        public static float GetValue(this AvatarParameter parameter) {
            return parameter.field_Private_EnumNPublicSealedvaUnBoInFl5vUnique_0 switch {
                AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique.Bool => parameter.prop_Boolean_0 ? 1f : 0f,
                AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique.Int => parameter.prop_Int32_1,
                AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique.Float => parameter.prop_Single_0,
                _ => 0f,
            };
        }

        public static void SetBoolProperty(this AvatarParameter parameter, bool value) {
            _boolPropertySetterDelegate(parameter.Pointer, value);
        }

        public static void SetIntProperty(this AvatarParameter parameter, int value) {
            _intPropertySetterDelegate(parameter.Pointer, value);
        }

        public static void SetFloatProperty(this AvatarParameter parameter, float value) {
            _floatPropertySetterDelegate(parameter.Pointer, value);
        }

        public static void ResetParameters(VRCAvatarManager manager) {
            var key = manager.field_Private_ApiAvatar_1.id;

            if (settings.ContainsKey(key))
                settings.Remove(key);

            var defaults = manager.field_Private_AvatarPlayableController_0?
                                  .field_Private_VRCAvatarDescriptor_0
                                  .expressionParameters;
            foreach (var parameter in manager.GetAvatarParameters()) {
                parameter.SetValue(defaults.FindParameter(parameter.field_Private_String_0).defaultValue);
                parameter.Unlock();
            }
        }

        public static void Unlock(this AvatarParameter parameter) {
            /* Reenable parameter override */
            if (parameter.IsLocked())
                s_ParameterOverrideList.Remove(parameter.Pointer);
        }

        public static void Lock(this AvatarParameter parameter) {
            /* Disable override parameters */
            if (!parameter.IsLocked())
                s_ParameterOverrideList.Add(parameter.Pointer);
        }

        public static bool IsLocked(this AvatarParameter parameter) {
            return s_ParameterOverrideList.Contains(parameter.Pointer);
        }

        private static bool ShouldIgnoreSetter(IntPtr parameter) {
            return s_ParameterOverrideList.Contains(parameter);
        }

        internal delegate void BoolPropertySetterDelegate(IntPtr @this, bool value);
        internal static BoolPropertySetterDelegate _boolPropertySetterDelegate;

        internal static void BoolPropertySetter(IntPtr @this, bool value) {
            /* Block manually overwritten parameters */
            var param = new AvatarParameter(@this);
            if (param.prop_Boolean_0 != value && ShouldIgnoreSetter(@this)) {
                return;
            }

            /* Invoke original function pointer */
            _boolPropertySetterDelegate(@this, value);
        }

        internal delegate void IntPropertySetterDelegate(IntPtr @this, int value);
        internal static IntPropertySetterDelegate _intPropertySetterDelegate;

        internal static void IntPropertySetter(IntPtr @this, int value) {
            /* Block manually overwritten parameters */
            var param = new AvatarParameter(@this);
            if (param.prop_Int32_1 != value && ShouldIgnoreSetter(@this)) {
                return;
            }

            /* Invoke original function pointer */
            _intPropertySetterDelegate(@this, value);
        }

        internal delegate void FloatPropertySetterDelegate(IntPtr @this, float value);
        internal static FloatPropertySetterDelegate _floatPropertySetterDelegate;

        internal static void FloatPropertySetter(IntPtr @this, float value) {
            /* Block manually overwritten parameters */
            var param = new AvatarParameter(@this);
            if (param.prop_Single_0 != value && ShouldIgnoreSetter(@this)) {
                return;
            }

            /* Invoke original function pointer */
            _floatPropertySetterDelegate(@this, value);
        }

        private static readonly string ConfigFileName = "AvatarParameterConfig.json";

        public static void LoadConfig() {
            try {
                var config = Settings.LoadConfigFile(ConfigFileName);
                settings = JsonConvert.DeserializeObject<Dictionary<string, AvatarSettings>>(config);
            } catch {
                MelonLogger.Error("Failed to load parameter config!");
            }

            /* Note: Newtonsoft Json believes "null" is a valid input */
            if (settings == null)
                settings = new Dictionary<string, AvatarSettings>();
        }

        public static void FlushConfig() {
            var serialized = JsonConvert.SerializeObject(settings);
            Settings.StoreConfigFile(ConfigFileName, serialized);
        }
    }
}
