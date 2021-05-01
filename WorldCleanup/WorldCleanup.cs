using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UIExpansionKit.API;
using System;
using System.Reflection;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using VRC.Playables;
using VRC.SDKBase;
using VRC;
using VRC.SDK3.Avatars.ScriptableObjects;
using WorldCleanup.UI;
using ActionMenuApi;

namespace WorldCleanup {
    public class WorldCleanupMod : MelonMod {
        private static Dictionary<string, GameObject> s_PlayerList;
        private static List<Tuple<Light, LightShadows>> s_Lights;
        private static List<Tuple<PostProcessVolume, bool>> s_PostProcessingVolumes;

        public override void OnApplicationQuit() {
            /* Flush avatar parameters */
            Parameters.OnPreferencesSaved();

            /* Flush audio config */
            WorldAudio.OnPreferencesSaved();

            /* Flush misc settings */
            Settings.OnPreferencesSaved();
        }

        public override void VRChat_OnUiManagerInit() {
            /* Register settings */
            Settings.OnPreferencesLoaded();

            /* Load audio settings */
            WorldAudio.OnPreferencesLoaded();

            /* Load avatar parameters */
            Parameters.OnPreferencesLoaded();

            /* Load our custom UI elements */
            UiExpansion.LoadUiObjects();

            /* Hook into "OnAvatarInstantiated" */
            /* Note: Failure is an unrecoverable error */
            unsafe {
                var on_avatar_instantiated = (IntPtr)typeof(VRCAvatarManager.MulticastDelegateNPublicSealedVoGaVRBoUnique)
                    .GetField("NativeMethodInfoPtr_Invoke_Public_Virtual_New_Void_GameObject_VRC_AvatarDescriptor_Boolean_0",
                        BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                MelonUtils.NativeHookAttach(on_avatar_instantiated, new Action<IntPtr, IntPtr, IntPtr, bool>(OnAvatarInstantiated).Method.MethodHandle.GetFunctionPointer());
                _onAvatarInstantiatedDelegate = Marshal.GetDelegateForFunctionPointer<AvatarInstantiatedDelegate>(*(IntPtr*)(void*)on_avatar_instantiated);
            }

            NetworkManager.field_Internal_Static_NetworkManager_0.field_Internal_VRCEventDelegate_1_Player_0.field_Private_HashSet_1_UnityAction_1_T_0.Add((Action<Player>)OnPlayerJoined);
            NetworkManager.field_Internal_Static_NetworkManager_0.field_Internal_VRCEventDelegate_1_Player_1.field_Private_HashSet_1_UnityAction_1_T_0.Add((Action<Player>)OnPlayerLeft);

            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu).AddSimpleButton("Player List", PlayerList);
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu).AddSimpleButton("WorldCleanup", MainMenu);
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UserQuickMenu).AddSimpleButton("Avatar Toggles", OnUserQuickMenu);

            /* Hook into setter for parameter properties */
            unsafe {
                var param_prop_bool_set = (IntPtr)typeof(AvatarParameter).GetField("NativeMethodInfoPtr_Method_Public_set_Void_Boolean_0", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                MelonUtils.NativeHookAttach(param_prop_bool_set, new Action<IntPtr, bool>(Parameters.BoolPropertySetter).Method.MethodHandle.GetFunctionPointer());
                Parameters._boolPropertySetterDelegate = Marshal.GetDelegateForFunctionPointer<Parameters.BoolPropertySetterDelegate>(*(IntPtr*)(void*)param_prop_bool_set);

                var param_prop_int_set = (IntPtr)typeof(AvatarParameter).GetField("NativeMethodInfoPtr_Method_Public_set_Void_Int32_0", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                MelonUtils.NativeHookAttach(param_prop_int_set, new Action<IntPtr, int>(Parameters.IntPropertySetter).Method.MethodHandle.GetFunctionPointer());
                Parameters._intPropertySetterDelegate = Marshal.GetDelegateForFunctionPointer<Parameters.IntPropertySetterDelegate>(*(IntPtr*)(void*)param_prop_int_set);

                var param_prop_float_set = (IntPtr)typeof(AvatarParameter).GetField("NativeMethodInfoPtr_Method_Public_set_Void_Single_0", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                MelonUtils.NativeHookAttach(param_prop_float_set, new Action<IntPtr, float>(Parameters.FloatPropertySetter).Method.MethodHandle.GetFunctionPointer());
                Parameters._floatPropertySetterDelegate = Marshal.GetDelegateForFunctionPointer<Parameters.FloatPropertySetterDelegate>(*(IntPtr*)(void*)param_prop_float_set);
            }

            AMAPI.AddSubMenuToMenu(ActionMenuPageType.Main, "World Cleanup", () => {
                /* Filter inactive avatar objects */
                s_PlayerList = s_PlayerList.Where(o => o.Value).ToDictionary(o => o.Key, o => o.Value);

                /* Order by physical distance to camera */
                var query = from player in s_PlayerList
                            orderby Vector3.Distance(player.Value.transform.position, Camera.main.transform.position)
                            select player;

                /* Only allow a max of 10 players there at once */
                /* Note: Consider adding multiple pages */
                var remaining_count = 10;

                foreach (var entry in query) {
                    var manager = entry.Value.transform.GetComponentInParent<VRCAvatarManager>();
                    var controller = manager.field_Private_AvatarPlayableController_0;
                    if (controller == null)
                        continue;

                    AMAPI.AddSubMenuToSubMenu(entry.Key, (Action)(() => {
                        var parameters = controller.field_Private_Dictionary_2_Int32_AvatarParameter_0.Values;
                        var avatar_descriptor = manager.prop_VRCAvatarDescriptor_0;

                        /* Unlock all parameters to prevent state machine tomfoolery */
                        foreach (var parameter in parameters)
                            parameter.Unlock();

                        AvatarParameter FindParameter(string name) {
                            foreach (var parameter in parameters)
                                if (parameter.field_Private_String_0 == name)
                                    return parameter;
                            return null;
                        }

                        void ExpressionSubmenu(VRCExpressionsMenu expressions_menu) {
                            foreach (var control in expressions_menu.controls) {
                                try {
                                switch (control.type) {
                                    case VRCExpressionsMenu.Control.ControlType.Button:
                                    /* Note: Action Menu "Buttons" are actually Toggles */
                                    /*       that set on press and revert on release.   */
                                    /* TODO: Add proper implementation.                 */
                                    case VRCExpressionsMenu.Control.ControlType.Toggle: {
                                        var param = FindParameter(control.parameter.name);
                                        var old = param.GetValue();
                                        void set_value(bool value) {
                                            if (value) {
                                                old = param.GetValue();
                                                param.SetValue(control.value);
                                            } else {
                                                param.SetValue(old);
                                            }
                                        }
                                        bool get_value() { return param.GetValue() == control.value; }
                                        if (get_value())
                                            old = avatar_descriptor.expressionParameters.FindParameter(control.parameter.name).defaultValue;
                                        AMAPI.AddTogglePedalToSubMenu(control.name, get_value(), set_value, icon: control.icon);
                                        break;
                                    }

                                    case VRCExpressionsMenu.Control.ControlType.SubMenu: {
                                        AMAPI.AddSubMenuToSubMenu(control.name, () => { ExpressionSubmenu(control.subMenu); }, icon: control.icon);
                                        break;
                                    }

                                    case VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet: {
                                        var horizontal = FindParameter(control.subParameters[0].name);
                                        var vertical = FindParameter(control.subParameters[1].name);
                                        AMAPI.AddFourAxisPedalToSubMenu(control.name, (value) => {
                                            horizontal.SetFloatProperty(value.x);
                                            vertical.SetFloatProperty(value.y);
                                        }, icon: control.icon);
                                        break;
                                    }

                                    case VRCExpressionsMenu.Control.ControlType.FourAxisPuppet: {
                                        var up = FindParameter(control.subParameters[0].name);
                                        var down = FindParameter(control.subParameters[1].name);
                                        var left = FindParameter(control.subParameters[2].name);
                                        var right = FindParameter(control.subParameters[3].name);
                                        AMAPI.AddFourAxisPedalToSubMenu(control.name, (value) => {
                                            up.SetFloatProperty(Math.Max(0, value.y));
                                            down.SetFloatProperty(-Math.Min(0, value.y));
                                            left.SetFloatProperty(Math.Max(0, value.x));
                                            right.SetFloatProperty(-Math.Min(0, value.x));
                                        }, icon: control.icon);
                                        break;
                                    }

                                    case VRCExpressionsMenu.Control.ControlType.RadialPuppet: {
                                        var param = FindParameter(control.subParameters[0].name);
                                        AMAPI.AddRadialPedalToSubMenu(control.name, param.SetValue, startingValue: param.GetValue(), icon: control.icon);
                                        break;
                                    }
                                }
                                } catch (Exception e) {
                                    MelonLogger.Error(e.StackTrace);
                                }
                            }
                        }

                        ExpressionSubmenu(avatar_descriptor.expressionsMenu);
                    }));

                    if (--remaining_count == 0)
                        break;
                }
            });

            MelonLogger.Msg(ConsoleColor.Green, "WorldCleanup ready!");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName) {
            /* Get active scene */
            var active_scene = SceneManager.GetActiveScene();

            s_PlayerList = new Dictionary<string, GameObject>();
            s_Lights = new List<Tuple<Light, LightShadows>>();
            s_PostProcessingVolumes = new List<Tuple<PostProcessVolume, bool>>();

            /* Iterate root objects */
            foreach (var sceneObject in active_scene.GetRootGameObjects()) {
                /* Store all lights */
                foreach (var light in sceneObject.GetComponentsInChildren<Light>(true)) {
                    s_Lights.Add(new Tuple<Light, LightShadows>(light, light.shadows));

                    if (Settings.s_DisableLights)
                        light.shadows = LightShadows.None;
                }

                /* Store PostProcessVolume's */
                foreach (var volume in sceneObject.GetComponentsInChildren<PostProcessVolume>(true)) {
                    s_PostProcessingVolumes.Add(new Tuple<PostProcessVolume, bool>(volume, volume.gameObject.active));

                    if (Settings.s_DisablePostProcessing)
                        volume.gameObject.active = false;
                }

                /* Other? */
            }
        }

        private delegate void AvatarInstantiatedDelegate(IntPtr @this, IntPtr avatarPtr, IntPtr avatarDescriptorPtr, bool loaded);
        private static AvatarInstantiatedDelegate _onAvatarInstantiatedDelegate;

        private static void OnAvatarInstantiated(IntPtr @this, IntPtr avatarPtr, IntPtr avatarDescriptorPtr, bool loaded) {
            /* Invoke original function pointer. */
            _onAvatarInstantiatedDelegate(@this, avatarPtr, avatarDescriptorPtr, loaded);

            if (loaded) {
                var avatar = new GameObject(avatarPtr);
                var player_name = avatar.transform.root.GetComponentInChildren<VRCPlayer>().prop_String_0;
                s_PlayerList[player_name] = avatar;

                var manager = avatar.transform.GetComponentInParent<VRCAvatarManager>();

                Parameters.ApplyParameters(manager.field_Private_ApiAvatar_1, manager);

                var destroy_listener = avatar.AddComponent<UIExpansionKit.Components.DestroyListener>();
                var parameters = manager.GetAvatarParameters();
                destroy_listener.OnDestroyed += () => { foreach (var parameter in parameters) parameter.Unlock(); };
            }
        }

        private static void OnPlayerJoined(Player player) {
            WorldAudio.OnPlayerJoined(player.prop_VRCPlayerApi_0);
        }

        private static void OnPlayerLeft(Player player) {
            WorldAudio.OnPlayerLeft(player.prop_VRCPlayerApi_0);
        }

        private void MainMenu() {
            var settings_menu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
            settings_menu.AddLabel("\n World Cleanup");

            /* Light shadows */
            if (s_Lights.Count() > 0) {
                settings_menu.AddButtonToggleListItem("Shadows", $"Lights: {s_Lights.Count()}", () => {
                    var shadows_menu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
                    foreach (var light in s_Lights)
                        shadows_menu.AddDropdownListItem(light.Item1.name, typeof(LightShadows), (state) => { light.Item1.shadows = (LightShadows)state; }, (int)light.Item1.shadows);
                    shadows_menu.AddSimpleButton("Back", MainMenu);
                    shadows_menu.Show();
                }, (restore) => {
                    foreach (var (light, original) in s_Lights)
                        light.shadows = restore ? original : LightShadows.None;
                    Settings.s_DisableLights = !restore;
                }, () => !Settings.s_DisableLights, false);
            } else {
                settings_menu.AddLabel("No lights found on this map");
            }

            /* Post Processing */
            if (s_PostProcessingVolumes.Count() > 0) {
                settings_menu.AddButtonToggleListItem("Post Processing", $"Volumes: {s_PostProcessingVolumes.Count()}", () => {
                    var pp_menu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
                    foreach (var volume in s_PostProcessingVolumes)
                        pp_menu.AddToggleListItem(volume.Item1.name, (state) => { volume.Item1.gameObject.active = state; }, () => volume.Item1.gameObject.active, true);
                    pp_menu.AddSimpleButton("Back", MainMenu);
                    pp_menu.Show();
                }, (restore) => {
                    foreach (var (volume, original) in s_PostProcessingVolumes)
                        volume.gameObject.active = restore ? original : false;
                    Settings.s_DisablePostProcessing = !restore;
                }, () => !Settings.s_DisablePostProcessing, false);
            } else {
                settings_menu.AddLabel("No Post Processing found on this map");
            }

            /* PlayerMods */
            settings_menu.AddSimpleButton("Player Mods", () => {
                var player = Networking.LocalPlayer;

                var player_menu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);

                player_menu.AddFloatDiffListItem("Jump Impulse", player.SetJumpImpulse, player.GetJumpImpulse);
                player_menu.AddFloatDiffListItem("Run Speed", player.SetRunSpeed, player.GetRunSpeed);
                player_menu.AddFloatDiffListItem("Walk Speed", player.SetWalkSpeed, player.GetWalkSpeed);
                player_menu.AddFloatDiffListItem("Strafe Speed", player.SetStrafeSpeed, player.GetStrafeSpeed);
                player_menu.AddFloatDiffListItem("Gravity Strength", player.SetGravityStrength, player.GetGravityStrength);

                player_menu.AddSimpleButton("Back", () => { player_menu.Hide(); MainMenu(); });
                player_menu.Show();
            });

            /* World Sound */
            WorldAudio.RegisterSettings(settings_menu, MainMenu);

            /* Update interval */
            settings_menu.AddSliderListItem("Update interval (0-3s)", (value) => { Updater.s_UpdateInterval = Settings.s_UpdateInterval = value; }, () => Updater.s_UpdateInterval, 0f, 3f);

            settings_menu.AddSimpleButton("Back", settings_menu.Hide);

            settings_menu.Show();
        }

        private void OnUserQuickMenu() {
            var player = QuickMenu.prop_QuickMenu_0.field_Private_Player_0?.prop_VRCPlayer_0;
            if (player == null)
                return;

            AvatarList(player.prop_String_0, true);
        }

        private void PlayerList() {
            /* Filter inactive avatar objects */
            s_PlayerList = s_PlayerList.Where(o => o.Value).ToDictionary(o => o.Key, o => o.Value);

            var player_list = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
            foreach (var entry in s_PlayerList)
                player_list.AddSimpleButton(entry.Key, () => { AvatarList(entry.Key, false); });

            player_list.AddSimpleButton("Back", () => { player_list.Hide(); /* MainMenu(); */ });
            player_list.Show();
        }

        private void AvatarList(string player_name, bool close_on_exit) {
            var avatar = s_PlayerList[player_name];
            if (!avatar)
                return;

            var manager = avatar.transform.GetComponentInParent<VRCAvatarManager>();
            var api_avatar = manager.field_Private_ApiAvatar_1;

            var avatar_list = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
            {
                /* Animator Toggle */
                var animator = avatar.GetComponent<Animator>();
                /* Note: What in the... */
                if (animator != null)
                    avatar_list.AddToggleListItem("Animator", (state) => { animator.enabled = state; }, () => animator.enabled, false);
            }
            {
                /* Renderer Toggle */
                var renderers = manager.field_Private_ArrayOf_Renderer_0;

                /* Get Skinned Mesh Renderers */
                var smr = renderers.Where(o => { return o.TryCast<SkinnedMeshRenderer>(); });
                if (smr.Count() > 0) {
                    avatar_list.AddSimpleButton($"SkinnedMeshRenderer: {smr.Count()}", () => {
                        var mesh_list = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
                        mesh_list.AddLabel("SkinnedMeshRenderers");
                        foreach (var renderer in smr) {
                            void set_value(bool state) { renderer.gameObject.active = renderer.enabled = state; }
                            bool get_value() { return renderer.enabled && renderer.gameObject.active; };

                            var skinned_mesh_renderer = renderer.Cast<SkinnedMeshRenderer>();
                            var shared_mesh = skinned_mesh_renderer.sharedMesh;
                            if (shared_mesh == null) {
                                MelonLogger.Msg(ConsoleColor.Red, $"{player_name} misses mesh on SkinnedMeshRenderer {renderer.gameObject.name}!");
                                continue;
                            }

                            var count = shared_mesh.blendShapeCount;
                            if (count > 0) {
                                mesh_list.AddButtonToggleListItem(
                                    renderer.gameObject.name,
                                    $"Blendshapes: {count}",
                                    () => { BlendShapeList(skinned_mesh_renderer, mesh_list); },
                                    set_value,
                                    get_value,
                                    true
                                );
                            } else {
                                mesh_list.AddToggleListItem(renderer.gameObject.name, set_value, get_value, true);
                            }
                        }
                        mesh_list.AddSimpleButton("Back", () => { mesh_list.Hide(); AvatarList(player_name, close_on_exit); });
                        mesh_list.Show();
                    });
                }

                void ShowGenericRendererToggleList(string type, IEnumerable<Renderer> list) {
                    var mesh_list = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
                    if (type != null)
                        mesh_list.AddLabel(type);
                    foreach (var mesh in list) {
                        var name = type != null ? mesh.gameObject.name : $"{mesh.GetIl2CppType().Name}: {mesh.gameObject.name}";
                        mesh_list.AddToggleListItem(name, (state) => { mesh.enabled = mesh.gameObject.active = state; }, () => mesh.enabled && mesh.gameObject.active, true);
                    }
                    mesh_list.AddSimpleButton("Back", () => { mesh_list.Hide(); AvatarList(player_name, close_on_exit); });
                    mesh_list.Show();
                }

                /* Get Mesh Renderers */
                var mr = renderers.Where(o => { return o.TryCast<MeshRenderer>(); });
                if (mr.Count() > 0)
                    avatar_list.AddSimpleButton($"MeshRenderer: {mr.Count()}", () => { ShowGenericRendererToggleList("MeshRenderer", mr); });

                /* Get Particle System Renderers */
                var pr = renderers.Where(o => { return o.TryCast<ParticleSystemRenderer>(); });
                if (pr.Count() > 0)
                    avatar_list.AddSimpleButton($"ParticleSystemRenderer: {pr.Count()}", () => { ShowGenericRendererToggleList("ParticleSystemRenderer", pr); });

                /* Other renderers */
                var remainder = renderers.Where(o => { return !o.TryCast<SkinnedMeshRenderer>() && !o.TryCast<MeshRenderer>() && !o.TryCast<ParticleSystemRenderer>(); });
                if (remainder.Count() > 0)
                    avatar_list.AddSimpleButton($"Other: {remainder.Count()}", () => { ShowGenericRendererToggleList(null, remainder); });
            }
            {
                /* Parameters */
                var controller = manager.field_Private_AvatarPlayableController_0;

                /* Only populated on SDK3 avatars */
                if (controller != null) {
                    var parameters = controller.field_Private_Dictionary_2_Int32_AvatarParameter_0.Values;
                    var filtered = Parameters.FilterDefaultParameters(parameters);

                    if (filtered.Count > 0) {
                        var avatar_descriptor = controller.field_Private_VRCAvatarDescriptor_0;

                        avatar_list.AddSimpleButton($"Parameter Menu", () => {
                            /* Unlock all parameters to prevent state machine tomfoolery */
                            foreach (var parameter in parameters)
                                parameter.Unlock();

                            AvatarParameter FindParameter(string name) {
                                foreach (var parameter in parameters)
                                    if (parameter.field_Private_String_0 == name)
                                        return parameter;
                                return null;
                            }

                            void ExpressionSubmenu(ICustomShowableLayoutedMenu list, VRCExpressionsMenu expressions_menu) {
                                foreach (var control in expressions_menu.controls) {
                                    switch (control.type) {
                                        case VRCExpressionsMenu.Control.ControlType.Button:
                                        /* Note: Action Menu "Buttons" are actually Toggles */
                                        /*       that set on press and revert on release.   */
                                        /* TODO: Add proper implementation.                 */
                                        case VRCExpressionsMenu.Control.ControlType.Toggle: {
                                            var param = FindParameter(control.parameter.name);
                                            var old = param.GetValue();
                                            void set_value(bool value) {
                                                if (value) {
                                                    old = param.GetValue();
                                                    param.SetValue(control.value);
                                                } else {
                                                    param.SetValue(old);
                                                }
                                            }
                                            bool get_value() { return param.GetValue() == control.value; }
                                            if (get_value())
                                                old = avatar_descriptor.expressionParameters.FindParameter(control.parameter.name).defaultValue;
                                            list.AddToggleListItem(control.name, set_value, get_value, true);
                                            break;
                                        }

                                        case VRCExpressionsMenu.Control.ControlType.SubMenu: {
                                            list.AddSimpleButton(control.name, () => {
                                                var sub_menu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);

                                                ExpressionSubmenu(sub_menu, control.subMenu);

                                                sub_menu.AddSimpleButton("Back", () => { sub_menu.Hide(); list.Show(); });
                                                sub_menu.Show();
                                            });
                                            break;
                                        }

                                        case VRCExpressionsMenu.Control.ControlType.RadialPuppet: {
                                            var param = FindParameter(control.subParameters[0].name);
                                            list.AddSliderListItem(control.name, param.SetValue, param.GetValue, 0, 1);
                                            break;
                                        }

                                        default:
                                            list.AddLabel($"\n\n{control.name}: {control.type} unsupported");
                                            break;
                                    }
                                }
                            }
                            var menu_list = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);

                            ExpressionSubmenu(menu_list, avatar_descriptor.expressionsMenu);

                            menu_list.AddSimpleButton("Back", () => { menu_list.Hide(); AvatarList(player_name, close_on_exit); });
                            menu_list.Show();
                        });

                        avatar_list.AddSimpleButton($"Parameters: {filtered.Count}", () => {
                            /* Unlock all parameters to prevent state machine tomfoolery */
                            foreach (var parameter in parameters)
                                parameter.Unlock();

                            var parameter_list = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
                            foreach (var parameter in filtered) {
                                var name = parameter.field_Private_String_0;
                                var type = parameter.field_Private_EnumNPublicSealedvaUnBoInFl5vUnique_0;
                                switch (type) {
                                    case AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique.Bool:
                                        parameter_list.AddToggleListItem(name, parameter.SetBoolProperty, () => parameter.prop_Boolean_0, true);
                                        break;

                                    case AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique.Int:
                                        parameter_list.AddIntDiffListItem(name, parameter.SetIntProperty, () => parameter.prop_Int32_1);
                                        break;

                                    case AvatarParameter.EnumNPublicSealedvaUnBoInFl5vUnique.Float:
                                        parameter_list.AddSliderListItem(name, parameter.SetFloatProperty, () => parameter.prop_Single_0);
                                        break;

                                    default:
                                        MelonLogger.Msg(ConsoleColor.Red, $"Unsupported [{type}]: {name}");
                                        break;
                                }
                            }
                            parameter_list.AddSimpleButton("Back", () => { parameter_list.Hide(); AvatarList(player_name, close_on_exit); });
                            parameter_list.Show();
                        });
                    }
                }
            }
            avatar_list.AddSimpleButton("Save Config", () => { Parameters.StoreParameters(api_avatar, manager); });
            avatar_list.AddSimpleButton("Reset", () => { Parameters.ResetParameters(api_avatar, manager); });
            avatar_list.AddSimpleButton("Back", () => { avatar_list.Hide(); if (!close_on_exit) PlayerList(); });
            avatar_list.Show();
        }

        private void BlendShapeList(SkinnedMeshRenderer renderer, ICustomShowableLayoutedMenu parent) {
            var blendshape_list = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);

            var shared_mesh = renderer.sharedMesh;
            foreach (var i in Enumerable.Range(0, shared_mesh.blendShapeCount))
                blendshape_list.AddSliderListItem(shared_mesh.GetBlendShapeName(i), (value) => { renderer.SetBlendShapeWeight(i, value); }, () => renderer.GetBlendShapeWeight(i), 0f, 100f);

            blendshape_list.AddSimpleButton("Back", () => { blendshape_list.Hide(); parent.Show(); });
            blendshape_list.Show();
        }
    }
}
