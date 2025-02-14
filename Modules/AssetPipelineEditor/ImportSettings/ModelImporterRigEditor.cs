// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using UnityEditor.AssetImporters;

namespace UnityEditor
{
    internal class ModelImporterRigEditor : BaseAssetImporterTabUI
    {
        ModelImporter singleImporter { get { return targets[0] as ModelImporter; } }

        public int m_SelectedClipIndex = -1;

        Avatar m_Avatar;

#pragma warning disable 0649
        [CacheProperty]
        SerializedProperty m_AnimationType;
        [CacheProperty]
        SerializedProperty m_AvatarSetup;
        [CacheProperty("m_LastHumanDescriptionAvatarSource")]
        SerializedProperty m_AvatarSource;
        [CacheProperty]
        SerializedProperty m_LegacyGenerateAnimations;
        [CacheProperty]
        SerializedProperty m_AnimationCompression;
        [CacheProperty("skinWeightsMode")]
        SerializedProperty m_SkinWeightsMode;
        [CacheProperty("maxBonesPerVertex")]
        SerializedProperty m_MaxBonesPerVertex;
        [CacheProperty("minBoneWeight")]
        SerializedProperty m_MinBoneWeight;
        [CacheProperty("optimizeBones")]
        SerializedProperty m_OptimizeBones;

        [CacheProperty]
        SerializedProperty m_OptimizeGameObjects;

        [CacheProperty("m_HumanDescription.m_RootMotionBoneName")]
        SerializedProperty m_RootMotionBoneName;

        [CacheProperty("m_HasExtraRoot")]
        SerializedProperty m_SrcHasExtraRoot;
        [CacheProperty("m_HumanDescription.m_HasExtraRoot")]
        SerializedProperty m_DstHasExtraRoot;

        [CacheProperty]
        SerializedProperty m_RigImportErrors;
        [CacheProperty]
        SerializedProperty m_RigImportWarnings;

        [CacheProperty("m_HumanDescription.m_Human")]
        SerializedProperty m_HumanBoneArray;
        [CacheProperty("m_HumanDescription.m_Skeleton")]
        SerializedProperty m_Skeleton;

        [CacheProperty]
        SerializedProperty m_AutoGenerateAvatarMappingIfUnspecified;
#pragma warning restore 0649

        private static bool importMessageFoldout = false;

        GUIContent[] m_RootMotionBoneList;

        private ExposeTransformEditor m_ExposeTransformEditor;
        private SerializedObject m_AvatarSourceSerializedObject;

        SerializedObject avatarSourceSerializedObject
        {
            get
            {
                // to keep the serialized object in sync with the current source avatar
                Object currentAvatar = m_AvatarSource?.objectReferenceValue;
                if (currentAvatar == null || currentAvatar != m_AvatarSourceSerializedObject?.targetObject)
                {
                    m_AvatarSourceSerializedObject?.Dispose();
                    m_AvatarSourceSerializedObject = null;
                    if (currentAvatar != null)
                        m_AvatarSourceSerializedObject = new SerializedObject(currentAvatar);
                }

                m_AvatarSourceSerializedObject?.Update();
                return m_AvatarSourceSerializedObject;
            }
        }

        private ModelImporterAnimationType animationType
        {
            get { return (ModelImporterAnimationType)m_AnimationType.intValue; }
            set { m_AnimationType.intValue = (int)value; }
        }

        private bool m_CanMultiEditTransformList;

        bool m_IsBiped = false;
        List<string> m_BipedMappingReport = null;

        bool m_ExtraExposedTransformFoldout = false;

        static class Styles
        {
            public static GUIContent AnimationType = EditorGUIUtility.TrTextContent("Animation Type", "The type of animation to support / import.");
            public static GUIContent[] AnimationTypeOpt =
            {
                EditorGUIUtility.TrTextContent("None", "No animation present."),
                EditorGUIUtility.TrTextContent("Legacy", "Legacy animation system."),
                EditorGUIUtility.TrTextContent("Generic", "Generic Mecanim animation."),
                EditorGUIUtility.TrTextContent("Humanoid", "Humanoid Mecanim animation system.")
            };

            public static GUIContent SaveAvatar = EditorGUIUtility.TrTextContent("Save Avatar", "Saves the generated Avatar as a sub-asset.");

            public static GUIContent AnimLabel = EditorGUIUtility.TrTextContent("Generation", "Controls how animations are imported.");
            public static GUIContent[] AnimationsOpt =
            {
                EditorGUIUtility.TrTextContent("Don't Import", "No animation or skinning is imported."),
                EditorGUIUtility.TrTextContent("Store in Original Roots (Deprecated)", "Animations are stored in the root objects of your animation package (these might be different from the root objects in Unity)."),
                EditorGUIUtility.TrTextContent("Store in Nodes (Deprecated)", "Animations are stored together with the objects they animate. Use this when you have a complex animation setup and want full scripting control."),
                EditorGUIUtility.TrTextContent("Store in Root (Deprecated)", "Animations are stored in the scene's transform root objects. Use this when animating anything that has a hierarchy."),
                EditorGUIUtility.TrTextContent("Store in Root (New)")
            };

            public static GUIContent avatar = EditorGUIUtility.TrTextContent("Animator");
            public static GUIContent configureAvatar = EditorGUIUtility.TrTextContent("Configure...");
            public static GUIContent avatarValid = EditorGUIUtility.TrTextContent("\u2713");
            public static GUIContent avatarInvalid = EditorGUIUtility.TrTextContent("\u2715");
            public static GUIContent avatarPending = EditorGUIUtility.TrTextContent("...");


            public static GUIContent UpdateMuscleDefinitionFromSource = EditorGUIUtility.TrTextContent("Update", "Update the copy of the muscle definition from the source.");
            public static GUIContent RootNode = EditorGUIUtility.TrTextContent("Root node", "Specify the root node used to extract the animation translation.");

            public static GUIContent AvatarDefinition = EditorGUIUtility.TrTextContent("Avatar Definition", "Choose between Create From This Model or Copy From Other Avatar. The first one creates an Avatar for this file and the second one uses an Avatar from another file to import animation.");

            public static GUIContent SkinWeightsMode = EditorGUIUtility.TrTextContent("Skin Weights", "Control how many bone weights are imported.");
            public static GUIContent[] SkinWeightsModeOpt =
            {
                EditorGUIUtility.TrTextContent("Standard (4 Bones)", "Import a maximum of 4 bones per vertex."),
                EditorGUIUtility.TrTextContent("Custom", "Import a custom number of bones per vertex.")
            };
            public static GUIContent MaxBonesPerVertex = EditorGUIUtility.TrTextContent("Max Bones/Vertex", "Number of bones that can affect each vertex.");
            public static GUIContent MinBoneWeight = EditorGUIUtility.TrTextContent("Min Bone Weight", "Bone weights smaller than this value are rejected. The remaining weights are scaled to add up to 1.0.");
            public static GUIContent OptimizeBones = EditorGUIUtility.TrTextContent("Strip Bones", "Only adds bones to SkinnedMeshRenderers that have skin weights assigned to them.");

            public static GUIContent UpdateReferenceClips = EditorGUIUtility.TrTextContent("Update reference clips", "Click on this button to update all the @convention files referencing this file. Should set all these files to Copy From Other Avatar, set the source Avatar to this one and reimport all these files.");

            public static GUIContent ImportMessages = EditorGUIUtility.TrTextContent("Import Messages");
            public static GUIContent ExtraExposedTransform = EditorGUIUtility.TrTextContent("Extra Transforms to Expose", "Select the list of transforms to expose in the optimized GameObject hierarchy.");
        }

        public ModelImporterRigEditor(AssetImporterEditor panelContainer)
            : base(panelContainer)
        {}

        internal override void OnEnable()
        {
            Editor.AssignCachedProperties(this, serializedObject.GetIterator());

            m_ExposeTransformEditor = new ExposeTransformEditor();

            string[] transformPaths = singleImporter.transformPaths;
            m_RootMotionBoneList = new GUIContent[transformPaths.Length];
            for (int i = 0; i < transformPaths.Length; i++)
                m_RootMotionBoneList[i] = new GUIContent(transformPaths[i]);

            if (m_RootMotionBoneList.Length > 0)
                m_RootMotionBoneList[0] = EditorGUIUtility.TrTextContent("None");

            m_ExposeTransformEditor.OnEnable(singleImporter.transformPaths, serializedObject);

            m_CanMultiEditTransformList = CanMultiEditTransformList();

            m_IsBiped = false;
            m_BipedMappingReport = new List<string>();

            UpdateBipedMappingReport();

            if (m_AnimationType.intValue == (int)ModelImporterAnimationType.Human && m_Avatar == null)
            {
                ResetAvatar();
            }
        }

        internal override void OnDisable()
        {
            if (m_AvatarSourceSerializedObject != null)
                m_AvatarSourceSerializedObject.Dispose();
        }

        private void UpdateBipedMappingReport()
        {
            if (m_AnimationType.intValue == (int)ModelImporterAnimationType.Human)
            {
                GameObject go = assetTarget as GameObject;
                if (go != null)
                {
                    m_IsBiped = AvatarBipedMapper.IsBiped(go.transform, m_BipedMappingReport);
                }
            }
        }

        private bool CanMultiEditTransformList()
        {
            string[] transformPaths = singleImporter.transformPaths;
            for (int i = 1; i < targets.Length; ++i)
            {
                ModelImporter modelImporter = targets[i] as ModelImporter;
                if (!ArrayUtility.ArrayEquals(transformPaths, modelImporter.transformPaths))
                    return false;
            }

            return true;
        }

        bool AvatarCopyIsUpToDate()
        {
            if (!(animationType == ModelImporterAnimationType.Human || animationType == ModelImporterAnimationType.Generic) || m_AvatarSource.objectReferenceValue == null)
                return true;

            SerializedProperty humanDescription = serializedObject.FindProperty("m_HumanDescription");

            //Because of the constraint that Editors must share an Animation Source, if we have a mix of different human descriptions, then *at least* one is out of date
            if (humanDescription.hasMultipleDifferentValues)
                return false;

            //Does the HumanDescription in the Importer match the one in the referenced Avatar?
            return SerializedProperty.DataEquals(humanDescription, avatarSourceSerializedObject.FindProperty("m_HumanDescription"));
        }

        internal override void OnDestroy()
        {
            m_Avatar = null;
        }

        internal override void ResetValues()
        {
            base.ResetValues();
            ResetAvatar();
            m_ExposeTransformEditor.ResetExposedTransformList();
        }

        internal override void PostApply()
        {
            base.PostApply();

            //The import process may generate a new avatar, find it.
            ResetAvatar();
        }

        void ResetAvatar()
        {
            if (assetTarget != null)
            {
                var path = singleImporter.assetPath;
                m_Avatar = AssetDatabase.LoadAssetAtPath<Avatar>(path);
            }
        }

        void LegacyGUI()
        {
            EditorGUILayout.Popup(m_LegacyGenerateAnimations, Styles.AnimationsOpt, Styles.AnimLabel);
            // Show warning and fix button for deprecated import formats
            if (m_LegacyGenerateAnimations.intValue == 1 || m_LegacyGenerateAnimations.intValue == 2 || m_LegacyGenerateAnimations.intValue == 3)
                EditorGUILayout.HelpBox("The animation import setting \"" + Styles.AnimationsOpt[m_LegacyGenerateAnimations.intValue].text + "\" is deprecated.", MessageType.Warning);
        }

        void GenericGUI()
        {
            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                using (var propertyField = new EditorGUI.PropertyScope(horizontal.rect, Styles.AvatarDefinition, m_AvatarSetup))
                {
                    EditorGUI.showMixedValue = m_AvatarSetup.hasMultipleDifferentValues;
                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        var value = (ModelImporterAvatarSetup)EditorGUILayout.EnumPopup(propertyField.content, (ModelImporterAvatarSetup)m_AvatarSetup.intValue);
                        if (change.changed)
                            m_AvatarSetup.intValue = (int)value;
                    }

                    EditorGUI.showMixedValue = false;
                }
            }

            if (!m_AvatarSetup.hasMultipleDifferentValues)
            {
                if (m_AvatarSetup.intValue == (int)ModelImporterAvatarSetup.CreateFromThisModel)
                {
                    // Do not allow multi edit of root node if all rigs doesn't match
                    using (new EditorGUI.DisabledScope(!m_CanMultiEditTransformList))
                    {
                        if (assetTarget == null)
                        {
                            m_RootMotionBoneName.stringValue =
                                EditorGUILayout.TextField(Styles.RootNode, m_RootMotionBoneName.stringValue);
                        }
                        else
                        {
                            EditorGUI.BeginChangeCheck();
                            var currentIndex = ArrayUtility.FindIndex(m_RootMotionBoneList, content => FileUtil.GetLastPathNameComponent(content.text) == m_RootMotionBoneName.stringValue);
                            currentIndex = currentIndex < 1 ? 0 : currentIndex;
                            currentIndex = EditorGUILayout.Popup(Styles.RootNode, currentIndex, m_RootMotionBoneList);
                            if (EditorGUI.EndChangeCheck())
                            {
                                if (currentIndex > 0 && currentIndex < m_RootMotionBoneList.Length)
                                {
                                    m_RootMotionBoneName.stringValue =
                                        FileUtil.GetLastPathNameComponent(m_RootMotionBoneList[currentIndex].text);
                                }
                                else
                                {
                                    m_RootMotionBoneName.stringValue = "";
                                }
                            }
                        }
                    }
                }
                else if (m_AvatarSetup.intValue == (int)ModelImporterAvatarSetup.CopyFromOther)
                    CopyAvatarGUI();
            }
        }

        void HumanoidGUI()
        {
            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                using (var propertyField = new EditorGUI.PropertyScope(horizontal.rect, Styles.AvatarDefinition, m_AvatarSetup))
                {
                    EditorGUI.showMixedValue = m_AvatarSetup.hasMultipleDifferentValues;
                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        Rect r = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.popup);
                        var value = (ModelImporterAvatarSetup)EditorGUI.EnumPopup(r, propertyField.content, (ModelImporterAvatarSetup)m_AvatarSetup.intValue, e => (ModelImporterAvatarSetup)e != ModelImporterAvatarSetup.NoAvatar);
                        if (change.changed)
                        {
                            m_AvatarSetup.intValue = (int)value;

                            //Case 1213138 - When changing avatar setup value, we must reset the human, skeleton & auto-mapping to their default values.
                            //NB: This fix will be defunct once we have a reference-based solution for copying avatars.
                            AvatarSetupTool.ClearAll(m_HumanBoneArray, m_Skeleton);
                            m_AutoGenerateAvatarMappingIfUnspecified.boolValue = true;
                        }
                    }

                    EditorGUI.showMixedValue = false;
                }
            }

            if (!m_AvatarSetup.hasMultipleDifferentValues)
            {
                if (m_AvatarSetup.intValue == (int)ModelImporterAvatarSetup.CreateFromThisModel)
                    ConfigureAvatarGUI();
                else
                    CopyAvatarGUI();
            }

            if (m_IsBiped)
            {
                if (m_BipedMappingReport.Count > 0)
                {
                    string report = "A Biped was detected, but cannot be configured properly because of an unsupported hierarchy. Adjust Biped settings in 3DS Max before exporting to correct this problem.\n";

                    for (int reportIter = 0; reportIter < m_BipedMappingReport.Count; reportIter++)
                    {
                        report += m_BipedMappingReport[reportIter];
                    }

                    EditorGUILayout.HelpBox(report, MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("A Biped was detected. Default Biped mapping and T-Pose have been configured for this avatar. Translation DoFs have been activated. Use Configure to modify default Biped setup.", MessageType.Info);
                }
            }

            EditorGUILayout.Space();
        }

        void ConfigureAvatarGUI()
        {
            if (targets.Length > 1)
            {
                GUILayout.Label("Can't configure avatar in multi-editing mode", EditorStyles.helpBox);
                return;
            }

            if (singleImporter.transformPaths.Length <= HumanTrait.RequiredBoneCount)
            {
                GUILayout.Label(string.Format("Not enough bones to create human avatar (requires {0})", HumanTrait.RequiredBoneCount), EditorStyles.helpBox);
            }

            // Validation text
            GUIContent validationContent;
            if (m_Avatar && !HasModified())
            {
                if (m_Avatar.isHuman)
                    validationContent = Styles.avatarValid;
                else
                    validationContent = Styles.avatarInvalid;
            }
            else
            {
                validationContent = Styles.avatarPending;
                GUILayout.Label("The avatar can be configured after settings have been applied.", EditorStyles.helpBox);
            }

            Rect r = EditorGUILayout.GetControlRect();
            const int buttonWidth = 80;
            GUI.Label(new Rect(r.xMax - buttonWidth - 18, r.y, 18, r.height), validationContent, EditorStyles.label);

            // Configure button
            using (new EditorGUI.DisabledScope(m_Avatar == null))
            {
                if (GUI.Button(new Rect(r.xMax - buttonWidth, r.y + 1, buttonWidth, r.height - 1), Styles.configureAvatar, EditorStyles.miniButton))
                {
                    if (!isLocked)
                    {
                        Selection.activeObject = m_Avatar;
                        AvatarEditor.s_EditImmediatelyOnNextOpen = true;
                        GUIUtility.ExitGUI();
                    }
                    else
                        Debug.Log("Cannot configure avatar, inspector is locked");
                }
            }
        }

        bool IsAvatarValid(Avatar newAvatar)
        {
            if (newAvatar != null)
            {
                if (newAvatar.isHuman && (animationType != ModelImporterAnimationType.Human))
                {
                    if (EditorUtility.DisplayDialog("Assigning a Humanoid Avatar on a Generic Rig",
                        "Do you want to change Animation Type to Humanoid ?", "Yes", "No"))
                    {
                        animationType = ModelImporterAnimationType.Human;
                        return true;
                    }
                    else
                    {
                        //New Avatar is invalid
                        return false;
                    }
                }
                else if (!newAvatar.isHuman && (animationType != ModelImporterAnimationType.Generic))
                {
                    if (EditorUtility.DisplayDialog("Assigning a Generic Avatar on a Humanoid Rig",
                        "Do you want to change Animation Type to Generic ?", "Yes", "No"))
                    {
                        animationType = ModelImporterAnimationType.Generic;
                        return true;
                    }
                    else
                    {
                        //New Avatar is invalid
                        return false;
                    }
                }
            }

            return true;
        }

        void CopyAvatarGUI()
        {
            GUILayout.Label(
@"If you have already created an Avatar for another model with a rig identical to this one, you can copy its Avatar definition.
With this option, this model will not create any avatar but only import animations.", EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            Avatar previousAvatar = (Avatar)m_AvatarSource.objectReferenceValue;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.ObjectField(m_AvatarSource, typeof(Avatar), GUIContent.Temp("Source"), ValidateAvatarSource);
            var sourceAvatar = m_AvatarSource.objectReferenceValue as Avatar;
            if (EditorGUI.EndChangeCheck())
            {
                if (IsAvatarValid(sourceAvatar))
                {
                    AvatarSetupTool.ClearAll(m_HumanBoneArray, m_Skeleton);

                    if (sourceAvatar != null)
                        CopyHumanDescriptionFromOtherModel(sourceAvatar);
                }
                else
                    //Avatar is invalid so revert to the previous Avatar
                    m_AvatarSource.objectReferenceValue = previousAvatar;
            }

            if (sourceAvatar != null && !m_AvatarSource.hasMultipleDifferentValues && !AvatarCopyIsUpToDate())
            {
                if (GUILayout.Button(Styles.UpdateMuscleDefinitionFromSource, EditorStyles.miniButton))
                {
                    if (IsAvatarValid(sourceAvatar))
                    {
                        AvatarSetupTool.ClearAll(m_HumanBoneArray, m_Skeleton);
                        CopyHumanDescriptionFromOtherModel(sourceAvatar);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        Object ValidateAvatarSource(Object[] references, Type objType, SerializedProperty property, EditorGUI.ObjectFieldValidatorOptions options)
        {
            if (references.Length == 0)
                return null;

            string avatarPath = AssetDatabase.GetAssetPath(references[0]);
            foreach (AssetImporter importer in targets)
            {
                if (avatarPath == importer.assetPath)
                {
                    return null;
                }
            }
            return references[0];
        }

        void ShowUpdateReferenceClip()
        {
            if (targets.Length > 1 || m_AvatarSetup.intValue == (int)ModelImporterAvatarSetup.CopyFromOther || !m_Avatar || !m_Avatar.isValid)
                return;

            string[] paths = new string[0];
            ModelImporter importer = target as ModelImporter;
            if (importer.referencedClips.Length > 0)
            {
                foreach (string clipGUID in importer.referencedClips)
                    ArrayUtility.Add(ref paths, AssetDatabase.GUIDToAssetPath(clipGUID));
            }

            // Show only button if some clip reference this avatar.
            if (paths.Length > 0 && GUILayout.Button(Styles.UpdateReferenceClips, GUILayout.Width(150)))
            {
                foreach (string path in paths)
                    SetupReferencedClip(path);

                try
                {
                    AssetDatabase.StartAssetEditing();
                    foreach (string path in paths)
                        AssetDatabase.ImportAsset(path);
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            string errors = m_RigImportErrors.stringValue;
            string warnings = m_RigImportWarnings.stringValue;

            if (errors.Length > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Error(s) found while importing rig in this animation file. Open \"Import Messages\" foldout below for more details", MessageType.Error);
            }
            else
            {
                if (warnings.Length > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("Warning(s) found while importing rig in this animation file. Open \"Import Messages\" foldout below for more details", MessageType.Warning);
                }
            }


            // Animation type
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Popup(m_AnimationType, Styles.AnimationTypeOpt, Styles.AnimationType);
            if (EditorGUI.EndChangeCheck())
            {
                m_AvatarSource.objectReferenceValue = null;

                if (m_AvatarSourceSerializedObject != null)
                    m_AvatarSourceSerializedObject.Dispose();
                m_AvatarSourceSerializedObject = null;

                if (animationType == ModelImporterAnimationType.Legacy)
                    m_AnimationCompression.intValue = (int)ModelImporterAnimationCompression.KeyframeReduction;
                else if (animationType == ModelImporterAnimationType.Generic || animationType == ModelImporterAnimationType.Human)
                    m_AnimationCompression.intValue = (int)ModelImporterAnimationCompression.Optimal;

                m_DstHasExtraRoot.boolValue = m_SrcHasExtraRoot.boolValue;
            }

            EditorGUILayout.Space();

            if (!m_AnimationType.hasMultipleDifferentValues)
            {
                // Show GUI depending on animation type
                if (animationType == ModelImporterAnimationType.Human)
                    HumanoidGUI();
                else if (animationType == ModelImporterAnimationType.Generic)
                    GenericGUI();
                else if (animationType == ModelImporterAnimationType.Legacy)
                    LegacyGUI();
            }

            if (animationType != ModelImporterAnimationType.None || m_AnimationType.hasMultipleDifferentValues)
            {
                EditorGUILayout.Popup(m_SkinWeightsMode, Styles.SkinWeightsModeOpt, Styles.SkinWeightsMode);

                if (m_SkinWeightsMode.intValue == (int)ModelImporterSkinWeights.Custom)
                {
                    EditorGUILayout.IntSlider(m_MaxBonesPerVertex, 1, 255, Styles.MaxBonesPerVertex);
                    EditorGUILayout.Slider(m_MinBoneWeight, 0.001f, 0.5f, Styles.MinBoneWeight);
                }

                int currentSkinWeights = m_SkinWeightsMode.intValue == (int)ModelImporterSkinWeights.Standard ? 4 : m_MaxBonesPerVertex.intValue;
                if ((int)QualitySettings.skinWeights < currentSkinWeights)
                {
                    var msg =
                        $"Skin Weights is set to {(int)QualitySettings.skinWeights} bones per vertex in Quality Settings. Some bone influences may not be rendered.";
                    EditorGUILayout.HelpBox(msg, MessageType.Info);
                }
            }

            EditorGUILayout.PropertyField(m_OptimizeBones, Styles.OptimizeBones);

            ShowUpdateReferenceClip();

            // OptimizeGameObject is only supported on our own avatar when animation type is not Legacy.
            if (m_AvatarSetup.intValue == (int)ModelImporterAvatarSetup.CreateFromThisModel && animationType != ModelImporterAnimationType.Legacy)
            {
                EditorGUILayout.PropertyField(m_OptimizeGameObjects);
                if (m_OptimizeGameObjects.boolValue &&
                    serializedObject.targetObjectsCount == 1) // SerializedProperty can't handle multiple string arrays properly.
                {
                    bool wasChanged = GUI.changed;
                    m_ExtraExposedTransformFoldout = EditorGUILayout.Foldout(m_ExtraExposedTransformFoldout, Styles.ExtraExposedTransform, true);
                    GUI.changed = wasChanged;
                    if (m_ExtraExposedTransformFoldout)
                    {
                        // Do not allow multi edit of exposed transform list if all rigs doesn't match
                        using (new EditorGUI.DisabledScope(!m_CanMultiEditTransformList))
                        using (new EditorGUI.IndentLevelScope())
                            m_ExposeTransformEditor.OnGUI();
                    }
                }
            }

            if (errors.Length > 0 || warnings.Length > 0)
            {
                EditorGUILayout.Space();

                importMessageFoldout = EditorGUILayout.Foldout(importMessageFoldout, Styles.ImportMessages, true);

                if (importMessageFoldout)
                {
                    if (errors.Length > 0)
                        EditorGUILayout.HelpBox(errors, MessageType.None);
                    else if (warnings.Length > 0)
                        EditorGUILayout.HelpBox(warnings, MessageType.None);
                }
            }
        }

        static SerializedObject GetModelImporterSerializedObject(string assetPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
                return null;

            return new SerializedObject(importer);
        }

        static void CopyHumanDescriptionToDestination(SerializedObject sourceObject, SerializedObject targetObject)
        {
            targetObject.CopyFromSerializedProperty(sourceObject.FindProperty("m_HumanDescription"));
        }

        private void CopyHumanDescriptionFromOtherModel(Avatar sourceAvatar)
        {
            //This could be an Avatar sub-asset, or a distinct avatar asset
            using (SerializedObject avatarSerializedObject = new SerializedObject(sourceAvatar))
            {
                CopyHumanDescriptionToDestination(avatarSerializedObject, serializedObject);
            }
        }

        private void SetupReferencedClip(string otherModelImporterPath)
        {
            SerializedObject targetImporter = GetModelImporterSerializedObject(otherModelImporterPath);

            // We may receive a path that doesn't have a importer.
            if (targetImporter != null)
            {
                targetImporter.CopyFromSerializedProperty(serializedObject.FindProperty("m_AnimationType"));

                SerializedProperty copyAvatar = targetImporter.FindProperty("m_CopyAvatar");
                if (copyAvatar != null)
                    copyAvatar.boolValue = true;

                SerializedProperty avatar = targetImporter.FindProperty("m_LastHumanDescriptionAvatarSource");
                if (avatar != null)
                    avatar.objectReferenceValue = m_Avatar;

                CopyHumanDescriptionToDestination(serializedObject, targetImporter);
                targetImporter.ApplyModifiedProperties();
                targetImporter.Dispose();
            }
        }

        public bool isLocked
        {
            get
            {
                foreach (InspectorWindow i in InspectorWindow.GetAllInspectorWindows())
                {
                    ActiveEditorTracker activeEditor = i.tracker;
                    foreach (Editor e in activeEditor.activeEditors)
                    {
                        // the tab is no longer an editor, so we must always refer to the panel container
                        if (e is ModelImporterEditor && ((ModelImporterEditor)e).activeTab == this)
                        {
                            return i.isLocked;
                        }
                    }
                }
                return false;
            }
        }
    }
}
