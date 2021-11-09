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
using UnityEngine;
using UnityEngine.UI;
using UIExpansionKit.API;
using UnhollowerRuntimeLib;
using WorldCleanup.UI;
using MelonLoader;
using System.Collections;

namespace WorldCleanup {
    static class UiExpansion {
        private static GameObject IntChanger, FloatSlider, ButtonToggleItem, ComponentToggle, DropdownListItem, Header, CategoryHeader;
        public static Texture2D SaveIcon, LockClosedIcon, LockOpenIcon;
        public static GameObject PreviewCamera;

        public static void LoadUiObjects() {
            ClassInjector.RegisterTypeInIl2Cpp<Updater>();

            /* Load async to avoid race with UI Expansion kit */
            MelonCoroutines.Start(LoadUiElements());
        }

        private static IEnumerator LoadUiElements() {
            /* Get UIExpansionKit GameObject parent */
            GameObject parent = null;
            do {
                yield return new WaitForSeconds(1f);
                parent = GameObject.Find("UserInterface/QuickMenu/ModUiPreloadedBundleContents");
            } while (parent == null);

            /* Copy font reference */
            var noto_sans = parent.GetComponentInChildren<Text>()?.font;

            /* Load Asset bundle */
            AssetBundle asset_bundle = null;
            using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("WorldCleanup.mod.assetbundle"))
            using (var tempStream = new System.IO.MemoryStream((int)stream.Length)) {
                stream.CopyTo(tempStream);

                asset_bundle = AssetBundle.LoadFromMemory(tempStream.ToArray());
                asset_bundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            }

            GameObject LoadUiElement(string str) {
                var bundle_object = asset_bundle.LoadAsset<GameObject>(str);

                /* Attach it to QuickMenu to inherit render queue changes */
                var instantiated_object = GameObject.Instantiate(bundle_object, parent.transform);
                instantiated_object.SetActive(true);
                instantiated_object.hideFlags |= HideFlags.DontUnloadUnusedAsset;

                return instantiated_object;
            }

            IntChanger = LoadUiElement("Assets/UI/IntChanger.prefab");
            FloatSlider = LoadUiElement("Assets/UI/FloatSlider.prefab");
            ButtonToggleItem = LoadUiElement("Assets/UI/ButtonToggleItem.prefab");
            ComponentToggle = LoadUiElement("Assets/UI/ComponentToggle.prefab");
            DropdownListItem = LoadUiElement("Assets/UI/DropDown.prefab");
            Header = LoadUiElement("Header");
            CategoryHeader = LoadUiElement("CategoryHeader");

            T LoadGeneric<T>(string path) where T: UnityEngine.Object {
                var asset = asset_bundle.LoadAsset<T>(path);
                asset.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                return asset;
            }

            SaveIcon = LoadGeneric<Texture2D>("Assets/Sprite/floppy-disk_1f4be.png");
            LockClosedIcon = LoadGeneric<Texture2D>("Assets/Sprite/lock_1f512.png");
            LockOpenIcon = LoadGeneric<Texture2D>("Assets/Sprite/open-lock_1f513.png");

            PreviewCamera = LoadGeneric<GameObject>("Assets/Avatar Preview/AvatarPreviewCamera.prefab");
        }

        public static void AddIntDiffListItem(this ICustomLayoutedMenu list, string description, Action<int> set_value, Func<int> get_value) {
            list.AddCustomButton(IntChanger, (GameObject obj) => {
                /* Add description text */
                obj.transform.GetChild(0).GetComponent<Text>().text = description;

                /* Configure value field */
                var text_field = obj.transform.GetChild(1).GetComponent<Text>();
                text_field.text = get_value().ToString();

                /* Configure updater */
                var updater = obj.AddComponent<Updater>();
                updater.callback = () => { text_field.text = get_value().ToString(); };

                /* Configure buttons */
                Action ConstructChangeCallback(int diff) {
                    return () => {
                        var value = get_value() + diff;
                        set_value.Invoke(value);
                        text_field.text = value.ToString();
                    };
                }

                obj.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(ConstructChangeCallback(-1));
                obj.transform.GetChild(3).GetComponent<Button>().onClick.AddListener(ConstructChangeCallback(1));
            });
        }

        public static void AddFloatDiffListItem(this ICustomLayoutedMenu list, string description, Action<float> set_value, Func<float> get_value) {
            AddIntDiffListItem(list, description, (val) => set_value((float)val), () => { return (int)get_value(); });
        }

        public static void AddSliderListItem(this ICustomLayoutedMenu list, string description, Action<float> set_value, Func<float> get_value, float min = -1.0f, float max = 1.0f) {
            list.AddCustomButton(FloatSlider, (GameObject obj) => {
                /* Add description text */
                obj.transform.GetChild(0).GetComponent<Text>().text = description;

                /* Configure slider */
                var slider = obj.transform.GetChild(1).GetComponent<Slider>();
                slider.minValue = min;
                slider.maxValue = max;
                slider.value = get_value();
                slider.onValueChanged.AddListener(set_value);

                /* Configure updater */
                var updater = obj.AddComponent<Updater>();
                updater.callback = () => { slider.SetValueWithoutNotify(get_value()); };
            });
        }

        public static void AddToggleListItem(this ICustomLayoutedMenu list, string description, Action<bool> set_value, Func<bool> get_value, bool update) {
            list.AddCustomButton(ComponentToggle, (GameObject obj) => {
                /* Add description text */
                obj.transform.GetChild(0).GetComponent<Text>().text = description;

                /* Configure toggle */
                var toggle = obj.transform.GetChild(1).GetComponent<Toggle>();
                toggle.isOn = get_value();
                toggle.onValueChanged.AddListener(set_value);

                /* Add toggle updater script */
                if (update) {
                    var updater = obj.AddComponent<Updater>();
                    updater.callback = () => { toggle.SetIsOnWithoutNotify(get_value()); };
                }
            });
        }

        public static void AddButtonToggleListItem(this ICustomLayoutedMenu list, string description, string submenu_name, Action on_button, Action<bool> set_value, Func<bool> get_value, bool update) {
            list.AddCustomButton(ButtonToggleItem, (GameObject obj) => {
                /* Add description text */
                obj.transform.GetChild(0).GetComponent<Text>().text = description;

                /* Configure button */
                var button = obj.transform.GetChild(1);
                button.GetComponentInChildren<Text>().text = submenu_name;
                button.GetComponent<Button>().onClick.AddListener(on_button);

                /* Configure toggle */
                var toggle = obj.transform.GetChild(2).GetComponent<Toggle>();
                toggle.isOn = get_value();
                toggle.onValueChanged.AddListener(set_value);

                /* Configure updater */
                if (update) {
                    var updater = obj.AddComponent<Updater>();
                    updater.callback = () => { toggle.SetIsOnWithoutNotify(get_value()); };
                }
            });
        }

        public static void AddDropdownListItem(this ICustomLayoutedMenu list, string description, Type values, Action<int> on_change, int initial_state) {
            list.AddCustomButton(DropdownListItem, (GameObject obj) => {
                /* Add description text */
                obj.transform.GetChild(0).GetComponent<Text>().text = description;

                /* Configure Enum Dropdown */
                var dropdown = obj.transform.GetChild(1).GetComponent<Dropdown>();
                var options = new Il2CppSystem.Collections.Generic.List<string> { };
                foreach (var name in Enum.GetNames(values))
                    options.Add(name);
                dropdown.AddOptions(options);
                dropdown.value = initial_state;
                dropdown.onValueChanged.AddListener(on_change);
            });
        }

        private static void AddGenericHeader(this ICustomLayoutedMenu menu, GameObject layout, string header) {
            menu.AddCustomButton(layout, obj => {
                obj.GetComponentInChildren<Text>().text = header;
            });
        }

        public static void AddHeader(this ICustomLayoutedMenu menu, string header) {
            menu.AddGenericHeader(Header, header);
        }

        public static void AddCategoryHeader(this ICustomLayoutedMenu menu, string header) {
            menu.AddGenericHeader(CategoryHeader, header);
        }
    }
}
