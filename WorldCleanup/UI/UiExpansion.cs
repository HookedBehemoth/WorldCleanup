using System;
using UnityEngine;
using UnityEngine.UI;
using UIExpansionKit.API;
using Il2CppSystem.Collections.Generic;
using UnhollowerRuntimeLib;
using WorldCleanup.UI;

namespace WorldCleanup {
    static class UiExpansion {
        public static GameObject IntChanger, FloatSlider, ButtonToggleItem, ComponentToggle, DropdownListItem;

        public static void LoadUiObjects() {
            ClassInjector.RegisterTypeInIl2Cpp<Updater>();

            IntChanger = Assets.LoadGameObject("Assets/UI/IntChanger.prefab");
            FloatSlider = Assets.LoadGameObject("Assets/UI/FloatSlider.prefab");
            ButtonToggleItem = Assets.LoadGameObject("Assets/UI/ButtonToggleItem.prefab");
            ComponentToggle = Assets.LoadGameObject("Assets/UI/ComponentToggle.prefab");
            DropdownListItem = Assets.LoadGameObject("Assets/UI/DropDown.prefab");
        }

        public static void AddIntDiffListItem(this ICustomShowableLayoutedMenu list, string description, Action<int> set_value, Func<int> get_value) {
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

        public static void AddFloatDiffListItem(this ICustomShowableLayoutedMenu list, string description, Action<float> set_value, Func<float> get_value) {
            AddIntDiffListItem(list, description, (val) => set_value((float)val), () => { return (int)get_value(); });
        }

        public static void AddSliderListItem(this ICustomShowableLayoutedMenu list, string description, Action<float> set_value, Func<float> get_value, float min = -1.0f, float max = 1.0f) {
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

        public static void AddToggleListItem(this ICustomShowableLayoutedMenu list, string description, Action<bool> set_value, Func<bool> get_value, bool update) {
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

        public static void AddButtonToggleListItem(this ICustomShowableLayoutedMenu list, string description, string submenu_name, Action on_button, Action<bool> set_value, Func<bool> get_value, bool update) {
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

        public static void AddDropdownListItem(this ICustomShowableLayoutedMenu list, string description, Type values, Action<int> on_change, int initial_state) {
            list.AddCustomButton(DropdownListItem, (GameObject obj) => {
                /* Add description text */
                obj.transform.GetChild(0).GetComponent<Text>().text = description;

                /* Configure Enum Dropdown */
                var dropdown = obj.transform.GetChild(1).GetComponent<Dropdown>();
                var options = new List<string> { };
                foreach (var name in Enum.GetNames(values))
                    options.Add(name);
                dropdown.AddOptions(options);
                dropdown.value = initial_state;
                dropdown.onValueChanged.AddListener(on_change);
            });
        }
    }
}
