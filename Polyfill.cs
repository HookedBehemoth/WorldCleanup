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

global using VRCAvatarManager = Il2Cpp.MonoBehaviourPublicINetworkReadyReceiverSiGaObGaStObBoGaLiBoUnique;
global using Player = Il2Cpp.MonoBehaviourPublicAPOb_vOb_pBo_UObBoVRUnique;
global using VRCPlayer = Il2Cpp.MonoBehaviour1PublicOb_pOb_s_pBoGaOb_pStUnique;
global using AvatarPlayableController = Il2Cpp.MonoBehaviour1PublicAcBoAcHaBo1AcInFu2Unique;
global using AvatarParameterAccess = Il2Cpp.ObjectPublicIAnimParameterAccessObStInBoSiAcInBoOb2Unique;
global using AvatarParameterType = Il2Cpp.ObjectPublicIAnimParameterAccessObStInBoSiAcInBoOb2Unique.EnumNPublicSealedvaUnBoInFl5vUnique;

global using static Polyfill;

using UnityEngine;
using Il2CppSystem.Collections.Generic;

using Il2CppVRC.SDKBase;

public static class Polyfill {  
    public static void RegisterAvatarCallback(Il2CppSystem.Action<Player, GameObject, VRC_AvatarDescriptor> callback) {
        VRCAvatarManager.field_Private_Static_Action_3_MonoBehaviourPublicAPOb_vOb_pBo_UObBoVRUnique_GameObject_VRC_AvatarDescriptor_0 += callback;
    }

    public static VRCAvatarManager GetVRCAvatarManager(this VRCPlayer _this) {
        return _this.prop_MonoBehaviourPublicINetworkReadyReceiverSiGaObGaStObBoGaLiBoUnique_0;
    }

    public static AvatarPlayableController GetAvatarPlayableController(this VRCAvatarManager _this) {
        return _this.field_Private_MonoBehaviour1PublicAcBoAcHaBo1AcInFu2Unique_0;
    }

    public static Dictionary<int, AvatarParameterAccess> GetParameters(this AvatarPlayableController _this) {
        return _this.field_Private_Dictionary_2_Int32_ObjectPublicIAnimParameterAccessObStInBoSiAcInBoOb2Unique_0;
    }

    public static AvatarParameterType GetAvatarParameterType(this AvatarParameterAccess _this) {
        return _this.field_Public_EnumNPublicSealedvaUnBoInFl5vUnique_0;
    }
}