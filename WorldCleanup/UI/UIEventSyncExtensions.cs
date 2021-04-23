/* Courtesy of HonorableDaniel from the Unity Support forum. */
/* Proper implementation in mainline Unity with 2019.1+ */
using UnityEngine.UI;

public static class UIEventSyncExtensions {
    static readonly Slider.SliderEvent emptySliderEvent = new Slider.SliderEvent();
    public static void SetValueWithoutNotify(this Slider instance, float value) {
        var originalEvent = instance.onValueChanged;
        instance.onValueChanged = emptySliderEvent;
        instance.value = value;
        instance.onValueChanged = originalEvent;
    }

    static readonly Toggle.ToggleEvent emptyToggleEvent = new Toggle.ToggleEvent();
    public static void SetIsOnWithoutNotify(this Toggle instance, bool value) {
        var originalEvent = instance.onValueChanged;
        instance.onValueChanged = emptyToggleEvent;
        instance.isOn = value;
        instance.onValueChanged = originalEvent;
    }

    static readonly Dropdown.DropdownEvent emptyDropdownFieldEvent = new Dropdown.DropdownEvent();
    public static void SetValue(this Dropdown instance, int value) {
        var originalEvent = instance.onValueChanged;
        instance.onValueChanged = emptyDropdownFieldEvent;
        instance.value = value;
        instance.onValueChanged = originalEvent;
    }
}
