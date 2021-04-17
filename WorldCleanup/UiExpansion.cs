using System;
using UnityEngine;
using UnityEngine.UI;
using UIExpansionKit.API;
using Il2CppSystem.Collections.Generic;

namespace WorldCleanup {
    class UiExpansion {
        public static GameObject IntChanger, FloatSlider, ButtonToggleItem, ComponentToggle, DropdownListItem;

        public static void LoadUiObjects() {
            IntChanger = Assets.LoadGameObject("Assets/UI/IntChanger.prefab");
            FloatSlider = Assets.LoadGameObject("Assets/UI/FloatSlider.prefab");
            ButtonToggleItem = Assets.LoadGameObject("Assets/UI/ButtonToggleItem.prefab");
            ComponentToggle = Assets.LoadGameObject("Assets/UI/ComponentToggle.prefab");
            DropdownListItem = Assets.LoadGameObject("Assets/UI/DropDown.prefab");
        }

        public static void AddIntDiffListItem(ICustomShowableLayoutedMenu list, string name, Action<int> cb, int value = 0) {
            list.AddCustomButton(IntChanger, (GameObject obj) => {
                var text_field = obj.transform.GetChild(0).GetComponent<Text>();
                text_field.text = value.ToString();

                Action ConstructChangeCallback(int diff) {
                    return () => {
                        value += diff;
                        text_field.text = value.ToString();
                        cb.Invoke(value);
                    };
                }

                obj.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(ConstructChangeCallback(-1));
                obj.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(ConstructChangeCallback(1));

                obj.transform.GetChild(3).GetComponent<Text>().text = name;
            });
        }

        public static void AddFloatDiffListItem(ICustomShowableLayoutedMenu list, string name, Action<float> cb, float value = 0) {
            AddIntDiffListItem(list, name, (val) => cb((float)val), (int)value);
        }

        public static void AddFloatListItem(ICustomShowableLayoutedMenu list, string name, Action<float> cb, float value = 0.0f, float min = -1.0f, float max = 1.0f) {
            list.AddCustomButton(FloatSlider, (GameObject obj) => {
                var slider = obj.transform.GetChild(0).GetComponent<Slider>();
                slider.minValue = min;
                slider.maxValue = max;
                slider.value = value;
                slider.onValueChanged.AddListener(cb);
                obj.transform.GetChild(1).GetComponent<Text>().text = name;
            });
        }

        public static void AddToggleListItem(ICustomShowableLayoutedMenu list, string name, Action<bool> on_click, bool initial_state) {
            list.AddCustomButton(ComponentToggle, (GameObject obj) => {
                obj.transform.GetChild(0).GetComponent<Text>().text = name;
                var toggle = obj.transform.GetChild(1).GetComponent<Toggle>();
                toggle.isOn = initial_state;
                toggle.onValueChanged.AddListener(on_click);
            });
        }

        public static void AddButtonToggleListItem(ICustomShowableLayoutedMenu list, string description, string submenu_name, Action on_button, Action<bool> on_toggle, bool initial_state) {
            list.AddCustomButton(ButtonToggleItem, (GameObject obj) => {
                obj.transform.GetChild(0).GetComponent<Text>().text = description;

                var button = obj.transform.GetChild(1);
                button.GetComponentInChildren<Text>().text = submenu_name;
                button.GetComponent<Button>().onClick.AddListener(on_button);

                var toggle = obj.transform.GetChild(2).GetComponent<Toggle>();
                toggle.isOn = initial_state;
                toggle.onValueChanged.AddListener(on_toggle);
            });
        }

        public static void AddDropdownListItem(ICustomShowableLayoutedMenu list, string description, Type values, Action<int> on_change, int initial_state) {
            list.AddCustomButton(DropdownListItem, (GameObject obj) => {
                obj.transform.GetChild(0).GetComponent<Text>().text = description;

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
