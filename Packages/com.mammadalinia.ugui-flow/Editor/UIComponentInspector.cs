using UnityEditor;
using UnityEngine;

namespace UguiFlow.Editor
{
    [CustomEditor(typeof(UIComponent), true)]
    [CanEditMultipleObjects]
    internal sealed class UIComponentInspector : UnityEditor.Editor
    {
        private void OnEnable()
        {
            EditorApplication.hierarchyChanged += Repaint;
            EditorApplication.projectChanged += Repaint;
            Undo.undoRedoPerformed += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= Repaint;
            EditorApplication.projectChanged -= Repaint;
            Undo.undoRedoPerformed -= Repaint;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (targets.Length != 1)
                return;

            var component = (UIComponent)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Component Tree", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Parent", component.Parent, typeof(UIComponent), true);
                var children = component.Children;
                for (int i = 0; i < children.Count; i++)
                    EditorGUILayout.ObjectField($"Child {i + 1}", children[i], typeof(UIComponent), true);
            }
        }
    }
}
