/*
 * Copyright (c) 2021-2022 HookedBehemoth
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

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace WorldCleanup
{
    internal static class Parameters
    {
        public static readonly string[] DefaultParameterNames = new string[] {
            "Viseme",
            "Voice",
            "GestureLeft",
            "GestureLeftWeight",
            "GestureRight",
            "GestureRightWeight",
            "TrackingType",
            "VRMode",
            "MuteSelf",
            "Earmuffs",
            "Grounded",
            "AngularY",
            "Upright",
            "AFK",
            "Seated",
            "InStation",
            "VelocityX",
            "VelocityY",
            "VelocityZ",
            "MovementX",
            "MovementY",
            "MovementZ",
            "IsLocal",
            "AvatarVersion",
            "VRCEmote",
            "VRCFaceBlendH",
            "VRCFaceBlendV",
            "IsOnFriendsList",
        };

        public static List<AvatarParameterAccess> FilterDefaultParameters(IEnumerable<AvatarParameterAccess> src)
            => src.Where(param => !DefaultParameterNames.Contains(param.field_Protected_String_0)).ToList();

        class Parameter
        {
            public Parameter() { }
            public Parameter(AvatarParameterAccess src)
            {
                type = src.GetAvatarParameterType();
                switch (type)
                {
                    case AvatarParameterType.Bool:
                        val_bool = src.prop_Boolean_1;
                        break;

                    case AvatarParameterType.Int:
                        val_int = src.prop_Int32_1;
                        break;

                    case AvatarParameterType.Float:
                        val_float = src.prop_Single_1;
                        break;
                }
            }
            public void Apply(AvatarParameterAccess dst)
            {
                switch (type)
                {
                    case AvatarParameterType.Bool:
                        dst.SetBoolProperty(val_bool);
                        break;

                    case AvatarParameterType.Int:
                        dst.SetIntProperty(val_int);
                        break;

                    case AvatarParameterType.Float:
                        dst.SetFloatProperty(val_float);
                        break;
                }

                dst.Lock();
            }
            public AvatarParameterType type;
            public int val_int = 0;
            public float val_float = 0.0f;
            public bool val_bool = false;
        };

        class AvatarSettings
        {
            public string name;
            public int version;
            public Dictionary<string, Parameter> parameters;
            public List<bool> renderers;
        }

        static private Dictionary<string, AvatarSettings> settings;

        public static IEnumerable<AvatarParameterAccess> GetAllAvatarParameters(this VRCAvatarManager manager)
        {
            var parameters = manager.GetAvatarPlayableController()?
                                    .GetParameters();

            if (parameters == null)
                yield break;

            foreach (var param in parameters)
                yield return param.value;
        }

        public static List<AvatarParameterAccess> GetAvatarParameters(this VRCAvatarManager manager)
            => FilterDefaultParameters(manager.GetAllAvatarParameters());

        public static bool HasCustomExpressions(this VRCAvatarManager manager)
        {
            return manager &&
                   manager.GetAvatarPlayableController() != null &&
                   manager.prop_VRCAvatarDescriptor_0 != null &&
                   manager.prop_VRCAvatarDescriptor_0.customExpressions &&
                   /* Fuck you */
                   manager.prop_VRCAvatarDescriptor_0.expressionParameters != null &&
                   manager.prop_VRCAvatarDescriptor_0.expressionsMenu != null &&
                   /* This isn't funny */
                   manager.prop_VRCAvatarDescriptor_0.expressionsMenu.controls != null &&
                   manager.prop_VRCAvatarDescriptor_0.expressionsMenu.controls.Count > 0;
        }

        public static IEnumerable<Renderer> GetAvatarRenderers(this VRCAvatarManager manager)
        {
            return manager.field_Private_Il2CppReferenceArray_1_Renderer_0;
        }

        public static void ApplyParameters(VRCAvatarManager manager)
        {
            var api_avatar = manager?.prop_ApiAvatar_0;
            if (api_avatar == null)
                return;

            /* Look up store */
            var key = api_avatar.id;
            if (!settings.ContainsKey(key))
                return;
            var config = settings[key];

            /* Check version */
            if (config.version != api_avatar.version)
            {
                MelonLogger.Msg($"Avatar {api_avatar.name} version missmatch ({config.version} != {api_avatar.version}). Removing");
                settings.Remove(key);
                return;
            }

            MelonLogger.Msg($"Applying avatar state to {api_avatar.name}");

            /* Apply parameters */
            if (config.parameters != null)
                foreach (var parameter in manager.GetAvatarParameters())
                    if (config.parameters.TryGetValue(parameter.field_Protected_String_0, out var configParam))
                        configParam.Apply(parameter);

            /* Apply Meshes */
            if (config.renderers != null)
                foreach (var element in Enumerable.Zip(config.renderers, manager.GetAvatarRenderers(), (state, renderer) => new { state, renderer }))
                    element.renderer.gameObject.active = element.renderer.enabled = element.state;
        }

        public static void StoreParameters(VRCAvatarManager manager)
        {
            var api_avatar = manager?.prop_ApiAvatar_0;
            if (api_avatar == null)
                return;

            MelonLogger.Msg($"Storing avatar state for {api_avatar.name}");

            var config = new AvatarSettings
            {
                name = api_avatar.name,
                version = api_avatar.version,
                parameters = manager.GetAvatarParameters()?.ToDictionary(o => o.field_Protected_String_0, o => new Parameter(o)),
                renderers = manager.GetAvatarRenderers().Select(o => o.gameObject.active && o.enabled).ToList(),
            };

            var key = api_avatar.id;
            if (settings.ContainsKey(key))
            {
                settings[key] = config;
            }
            else
            {
                settings.Add(key, config);
            }

            /* Prevent override of changed parameters */
            foreach (var parameter in manager.GetAvatarParameters())
                parameter.Lock();
        }

        public static void SetValue(this AvatarParameterAccess parameter, float value)
        {
            if (parameter == null) return;
            /* Call original delegate to avoid self MITM */
            switch (parameter.GetAvatarParameterType())
            {
                case AvatarParameterType.Bool:
                    _boolPropertySetterDelegate(parameter.Pointer, value != 0.0f);
                    break;

                case AvatarParameterType.Int:
                    _intPropertySetterDelegate(parameter.Pointer, (int)value);
                    break;

                case AvatarParameterType.Float:
                    _floatPropertySetterDelegate(parameter.Pointer, value);
                    break;
            }
        }

        public static float GetValue(this AvatarParameterAccess parameter)
        {
            if (parameter == null) return 0f;
            return parameter.GetAvatarParameterType() switch
            {
                AvatarParameterType.Bool => parameter.prop_Boolean_2 ? 1f : 0f,
                AvatarParameterType.Int => parameter.prop_Int32_1,
                AvatarParameterType.Float => parameter.prop_Single_1,
                _ => 0f,
            };
        }

        public static void SetBoolProperty(this AvatarParameterAccess parameter, bool value)
        {
            if (parameter == null) return;
            _boolPropertySetterDelegate(parameter.Pointer, value);
        }

        public static void SetIntProperty(this AvatarParameterAccess parameter, int value)
        {
            if (parameter == null) return;
            _intPropertySetterDelegate(parameter.Pointer, value);
        }

        public static void SetFloatProperty(this AvatarParameterAccess parameter, float value)
        {
            if (parameter == null) return;
            _floatPropertySetterDelegate(parameter.Pointer, value);
        }

        public static void ResetParameters(VRCAvatarManager manager)
        {
            var api_avatar = manager?.prop_ApiAvatar_0;
            if (api_avatar == null)
                return;

            var key = api_avatar.id;

            if (settings.ContainsKey(key))
                settings.Remove(key);

            var defaults = manager.prop_VRCAvatarDescriptor_0?
                                  .expressionParameters;
            foreach (var parameter in manager.GetAvatarParameters())
            {
                parameter.SetValue(defaults.FindParameter(parameter.field_Protected_String_0).defaultValue);
                parameter.Unlock();
            }
        }

        private static readonly HashSet<IntPtr> s_ParameterOverrideList = new();

        public static void Unlock(this AvatarParameterAccess parameter)
        {
            /* Reenable parameter override */
            if (parameter.IsLocked())
                s_ParameterOverrideList.Remove(parameter.Pointer);
        }

        public static void Lock(this AvatarParameterAccess parameter)
        {
            /* Disable override parameters */
            if (!parameter.IsLocked())
                s_ParameterOverrideList.Add(parameter.Pointer);
        }

        public static bool IsLocked(this AvatarParameterAccess parameter)
        {
            return s_ParameterOverrideList.Contains(parameter.Pointer);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void BoolPropertySetterDelegate(IntPtr @this, bool value);
        internal static BoolPropertySetterDelegate _boolPropertySetterDelegate;

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static void BoolPropertySetter(IntPtr @this, sbyte value)
        {
            /* Block manually overwritten parameters */
            var param = new AvatarParameterAccess(@this);
            if (param.IsLocked())
                return;

            /* Invoke original function pointer */
            _boolPropertySetterDelegate(@this, value != 0);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void IntPropertySetterDelegate(IntPtr @this, int value);
        internal static IntPropertySetterDelegate _intPropertySetterDelegate;

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static void IntPropertySetter(IntPtr @this, int value)
        {
            /* Block manually overwritten parameters */
            var param = new AvatarParameterAccess(@this);
            if (param.IsLocked())
                return;

            /* Invoke original function pointer */
            _intPropertySetterDelegate(@this, value);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void FloatPropertySetterDelegate(IntPtr @this, float value);
        internal static FloatPropertySetterDelegate _floatPropertySetterDelegate;

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static void FloatPropertySetter(IntPtr @this, float value)
        {
            /* Block manually overwritten parameters */
            var param = new AvatarParameterAccess(@this);
            if (param.IsLocked())
                return;

            /* Invoke original function pointer */
            _floatPropertySetterDelegate(@this, value);
        }

        private static readonly string ConfigFileName = "AvatarParameterConfig.json";

        public static void LoadConfig()
        {
            try
            {
                var config = Settings.LoadConfigFile(ConfigFileName);
                settings = JsonConvert.DeserializeObject<Dictionary<string, AvatarSettings>>(config);
            }
            catch
            {
                MelonLogger.Error("Failed to load parameter config!");
            }

            /* Note: Newtonsoft Json believes "null" is a valid input */
            if (settings == null)
                settings = new Dictionary<string, AvatarSettings>();
        }

        public static void FlushConfig()
        {
            var serialized = JsonConvert.SerializeObject(settings);
            Settings.StoreConfigFile(ConfigFileName, serialized);
        }
    }
}
