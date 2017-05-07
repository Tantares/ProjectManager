using System;
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
                window = new GameObject("GUIWindow", typeof(GUIWindow));
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

        private void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", GUILayout.Width(25)))
            {
                SetNode(PagingType.Backward);
            }
            var centeredStyle = GUI.skin.GetStyle("Label");
            centeredStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label(!string.IsNullOrEmpty(NodeInfo.Selected) ? NodeInfo.Label : "Nothing selected", centeredStyle);
            if (GUILayout.Button(">", GUILayout.Width(25)))
            {
                SetNode(PagingType.Forward);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-", GUILayout.Width(25)))
            {
                var value = GetValue(OperationType.Decrement);
                if (value.HasValue)
                {
                    NodeInfo.Value = value.ToString();
                }
            }
            NodeInfo.Value = GUILayout.TextField(NodeInfo.Value);
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                var value = GetValue(OperationType.Increment);
                if (value.HasValue)
                {
                    NodeInfo.Value = value.ToString();
                }
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Save"))
            {
                var val = GetValue(OperationType.None);
                if (val.HasValue)
                {
                    var node = GetNode();
                    if (node != null)
                    {
                        node.SetValue(ProjectManagerScenario.launchCountKey, val.ToString(), true);
                    }
                }
                else
                {
                    var node = GetNode();
                    if (node != null)
                    {
                        NodeInfo.Value = node.GetValue(ProjectManagerScenario.launchCountKey);
                    }
                }
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private ConfigNode GetNode()
        {
            if (ProjectManagerScenario.rootNode != null)
            {
                return ProjectManagerScenario.rootNode.GetNodes().FirstOrDefault(p => p.name == NodeInfo.Selected);
            }
            return null;
        }

        private int? GetValue(OperationType type)
        {
            int value = 0;
            var node = GetNode();
            if (node != null && int.TryParse(NodeInfo.Value, out value))
            {
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
                if (value < 0)
                {
                    value = 0;
                }
                return value;
            }
            return null;
        }

        private void InitialLoad()
        {
            var currentNode = GetNode();
            if (currentNode != null)
            {
                NodeInfo.Value = currentNode.GetValue(ProjectManagerScenario.launchCountKey);
            }
            else
            {
                SetNode(PagingType.Forward);
            }
        }

        private void SetNode(PagingType type)
        {
            if (ProjectManagerScenario.rootNode != null)
            {
                var currentNode = GetNode();
                if (currentNode != null)
                {
                    var index = ProjectManagerScenario.rootNode.GetNodes().IndexOf(currentNode);
                    switch (type)
                    {
                        case PagingType.Backward:
                            index--;
                            if (index < 0)
                            {
                                index = ProjectManagerScenario.rootNode.GetNodes().Count() - 1;
                            }
                            break;

                        default:
                            index++;
                            if (index > (ProjectManagerScenario.rootNode.GetNodes().Count() - 1))
                            {
                                index = 0;
                            }
                            break;
                    }
                    NodeInfo.Selected = ProjectManagerScenario.rootNode.GetNodes()[index].name;
                }
                else
                {
                    if (ProjectManagerScenario.rootNode.GetNodes().Count() > 0)
                    {
                        switch (type)
                        {
                            case PagingType.Backward:
                                NodeInfo.Selected = ProjectManagerScenario.rootNode.GetNodes()[ProjectManagerScenario.rootNode.GetNodes().Count() - 1].name;
                                break;

                            default:
                                NodeInfo.Selected = ProjectManagerScenario.rootNode.GetNodes()[0].name;
                                break;
                        }
                    }
                }
                currentNode = GetNode();
                if (currentNode != null)
                {
                    NodeInfo.Value = currentNode.GetValue(ProjectManagerScenario.launchCountKey);
                    NodeInfo.Label = currentNode.GetValue(ProjectManagerScenario.seriesNameKey);
                }
            }
        }

        #endregion Methods

        #region Classes

        private class NodeInfo
        {
            #region Properties

            public static string Label { get; set; }
            public static string Selected { get; set; }
            public static string Value { get; set; }

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