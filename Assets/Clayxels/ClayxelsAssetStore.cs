
#if UNITY_EDITOR // exclude from build

using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class ClayxelsAssetStore {
    static ClayxelsAssetStore(){
        PlayerSettings.allowUnsafeCode = true;

        string currDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup( EditorUserBuildSettings.selectedBuildTargetGroup );
        if(!currDefines.Contains("CLAYXELS_ONEUP")){
    		PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, "CLAYXELS_ONEUP;" + currDefines);
    	}
    }
}

#endif