using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSP.IO;
using KSP.UI.Screens;
using System.Text.RegularExpressions;

namespace ProjectManager
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new GameScenes[] { GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT })]
    public class ProjectManagerScenario : ScenarioModule
    {
        public static ProjectManagerScenario Instance;

        // String constants.
        const string DebugTag = "[Project Manager]";
        const string SeriesTag = "[S]";

        // Settings node.
        private ConfigNode rootNode;

        public override void OnAwake()
        {
            base.OnAwake();

            // Create external reference.
            Instance = this;

            Debug.Log(DebugTag + " Loaded plugin.");

            // Subscribe to vessel rollout event.
            GameEvents.OnVesselRollout.Add(ApplyLaunchNumber);
        }

        public void ApplyLaunchNumber(ShipConstruct shipConstruct)
        {
            var vessel = FlightGlobals.ActiveVessel;

            Debug.Log(DebugTag + " Launching new vessel (" + vessel.vesselName + ").");

            string rolloutName = vessel.vesselName;

            // Check if vessel name includes the series tag.
            if(rolloutName.Contains(SeriesTag))
            {
                Debug.Log(DebugTag + " Vessel is a series craft.");

                var projectNodes = rootNode.GetNodes();

                if(projectNodes != null)
                {
                    // Node name will be rolloutname, with all whitespace stripped.
                    string nodeName = Regex.Replace(rolloutName, @"\s+", "");

                    // Store whether a project entry already exists.
                    bool projectFound = false;

                    foreach(var projectNode in projectNodes)
                    {
                        if(projectNode.name == nodeName)
                        {
                            Debug.Log(DebugTag + " Project definition found.");
                            projectFound = true;
                        }
                    }
                    

                    // Increment launch count and rename, or create new project entry.
                    if (projectFound)
                    {
                        // Strip spaces from vessel name and store in the settings file.
                        var projectNode = rootNode.GetNode(nodeName);

                        if(projectNode != null)
                        {
                            string launchCountString = projectNode.GetValue("launchCount");

                            if(launchCountString != null)
                            {
                                int launchCount = Convert.ToInt32(launchCountString);
                                int newLaunchCount = launchCount + 1;

                                string launchName = rolloutName.Replace(SeriesTag, "");
                                launchName = string.Format("{0} {1}",launchName, newLaunchCount);

                                vessel.vesselName = launchName;
                                projectNode.SetValue("launchCount", newLaunchCount.ToString());
                            }
                        }
                    }
                    else
                    {
                        string launchName = rolloutName.Replace(SeriesTag, "");
                        launchName = string.Format("{0} {1}",launchName, 1);

                        vessel.vesselName = launchName;

                        // Strip spaces from vessel name and store in the settings file.
                        var projectNode = rootNode.AddNode(nodeName);
                        projectNode.AddValue("launchCount", "1");
                    }
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            // Save the rootnode to a file on disk.
            rootNode.Save(KSPUtil.ApplicationRootPath + "GameData/ProjectManager/ProjectManager.settings");
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            // Get root node from settings file on disk.
            rootNode = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/ProjectManager/ProjectManager.settings");

            if(rootNode == null)
            {
                rootNode = new ConfigNode("PROJECTS");
                rootNode.Save(KSPUtil.ApplicationRootPath + "GameData/ProjectManager/ProjectManager.settings");
            }
        }
    }
}
