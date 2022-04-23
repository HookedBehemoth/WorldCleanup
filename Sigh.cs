/* Note: This is also an issue with the official menu    */
/* Note: While loading a 400KiB string isn't really a    */
/*       problem, trying to render that to a texture is. */

namespace WorldCleanup
{
    public static class ZipBombExtend
    {
        public static string Truncate(this string value, int max_length)
        {
            if (!string.IsNullOrEmpty(value) && value.Length > max_length)
                return value.Substring(0, max_length) + "…";
            return value;
        }

        public static string TrucatedName(this VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.Parameter param)
            => param.name?.Truncate(32);

        public static string TruncatedName(this VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control control)
            => control.name?.Truncate(32);

        public static string TruncatedName(this VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter param)
            => param.name?.Truncate(32);

        public static string TruncatedName(this VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Label label)
            => label.name?.Truncate(32);

        public static string TruncatedName(this VRC.Playables.AvatarParameter param)
            => param.field_Private_String_0?.Truncate(32);
    }
}
