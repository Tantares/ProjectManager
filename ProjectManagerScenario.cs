using System;
using System.Text.RegularExpressions;

namespace ProjectManager
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new GameScenes[] { GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT })]
    public class ProjectManagerScenario : ScenarioModule
    {
        #region Fields

        public const string launchCountKey = "launchCount";
        public const string seriesNameKey = "seriesName";
        public static ConfigNode rootNode;

        #endregion Fields

        #region Properties

        public static ProjectManagerScenario Instance { get; private set; }

        #endregion Properties

        #region Methods

        public void ApplyLaunchNumber(ShipConstruct shipConstruct)
        {
            // Get rollout vessel.
            var vessel = FlightGlobals.ActiveVessel;

            // Get rollout name.
            string rolloutName = vessel.vesselName;

            if (!string.IsNullOrEmpty(rolloutName))
            {
                // Pattern to detect any substrings in square brackets.
                var seriesPattern = @"\[(.*?)\]";
                var seriesMatch = Regex.Match(rolloutName, seriesPattern);

                // Check if a series tag is in the vessel rollout name.
                if (seriesMatch.Value != string.Empty)
                {
                    // Get the name of the series.
                    string seriesName = seriesMatch.Value;

                    // Strip square brackets from series name.
                    seriesName = seriesName.Replace("[", string.Empty).Replace("]", string.Empty).Trim();

                    // NOTE: Whitespace should be allowed in series name, so switch to internally using this as a node key while the series name is saved in config
                    string seriesId = Regex.Replace(seriesName, @"\s+", "");

                    var projectNodes = rootNode.GetNodes();

                    // Store whether a project entry already exists.
                    bool projectFound = false;

                    foreach (var projectNode in projectNodes)
                    {
                        if (projectNode.name == seriesId)
                        {
                            projectFound = true;
                        }
                    }

                    if (projectFound)
                    {
                        var projectNode = rootNode.GetNode(seriesId);

                        if (projectNode != null)
                        {
                            string launchCountString = projectNode.GetValue(launchCountKey);

                            if (!string.IsNullOrEmpty(launchCountString))
                            {
                                // Get the current launch count and increment it.
                                int launchCount = Convert.ToInt32(launchCountString);
                                launchCount++;

                                // Construct the new vessel name.
                                string launchName = string.Format("{0} {1}", seriesName, launchCount);

                                // Apply new name to vessel.
                                vessel.vesselName = launchName;

                                // Write new launchcount back to node.
                                projectNode.SetValue(launchCountKey, launchCount.ToString(), true);
                                projectNode.SetValue(seriesNameKey, seriesName, true);
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
                        var projectNode = rootNode.AddNode(seriesId);
                        projectNode.AddValue(launchCountKey, "1");
                        projectNode.AddValue(seriesNameKey, seriesName);
                    }
                }
            }
        }

        public override void OnAwake()
        {
            base.OnAwake();

            // Create singleton accessor.
            Instance = this;

            // Subscribe to launch event.
            GameEvents.OnVesselRollout.Add(ApplyLaunchNumber);
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

        #endregion Methods
    }
}