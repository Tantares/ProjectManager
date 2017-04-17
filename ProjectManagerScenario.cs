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
    class ProjectManagerScenario : ScenarioModule
    {
        public static ProjectManagerScenario Instance { get; private set; }

        // String constants.
        const string DebugTag = "[Project Manager]";

        // Settings node.
        private ConfigNode rootNode;

        public override void OnAwake()
        {
            base.OnAwake();

            // Create singleton accessor.
            Instance = this;

            // Subscribe to launch event.
            GameEvents.OnVesselRollout.Add(ApplyLaunchNumber);
        }

        public void ApplyLaunchNumber(ShipConstruct shipConstruct)
        {
            // Get rollout vessel.
            var vessel = FlightGlobals.ActiveVessel;

            // Get rollout name.
            string rolloutName = vessel.vesselName;

            if(!string.IsNullOrEmpty(rolloutName))
            {
                // Pattern to detect any substrings in square brackets.
                var seriesPattern = @"\[(.*?)\]";
                var seriesMatch = Regex.Match(rolloutName, seriesPattern);

                // Check if a series tag is in the vessel rollout name.
                if(seriesMatch.Value != string.Empty)
                {
                    // Get the name of the series.
                    string seriesName = seriesMatch.Value;

                    // Strip square brackets from series name.
                    seriesName = seriesName.Replace("[", "");
                    seriesName = seriesName.Replace("]", "");

                    // Create series node name by stripping any whitespace.
                    string seriesNodeName = Regex.Replace(seriesName, @"\s+", "");

                    var projectNodes = rootNode.GetNodes();

                    // Store whether a project entry already exists.
                    bool projectFound = false;

                    foreach (var projectNode in projectNodes)
                    {
                        if (projectNode.name == seriesNodeName)
                        {
                            projectFound = true;
                        }
                    }

                    if (projectFound)
                    {
                        var projectNode = rootNode.GetNode(seriesNodeName);

                        if(projectNode != null)
                        {
                            string launchCountString = projectNode.GetValue("launchCount");

                            if(!string.IsNullOrEmpty(launchCountString))
                            {
                                // Get the current launch count and increment it.
                                int launchCount = Convert.ToInt32(launchCountString);
                                launchCount++;

                                // Construct the new vessel name.
                                string launchName = string.Format("{0} {1}", seriesName, launchCount);

                                // Apply new name to vessel.
                                vessel.vesselName = launchName;

                                // Write new launchcount back to node.
                                projectNode.SetValue("launchCount", launchCount.ToString());
                            }
                        }
                    }
                    else
                    {
                        // Construct the new vessel name.
                        string launchName = string.Format("{0} {1}", seriesName, 1);

                        // Apply new name to vessel.
                        vessel.vesselName = launchName;

                        // Create new entry for this series, set launch count to one.
                        var projectNode = rootNode.AddNode(seriesNodeName);
                        projectNode.AddValue("launchCount", "1");
                    }
                }
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            // Get root node from settings file on disk.
            rootNode = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/ProjectManager/ProjectManager.settings");

            if (rootNode == null)
            {
                rootNode = new ConfigNode("PROJECTS");
                rootNode.Save(KSPUtil.ApplicationRootPath + "GameData/ProjectManager/ProjectManager.settings");
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            // Save the rootnode to a file on disk.
            rootNode.Save(KSPUtil.ApplicationRootPath + "GameData/ProjectManager/ProjectManager.settings");
        }
    }
}
