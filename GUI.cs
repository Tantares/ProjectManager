using System;
using System.Linq;
using UnityEngine;

namespace ProjectManager
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class GUILauncher : MonoBehaviour
    {
        #region Fields

        private static KSP.UI.Screens.ApplicationLauncherButton appButton;
        private static Texture appIcon;
        private static GameObject window;

        #endregion Fields

        #region Methods

        public void Awake()
        {
            appIcon = GameDatabase.Instance.GetTexture("ProjectManager/Plugins/pm", false);
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
                Destroy(window);
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

        private static string launchCountValue = string.Empty;
        private static bool positionSet = false;
        private static string selectedNode = string.Empty;
        private static Rect windowPosition;
        private readonly int windowId = Guid.NewGuid().GetHashCode();
        private bool initialLoad = true;

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
            None = 0,
            Forward = 1,
            Backward = 2
        }

        #endregion Enums

        #region Methods

        public void OnGUI()
        {
            if (!positionSet)
            {
                var center = new Vector2(Screen.width / 2.0f, Screen.height / 2.0f);
                windowPosition = new Rect(center.x, center.y, 250, 100);
                positionSet = true;
            }
            if (initialLoad)
            {
                SetNode(PagingType.None);
                initialLoad = false;
            }
            GUILayout.BeginArea(windowPosition);
            windowPosition = GUILayout.Window(windowId, windowPosition, DrawWindow, "Project Manager");
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
            GUILayout.Label(!string.IsNullOrEmpty(selectedNode) ? selectedNode : "Nothing selected", centeredStyle);
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
                    launchCountValue = value.ToString();
                }
            }
            launchCountValue = GUILayout.TextField(launchCountValue);
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                var value = GetValue(OperationType.Increment);
                if (value.HasValue)
                {
                    launchCountValue = value.ToString();
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
                        launchCountValue = node.GetValue(ProjectManagerScenario.launchCountKey);
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
                return ProjectManagerScenario.rootNode.GetNodes().FirstOrDefault(p => p.name == selectedNode);
            }
            return null;
        }

        private int? GetValue(OperationType type)
        {
            int value = 0;
            var node = GetNode();
            if (node != null && int.TryParse(launchCountValue, out value))
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

        private void SetNode(PagingType type)
        {
            if (ProjectManagerScenario.rootNode != null)
            {
                var currentNode = GetNode();
                if (type == PagingType.None)
                {
                    if (currentNode != null)
                    {
                        launchCountValue = currentNode.GetValue(ProjectManagerScenario.launchCountKey);
                    }
                    return;
                }
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
                    selectedNode = ProjectManagerScenario.rootNode.GetNodes()[index].name;
                }
                else
                {
                    if (ProjectManagerScenario.rootNode.GetNodes().Count() > 0)
                    {
                        switch (type)
                        {
                            case PagingType.Backward:
                                selectedNode = ProjectManagerScenario.rootNode.GetNodes()[ProjectManagerScenario.rootNode.GetNodes().Count() - 1].name;
                                break;

                            default:
                                selectedNode = ProjectManagerScenario.rootNode.GetNodes()[0].name;
                                break;
                        }
                    }
                }
                currentNode = GetNode();
                if (currentNode != null)
                {
                    launchCountValue = currentNode.GetValue(ProjectManagerScenario.launchCountKey);
                }
            }
        }

        #endregion Methods
    }
}