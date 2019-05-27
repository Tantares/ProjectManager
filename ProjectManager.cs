using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ProjectManager
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT)]
    public class ProjectManager : ScenarioModule
    {
        // Constants.

        private const string NODE_NAME_PROJECT_MANAGER = "PROJECTMANAGER";
        private const string VALUE_NAME_LAUNCH_NUMBER = "LAUNCH_NUMBER";
        private const string LOG_PREFIX = "Project Manager";
        private const string ROMAN_NUMERAL_FLAG = @"]*";
        private const string REGEX_PROJECT_FORMAT = @"\[([^]]*)\]";

        // Fields.

        private ConfigNode project_manager_node;

        public void Start()
        {
            // Subscribe to the launch event.
            GameEvents.OnVesselRollout.Add(ApplyLaunchNumber);
        }

        public void OnDisable()
        {
            // Clean up, un-subscribe from launch event.
            GameEvents.OnVesselRollout.Remove(ApplyLaunchNumber);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            // Remove the old project node.
            node.RemoveNode(NODE_NAME_PROJECT_MANAGER);
            
            // Write the new project node.
            node.AddNode(project_manager_node);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (node.HasNode(NODE_NAME_PROJECT_MANAGER))
            {
                project_manager_node = node.GetNode(NODE_NAME_PROJECT_MANAGER);
            }
            else
            {
                project_manager_node = new ConfigNode(NODE_NAME_PROJECT_MANAGER);
            }
        }

        private void ApplyLaunchNumber(ShipConstruct ship_construct)
        {
            // Get the vessel.

            var vessel = FlightGlobals.ActiveVessel;

            if (vessel is null)
                return;

            Debug.LogFormat("[{0}] Starting up.",LOG_PREFIX);

            // Get the project name from the vessel name.

            string project_name = GetProjectNameFromVesselName(vessel.vesselName);

            if (project_name is null)
                return;

            Debug.LogFormat("[{0}] Project name is {1}.", LOG_PREFIX, project_name);

            // Initialise the launch number at one.

            int launch_number = 1;

            // Increment on the previous launch number, if it's saved.

            int? previous_launch_number = GetPreviousLaunchNumber(project_name);

            if (previous_launch_number != null)
                launch_number = previous_launch_number.Value + 1;

            Debug.LogFormat("[{0}] Launch number is {1}.", LOG_PREFIX,launch_number);

            // Rename the vessel.

            vessel.vesselName = GetFormattedVesselName(vessel.vesselName, project_name, launch_number);

            // Write the existing project node, and launch number.

            if (project_manager_node.HasNode(project_name))
            {
                var project_node = project_manager_node.GetNode(project_name);
                project_node.SetValue(VALUE_NAME_LAUNCH_NUMBER, launch_number, true);
            }
            else
            {
                var project_node = project_manager_node.AddNode(project_name);
                project_node.SetValue(VALUE_NAME_LAUNCH_NUMBER, launch_number, true);
            }
        }


        private string GetProjectNameFromVesselName(string vessel_name)
        {
            var match = Regex.Match(vessel_name, REGEX_PROJECT_FORMAT);

            if (!match.Success)
                return null;

            if (match.Value.Length == 0)
                return null;

            return match.Value
                .Replace("[",string.Empty)
                .Replace("]",string.Empty);
        }

        private int? GetPreviousLaunchNumber(string project_name)
        {
            if (!project_manager_node.HasNode(project_name))
                return null;

            var project_node = project_manager_node.GetNode(project_name);

            if (!project_node.HasValue(VALUE_NAME_LAUNCH_NUMBER))
                return null;

            string launch_number_text = project_node.GetValue(VALUE_NAME_LAUNCH_NUMBER);

            try
            {
                int launch_number = Convert.ToInt32(launch_number_text);

                return launch_number;
            }
            catch(InvalidCastException e)
            {
                return null;
            }
        }

        private string GetFormattedVesselName(string vessel_name, string project_name, int launch_number)
        {
            if (vessel_name.Contains(ROMAN_NUMERAL_FLAG))
            {
                return string.Format("{0} {1}", project_name, GetRomanNumeral(launch_number));
            }
            else
            {
                return string.Format("{0} {1}", project_name, launch_number);
            }
        }

        private string GetRomanNumeral(int number)
        {
            if ((number < 0) || (number > 3999)) return "?";
            if (number < 1) return string.Empty;
            if (number >= 1000) return "M" + GetRomanNumeral(number - 1000);
            if (number >= 900) return "CM" + GetRomanNumeral(number - 900);
            if (number >= 500) return "D" + GetRomanNumeral(number - 500);
            if (number >= 400) return "CD" + GetRomanNumeral(number - 400);
            if (number >= 100) return "C" + GetRomanNumeral(number - 100);
            if (number >= 90) return "XC" + GetRomanNumeral(number - 90);
            if (number >= 50) return "L" + GetRomanNumeral(number - 50);
            if (number >= 40) return "XL" + GetRomanNumeral(number - 40);
            if (number >= 10) return "X" + GetRomanNumeral(number - 10);
            if (number >= 9) return "IX" + GetRomanNumeral(number - 9);
            if (number >= 5) return "V" + GetRomanNumeral(number - 5);
            if (number >= 4) return "IV" + GetRomanNumeral(number - 4);
            if (number >= 1) return "I" + GetRomanNumeral(number - 1);
            return "?";
        }
    }
}
