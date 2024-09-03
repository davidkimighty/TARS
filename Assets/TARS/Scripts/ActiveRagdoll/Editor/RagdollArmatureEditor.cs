using UnityEditor;
using UnityEngine;

namespace TARS.ActiveRagdoll
{
    [CustomEditor(typeof(RagdollArmature))]
    public class RagdollArmatureEditor : Editor
    {
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

        private GUIStyle titleStyle = null;
        private GUIStyle greenStyle = null;
        private GUIStyle redStyle = null;

        private bool isGUIInitialized = false;

        private void InitGUIStyles()
        {
            if (isGUIInitialized) return;

            titleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            greenStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.green }
            };

            redStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.red }
            };
            isGUIInitialized = true;
        }
    }
}
