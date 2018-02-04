using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace XNodeEditor {
    /// <summary> xNode-specific version of <see cref="EditorGUILayout"/> </summary>
    public static class NodeEditorGUILayout {
        private static GUIStyle editorStyleCache;

        /// <summary> Make a field for a serialized property. Automatically displays relevant node port. </summary>
        public static void PropertyField(SerializedProperty property, bool includeChildren = true, GUIStyle style = null, params GUILayoutOption[] options) {
            PropertyField(property, (GUIContent) null, includeChildren, style, options);
        }

        /// <summary> Make a field for a serialized property. Automatically displays relevant node port. </summary>
        public static void PropertyField(SerializedProperty property, GUIContent label, bool includeChildren = true, GUIStyle style = null, params GUILayoutOption[] options) {
            if (property == null) throw new NullReferenceException();
            XNode.Node node = property.serializedObject.targetObject as XNode.Node;
            XNode.NodePort port = node.GetPort(property.name);
            PropertyField(property, label, port, includeChildren, style, options);
        }

        /// <summary> Make a field for a serialized property. Manual node port override. </summary>
        public static void PropertyField(SerializedProperty property, XNode.NodePort port, bool includeChildren = true, GUIStyle style = null, params GUILayoutOption[] options) {
            PropertyField(property, null, port, includeChildren, style, options);
        }

        /// <summary> Make a field for a serialized property. Manual node port override. </summary>
        public static void PropertyField(SerializedProperty property, GUIContent label, XNode.NodePort port, bool includeChildren = true, GUIStyle style = null, params GUILayoutOption[] options) {
            if (property == null) throw new NullReferenceException();

            // If property is not a port, display a regular property field
            if (port == null) {
                if (style != null) SetEditorLabel(style);
                EditorGUILayout.PropertyField(property, label, includeChildren, GUILayout.MinWidth(30));
                if (style != null) RevertEditorLabel();
            } else {
                Rect rect = new Rect();

                // If property is an input, display a regular property field and put a port handle on the left side
                if (port.direction == XNode.NodePort.IO.Input) {
                    // Get data from [Input] attribute
                    XNode.Node.ShowBackingValue showBacking = XNode.Node.ShowBackingValue.Unconnected;
                    XNode.Node.InputAttribute inputAttribute;
                    if (NodeEditorUtilities.GetAttrib(port.node.GetType(), property.name, out inputAttribute)) showBacking = inputAttribute.backingValue;

                    switch (showBacking) {
                        case XNode.Node.ShowBackingValue.Unconnected:
                            // Display a label if port is connected
                            if (port.IsConnected) {
                                if (style != null) EditorGUILayout.LabelField(label != null ? label : new GUIContent(property.displayName), style);
                                else EditorGUILayout.LabelField(label != null ? label : new GUIContent(property.displayName));
                            } // Display an editable property field if port is not connected
                            else {
                                if (style != null) SetEditorLabel(style);
                                EditorGUILayout.PropertyField(property, label, includeChildren, GUILayout.MinWidth(30));
                                if (style != null) RevertEditorLabel();
                            }
                            break;
                        case XNode.Node.ShowBackingValue.Never:
                            // Display a label
                            if (style != null) EditorGUILayout.LabelField(label != null ? label : new GUIContent(property.displayName), style);
                            else EditorGUILayout.LabelField(label != null ? label : new GUIContent(property.displayName));
                            break;
                        case XNode.Node.ShowBackingValue.Always:
                            // Display an editable property field
                            if (style != null) SetEditorLabel(style);
                            EditorGUILayout.PropertyField(property, label, includeChildren, GUILayout.MinWidth(30));
                            if (style != null) RevertEditorLabel();
                            break;
                    }

                    rect = GUILayoutUtility.GetLastRect();
                    rect.position = rect.position - new Vector2(16, 0);
                    // If property is an output, display a text label and put a port handle on the right side
                } else if (port.direction == XNode.NodePort.IO.Output) {
                    // Get data from [Output] attribute
                    XNode.Node.ShowBackingValue showBacking = XNode.Node.ShowBackingValue.Unconnected;
                    XNode.Node.OutputAttribute outputAttribute;
                    if (NodeEditorUtilities.GetAttrib(port.node.GetType(), property.name, out outputAttribute)) showBacking = outputAttribute.backingValue;

                    switch (showBacking) {
                        case XNode.Node.ShowBackingValue.Unconnected:
                            // Display a label if port is connected
                            if (port.IsConnected) {
                                if (style != null) EditorGUILayout.LabelField(label != null ? label : new GUIContent(property.displayName), GetOutputStyle(style), GUILayout.MinWidth(30));
                                else EditorGUILayout.LabelField(label != null ? label : new GUIContent(property.displayName), NodeEditorResources.styles.outputPort, GUILayout.MinWidth(30));
                            }
                            // Display an editable property field if port is not connected
                            else {
                                if (style != null) SetEditorLabel(style);
                                EditorGUILayout.PropertyField(property, label, includeChildren, GUILayout.MinWidth(30));
                                if (style != null) RevertEditorLabel();
                            }
                            break;
                        case XNode.Node.ShowBackingValue.Never:
                            // Display a label
                            if (style != null) EditorGUILayout.LabelField(label != null ? label : new GUIContent(property.displayName), GetOutputStyle(style), GUILayout.MinWidth(30));
                            else EditorGUILayout.LabelField(label != null ? label : new GUIContent(property.displayName), NodeEditorResources.styles.outputPort, GUILayout.MinWidth(30));
                            break;
                        case XNode.Node.ShowBackingValue.Always:
                            // Display an editable property field
                            if (style != null) SetEditorLabel(style);
                            EditorGUILayout.PropertyField(property, label, includeChildren, GUILayout.MinWidth(30));
                            if (style != null) RevertEditorLabel();
                            break;
                    }

                    rect = GUILayoutUtility.GetLastRect();
                    rect.position = rect.position + new Vector2(rect.width, 0);
                }

                rect.size = new Vector2(16, 16);

                Color backgroundColor = new Color32(90, 97, 105, 255);
                if (NodeEditorWindow.nodeTint.ContainsKey(port.node.GetType())) backgroundColor *= NodeEditorWindow.nodeTint[port.node.GetType()];
                Color col = NodeEditorWindow.current.graphEditor.GetTypeColor(port.ValueType);
                DrawPortHandle(rect, backgroundColor, col);

                // Register the handle position
                Vector2 portPos = rect.center;
                if (NodeEditor.portPositions.ContainsKey(port)) NodeEditor.portPositions[port] = portPos;
                else NodeEditor.portPositions.Add(port, portPos);
            }
        }

        /// <summary> Make a simple port field. </summary>
        public static void PortField(XNode.NodePort port, params GUILayoutOption[] options) {
            PortField(null, port, options);
        }

        /// <summary> Make a simple port field. </summary>
        public static void PortField(GUIContent label, XNode.NodePort port, params GUILayoutOption[] options) {
            if (port == null) return;
            if (label == null) EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(port.fieldName), options);
            else EditorGUILayout.LabelField(label, options);
            Rect rect = GUILayoutUtility.GetLastRect();
            if (port.direction == XNode.NodePort.IO.Input) rect.position = rect.position - new Vector2(16, 0);
            else if (port.direction == XNode.NodePort.IO.Output) rect.position = rect.position + new Vector2(rect.width, 0);
            rect.size = new Vector2(16, 16);

            Color backgroundColor = new Color32(90, 97, 105, 255);
            if (NodeEditorWindow.nodeTint.ContainsKey(port.node.GetType())) backgroundColor *= NodeEditorWindow.nodeTint[port.node.GetType()];
            Color col = NodeEditorWindow.current.graphEditor.GetTypeColor(port.ValueType);
            DrawPortHandle(rect, backgroundColor, col);

            // Register the handle position
            Vector2 portPos = rect.center;
            if (NodeEditor.portPositions.ContainsKey(port)) NodeEditor.portPositions[port] = portPos;
            else NodeEditor.portPositions.Add(port, portPos);
        }

        private static void SetEditorLabel(GUIStyle style) {
            if (editorStyleCache == null) {
                editorStyleCache = new GUIStyle(EditorStyles.label);
            }
            EditorStyles.label.normal = style.normal;
            EditorStyles.label.onNormal = style.onNormal;
            EditorStyles.label.hover = style.hover;
            EditorStyles.label.onHover = style.onHover;
            EditorStyles.label.active = style.active;
            EditorStyles.label.onActive = style.onActive;
            EditorStyles.label.onFocused = style.onFocused;
            EditorStyles.label.focused = style.focused;
        }

        private static void RevertEditorLabel() {
            if (editorStyleCache == null) return;
            EditorStyles.label.normal = editorStyleCache.normal;
            EditorStyles.label.onNormal = editorStyleCache.onNormal;
            EditorStyles.label.hover = editorStyleCache.hover;
            EditorStyles.label.onHover = editorStyleCache.onHover;
            EditorStyles.label.active = editorStyleCache.active;
            EditorStyles.label.onActive = editorStyleCache.onActive;
            EditorStyles.label.onFocused = editorStyleCache.onFocused;
            EditorStyles.label.focused = editorStyleCache.focused;
        }

        private static GUIStyle GetOutputStyle(GUIStyle style) {
            GUIStyle outputStyle = new GUIStyle(style);
            outputStyle.alignment = TextAnchor.UpperRight;
            outputStyle.padding.right = 10;
            return outputStyle;
        }

        private static void DrawPortHandle(Rect rect, Color backgroundColor, Color typeColor) {
            Color col = GUI.color;
            GUI.color = backgroundColor;
            GUI.DrawTexture(rect, NodeEditorResources.dotOuter);
            GUI.color = typeColor;
            GUI.DrawTexture(rect, NodeEditorResources.dot);
            GUI.color = col;
        }
    }
}