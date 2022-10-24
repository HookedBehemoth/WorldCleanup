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

/* Note: This is also an issue with the official menu    */
/* Note: While loading a 400KiB string isn't really a    */
/*       problem, trying to render that to a texture is. */

using AvatarParameterAccess = ObjectPublicIAnimParameterAccessAnStInObLi1BoObSiAcUnique;

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

        public static string TruncatedName(this AvatarParameterAccess param)
            => param.field_Private_String_0?.Truncate(32);
    }
}
