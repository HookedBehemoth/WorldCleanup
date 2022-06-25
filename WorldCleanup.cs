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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ActionMenuApi.Api;
using MelonLoader;
using UIExpansionKit.API;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using VRC;
using VRC.Playables;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;

[assembly: MelonInfo(typeof(WorldCleanup.WorldCleanupMod), "WorldCleanup", "1.1.3", "Behemoth")]
[assembly: MelonGame("VRChat", "VRChat")]

namespace WorldCleanup
{
    public class WorldCleanupMod : MelonMod
    {
        // struct ToggleState {
        // };
        // private static Dictionary<string, ToggleState> s_ToggleStates;
        private static Dictionary<string, GameObject> s_PlayerList;
        private static List<Tuple<Light, LightShadows>> s_Lights;
        private static List<Tuple<PostProcessVolume, bool>> s_PostProcessingVolumes;
        private static List<VRC_MirrorReflection> s_Mirrors;
        private static Dictionary<string, RefCountedObject<Texture2D>> s_Portraits;
        private static GameObject s_PreviewCaptureCamera;

        public override void OnApplicationStart()
        {
            /* Register settings */
            Settings.RegisterConfig();

            /* Load audio settings */
            WorldAudio.LoadConfig();

            /* Load avatar parameters */
            Parameters.LoadConfig();

            /* Load our custom UI elements */
            UiExpansion.LoadUiObjects();

            /* TODO: Consider switching to operator+ when everyone had to update the assembly unhollower */
            /*       The current solution might be prefereable so we are always first */
            VRCAvatarManager.field_Private_Static_Action_3_Player_GameObject_VRC_AvatarDescriptor_0 += (Il2CppSystem.Action<Player, GameObject, VRC.SDKBase.VRC_AvatarDescriptor>)OnAvatarInstantiate;

            /* Register async, awaiting network manager */
            MelonCoroutines.Start(RegisterJoinLeaveNotifier());

            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu).AddSimpleButton("Player List", PlayerList);
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu).AddSimpleButton("WorldCleanup", MainMenu);
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UserQuickMenu).AddSimpleButton("Avatar Toggles", OnUserQuickMenu);

            /* Hook into setter for parameter properties */
            unsafe
            {
                var param_prop_bool_set = (IntPtr)typeof(AvatarParameter).GetField("NativeMethodInfoPtr_set_boolVal_Public_Virtual_Final_New_set_Void_Boolean_0", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                MelonUtils.NativeHookAttach(param_prop_bool_set, new Action<IntPtr, bool>(Parameters.BoolPropertySetter).Method.MethodHandle.GetFunctionPointer());
                Parameters._boolPropertySetterDelegate = Marshal.GetDelegateForFunctionPointer<Parameters.BoolPropertySetterDelegate>(*(IntPtr*)(void*)param_prop_bool_set);

                var param_prop_int_set = (IntPtr)typeof(AvatarParameter).GetField("NativeMethodInfoPtr_set_intVal_Public_Virtual_Final_New_set_Void_Int32_0", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                MelonUtils.NativeHookAttach(param_prop_int_set, new Action<IntPtr, int>(Parameters.IntPropertySetter).Method.MethodHandle.GetFunctionPointer());
                Parameters._intPropertySetterDelegate = Marshal.GetDelegateForFunctionPointer<Parameters.IntPropertySetterDelegate>(*(IntPtr*)(void*)param_prop_int_set);

                var param_prop_float_set = (IntPtr)typeof(AvatarParameter).GetField("NativeMethodInfoPtr_set_floatVal_Public_Virtual_Final_New_set_Void_Single_0", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                MelonUtils.NativeHookAttach(param_prop_float_set, new Action<IntPtr, float>(Parameters.FloatPropertySetter).Method.MethodHandle.GetFunctionPointer());
                Parameters._floatPropertySetterDelegate = Marshal.GetDelegateForFunctionPointer<Parameters.FloatPropertySetterDelegate>(*(IntPtr*)(void*)param_prop_float_set);
            }

            VRCActionMenuPage.AddSubMenu(ActionMenuPage.Main,"Player Toggles", () =>
            {
                /* Filter inactive avatar objects */
                s_PlayerList = s_PlayerList.Where(o => o.Value).ToDictionary(o => o.Key, o => o.Value);

                /* Order by physical distance to camera */
                var query = from player in s_PlayerList
                            orderby Vector3.Distance(player.Value.transform.position, Camera.main.transform.position)
                            select player;

                /* Only allow a max of 10 players there at once */
                /* TODO: Consider adding multiple pages */
                var remaining_count = 10;

                foreach (var entry in query)
                {
                    var manager = entry.Value.GetComponentInParent<VRCAvatarManager>();

                    /* Ignore SDK2 & avatars w/o custom expressions */
                    if (!manager.HasCustomExpressions())
                        continue;

                    var avatar_id = entry.Value.GetComponent<VRC.Core.PipelineManager>().blueprintId;
                    var user_icon = s_Portraits[avatar_id].Get();

                    /* Source default expression icon */
                    var menu_icons = ActionMenuDriver.prop_ActionMenuDriver_0.field_Public_MenuIcons_0;
                    var default_expression = menu_icons.defaultExpression;

                    CustomSubMenu.AddSubMenu(entry.Key, () =>
                    {
                        if (entry.Value == null || !entry.Value.active)
                            return;

                        var parameters = manager.GetAllAvatarParameters();
                        var filtered = Parameters.FilterDefaultParameters(parameters);
                        var avatar_descriptor = manager.prop_VRCAvatarDescriptor_0;

                        CustomSubMenu.AddToggle("Lock", filtered.Any(Parameters.IsLocked), (state) => { filtered.ForEach(state ? Parameters.Lock : Parameters.Unlock); }, icon: UiExpansion.LockClosedIcon);
                        CustomSubMenu.AddButton("Save", () => Parameters.StoreParameters(manager), icon: UiExpansion.SaveIcon);

                        AvatarParameter FindParameter(string name)
                        {
                            foreach (var parameter in parameters)
                                if (parameter.field_Private_String_0 == name)
                                    return parameter;
                            return null;
                        }

                        void ExpressionSubmenu(VRCExpressionsMenu expressions_menu)
                        {
                            if (entry.Value == null || !entry.Value.active)
                                return;

                            void FourAxisControl(VRCExpressionsMenu.Control control, Action<Vector2> callback)
                            {
                                CustomSubMenu.AddFourAxisPuppet(
                                    control.TruncatedName(),
                                    callback,
                                    icon: control.icon ?? default_expression,
                                    topButtonText: control.labels[0]?.TruncatedName() ?? "Up",
                                    rightButtonText: control.labels[1]?.TruncatedName() ?? "Right",
                                    downButtonText: control.labels[2]?.TruncatedName() ?? "Down",
                                    leftButtonText: control.labels[3]?.TruncatedName() ?? "Left");
                            }

                            foreach (var control in expressions_menu.controls)
                            {
                                try
                                {
                                    switch (control.type)
                                    {
                                        case VRCExpressionsMenu.Control.ControlType.Button:
                                        /* Note: Action Menu "Buttons" are actually Toggles */
                                        /*       that set on press and revert on release.   */
                                        /* TODO: Add proper implementation.                 */
                                        case VRCExpressionsMenu.Control.ControlType.Toggle:
                                            {
                                                var param = FindParameter(control.parameter.name);
                                                var current_value = param.GetValue();
                                                var default_value = avatar_descriptor.expressionParameters.FindParameter(control.parameter.name)?.defaultValue ?? 0f;
                                                var target_value = control.value;
                                                void SetIntFloat(bool state) => param.SetValue(state ? target_value : default_value);
                                                void SetBool(bool state) => param.SetValue(state ? 1f : 0f);

                                                CustomSubMenu.AddToggle(
                                                    control.TruncatedName(),
                                                    current_value == target_value,
                                                    param.field_Public_ParameterType_0 == AvatarParameter.ParameterType.Bool ? SetBool : SetIntFloat,
                                                    icon: control.icon ?? default_expression);
                                                break;
                                            }

                                        case VRCExpressionsMenu.Control.ControlType.SubMenu:
                                            {
                                                CustomSubMenu.AddSubMenu(control.TruncatedName(), () => ExpressionSubmenu(control.subMenu), icon: control.icon ?? default_expression);
                                                break;
                                            }

                                        case VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet:
                                            {
                                                var horizontal = FindParameter(control.subParameters[0]?.name);
                                                var vertical = FindParameter(control.subParameters[1]?.name);
                                                FourAxisControl(control, (value) =>
                                                {
                                                    horizontal.SetFloatProperty(value.x);
                                                    vertical.SetFloatProperty(value.y);
                                                });
                                                break;
                                            }

                                        case VRCExpressionsMenu.Control.ControlType.FourAxisPuppet:
                                            {
                                                var up = FindParameter(control.subParameters[0]?.name);
                                                var down = FindParameter(control.subParameters[1]?.name);
                                                var left = FindParameter(control.subParameters[2]?.name);
                                                var right = FindParameter(control.subParameters[3]?.name);
                                                FourAxisControl(control, (value) =>
                                                {
                                                    up.SetFloatProperty(Math.Max(0, value.y));
                                                    down.SetFloatProperty(-Math.Min(0, value.y));
                                                    left.SetFloatProperty(Math.Max(0, value.x));
                                                    right.SetFloatProperty(-Math.Min(0, value.x));
                                                });
                                                break;
                                            }

                                        case VRCExpressionsMenu.Control.ControlType.RadialPuppet:
                                            {
                                                var param = FindParameter(control.subParameters[0]?.name);
                                                CustomSubMenu.AddRestrictedRadialPuppet(control.TruncatedName(), param.SetValue, startingValue: param.GetValue(), icon: control.icon ?? default_expression);
                                                break;
                                            }
                                    }
                                }
                                catch (Exception e)
                                {
                                    MelonLogger.Error(e.StackTrace);
                                }
                            }
                        }

                        ExpressionSubmenu(avatar_descriptor.expressionsMenu);
                    }, icon: user_icon);

                    if (--remaining_count == 0)
                        break;
                }
            });

            MelonLogger.Msg(ConsoleColor.Green, "WorldCleanup ready!");
        }

        public override void OnApplicationQuit()
        {
            /* Flush avatar parameters */
            Parameters.FlushConfig();

            /* Flush audio config */
            WorldAudio.FlushConfig();
        }

        public override void OnPreferencesLoaded()
            => LoadAndApplyPreferences();

        public override void OnPreferencesSaved()
            => LoadAndApplyPreferences();

        private void LoadAndApplyPreferences()
        {
            /* Load audio settings */
            WorldAudio.LoadConfig();
            WorldAudio.ApplySettingsToAll();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName == "Application2" || sceneName == "ui")
                return;

            /* Get active scene */
            var active_scene = SceneManager.GetActiveScene();

            s_PlayerList = new Dictionary<string, GameObject>();
            s_Lights = new List<Tuple<Light, LightShadows>>();
            s_PostProcessingVolumes = new List<Tuple<PostProcessVolume, bool>>();
            s_Mirrors = new List<VRC_MirrorReflection>();
            s_Portraits = new Dictionary<string, RefCountedObject<Texture2D>>();
            if (UiExpansion.PreviewCamera != null)
            {
                s_PreviewCaptureCamera = GameObject.Instantiate(UiExpansion.PreviewCamera);
                s_PreviewCaptureCamera.SetActive(false);
            }

            var disable_shadows = Settings.s_DisableLights.Value;
            var disable_ppv = Settings.s_DisablePostProcessing.Value;
            var disable_mirrors = Settings.s_DisableMirrors.Value;
            /* Iterate root objects */
            foreach (var sceneObject in active_scene.GetRootGameObjects())
            {
                /* Store all lights */
                foreach (var light in sceneObject.GetComponentsInChildren<Light>(true))
                {
                    s_Lights.Add(new Tuple<Light, LightShadows>(light, light.shadows));

                    if (disable_shadows)
                        light.shadows = LightShadows.None;
                }

                /* Store PostProcessVolume's */
                foreach (var volume in sceneObject.GetComponentsInChildren<PostProcessVolume>(true))
                {
                    s_PostProcessingVolumes.Add(new Tuple<PostProcessVolume, bool>(volume, volume.gameObject.active));

                    if (disable_ppv)
                        volume.gameObject.active = false;
                }

                /* Store Mirrors */
                foreach (var mirror in sceneObject.GetComponentsInChildren<VRC_MirrorReflection>(true))
                {
                    s_Mirrors.Add(mirror);

                    if (disable_mirrors)
                        mirror.enabled = false;
                }

                /* Other? */
            }
        }

        private static void OnAvatarInstantiate(Player player, GameObject avatar, VRC_AvatarDescriptor descriptor)
        {
            var manager = player._vrcplayer.prop_VRCAvatarManager_0;
            var player_name = player._vrcplayer.prop_String_1;
            if (player_name == null) return;
            s_PlayerList[player_name] = avatar;

            Parameters.ApplyParameters(manager);

            var avatar_id = avatar.GetComponent<VRC.Core.PipelineManager>().blueprintId;

            var destroy_listener = avatar.AddComponent<UIExpansionKit.Components.DestroyListener>();
            var parameters = manager.GetAvatarParameters().ToArray();
            destroy_listener.OnDestroyed += () =>
            {
                /* Unlock expression parameters */
                foreach (var parameter in parameters) parameter.Unlock();

                /* Decrement ref count on avatar portrait */
                if (s_Portraits.ContainsKey(avatar_id)) if (s_Portraits[avatar_id].Decrement()) s_Portraits.Remove(avatar_id);
            };

            /* Take preview image for action menu */
            /* Note: in this state, everyone should be t-posing and your own head is still there */
            if (manager.HasCustomExpressions())
            {
                if (s_Portraits.ContainsKey(avatar_id))
                {
                    s_Portraits[avatar_id].Increment();
                }
                else
                {
                    /* Enable camera */
                    s_PreviewCaptureCamera.SetActive(true);

                    /* Move camera infront of head */
                    var head_height = descriptor.ViewPosition.y;
                    var head = avatar.transform.position + new Vector3(0, head_height, 0);
                    var target = head + avatar.transform.forward * 0.3f;
                    var camera = s_PreviewCaptureCamera.GetComponent<Camera>();
                    camera.transform.position = target;
                    camera.transform.LookAt(head);
                    camera.cullingMask = 1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("PlayerLocal");
                    camera.orthographicSize = head_height / 8;

                    /* Set render target */
                    var currentRT = RenderTexture.active;
                    RenderTexture.active = camera.targetTexture;

                    /* Render the camera's view */
                    camera.Render();

                    /* Make a new texture and read the active Render Texture into it */
                    var image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGBA32, false, true);
                    image.name = $"{avatar_id} portrait";
                    image.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
                    image.Apply();
                    image.hideFlags = HideFlags.DontUnloadUnusedAsset;

                    /* Replace the original active Render Texture */
                    RenderTexture.active = currentRT;

                    /* Store image */
                    s_Portraits.Add(avatar_id, new RefCountedObject<Texture2D>(image));

                    /* Disable camera again */
                    s_PreviewCaptureCamera.SetActive(false);
                }
            }
        }

        private static IEnumerator RegisterJoinLeaveNotifier()
        {
            while (NetworkManager.field_Internal_Static_NetworkManager_0 == null) yield return new WaitForSeconds(1f);

            NetworkManager.field_Internal_Static_NetworkManager_0.field_Internal_VRCEventDelegate_1_Player_0.field_Private_HashSet_1_UnityAction_1_T_0.Add((Action<Player>)OnPlayerJoined);
            NetworkManager.field_Internal_Static_NetworkManager_0.field_Internal_VRCEventDelegate_1_Player_1.field_Private_HashSet_1_UnityAction_1_T_0.Add((Action<Player>)OnPlayerLeft);
        }

        private static void OnPlayerJoined(Player player)
        {
            WorldAudio.OnPlayerJoined(player.prop_VRCPlayerApi_0);
        }

        private static void OnPlayerLeft(Player player)
        {
            WorldAudio.OnPlayerLeft(player.prop_VRCPlayerApi_0);
        }

        private void MainMenu()
        {
            var settings_menu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
            settings_menu.AddHeader("World Cleanup");

            /* Light shadows */
            if (s_Lights.Count() > 0)
            {
                settings_menu.AddButtonToggleListItem("Shadows", $"Lights: {s_Lights.Count()}", () =>
                {
                    var shadows_menu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
                    foreach (var light in s_Lights)
                        shadows_menu.AddDropdownListItem(light.Item1.name, typeof(LightShadows), (state) => { light.Item1.shadows = (LightShadows)state; }, (int)light.Item1.shadows);
                    shadows_menu.AddSimpleButton("Back", MainMenu);
                    shadows_menu.Show();
                }, (restore) =>
                {
                    foreach (var (light, original) in s_Lights)
                        light.shadows = restore ? original : LightShadows.None;
                    Settings.s_DisableLights.Value = !restore;
                }, () => !Settings.s_DisableLights.Value, false);
            }
            else
            {
                settings_menu.AddLabel("No lights found on this map");
            }

            /* Post Processing */
            if (s_PostProcessingVolumes.Count() > 0)
            {
                settings_menu.AddButtonToggleListItem("Post Processing", $"Volumes: {s_PostProcessingVolumes.Count()}", () =>
                {
                    var pp_menu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
                    foreach (var volume in s_PostProcessingVolumes)
                        pp_menu.AddToggleListItem(volume.Item1.name, (state) => { volume.Item1.gameObject.active = state; }, () => volume.Item1.gameObject.active, true);
                    pp_menu.AddSimpleButton("Back", MainMenu);
                    pp_menu.Show();
                }, (restore) =>
                {
                    foreach (var (volume, original) in s_PostProcessingVolumes)
                        volume.gameObject.active = restore && original;
                    Settings.s_DisablePostProcessing.Value = !restore;
                }, () => !Settings.s_DisablePostProcessing.Value, false);
            }
            else
            {
                settings_menu.AddLabel("No Post Processing found on this map");
            }

            /* Mirrors */
            if (s_Mirrors.Count() > 0)
            {
                settings_menu.AddButtonToggleListItem("Mirror", $"Mirrors: {s_Mirrors.Count()}", () =>
                {
                    var mirror_menu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
                    foreach (var mirror in s_Mirrors)
                        mirror_menu.AddToggleListItem(mirror.name, (state) => { mirror.enabled = state; }, () => mirror.enabled, true);
                    mirror_menu.AddSimpleButton("Back", MainMenu);
                    mirror_menu.Show();
                }, (enable) =>
                {
                    foreach (var mirror in s_Mirrors)
                        mirror.enabled = enable;
                    Settings.s_DisableMirrors.Value = !enable;
                }, () => !Settings.s_DisableMirrors.Value, false);
            }
            else
            {
                settings_menu.AddLabel("No Mirrors found on this map");
            }

            /* PlayerMods */
            settings_menu.AddSimpleButton("Player Mods", () =>
            {
                var player = Networking.LocalPlayer;

                var player_menu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
                player_menu.AddHeader("Player Mod Settings");

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
            settings_menu.AddSliderListItem("Update interval (0-3s)", (value) => { Settings.s_UpdateInterval.Value = value; }, () => Settings.s_UpdateInterval.Value, 0f, 3f);

            settings_menu.AddSimpleButton("Back", settings_menu.Hide);

            settings_menu.Show();
        }

        private void OnUserQuickMenu()
        {
            var player = VRC.DataModel.UserSelectionManager.field_Private_Static_UserSelectionManager_0?.field_Private_APIUser_1;
            if (player == null)
                return;

            AvatarList(player.displayName, true);
        }

        private void PlayerList()
        {
            /* Filter inactive avatar objects */
            s_PlayerList = s_PlayerList.Where(o => o.Value).ToDictionary(o => o.Key, o => o.Value);

            var player_list = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
            player_list.AddHeader("Player List");

            foreach (var entry in s_PlayerList)
                player_list.AddSimpleButton(entry.Key, () => { AvatarList(entry.Key, false); });

            player_list.AddSimpleButton("Back", () => { player_list.Hide(); /* MainMenu(); */ });
            player_list.Show();
        }

        private void AvatarList(string player_name, bool close_on_exit)
        {
            var avatar = s_PlayerList[player_name];
            if (!avatar)
                return;

            var manager = avatar.GetComponentInParent<VRCAvatarManager>();

            var avatar_list = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
            avatar_list.AddHeader(player_name);

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
                if (smr.Count() > 0)
                {
                    avatar_list.AddSimpleButton($"SkinnedMeshRenderer: {smr.Count()}", () =>
                    {
                        var mesh_list = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
                        mesh_list.AddHeader("SkinnedMeshRenderers");
                        foreach (var renderer in smr)
                        {
                            void set_value(bool state) { renderer.gameObject.active = renderer.enabled = state; }
                            bool get_value() { return renderer.enabled && renderer.gameObject.active; };

                            var skinned_mesh_renderer = renderer.Cast<SkinnedMeshRenderer>();
                            var shared_mesh = skinned_mesh_renderer.sharedMesh;
                            if (shared_mesh == null)
                            {
                                MelonLogger.Msg(ConsoleColor.Red, $"{player_name} misses mesh on SkinnedMeshRenderer {renderer.gameObject.name}!");
                                continue;
                            }

                            var count = shared_mesh.blendShapeCount;
                            if (count > 0)
                            {
                                mesh_list.AddButtonToggleListItem(
                                    renderer.gameObject.name,
                                    $"Blendshapes: {count}",
                                    () => { BlendShapeList(skinned_mesh_renderer, mesh_list); },
                                    set_value,
                                    get_value,
                                    true
                                );
                            }
                            else
                            {
                                mesh_list.AddToggleListItem(renderer.gameObject.name, set_value, get_value, true);
                            }
                        }
                        mesh_list.AddSimpleButton("Back", () => { mesh_list.Hide(); AvatarList(player_name, close_on_exit); });
                        mesh_list.Show();
                    });
                }

                void ShowGenericRendererToggleList(string type, IEnumerable<Renderer> list)
                {
                    var mesh_list = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
                    if (type != null)
                        mesh_list.AddHeader(type);
                    foreach (var mesh in list)
                    {
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

                avatar_list.AddSimpleButton("Material Toggles", () =>
                {
                    var list = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
                    foreach (var _renderer in renderers)
                    {
                        var renderer = _renderer;
                        var materials = renderer.materials;
                        var materialsBackup = renderer.materials;
                        list.AddHeader($"{renderer.name}:");
                        for (int _i = 0; _i < renderer.materials.Length; _i++)
                        {
                            var i = _i;
                            var material = renderer.materials[i];
                            list.AddToggleListItem(material.name, (state) =>
                            {
                                materials[i] = state ? UiExpansion.InvisibleMaterial : material;
                                renderer.materials = materials;
                            }, () => false, false);
                        }
                        list.AddSimpleButton("Reset", () =>
                        {
                            renderer.materials = materialsBackup;
                            materials = renderer.materials;
                        });
                    }
                    list.AddSimpleButton("Back", () => { list.Hide(); AvatarList(player_name, close_on_exit); });
                    list.Show();
                });
            }
            {
                /* Ignore SDK2 & avatars w/o custom expressions */
                if (manager.HasCustomExpressions())
                {
                    var parameters = manager.GetAllAvatarParameters();
                    var filtered = Parameters.FilterDefaultParameters(parameters);

                    var avatar_descriptor = manager.prop_VRCAvatarDescriptor_0;

                    avatar_list.AddSimpleButton($"Parameter Menu", () =>
                    {
                        /* Unlock all parameters to prevent state machine tomfoolery */
                        foreach (var parameter in parameters)
                            parameter.Unlock();

                        AvatarParameter FindParameter(string name)
                        {
                            foreach (var parameter in parameters)
                                if (parameter.field_Private_String_0 == name)
                                    return parameter;
                            return null;
                        }

                        void ExpressionSubmenu(ICustomShowableLayoutedMenu list, VRCExpressionsMenu expressions_menu)
                        {
                            foreach (var control in expressions_menu.controls)
                            {
                                switch (control.type)
                                {
                                    case VRCExpressionsMenu.Control.ControlType.Button:
                                    /* Note: Action Menu "Buttons" are actually Toggles */
                                    /*       that set on press and revert on release.   */
                                    /* TODO: Add proper implementation.                 */
                                    case VRCExpressionsMenu.Control.ControlType.Toggle:
                                        {
                                            var param = FindParameter(control.parameter.name);
                                            var current_value = param.GetValue();
                                            var default_value = avatar_descriptor.expressionParameters.FindParameter(control.parameter.name)?.defaultValue ?? 0f;
                                            var target_value = control.value;
                                            void SetIntFloat(bool state) => param.SetValue(state ? target_value : default_value);
                                            void SetBool(bool state) => param.SetValue(state ? 1f : 0f);

                                            list.AddToggleListItem(
                                                control.TruncatedName(),
                                                param.field_Public_ParameterType_0 == AvatarParameter.ParameterType.Bool ? SetBool : SetIntFloat,
                                                () => { return current_value == target_value; },
                                                true);
                                            break;
                                        }

                                    case VRCExpressionsMenu.Control.ControlType.SubMenu:
                                        {
                                            list.AddSimpleButton(control.TruncatedName(), () =>
                                            {
                                                var sub_menu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
                                                sub_menu.AddHeader(control.TruncatedName());

                                                ExpressionSubmenu(sub_menu, control.subMenu);

                                                sub_menu.AddSimpleButton("Back", () => { sub_menu.Hide(); list.Show(); });
                                                sub_menu.Show();
                                            });
                                            break;
                                        }

                                    case VRCExpressionsMenu.Control.ControlType.RadialPuppet:
                                        {
                                            var param = FindParameter(control.subParameters[0].name);
                                            list.AddSliderListItem(control.TruncatedName(), param.SetValue, param.GetValue, 0, 1);
                                            break;
                                        }

                                    default:
                                        list.AddLabel($"\n\n{control.TruncatedName()}: {control.type} unsupported");
                                        break;
                                }
                            }
                        }
                        var menu_list = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);

                        ExpressionSubmenu(menu_list, avatar_descriptor.expressionsMenu);

                        menu_list.AddSimpleButton($"Raw Parameters: {filtered.Count}", () =>
                        {
                            var parameter_list = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
                            foreach (var parameter in filtered)
                            {
                                var name = parameter.TruncatedName();
                                var type = parameter.field_Public_ParameterType_0;
                                switch (type)
                                {
                                    case AvatarParameter.ParameterType.Bool:
                                        parameter_list.AddToggleListItem(name, parameter.SetBoolProperty, () => parameter.prop_Boolean_1, true);
                                        break;

                                    case AvatarParameter.ParameterType.Int:
                                        parameter_list.AddIntDiffListItem(name, parameter.SetIntProperty, () => parameter.prop_Int32_1);
                                        break;

                                    case AvatarParameter.ParameterType.Float:
                                        parameter_list.AddSliderListItem(name, parameter.SetFloatProperty, () => parameter.prop_Single_1);
                                        break;

                                    default:
                                        MelonLogger.Msg(ConsoleColor.Red, $"Unsupported [{type}]: {name}");
                                        break;
                                }
                            }
                            parameter_list.AddSimpleButton("Back", () => { parameter_list.Hide(); AvatarList(player_name, close_on_exit); });
                            parameter_list.Show();
                        });

                        menu_list.AddSimpleButton("Back", () => { menu_list.Hide(); AvatarList(player_name, close_on_exit); });
                        menu_list.Show();
                    });
                }
            }
            avatar_list.AddSimpleButton("Save Config", () => { Parameters.StoreParameters(manager); });
            avatar_list.AddSimpleButton("Reset", () => { Parameters.ResetParameters(manager); });
            avatar_list.AddSimpleButton("Back", () => { avatar_list.Hide(); if (!close_on_exit) PlayerList(); });
            avatar_list.Show();
        }

        private void BlendShapeList(SkinnedMeshRenderer renderer, ICustomShowableLayoutedMenu parent)
        {
            var blendshape_list = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);

            var shared_mesh = renderer.sharedMesh;
            foreach (var i in Enumerable.Range(0, shared_mesh.blendShapeCount))
                blendshape_list.AddSliderListItem(shared_mesh.GetBlendShapeName(i), (value) => { renderer.SetBlendShapeWeight(i, value); }, () => renderer.GetBlendShapeWeight(i), 0f, 100f);

            blendshape_list.AddSimpleButton("Back", () => { blendshape_list.Hide(); parent.Show(); });
            blendshape_list.Show();
        }
    }
}
