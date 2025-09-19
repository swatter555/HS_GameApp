using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityEngine;
using HammerAndSickle.Models;
using HammerAndSickle.Controllers;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HammerAndSickle.Tools
{
    /// <summary>
    /// A flat data representation of a combat unit for serialization to/from OOB files.
    /// </summary>
    [Serializable]
    public class OobUnitData
    {
        public string UnitID { get; set; }
        public string UnitName { get; set; }
        public float MapPosX { get; set; }
        public float MapPosY { get; set; }
        public Side Side { get; set; }
        public Nationality Nationality { get; set; }
        public UnitClassification Classification { get; set; }
        public UnitRole Role { get; set; }
        public IntelProfileTypes IntelProfileType { get; set; }
        public WeaponSystems DeployedProfileID { get; set; }
        public WeaponSystems MobileProfileID { get; set; }
        public WeaponSystems EmbarkedProfileID { get; set; }
        public bool IsMountable { get; set; }
        public bool IsEmbarkable { get; set; }
        public ExperienceLevel Experience { get; set; }
        public EfficiencyLevel Efficiency { get; set; }
        public DeploymentPosition Deployment { get; set; }
        public SpottedLevel Spotted { get; set; }
        public float ICM { get; set; }
        public float HitPointsPercent { get; set; }
        public float SupplyPercent { get; set; }
        public DepotCategory DepotCategory { get; set; }
        public DepotSize DepotSize { get; set; }
        public List<string> AttachedAirUnitIDs { get; set; } = new List<string>();
    }

    /// <summary>
    /// Unity Editor tool for creating and managing combat units for OOB files.
    /// Only active during play mode to utilize the initialized game systems.
    /// </summary>
    public class UnitEntryTool : MonoBehaviour
    {
        #region Private Fields

        // UI State
        [System.NonSerialized]
        private List<CombatUnit> playerUnits = new List<CombatUnit>();
        [System.NonSerialized]
        private List<CombatUnit> aiUnits = new List<CombatUnit>();
        private string saveFileName = "new_oob";
        private string loadFileName = "";
        private string statusMessage = "Ready";

        // Template Selection
        private string selectedTemplateId = "";
        private int selectedTemplateIndex = 0;
        [System.NonSerialized]
        private List<string> allTemplateIds = new List<string>();
        private string templateName = "";
        private string templateNationality = "";
        private string templateClassification = "";

        // Unit Configuration
        private string unitName = "";
        private Vector2 mapPosition = Vector2.zero;
        private Nationality unitNationality = Nationality.USSR;
        private UnitRole unitRole = UnitRole.GroundCombat;
        private ExperienceLevel experienceLevel = ExperienceLevel.Elite;
        private EfficiencyLevel efficiencyLevel = EfficiencyLevel.FullOperations;
        private DeploymentPosition deploymentPosition = DeploymentPosition.Deployed;
        private SpottedLevel spottedLevel = SpottedLevel.Level1;
        private float icm = 1.0f;
        private float hitPointsPercent = 1.0f;
        private float supplyPercent = 1.0f;

        // Attachments (for AIRB units)
        [System.NonSerialized]
        private List<CombatUnit> availableAirUnits = new List<CombatUnit>();
        [System.NonSerialized]
        private List<CombatUnit> attachedAirUnits = new List<CombatUnit>();
        private Vector2 availableAirScrollPos;
        private Vector2 attachedAirScrollPos;

        // Airbase Management
        [System.NonSerialized]
        private CombatUnit selectedAirbase = null;
        private int selectedAirbaseIndex = -1;
        private Vector2 airbaseScrollPos;

        // UI State
        private Vector2 playerUnitsScrollPos;
        private Vector2 aiUnitsScrollPos;
        private bool isInitialized = false;
        private bool showTemplateDropdown = false;
        private Vector2 templateDropdownScrollPos;
        private Vector2 attachedAirbaseScrollPos;

        [Header("UI Settings")]
        [SerializeField] public bool showGUI = false;

        // Constants
        private const string SAVE_DIRECTORY = "Assets/Data Files/oob";
        private const int WINDOW_WIDTH = 1000;
        private const int WINDOW_HEIGHT = 1200;

        #endregion

        #region Unity Lifecycle

        void Start()
        {
            if (!Application.isPlaying)
                return;

            InitializeTool();
        }

        void OnGUI()
        {
            if (!Application.isPlaying || !isInitialized || !showGUI)
                return;

            // Center the GUI window on screen
            float centerX = (Screen.width - WINDOW_WIDTH) / 2f;
            float centerY = (Screen.height - WINDOW_HEIGHT) / 2f;

            // Draw black background panel (80% opaque)
            Color backgroundColor = new Color(0f, 0f, 0f, 0.8f);
            GUI.color = backgroundColor;
            GUI.DrawTexture(new Rect(centerX, centerY, WINDOW_WIDTH, WINDOW_HEIGHT), Texture2D.whiteTexture);
            GUI.color = Color.white; // Reset color for other elements

            GUILayout.BeginArea(new Rect(centerX, centerY, WINDOW_WIDTH, WINDOW_HEIGHT));
            GUILayout.Label("Unit Entry Tool", GUI.skin.label);

            // Top row - File Operations and Template Selection side by side
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(WINDOW_WIDTH / 2 - 10));
            DrawFileOperationsSection();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical(GUILayout.Width(WINDOW_WIDTH / 2 - 10));
            DrawTemplateSelectionSection();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Middle row - Unit Configuration
            DrawUnitConfigurationSection();
            GUILayout.Space(10);

            // Attachments section (for AIRB units)
            //DrawAttachmentsSection();
            GUILayout.Space(10);

            // Bottom row - Unit Management (most important - the lists)
            DrawUnitManagementSection();

            GUILayout.EndArea();
        }

        #endregion

        #region Initialization

        private void InitializeTool()
        {
            try
            {
                // Ensure non-serialized fields are initialized (important after Unity recompilation)
                if (playerUnits == null) playerUnits = new List<CombatUnit>();
                if (aiUnits == null) aiUnits = new List<CombatUnit>();
                if (allTemplateIds == null) allTemplateIds = new List<string>();

                // Initialize databases if not already done
                if (!GameDataManager.AreAllDatabasesInitialized())
                {
                    statusMessage = "Databases not initialized - waiting...";
                    return;
                }

                // Initialize template data
                LoadAllTemplates();

                // Ensure save directory exists
                EnsureSaveDirectoryExists();

                isInitialized = true;
                statusMessage = "Tool initialized successfully";
            }
            catch (Exception e)
            {
                statusMessage = $"Initialization failed: {e.Message}";
                Debug.LogError($"UnitEntryTool initialization failed: {e}");
            }
        }

        private void LoadAllTemplates()
        {
            try
            {
                allTemplateIds = GameDataManager.GetAllTemplateIds();
                if (allTemplateIds.Count > 0)
                {
                    selectedTemplateIndex = 0;
                    selectedTemplateId = allTemplateIds[0];
                    UpdateTemplateDetails();
                }
                else
                {
                    statusMessage = "No templates found in database";
                }
            }
            catch (Exception e)
            {
                statusMessage = $"Error loading templates: {e.Message}";
                Debug.LogError($"UnitEntryTool LoadAllTemplates failed: {e}");
            }
        }

        private void UpdateTemplateDetails()
        {
            try
            {
                var template = GameDataManager.GetUnitTemplate(selectedTemplateId);
                if (template != null)
                {
                    templateName = template.UnitName;
                    templateNationality = template.Nationality.ToString();
                    templateClassification = template.Classification.ToString();

                    // Auto-populate unit configuration from template
                    unitName = template.UnitName; // Start with template name, user can modify
                    unitNationality = template.Nationality; // Set from template but allow override
                    unitRole = template.Role; // Set from template but allow override
                    // Reset other values to defaults
                    mapPosition = Vector2.zero;
                    experienceLevel = ExperienceLevel.Trained;
                    efficiencyLevel = EfficiencyLevel.FullOperations;
                    deploymentPosition = DeploymentPosition.Deployed;
                    spottedLevel = SpottedLevel.Level1;
                    icm = 1.0f;
                    hitPointsPercent = 1.0f;
                    supplyPercent = 1.0f;

                    // Clear air unit attachments when switching templates
                    attachedAirUnits.Clear();
                }
                else
                {
                    templateName = "Template not found";
                    templateNationality = "Unknown";
                    templateClassification = "Unknown";
                }
            }
            catch (Exception e)
            {
                statusMessage = $"Error updating template details: {e.Message}";
                Debug.LogError($"UnitEntryTool UpdateTemplateDetails failed: {e}");
            }
        }

        private void EnsureSaveDirectoryExists()
        {
            if (!Directory.Exists(SAVE_DIRECTORY))
            {
                Directory.CreateDirectory(SAVE_DIRECTORY);
            }
        }

        #endregion

        #region UI Sections

        private void DrawFileOperationsSection()
        {
            GUILayout.Label("File Operations", GUI.skin.box);
            GUILayout.BeginHorizontal();

            GUILayout.Label("Load File Name:", GUILayout.Width(100));
            loadFileName = GUILayout.TextField(loadFileName, GUILayout.Width(200));

            if (GUILayout.Button("Load", GUILayout.Width(60)))
            {
                LoadOobFile();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Save File Name:", GUILayout.Width(100));
            saveFileName = GUILayout.TextField(saveFileName, GUILayout.Width(200));

            if (GUILayout.Button("Save OOB", GUILayout.Width(80)))
            {
                SaveOobFile();
            }

            GUILayout.EndHorizontal();

            GUILayout.Label($"Status: {statusMessage}");
        }

        private void DrawTemplateSelectionSection()
        {
            GUILayout.Label("Template Selection", GUI.skin.box);

            // Template Dropdown (Primary Selection)
            GUILayout.BeginHorizontal();
            GUILayout.Label("Template:", GUILayout.Width(80));
            if (allTemplateIds.Count > 0)
            {
                if (GUILayout.Button($"{selectedTemplateId} ▼", GUILayout.Width(300)))
                {
                    showTemplateDropdown = !showTemplateDropdown;
                }

                // Dropdown list
                if (showTemplateDropdown)
                {
                    GUILayout.EndHorizontal();
                    Rect dropdownRect = GUILayoutUtility.GetRect(300, Mathf.Min(200, allTemplateIds.Count * 20));
                    GUI.Box(dropdownRect, "");

                    Rect scrollRect = new Rect(dropdownRect.x, dropdownRect.y, dropdownRect.width, dropdownRect.height);
                    Rect contentRect = new Rect(0, 0, dropdownRect.width - 20, allTemplateIds.Count * 20);

                    templateDropdownScrollPos = GUI.BeginScrollView(scrollRect, templateDropdownScrollPos, contentRect);

                    for (int i = 0; i < allTemplateIds.Count; i++)
                    {
                        Rect itemRect = new Rect(5, i * 20, dropdownRect.width - 25, 18);

                        if (i == selectedTemplateIndex)
                        {
                            GUI.backgroundColor = Color.cyan;
                            GUI.Box(itemRect, "");
                            GUI.backgroundColor = Color.white;
                        }

                        if (GUI.Button(itemRect, allTemplateIds[i], GUI.skin.label))
                        {
                            selectedTemplateIndex = i;
                            selectedTemplateId = allTemplateIds[i];
                            UpdateTemplateDetails();
                            showTemplateDropdown = false;
                        }
                    }

                    GUI.EndScrollView();
                    GUILayout.BeginHorizontal();
                }
            }
            else
            {
                GUILayout.Label("No templates available", GUILayout.Width(300));
            }
            GUILayout.EndHorizontal();

            // Template Name (Read-only confirmation)
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:", GUILayout.Width(80));
            GUILayout.TextField(templateName, GUILayout.Width(300));
            GUILayout.EndHorizontal();

            // Nationality (Read-only, from template)
            GUILayout.BeginHorizontal();
            GUILayout.Label("Nationality:", GUILayout.Width(80));
            GUILayout.TextField(templateNationality, GUILayout.Width(120));
            GUILayout.EndHorizontal();

            // Classification (Read-only, from template)
            GUILayout.BeginHorizontal();
            GUILayout.Label("Classification:", GUILayout.Width(80));
            GUILayout.TextField(templateClassification, GUILayout.Width(120));
            GUILayout.EndHorizontal();
        }

        private void DrawUnitConfigurationSection()
        {
            GUILayout.BeginHorizontal();

            // Left Column - Unit Configuration
            GUILayout.BeginVertical(GUILayout.Width(WINDOW_WIDTH / 2 - 10));
            GUILayout.Label("Unit Configuration", GUI.skin.box);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Unit Name:", GUILayout.Width(100));
            unitName = GUILayout.TextField(unitName, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Map Position:", GUILayout.Width(100));
            GUILayout.Label("X:", GUILayout.Width(20));
            string xText = GUILayout.TextField(mapPosition.x.ToString(), GUILayout.Width(60));
            if (float.TryParse(xText, out float x)) mapPosition.x = x;
            GUILayout.Label("Y:", GUILayout.Width(20));
            string yText = GUILayout.TextField(mapPosition.y.ToString(), GUILayout.Width(60));
            if (float.TryParse(yText, out float y)) mapPosition.y = y;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Nationality:", GUILayout.Width(100));
            if (GUILayout.Button(unitNationality.ToString(), GUILayout.Width(120)))
            {
                var natValues = Enum.GetValues(typeof(Nationality));
                int currentIndex = Array.IndexOf(natValues, unitNationality);
                currentIndex = (currentIndex + 1) % natValues.Length;
                unitNationality = (Nationality)natValues.GetValue(currentIndex);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Unit Role:", GUILayout.Width(100));
            if (GUILayout.Button(unitRole.ToString(), GUILayout.Width(120)))
            {
                var roleValues = Enum.GetValues(typeof(UnitRole));
                int currentIndex = Array.IndexOf(roleValues, unitRole);
                currentIndex = (currentIndex + 1) % roleValues.Length;
                unitRole = (UnitRole)roleValues.GetValue(currentIndex);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Experience:", GUILayout.Width(100));
            if (GUILayout.Button(experienceLevel.ToString(), GUILayout.Width(120)))
            {
                var expValues = Enum.GetValues(typeof(ExperienceLevel));
                int currentIndex = Array.IndexOf(expValues, experienceLevel);
                currentIndex = (currentIndex + 1) % expValues.Length;
                experienceLevel = (ExperienceLevel)expValues.GetValue(currentIndex);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Efficiency:", GUILayout.Width(100));
            if (GUILayout.Button(efficiencyLevel.ToString(), GUILayout.Width(120)))
            {
                var effValues = Enum.GetValues(typeof(EfficiencyLevel));
                int currentIndex = Array.IndexOf(effValues, efficiencyLevel);
                currentIndex = (currentIndex + 1) % effValues.Length;
                efficiencyLevel = (EfficiencyLevel)effValues.GetValue(currentIndex);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Deployment:", GUILayout.Width(100));
            if (GUILayout.Button(deploymentPosition.ToString(), GUILayout.Width(120)))
            {
                var depValues = Enum.GetValues(typeof(DeploymentPosition));
                int currentIndex = Array.IndexOf(depValues, deploymentPosition);
                currentIndex = (currentIndex + 1) % depValues.Length;
                deploymentPosition = (DeploymentPosition)depValues.GetValue(currentIndex);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Spotted Level:", GUILayout.Width(100));
            if (GUILayout.Button(spottedLevel.ToString(), GUILayout.Width(120)))
            {
                var spotValues = Enum.GetValues(typeof(SpottedLevel));
                int currentIndex = Array.IndexOf(spotValues, spottedLevel);
                currentIndex = (currentIndex + 1) % spotValues.Length;
                spottedLevel = (SpottedLevel)spotValues.GetValue(currentIndex);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Combat Modifier:", GUILayout.Width(100));
            icm = GUILayout.HorizontalSlider(icm, 0.5f, 2.0f, GUILayout.Width(150));
            GUILayout.Label(icm.ToString("F2"), GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hit Points %:", GUILayout.Width(100));
            hitPointsPercent = GUILayout.HorizontalSlider(hitPointsPercent, 0f, 1f, GUILayout.Width(150));
            GUILayout.Label((hitPointsPercent * 100).ToString("F0") + "%", GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Supply %:", GUILayout.Width(100));
            supplyPercent = GUILayout.HorizontalSlider(supplyPercent, 0f, 1f, GUILayout.Width(150));
            GUILayout.Label((supplyPercent * 100).ToString("F0") + "%", GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            if (GUILayout.Button("Create Unit", GUILayout.Height(30)))
            {
                CreateUnitFromTemplate();
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Right Column - Airbase Management
            GUILayout.BeginVertical(GUILayout.Width(WINDOW_WIDTH / 2 - 10));
            DrawAirbaseManagementSection();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawAirbaseManagementSection()
        {
            GUILayout.Label("Airbase Management", GUI.skin.box);

            // Get all airbases from both lists
            var allAirbases = playerUnits.Concat(aiUnits)
                .Where(u => u.Classification == UnitClassification.AIRB)
                .ToList();

            // Airbase selection dropdown
            GUILayout.BeginHorizontal();
            GUILayout.Label("Select Airbase:", GUILayout.Width(100));

            string airbaseName = selectedAirbase != null ? selectedAirbase.UnitName : "None Selected";
            if (GUILayout.Button($"{airbaseName} ▼", GUILayout.Width(250)))
            {
                // Simple cycling through airbases
                if (allAirbases.Count > 0)
                {
                    selectedAirbaseIndex = (selectedAirbaseIndex + 1) % allAirbases.Count;
                    selectedAirbase = allAirbases[selectedAirbaseIndex];
                }
            }
            GUILayout.EndHorizontal();

            if (selectedAirbase == null || allAirbases.Count == 0)
            {
                GUILayout.Label("No airbases created yet", GUI.skin.label);
                return;
            }

            // Show airbase details
            GUILayout.Label($"Location: ({selectedAirbase.MapPos.X}, {selectedAirbase.MapPos.Y})", GUI.skin.label);
            GUILayout.Label($"Side: {selectedAirbase.Side}", GUI.skin.label);
            GUILayout.Label($"Attached Units: {selectedAirbase.AirUnitsAttached.Count}/4", GUI.skin.label);

            GUILayout.Space(10);

            // Available air units for attachment
            var availableForAttachment = playerUnits.Concat(aiUnits)
                .Where(u => IsAirUnit(u) && u.Classification != UnitClassification.HELO)
                .Where(u => !IsAttachedToAnyAirbase(u))
                .ToList();

            GUILayout.Label("Available Air Units", GUI.skin.box);
            airbaseScrollPos = GUILayout.BeginScrollView(airbaseScrollPos, GUILayout.Height(150));

            foreach (var airUnit in availableForAttachment)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{airUnit.UnitName}", GUILayout.Width(150));
                GUILayout.Label($"{airUnit.Classification}", GUILayout.Width(50));

                if (GUILayout.Button("Attach", GUILayout.Width(60)))
                {
                    if (selectedAirbase.AirUnitsAttached.Count < 4)
                    {
                        selectedAirbase.AddAirUnit(airUnit);
                        statusMessage = $"Attached {airUnit.UnitName} to {selectedAirbase.UnitName}";
                    }
                    else
                    {
                        statusMessage = "Airbase is at capacity (4 units)";
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (availableForAttachment.Count == 0)
            {
                GUILayout.Label("No air units available", GUI.skin.label);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Currently attached units
            GUILayout.Label("Attached Air Units", GUI.skin.box);
            attachedAirbaseScrollPos = GUILayout.BeginScrollView(attachedAirbaseScrollPos, GUILayout.Height(120));

            foreach (var attachedUnit in selectedAirbase.AirUnitsAttached.ToList())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{attachedUnit.UnitName}", GUILayout.Width(150));
                GUILayout.Label($"{attachedUnit.Classification}", GUILayout.Width(50));

                if (GUILayout.Button("Detach", GUILayout.Width(60)))
                {
                    selectedAirbase.RemoveAirUnit(attachedUnit);
                    statusMessage = $"Detached {attachedUnit.UnitName} from {selectedAirbase.UnitName}";
                }
                GUILayout.EndHorizontal();
            }

            if (selectedAirbase.AirUnitsAttached.Count == 0)
            {
                GUILayout.Label("No units attached", GUI.skin.label);
            }

            GUILayout.EndScrollView();
        }

        private bool IsAttachedToAnyAirbase(CombatUnit airUnit)
        {
            var allAirbases = playerUnits.Concat(aiUnits)
                .Where(u => u.Classification == UnitClassification.AIRB);

            foreach (var airbase in allAirbases)
            {
                if (airbase.AirUnitsAttached.Contains(airUnit))
                    return true;
            }
            return false;
        }

        private void DrawUnitManagementSection()
        {
            GUILayout.Label($"★ CREATED UNITS (Player: {playerUnits.Count}, AI: {aiUnits.Count}) ★", GUI.skin.box);

            // Side-by-side layout for Player and AI units
            GUILayout.BeginHorizontal();

            // Player Units Section (Left side)
            GUILayout.BeginVertical(GUILayout.Width(WINDOW_WIDTH / 2 - 20));
            GUILayout.Label("PLAYER UNITS", GUI.skin.box);
            playerUnitsScrollPos = GUILayout.BeginScrollView(playerUnitsScrollPos, GUILayout.Height(200));

            for (int i = 0; i < playerUnits.Count; i++)
            {
                var unit = playerUnits[i];
                GUILayout.BeginHorizontal();

                GUILayout.Label($"{unit.UnitName}", GUILayout.Width(120));
                GUILayout.Label($"{unit.Classification}", GUILayout.Width(50));
                GUILayout.Label($"({unit.MapPos.X}, {unit.MapPos.Y})", GUILayout.Width(60));

                if (GUILayout.Button("Edit", GUILayout.Width(40)))
                {
                    LoadUnitForEditing(unit);
                }

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    RemovePlayerUnit(i);
                }

                GUILayout.EndHorizontal();
            }

            if (playerUnits.Count == 0)
            {
                GUILayout.Label("No Player units created yet", GUI.skin.label);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(20);

            // AI Units Section (Right side)
            GUILayout.BeginVertical(GUILayout.Width(WINDOW_WIDTH / 2 - 20));
            GUILayout.Label("AI UNITS", GUI.skin.box);
            aiUnitsScrollPos = GUILayout.BeginScrollView(aiUnitsScrollPos, GUILayout.Height(200));

            for (int i = 0; i < aiUnits.Count; i++)
            {
                var unit = aiUnits[i];
                GUILayout.BeginHorizontal();

                GUILayout.Label($"{unit.UnitName}", GUILayout.Width(120));
                GUILayout.Label($"{unit.Classification}", GUILayout.Width(50));
                GUILayout.Label($"({unit.MapPos.X}, {unit.MapPos.Y})", GUILayout.Width(60));

                if (GUILayout.Button("Edit", GUILayout.Width(40)))
                {
                    LoadUnitForEditing(unit);
                }

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    RemoveAIUnit(i);
                }

                GUILayout.EndHorizontal();
            }

            if (aiUnits.Count == 0)
            {
                GUILayout.Label("No AI units created yet", GUI.skin.label);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        #endregion

        #region Unit Operations

        private void CreateUnitFromTemplate()
        {
            try
            {
                if (string.IsNullOrEmpty(selectedTemplateId))
                {
                    statusMessage = "No template selected";
                    return;
                }

                if (string.IsNullOrEmpty(unitName))
                {
                    statusMessage = "Unit name required";
                    return;
                }

                var newUnit = GameDataManager.CreateUnitFromTemplate(selectedTemplateId, unitName);
                if (newUnit == null)
                {
                    statusMessage = "Failed to create unit from template";
                    return;
                }

                // Log what we actually created
                Debug.Log($"Created unit: {newUnit.UnitName}, Side: {newUnit.Side}, Nationality: {newUnit.Nationality} (wanted: {unitNationality})");

                // Apply custom settings
                newUnit.SetPosition(new Position2D((int)mapPosition.x, (int)mapPosition.y));
                newUnit.SetEfficiencyLevel(efficiencyLevel);
                newUnit.SetDeploymentPosition(deploymentPosition);
                newUnit.SetSpottedLevel(spottedLevel);
                newUnit.SetICM(icm);

                // Adjust hit points and supply
                newUnit.HitPoints.SetCurrent(newUnit.HitPoints.Max * hitPointsPercent);
                newUnit.DaysSupply.SetCurrent(newUnit.DaysSupply.Max * supplyPercent);

                // Handle air unit attachments for AIRB units
                if (newUnit.Classification == UnitClassification.AIRB)
                {
                    foreach (var airUnit in attachedAirUnits)
                    {
                        newUnit.AddAirUnit(airUnit);
                    }
                }

                // Add to appropriate list based side.
                if (newUnit.Side == Side.Player)
                {
                    playerUnits.Add(newUnit);
                    statusMessage = $"Created Player unit: {unitName} (Nationality: {newUnit.Nationality})";
                }
                else
                {
                    aiUnits.Add(newUnit);
                    statusMessage = $"Created AI unit: {unitName} (Nationality: {newUnit.Nationality})";
                }

                // Prepare form for next unit.
                ResetFormToDefaults();
                RefreshAirUnitsLists();
            }
            catch (Exception e)
            {
                statusMessage = $"Error creating unit: {e.Message}";
                Debug.LogError($"UnitEntryTool CreateUnitFromTemplate failed: {e}");
            }
        }

        private void RemovePlayerUnit(int index)
        {
            if (index >= 0 && index < playerUnits.Count)
            {
                var unit = playerUnits[index];
                string unitName = unit.UnitName;

                // Remove from attachment lists if it's an air unit
                if (IsAirUnit(unit))
                {
                    attachedAirUnits.Remove(unit);
                    availableAirUnits.Remove(unit);
                }

                playerUnits.RemoveAt(index);
                statusMessage = $"Removed Player unit: {unitName}";
            }
        }

        private void RemoveAIUnit(int index)
        {
            if (index >= 0 && index < aiUnits.Count)
            {
                var unit = aiUnits[index];
                string unitName = unit.UnitName;

                // Remove from attachment lists if it's an air unit
                if (IsAirUnit(unit))
                {
                    attachedAirUnits.Remove(unit);
                    availableAirUnits.Remove(unit);
                }

                aiUnits.RemoveAt(index);
                statusMessage = $"Removed AI unit: {unitName}";
            }
        }

        private void LoadUnitForEditing(CombatUnit unit)
        {
            // Sync all form controls with unit data
            unitName = unit.UnitName;
            mapPosition = new Vector2(unit.MapPos.X, unit.MapPos.Y);
            unitNationality = unit.Nationality;
            unitRole = unit.Role;
            experienceLevel = unit.ExperienceLevel;
            efficiencyLevel = unit.EfficiencyLevel;
            deploymentPosition = unit.DeploymentPosition;
            spottedLevel = unit.SpottedLevel;
            icm = unit.IndividualCombatModifier;
            hitPointsPercent = unit.HitPoints.Current / unit.HitPoints.Max;
            supplyPercent = unit.DaysSupply.Current / unit.DaysSupply.Max;

            // Load air unit attachments if this is an AIRB unit
            if (unit.Classification == UnitClassification.AIRB)
            {
                attachedAirUnits.Clear();
                foreach (var airUnit in unit.AirUnitsAttached)
                {
                    attachedAirUnits.Add(airUnit);
                }
            }
            else
            {
                // Clear attachments if not an AIRB unit
                attachedAirUnits.Clear();
            }

            statusMessage = $"Loaded unit for editing: {unit.UnitName}";
        }

        private void ResetFormToDefaults()
        {
            // Reset form to first template defaults
            if (allTemplateIds.Count > 0)
            {
                selectedTemplateIndex = 0;
                selectedTemplateId = allTemplateIds[0];
                UpdateTemplateDetails();
            }
            else
            {
                // Clear form if no templates
                unitName = "";
                templateName = "";
                templateNationality = "";
                templateClassification = "";
            }

            // Reset position and other values
            mapPosition = Vector2.zero;
            experienceLevel = ExperienceLevel.Elite;
            efficiencyLevel = EfficiencyLevel.FullOperations;
            deploymentPosition = DeploymentPosition.Deployed;
            spottedLevel = SpottedLevel.Level1;
            icm = 1.0f;
            hitPointsPercent = 1.0f;
            supplyPercent = 1.0f;

            // Clear attachment lists
            attachedAirUnits.Clear();
        }

        #endregion

        #region File Operations

        private void LoadOobFile()
        {
            try
            {
                if (string.IsNullOrEmpty(loadFileName))
                {
                    statusMessage = "No filename specified";
                    return;
                }

                string filePath = Path.Combine(SAVE_DIRECTORY, $"{loadFileName}.oob");
                if (!File.Exists(filePath))
                {
                    statusMessage = $"File not found: {filePath}";
                    return;
                }

                string jsonContent = File.ReadAllText(filePath);
                if (string.IsNullOrEmpty(jsonContent))
                {
                    statusMessage = "File is empty";
                    return;
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var oobDataList = JsonSerializer.Deserialize<List<OobUnitData>>(jsonContent, options);

                if (oobDataList == null || oobDataList.Count == 0)
                {
                    statusMessage = "No units found in file";
                    return;
                }

                playerUnits.Clear();
                aiUnits.Clear();
                attachedAirUnits.Clear();
                availableAirUnits.Clear();

                // First pass: Create all units from scratch using constructor
                var unitMap = new Dictionary<string, CombatUnit>();

                foreach (var data in oobDataList)
                {
                    // Create unit directly with full constructor
                    var unit = new CombatUnit(
                        data.UnitName,
                        data.Classification,
                        data.Role,
                        data.Side,
                        data.Nationality,
                        data.IntelProfileType,
                        data.DeployedProfileID,
                        data.IsMountable,
                        data.MobileProfileID,
                        data.IsEmbarkable,
                        data.EmbarkedProfileID,
                        data.DepotCategory,
                        data.DepotSize
                    );

                    unit.SetUnitID(data.UnitID);
                    unit.SetPosition(new Position2D(data.MapPosX, data.MapPosY));
                    unit.SetExperienceLevel(data.Experience);
                    unit.SetEfficiencyLevel(data.Efficiency);
                    unit.SetDeploymentPosition(data.Deployment);
                    unit.SetSpottedLevel(data.Spotted);
                    unit.SetICM(data.ICM);
                    unit.HitPoints.SetCurrent(unit.HitPoints.Max * data.HitPointsPercent);
                    unit.DaysSupply.SetCurrent(unit.DaysSupply.Max * data.SupplyPercent);

                    unitMap[unit.UnitID] = unit;

                    if (unit.Side == Side.Player)
                        playerUnits.Add(unit);
                    else
                        aiUnits.Add(unit);
                }

                // Second pass: Restore air attachments
                foreach (var data in oobDataList.Where(d => d.AttachedAirUnitIDs != null && d.AttachedAirUnitIDs.Count > 0))
                {
                    if (unitMap.TryGetValue(data.UnitID, out var airbase))
                    {
                        foreach (var airUnitId in data.AttachedAirUnitIDs)
                        {
                            if (unitMap.TryGetValue(airUnitId, out var airUnit))
                            {
                                airbase.AddAirUnit(airUnit);
                            }
                        }
                    }
                }

                RefreshAirUnitsLists();

                int totalUnits = playerUnits.Count + aiUnits.Count;
                statusMessage = $"Loaded {totalUnits} units (Player: {playerUnits.Count}, AI: {aiUnits.Count}) from {loadFileName}.oob";
            }
            catch (Exception e)
            {
                statusMessage = $"Error loading file: {e.Message}";
                Debug.LogError($"UnitEntryTool LoadOobFile failed: {e}");
            }
        }

        private void SaveOobFile()
        {
            try
            {
                int totalUnits = playerUnits.Count + aiUnits.Count;
                if (totalUnits == 0)
                {
                    statusMessage = "No units to save";
                    return;
                }

                if (string.IsNullOrEmpty(saveFileName))
                {
                    statusMessage = "Save filename required";
                    return;
                }

                // Convert to flat OOB data structure with ALL unit properties
                var oobDataList = new List<OobUnitData>();

                foreach (var unit in playerUnits.Concat(aiUnits))
                {
                    var data = new OobUnitData
                    {
                        UnitID = unit.UnitID,
                        UnitName = unit.UnitName,
                        MapPosX = unit.MapPos.X,
                        MapPosY = unit.MapPos.Y,
                        Side = unit.Side,
                        Nationality = unit.Nationality,
                        Classification = unit.Classification,
                        Role = unit.Role,
                        IntelProfileType = unit.IntelProfileType,
                        DeployedProfileID = unit.DeployedProfileID,
                        MobileProfileID = unit.MobileProfileID,
                        EmbarkedProfileID = unit.EmbarkedProfileID,
                        IsMountable = unit.IsMountable,
                        IsEmbarkable = unit.IsEmbarkable,
                        Experience = unit.ExperienceLevel,
                        Efficiency = unit.EfficiencyLevel,
                        Deployment = unit.DeploymentPosition,
                        Spotted = unit.SpottedLevel,
                        ICM = unit.IndividualCombatModifier,
                        HitPointsPercent = unit.HitPoints.Current / unit.HitPoints.Max,
                        SupplyPercent = unit.DaysSupply.Current / unit.DaysSupply.Max,
                        DepotCategory = unit.DepotCategory,
                        DepotSize = unit.DepotSize
                    };

                    // Handle air attachments for airbases
                    if (unit.Classification == UnitClassification.AIRB && unit.AirUnitsAttached != null)
                    {
                        data.AttachedAirUnitIDs = unit.AirUnitsAttached
                            .Select(au => au.UnitID)
                            .ToList();
                    }

                    oobDataList.Add(data);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(oobDataList, options);
                string filePath = Path.Combine(SAVE_DIRECTORY, $"{saveFileName}.oob");

                File.WriteAllText(filePath, json);

                statusMessage = $"Saved {totalUnits} units (Player: {playerUnits.Count}, AI: {aiUnits.Count}) to {filePath}";
            }
            catch (Exception e)
            {
                statusMessage = $"Error saving file: {e.Message}";
                Debug.LogError($"UnitEntryTool SaveOobFile failed: {e}");
            }
        }

        #endregion

        #region Inspector Button

#if UNITY_EDITOR
        [ContextMenu("Toggle Show GUI")]
        public void ToggleShowGUI()
        {
            showGUI = !showGUI;
        }
#endif

        #endregion

        #region Air Unit Attachment Methods

        private void RefreshAirUnitsLists()
        {
            // Clear the current lists
            availableAirUnits.Clear();

            // Get all air unit templates from both player and AI lists
            var allAirUnits = new List<CombatUnit>();
            allAirUnits.AddRange(playerUnits.Where(IsAirUnit));
            allAirUnits.AddRange(aiUnits.Where(IsAirUnit));

            // Separate into available (unattached) and currently attached
            foreach (var airUnit in allAirUnits)
            {
                if (!attachedAirUnits.Contains(airUnit))
                {
                    availableAirUnits.Add(airUnit);
                }
            }
        }

        private bool IsAirUnit(CombatUnit unit)
        {
            // Check if unit is an air unit type
            return unit.Classification == UnitClassification.FGT ||
                   unit.Classification == UnitClassification.ATT ||
                   unit.Classification == UnitClassification.BMB ||
                   unit.Classification == UnitClassification.RECONA ||
                   unit.Classification == UnitClassification.HELO;
        }

        private void AttachAirUnit(int availableIndex)
        {
            if (availableIndex < 0 || availableIndex >= availableAirUnits.Count)
                return;

            // Check if we've reached the 4-unit limit
            if (attachedAirUnits.Count >= 4)
            {
                statusMessage = "Airbase can only hold 4 air units maximum";
                return;
            }

            // Move unit from available to attached
            var unitToAttach = availableAirUnits[availableIndex];
            attachedAirUnits.Add(unitToAttach);
            availableAirUnits.RemoveAt(availableIndex);

            statusMessage = $"Attached {unitToAttach.UnitName} to airbase ({attachedAirUnits.Count}/4)";
        }

        private void DetachAirUnit(int attachedIndex)
        {
            if (attachedIndex < 0 || attachedIndex >= attachedAirUnits.Count)
                return;

            // Move unit from attached to available
            var unitToDetach = attachedAirUnits[attachedIndex];
            availableAirUnits.Add(unitToDetach);
            attachedAirUnits.RemoveAt(attachedIndex);

            statusMessage = $"Detached {unitToDetach.UnitName} from airbase ({attachedAirUnits.Count}/4)";
        }

        #endregion
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(UnitEntryTool))]
    public class UnitEntryToolEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);

            UnitEntryTool tool = (UnitEntryTool)target;

            string buttonText = tool.showGUI ? "Hide GUI" : "Show GUI";
            if (GUILayout.Button(buttonText, GUILayout.Height(30)))
            {
                tool.showGUI = !tool.showGUI;
                EditorUtility.SetDirty(tool);
            }
        }
    }
    #endif
}