using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectManager
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class GUILauncher : MonoBehaviour
    {
        #region Fields

        private const string iconPath = "ProjectManager/Plugins/pm";
        private static KSP.UI.Screens.ApplicationLauncherButton appButton;
        private static Texture appIcon;
        private static GameObject window;

        #endregion Fields

        #region Methods

        public void Awake()
        {
            appIcon = GameDatabase.Instance.GetTexture(iconPath, false);
            GameEvents.onGUIApplicationLauncherReady.Add(AddButton);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveButton);
        }

        private void AddButton()
        {
            if (appButton == null)
            {
                appButton = KSP.UI.Screens.ApplicationLauncher.Instance.AddModApplication(OnShow, OnClose, null, null, null, null,
                KSP.UI.Screens.ApplicationLauncher.AppScenes.SPACECENTER | KSP.UI.Screens.ApplicationLauncher.AppScenes.SPH |
                KSP.UI.Screens.ApplicationLauncher.AppScenes.TRACKSTATION | KSP.UI.Screens.ApplicationLauncher.AppScenes.VAB,
                appIcon);
            }
        }

        private void OnClose()
        {
            if (window != null)
            {
                DestroyImmediate(window);
                window = null;
            }
        }

        private void OnShow()
        {
            if (ProjectManagerScenario.rootNode != null)
            {
                OnClose();
                window = new GameObject(typeof(GUIWindow).Name, typeof(GUIWindow));
            }
        }

        private void RemoveButton()
        {
            if (appButton != null)
            {
                GameEvents.onGUIApplicationLauncherReady.Remove(AddButton);
                GameEvents.onGUIApplicationLauncherDestroyed.Remove(RemoveButton);
                KSP.UI.Screens.ApplicationLauncher.Instance.RemoveModApplication(appButton);
                appButton = null;
            }
        }

        #endregion Methods
    }

    public class GUIWindow : MonoBehaviour
    {
        #region Fields

        private const string title = "Project Manager";

        #endregion Fields

        #region Enums

        private enum OperationType
        {
            None = 0,
            Increment = 1,
            Decrement = 2
        }

        private enum PagingType
        {
            Forward = 0,
            Backward = 1
        }

        #endregion Enums

        #region Methods

        public void Awake()
        {
            if (!WindowInfo.IsSet)
            {
                var center = new Vector2((Screen.width / 2.0f) - (WindowInfo.Width / 2.0f), (Screen.height / 2.0f) - (WindowInfo.Height / 2.0f));
                WindowInfo.Position = new Rect(center.x, center.y, WindowInfo.Width, WindowInfo.Height);
                WindowInfo.IsSet = true;
            }
            WindowInfo.Id = Guid.NewGuid().GetHashCode();
            InitialLoad();
        }

        public void OnGUI()
        {
            GUILayout.BeginArea(WindowInfo.Position);
            WindowInfo.Position = GUILayout.Window(WindowInfo.Id, WindowInfo.Position, DrawWindow, title);
            GUILayout.EndArea();
        }

        private void CopyNodeInfo()
        {
            if (ProjectManagerScenario.rootNode != null)
            {
                List<ProjectInfo> configs = new List<ProjectInfo>();
                foreach (var node in ProjectManagerScenario.rootNode.GetNodes())
                {
                    var value = 0;
                    int.TryParse(node.GetValue(ProjectManagerScenario.launchCountKey), out value);
                    configs.Add(new ProjectInfo()
                    {
                        Name = node.name,
                        Value = value,
                        Label = node.GetValue(ProjectManagerScenario.seriesNameKey)
                    });
                }
                NodeInfo.Nodes = configs.OrderBy(p => p.Label).ToList();
            }
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", GUILayout.Width(25)))
            {
                NodeInfo.SetNode(PagingType.Backward);
            }
            var centeredStyle = GUI.skin.GetStyle("Label");
            centeredStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label(NodeInfo.Selected != null ? NodeInfo.Selected.Label : "Nothing selected", centeredStyle);
            if (GUILayout.Button(">", GUILayout.Width(25)))
            {
                NodeInfo.SetNode(PagingType.Forward);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-", GUILayout.Width(25)))
            {
                var value = NodeInfo.GetValue(OperationType.Decrement);
                if (value.HasValue && NodeInfo.Selected != null)
                {
                    NodeInfo.Selected.Value = value.GetValueOrDefault();
                }
            }
            if (NodeInfo.Selected != null)
            {
                int value = 0;
                if (int.TryParse(GUILayout.TextField(NodeInfo.Selected.Value.ToString()), out value))
                {
                    NodeInfo.Selected.Value = value;
                }
            }
            else
            {
                GUILayout.TextField("0");
            }
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                var value = NodeInfo.GetValue(OperationType.Increment);
                if (value.HasValue && NodeInfo.Selected != null)
                {
                    NodeInfo.Selected.Value = value.GetValueOrDefault();
                }
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Save"))
            {
                var val = NodeInfo.GetValue(OperationType.None);
                if (val.HasValue)
                {
                    var node = GetConfigNode();
                    if (node != null)
                    {
                        node.SetValue(ProjectManagerScenario.launchCountKey, val.ToString(), true);
                    }
                }
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private ConfigNode GetConfigNode()
        {
            if (ProjectManagerScenario.rootNode != null && NodeInfo.Selected != null)
            {
                return ProjectManagerScenario.rootNode.GetNodes().FirstOrDefault(p => p.name == NodeInfo.Selected.Name);
            }
            return null;
        }

        private void InitialLoad()
        {
            CopyNodeInfo();
            if (NodeInfo.Selected != null)
            {
                NodeInfo.RestoreSelected();
            }
            else
            {
                NodeInfo.SetNode(PagingType.Forward);
            }
        }

        #endregion Methods

        #region Classes

        private class NodeInfo
        {
            #region Fields

            private static ProjectInfo _selected;
            private static string selectedId;

            #endregion Fields

            #region Properties

            public static List<ProjectInfo> Nodes { get; set; }

            public static ProjectInfo Selected
            {
                get
                {
                    return _selected;
                }
                set
                {
                    _selected = value;
                    if (value != null)
                    {
                        selectedId = value.Name;
                    }
                }
            }

            #endregion Properties

            #region Methods

            public static int? GetValue(OperationType type)
            {
                if (Selected != null)
                {
                    var value = Selected.Value;
                    switch (type)
                    {
                        case OperationType.Increment:
                            value++;
                            break;

                        case OperationType.Decrement:
                            value--;
                            break;

                        default:
                            break;
                    }
                    return value;
                }
                return null;
            }

            public static void RestoreSelected()
            {
                if (!string.IsNullOrEmpty(selectedId) && Nodes.Count > 0)
                {
                    var node = Nodes.FirstOrDefault(p => p.Name == selectedId);
                    if (node != null)
                    {
                        Selected = node;
                    }
                }
            }

            public static void SetNode(PagingType type)
            {
                if (Selected != null)
                {
                    var index = Nodes.IndexOf(Selected);
                    switch (type)
                    {
                        case PagingType.Backward:
                            index--;
                            if (index < 0)
                            {
                                index = Nodes.Count() - 1;
                            }
                            break;

                        default:
                            index++;
                            if (index > (Nodes.Count() - 1))
                            {
                                index = 0;
                            }
                            break;
                    }
                    Selected = Nodes[index];
                }
                else
                {
                    if (Nodes.Count() > 0)
                    {
                        switch (type)
                        {
                            case PagingType.Backward:
                                Selected = Nodes[Nodes.Count - 1];
                                break;

                            default:
                                Selected = Nodes[0];
                                break;
                        }
                    }
                }
            }

            #endregion Methods
        }

        private class ProjectInfo
        {
            #region Properties

            public string Label { get; set; }
            public string Name { get; set; }
            public int Value { get; set; }

            #endregion Properties
        }

        private class WindowInfo
        {
            #region Properties

            public static int Height
            {
                get
                {
                    return 100;
                }
            }

            public static int Id { get; set; }
            public static bool IsSet { get; set; }

            public static Rect Position { get; set; }

            public static int Width
            {
                get
                {
                    return 250;
                }
            }

            #endregion Properties
        }

        #endregion Classes
    }
}