using System;
using UnityEngine;
using UnityEngine.UI;
using UIExpansionKit.API;

namespace WorldCleanup
{
    class UiExpansion
    {
        private static AssetBundle s_AssetBundle;
        public static GameObject IntChanger, FloatSlider, ButtonToggleItem, ComponentToggle;

        public static void LoadUiObjects()
        {
            s_AssetBundle = Assets.LoadFromAssembly("WorldCleanup.mod.assetbundle");

            IntChanger = Assets.LoadGameObject(s_AssetBundle, "Assets/IntChanger.prefab");
            FloatSlider = Assets.LoadGameObject(s_AssetBundle, "Assets/FloatSlider.prefab");
            ButtonToggleItem = Assets.LoadGameObject(s_AssetBundle, "Assets/ButtonToggleItem.prefab");
            ComponentToggle = Assets.LoadGameObject(s_AssetBundle, "Assets/ComponentToggle.prefab");
        }

        public static void AddIntListItem(ICustomShowableLayoutedMenu list, string name, Action<int> cb, int value = 0)
        {
            list.AddCustomButton(IntChanger, (GameObject obj) =>
            {
                var text_field = obj.transform.GetChild(0).GetComponent<Text>();
                text_field.text = value.ToString();

                Action ConstructChangeCallback(int diff)
                {
                    return () =>
                    {
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

        public static void AddFloatListItem(ICustomShowableLayoutedMenu list, string name, Action<float> cb, float value = 0.0f, float min = -1.0f, float max = 1.0f)
        {
            list.AddCustomButton(FloatSlider, (GameObject obj) =>
            {
                var slider = obj.transform.GetChild(0).GetComponent<Slider>();
                slider.minValue = min;
                slider.maxValue = max;
                slider.value = value;
                slider.onValueChanged.AddListener(cb);
                obj.transform.GetChild(1).GetComponent<Text>().text = name;
            });
        }

        public static void AddToggleListItem(ICustomShowableLayoutedMenu list, string name, Action<bool> on_click, Func<bool> get_initial_state)
        {
            list.AddCustomButton(ComponentToggle, (GameObject obj) =>
            {
                obj.transform.GetChild(0).GetComponent<Text>().text = name;
                var toggle = obj.transform.GetChild(1).GetComponent<Toggle>();
                toggle.isOn = get_initial_state.Invoke();
                toggle.onValueChanged.AddListener(on_click);
            });
        }

        public static void AddButtonToggleListItem(ICustomShowableLayoutedMenu list, string description, string submenu_name, Action on_button, Action<bool> on_toggle, Func<bool> get_initial_state)
        {
            list.AddCustomButton(ButtonToggleItem, (GameObject obj) =>
            {
                obj.transform.GetChild(0).GetComponent<Text>().text = description;

                var button = obj.transform.GetChild(1);
                button.GetComponentInChildren<Text>().text = submenu_name;
                button.GetComponent<Button>().onClick.AddListener(on_button);

                var toggle = obj.transform.GetChild(2).GetComponent<Toggle>();
                toggle.isOn = get_initial_state();
                toggle.onValueChanged.AddListener(on_toggle);
            });
        }
    }
}
