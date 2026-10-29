using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot migration for the player refactor (plain-class modules + profile ScriptableObjects).
/// Creates the three profile assets seeded with the values tuned in Player Prefab (the former
/// PlayerController / PlayerDash / PlayerVisuals serialized fields), then rewires the prefab:
/// assigns the profile refs, points dashEchoParent at the old PlayerDash echo parent, migrates
/// the router's nested touch config, and drops the now-deleted PlayerDash / PlayerKeyboardInput
/// components. Run once via Tools > StopMotion > Migrate Player Profiles (idempotent).
/// </summary>
public static class PlayerProfileMigrator
{
    const string PrefabPath = "Assets/Prefab/PlayerPrefab/Player Prefab.prefab";
    const string ProfilesFolder = "Assets/Prefab/PlayerPrefab/Profiles";

    [MenuItem("Tools/StopMotion/Migrate Player Profiles")]
    public static void Migrate()
    {
        if (!System.IO.Directory.Exists(ProfilesFolder))
            System.IO.Directory.CreateDirectory(ProfilesFolder);

        CreateLocomotionProfile();
        CreateDashProfile();
        CreateVisualProfile();
        AssetDatabase.SaveAssets();

        RewirePrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<b><color=cyan>[Migration]</color></b> Profile assets created + Player Prefab rewired. Review the Player in the Inspector before play-testing.");
    }

    static void CreateLocomotionProfile()
    {
        var profile = LoadOrCreate<PlayerLocomotionProfileSO>(ProfilesFolder + "/PlayerLocomotionProfile.asset");
        var so = new SerializedObject(profile);

        // Migrated from Player Prefab (PlayerController block)
        SetFloat(so, "maxSpeed", 8f);                    // was maxSpeed: 8
        SetFloat(so, "acceleration", 60f);               // was acceleration: 60
        SetFloat(so, "deceleration", 15f);               // was deceleration: 15
        SetFloat(so, "maxJumpHeight", 3.5f);             // was maxJumpHeight: 3.5
        SetFloat(so, "timeToApex", 0.5f);                // was timeToApex: 0.5
        SetFloat(so, "fallGravityMultiplier", 2f);       // was fallGravityMultiplier: 2
        SetFloat(so, "jumpCutMultiplier", 0.5f);         // was jumpCutMultiplier: 0.5
        SetFloat(so, "coyoteTime", 0.1f);                // was coyoteTime: 0.1
        SetFloat(so, "jumpBufferTime", 0.2f);            // was jumpBufferTime: 0.2
        SetFloat(so, "landingMinAirTime", 0.3f);         // was landingMinAirTime: 0.3

        // GroundCheckConfig nested — was groundCheckWidth: 0.6, groundCheckHeight: 0.1, groundCheckRadius: 0.2
        var ground = so.FindProperty("groundCheck");
        SetFloatRel(ground, "checkWidth", 0.6f);
        SetFloatRel(ground, "checkHeight", 0.1f);
        SetFloatRel(ground, "gizmoRadius", 0.2f);

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(profile);
    }

    static void CreateDashProfile()
    {
        var profile = LoadOrCreate<PlayerDashProfileSO>(ProfilesFolder + "/PlayerDashProfile.asset");
        var so = new SerializedObject(profile);

        // Migrated from Player Prefab (PlayerDash block)
        SetBool(so, "canDash", true);                    // was canDash: 1 (component was toggled off — see summary note)
        SetFloat(so, "dashSpeed", 20f);                  // was dashSpeed: 20
        SetFloat(so, "dashDuration", 0.2f);              // was dashDuration: 0.2
        SetFloat(so, "dashCoolDownTime", 1f);            // was dashCoolDownTime: 1
        SetInt(so, "maxDashCollectableCount", 3);        // was maxDashCollectableCount: 3
        SetInt(so, "startingDashCharges", 3);            // full-tank start (was the testDashCount: 10 temp hack)
        SetFloat(so, "echoSpawnInterval", 0.04f);        // was echoSpawnInterval: 0.04
        SetInt(so, "echoPoolSize", 5);                   // was poolSize: 5

        // echoPrefab asset ref — migrated from the old serialized {guid: 82c3da09b139c4c66bb176f161e51c13}
        var echoPath = AssetDatabase.GUIDToAssetPath("82c3da09b139c4c66bb176f161e51c13");
        if (!string.IsNullOrEmpty(echoPath))
        {
            var echoPrefab = AssetDatabase.LoadAssetAtPath<DashEchoEffect>(echoPath);
            var echoProp = so.FindProperty("echoPrefab");
            if (echoPrefab != null && echoProp != null) echoProp.objectReferenceValue = echoPrefab;
        }
        else
        {
            Debug.LogWarning("<b><color=yellow>[Migration]</color></b> Dash echo prefab (guid 82c3da09b139c4c66bb176f161e51c13) not found — assign it on the Dash profile asset.");
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(profile);
    }

    static void CreateVisualProfile()
    {
        var profile = LoadOrCreate<PlayerVisualProfileSO>(ProfilesFolder + "/PlayerVisualProfile.asset");
        var so = new SerializedObject(profile);

        // Migrated from Player Prefab (PlayerVisuals block)
        SetFloat(so, "dashSquashAmount", 0.8f);          // was dashSquashAmount: 0.8
        SetFloat(so, "dashSquashDuration", 0.2f);        // was dashSquashDuration: 0.2
        SetFloat(so, "jumpUpSquashAmount", 1.5f);        // was jumpUpSquashAmount: 1.5
        SetFloat(so, "jumpUpSquashDuration", 0.2f);      // was jumpUpSquashDuration: 0.2
        SetFloat(so, "landSquashAmount", 0.7f);          // was landSquashAmount: 0.7
        SetFloat(so, "landSquashDuration", 0.2f);        // was landSquashDuration: 0.2
        SetFloat(so, "blinkDelay", 2f);                  // was blinkDelay: 2
        SetFloat(so, "blinkDuration", 0.5f);             // was blinkDuration: 0.5
        SetFloat(so, "idleDelay", 2f);                   // was idleDelay: 2
        SetFloat(so, "keyFadeDuration", 0.35f);          // was keyFadeDuration: 0.35
        SetInt(so, "smokePoolSize", 6);                  // was smokePoolSize default: 6

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(profile);
    }

    static void RewirePrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var controller = root.GetComponentInChildren<PlayerController>(true);
            if (controller != null)
            {
                var so = new SerializedObject(controller);
                AssignRef(so, "locomotionProfile", "PlayerLocomotionProfile");
                AssignRef(so, "dashProfile", "PlayerDashProfile");
                // dashEchoParent — migrated from old PlayerDash.parentTransform ("Pool" child of the player root)
                var oldEchoParent = root.transform.Find("Pool");
                if (oldEchoParent != null)
                {
                    var parentProp = so.FindProperty("dashEchoParent");
                    if (parentProp != null) parentProp.objectReferenceValue = oldEchoParent;
                }
                else
                {
                    Debug.LogWarning("<b><color=yellow>[Migration]</color></b> 'Pool' child not found — assign dashEchoParent on PlayerController manually.");
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogError("<b><color=yellow>[Migration]</color></b> PlayerController not found in the prefab!");
            }

            var visuals = root.GetComponentInChildren<PlayerVisuals>(true);
            if (visuals != null)
            {
                var so = new SerializedObject(visuals);
                AssignRef(so, "visualProfile", "PlayerVisualProfile");
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var router = root.GetComponentInChildren<PlayerInputRouter>(true);
            if (router != null)
            {
                var so = new SerializedObject(router);
                var touch = so.FindProperty("touchConfig");
                if (touch != null)
                {
                    // Migrated from the old top-level router fields
                    touch.FindPropertyRelative("screenSplitRatio").floatValue = 0.5f;    // was screenSplitRatio: 0.5
                    touch.FindPropertyRelative("maxDragDistance").floatValue = 150f;     // was maxDragDistance: 150
                    touch.FindPropertyRelative("upwardDragThreshold").floatValue = 0.5f; // was upwardDragThreshold: 0.5
                    touch.FindPropertyRelative("tapDelay").floatValue = 0.01f;           // was tapDelay: 0.01
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // Drop the deleted PlayerDash / PlayerKeyboardInput components (missing scripts now).
            int removed = 0;
            foreach (var go in root.GetComponentsInChildren<Transform>(true))
                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go.gameObject);
            if (removed > 0)
                Debug.Log($"<b><color=cyan>[Migration]</color></b> Removed {removed} missing script component(s) (PlayerDash / PlayerKeyboardInput).");

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;

        var created = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(created, path);
        return created;
    }

    static void SetFloat(SerializedObject so, string field, float value)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.floatValue = value;
        else WarnMissing(field);
    }

    static void SetBool(SerializedObject so, string field, bool value)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.boolValue = value;
        else WarnMissing(field);
    }

    static void SetInt(SerializedObject so, string field, int value)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.intValue = value;
        else WarnMissing(field);
    }

    static void SetFloatRel(SerializedProperty parent, string field, float value)
    {
        var prop = parent != null ? parent.FindPropertyRelative(field) : null;
        if (prop != null) prop.floatValue = value;
        else WarnMissing(field);
    }

    static void AssignRef(SerializedObject so, string field, string assetName)
    {
        var prop = so.FindProperty(field);
        var asset = AssetDatabase.LoadAssetAtPath<Object>($"{ProfilesFolder}/{assetName}.asset");
        if (prop != null && asset != null) prop.objectReferenceValue = asset;
        else WarnMissing(field);
    }

    static void WarnMissing(string field)
    {
        Debug.LogWarning($"<b><color=yellow>[Migration]</color></b> Field '{field}' not found — check the profile script matches the migrator.");
    }
}
