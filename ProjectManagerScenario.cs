using System;
using UnityEngine;
using System.Text.RegularExpressions;


namespace ProjectManager
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER)]
    class ProjectManagerScenario : ScenarioModule
    {
        public static ProjectManagerScenario Instance { get; private set; }

        // String constants.
        const string DebugTag = "[Project Manager]";

        // Settings node.
        public ConfigNode rootNode;

        static bool subscribed = false;

        public override void OnAwake()
        {
            base.OnAwake();

            // Create singleton accessor.
            Instance = this;

            // Subscribe to launch event.
            if(!subscribed)
            {
                GameEvents.OnVesselRollout.Add(ApplyLaunchNumber);
                Debug.LogFormat("{0} Subscribed to rollout event.",DebugTag);
                subscribed = true;
            }
            
        }

        public void ApplyLaunchNumber(ShipConstruct shipConstruct)
        {
            // Get rollout vessel.
            var vessel = FlightGlobals.ActiveVessel;

            if(vessel == null)
            {
                return;
            }

            // Get rollout vesel name.
            string rolloutName = vessel.vesselName;

            if (string.IsNullOrEmpty(rolloutName))
            {
                Debug.LogFormat("{0} Vessel name was blank.", DebugTag);
                return;
            }

            // Pattern to detect any substrings in square brackets.
            var seriesPattern = @"\[(.*?)\]";
            var seriesMatch = Regex.Match(rolloutName, seriesPattern);

            // Check if a series tag is in the vessel rollout name.
            if (seriesMatch.Value == string.Empty)
            {
                Debug.LogFormat("{0} Vessel was not part of a series.", DebugTag);
                return;
            }

            // Get the name of the series.
            string seriesName = seriesMatch.Value;

            // Strip square brackets from series name.
            seriesName = seriesName.Replace("[", "");
            seriesName = seriesName.Replace("]", "");

            // Create project node name by stripping any whitespace.
            string projectName = Regex.Replace(seriesName, @"\s+", "");

            // Strip all special characters from the project name.
            projectName = Regex.Replace(projectName, "[^0-9a-zA-Z]+", "");

            // Get a list of all project nodes.
            var projectNodes = rootNode.GetNodes();

            // Get whether the current project already exists.
            bool projectFound = CheckNodeExists(projectNodes, projectName);

            if (projectFound)
            {
                var projectNode = rootNode.GetNode(projectName);

                string launchCountString = projectNode.GetValue("launchCount");

                if (string.IsNullOrEmpty(launchCountString))
                {
                    Debug.LogErrorFormat("{0} Launch count entry missing.", DebugTag);
                    return;
                }

                // Get the current launch count and increment it.
                int launchCount = Convert.ToInt32(launchCountString);
                launchCount++;

                // Apply new name to vessel.
                vessel.vesselName = string.Format("{0} {1}", seriesName, launchCount);

                // Write new launchcount back to node.
                projectNode.SetValue("launchCount", launchCount.ToString());
                
            }
            else
            {
                int launchCount = 1;

                // Apply new name to vessel.
                vessel.vesselName = string.Format("{0} {1}", seriesName, launchCount);

                // Create new entry for this series, set launch count to one.
                var projectNode = rootNode.AddNode(projectName);
                projectNode.AddValue("launchCount", launchCount.ToString());
            }          
        }

        private bool CheckNodeExists(ConfigNode[] nodes, string nodeName)
        {
            foreach(var node in nodes)
            {
                if(node.name == nodeName)
                {
                    return true;
                }
            }

            return false;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            string saveFolder = HighLogic.SaveFolder;

            // Get root node from settings file on disk.
            rootNode = ConfigNode.Load(KSPUtil.ApplicationRootPath + "saves/" + saveFolder + "/ProjectManager.settings");

            if (rootNode == null)
            {
                rootNode = new ConfigNode("PROJECTS");
                rootNode.Save(KSPUtil.ApplicationRootPath + "saves/" + saveFolder + "/ProjectManager.settings");
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            string saveFolder = HighLogic.SaveFolder;

            // Save the rootnode to a file on disk.
            rootNode.Save(KSPUtil.ApplicationRootPath + "saves/" + saveFolder + "/ProjectManager.settings");
        }
    }
}
