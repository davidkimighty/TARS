using UnityEditor;
using UnityEngine;

namespace CollieLab.ActiveRagdoll
{
    [CustomEditor(typeof(RagdollArmature))]
    public class RagdollArmatureEditor : Editor
    {
        #region Private Field
        private const int HORIZONTALINDENT = 15;
        private const int VERTICALEDIVIDESPACE = 2;
        private const int VERTICALELEMENTSPACE = -1;

        private RagdollArmature armature = null;
        private SerializedProperty headBoneProperty = null;
        private SerializedProperty bodyBoneProperty = null;
        private SerializedProperty legBoneProperty = null;
        private SerializedProperty tailBoneProperty = null;
        private SerializedProperty ignoreCollidersProperty = null;

        private bool armatureFoldout = false;
        private bool ignoreCollidersFoldout = false;
        #endregion

        private void OnEnable()
        {
            armature = (RagdollArmature)target;
            headBoneProperty = serializedObject.FindProperty("head");
            bodyBoneProperty = serializedObject.FindProperty("bodies");
            legBoneProperty = serializedObject.FindProperty("legs");
            tailBoneProperty = serializedObject.FindProperty("tail");
            ignoreCollidersProperty = serializedObject.FindProperty("ignoreColliderGroup");
        }

        public override void OnInspectorGUI()
        {
            InitGUIStyles();

            serializedObject.Update();

            EditorGUILayout.LabelField("Armature", titleStyle);
            EditorGUILayout.Space();

            ArmatureSettingGUI();
            CollisionSettingGUI();

            serializedObject.ApplyModifiedProperties();
        }

        #region Armature GUI
        /// <summary>
        /// 
        /// </summary>
        private void ArmatureSettingGUI()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical(GUI.skin.GetStyle("HelpBox"));
            {
                armatureFoldout = EditorGUILayout.Foldout(armatureFoldout, "BoneInfo");
                if (armatureFoldout)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(HORIZONTALINDENT);
                        if (GUILayout.Button("Initialize"))
                        {
                            InitializeBoneElement();
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(VERTICALEDIVIDESPACE);

                    EditorGUILayout.PropertyField(headBoneProperty);
                    EditorGUILayout.PropertyField(bodyBoneProperty);
                    EditorGUILayout.PropertyField(legBoneProperty);
                    EditorGUILayout.PropertyField(tailBoneProperty);

                    EditorGUILayout.Space(VERTICALELEMENTSPACE);
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(VERTICALEDIVIDESPACE);
            EditorGUI.indentLevel--;
        }
        #endregion

        #region Armature Controls
        /// <summary>
        /// Get components required for BoneInfo.
        /// </summary>
        private void InitializeBoneElement()
        {
            for (int i = 0; i < armature.Head.Length; i++)
            {
                GetBoneComponents(armature.Head[i]);
            }

            for (int i = 0; i < armature.Bodies.Length; i++)
            {
                GetBoneComponents(armature.Bodies[i]);
            }

            for (int i = 0; i < armature.Legs.Length; i++)
            {
                GetLimbBoneComponents(armature.Legs[i].leftLeg);
                GetLimbBoneComponents(armature.Legs[i].rightLeg);
            }

            for (int i = 0; i < armature.Tail.Length; i++)
            {
                GetBoneComponents(armature.Tail[i]);
            }
        }

        private void GetLimbBoneComponents(LimbInfo limb)
        {
            GetBoneComponents(limb.upper);
            GetBoneComponents(limb.lower);
            GetBoneComponents(limb.end);
        }

        private void GetBoneComponents(BoneInfo bone)
        {
            if (bone.obj == null) return;

            bone.joint = bone.obj.GetComponent<ConfigurableJoint>();
            bone.body = bone.obj.GetComponent<Rigidbody>();
            bone.collider = bone.obj.GetComponent<Collider>();
        }
        #endregion

        #region Collision GUI
        /// <summary>
        /// 
        /// </summary>
        private void CollisionSettingGUI()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical(GUI.skin.GetStyle("HelpBox"));
            {
                ignoreCollidersFoldout = EditorGUILayout.Foldout(ignoreCollidersFoldout, "Ignore Colliders");
                if (ignoreCollidersFoldout)
                {
                    EditorGUILayout.PropertyField(ignoreCollidersProperty);
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(VERTICALEDIVIDESPACE);
            EditorGUI.indentLevel--;
        }
        #endregion

        #region GUI Styles
        private GUIStyle titleStyle = null;
        private GUIStyle greenStyle = null;
        private GUIStyle redStyle = null;

        private bool isGUIInitialized = false;
        #endregion

        #region GUI Initialize
        private void InitGUIStyles()
        {
            if (isGUIInitialized) return;

            titleStyle = new GUIStyle(EditorStyles.label);
            titleStyle.fontSize = 16;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.wordWrap = true;

            greenStyle = new GUIStyle(EditorStyles.label);
            greenStyle.normal.textColor = Color.green;

            redStyle = new GUIStyle(EditorStyles.label);
            redStyle.normal.textColor = Color.red;

            isGUIInitialized = true;
        }
        #endregion
    }
}
