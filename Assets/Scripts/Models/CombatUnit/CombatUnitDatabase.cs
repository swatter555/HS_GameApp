using System;
using System.Collections.Generic;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Static repository of CombatUnit templates for scenario OOB generation.
    /// Templates are immutable once created and provide baseline configurations
    /// for spawning actual combat units in scenarios.
    /// </summary>
    public static class CombatUnitDatabase
    {
        #region Private Fields

        private static readonly Dictionary<string, CombatUnit> _unitTemplates = new Dictionary<string, CombatUnit>();
        private static readonly object _lock = new object();
        private static bool _isInitialized = false;

        #endregion //Private Fields

        #region Public Properties

        /// <summary>
        /// Returns true after successful static initialization
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Total number of unit templates currently stored
        /// </summary>
        public static int TemplateCount => _unitTemplates.Count;

        #endregion //Public Properties

        #region Public Methods

        /// <summary>
        /// One-time initialization of all unit templates. Call once at startup.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                if (_isInitialized)
                    return;

                lock (_lock)
                {
                    if (_isInitialized)
                        return;

                    CreateAllUnitTemplates();
                    _isInitialized = true;
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(nameof(CombatUnitDatabase), nameof(Initialize), e);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a unit template by unique identifier
        /// </summary>
        /// <param name="templateId">Template identifier string</param>
        /// <returns>Unit template or null if not found</returns>
        public static CombatUnit GetUnitTemplate(string templateId)
        {
            try
            {
                if (string.IsNullOrEmpty(templateId))
                    return null;

                return _unitTemplates.TryGetValue(templateId, out CombatUnit template) ? template : null;
            }
            catch (Exception e)
            {
                AppService.HandleException(nameof(CombatUnitDatabase), nameof(GetUnitTemplate), e);
                return null;
            }
        }

        /// <summary>
        /// Checks if a template exists without retrieving it
        /// </summary>
        /// <param name="templateId">Template identifier to check</param>
        /// <returns>True if template exists</returns>
        public static bool HasUnitTemplate(string templateId)
        {
            try
            {
                if (string.IsNullOrEmpty(templateId))
                    return false;

                return _unitTemplates.ContainsKey(templateId);
            }
            catch (Exception e)
            {
                AppService.HandleException(nameof(CombatUnitDatabase), nameof(HasUnitTemplate), e);
                return false;
            }
        }

        /// <summary>
        /// Creates a new combat unit instance from template
        /// </summary>
        /// <param name="templateId">Template identifier</param>
        /// <param name="unitName">Name for the new unit instance</param>
        /// <returns>New CombatUnit instance or null if template not found</returns>
        public static CombatUnit CreateUnitFromTemplate(string templateId, string unitName)
        {
            try
            {
                var template = GetUnitTemplate(templateId);
                if (template == null)
                    return null;

                var newUnit = template.CreateTemplateClone();
                newUnit.UnitName = unitName;
                return newUnit;
            }
            catch (Exception e)
            {
                AppService.HandleException(nameof(CombatUnitDatabase), nameof(CreateUnitFromTemplate), e);
                return null;
            }
        }

        /// <summary>
        /// Gets all template identifiers for a specific nationality
        /// </summary>
        /// <param name="nationality">Nationality to filter by</param>
        /// <returns>List of template IDs matching nationality</returns>
        public static List<string> GetTemplatesByNationality(Nationality nationality)
        {
            try
            {
                var results = new List<string>();
                foreach (var kvp in _unitTemplates)
                {
                    if (kvp.Value.Nationality == nationality)
                        results.Add(kvp.Key);
                }
                return results;
            }
            catch (Exception e)
            {
                AppService.HandleException(nameof(CombatUnitDatabase), nameof(GetTemplatesByNationality), e);
                return new List<string>();
            }
        }

        /// <summary>
        /// Gets all template identifiers for a specific unit classification
        /// </summary>
        /// <param name="classification">Unit classification to filter by</param>
        /// <returns>List of template IDs matching classification</returns>
        public static List<string> GetTemplatesByClassification(UnitClassification classification)
        {
            try
            {
                var results = new List<string>();
                foreach (var kvp in _unitTemplates)
                {
                    if (kvp.Value.Classification == classification)
                        results.Add(kvp.Key);
                }
                return results;
            }
            catch (Exception e)
            {
                AppService.HandleException(nameof(CombatUnitDatabase), nameof(GetTemplatesByClassification), e);
                return new List<string>();
            }
        }

        #endregion //Public Methods

        #region Private Methods

        /// <summary>
        /// Adds a template to the database with validation
        /// </summary>
        /// <param name="templateId">Unique identifier for template</param>
        /// <param name="template">CombatUnit template</param>
        private static void AddTemplate(string templateId, CombatUnit template)
        {
            try
            {
                if (string.IsNullOrEmpty(templateId))
                    throw new ArgumentException("Template ID cannot be null or empty");

                if (template == null)
                    throw new ArgumentNullException(nameof(template));

                if (_unitTemplates.ContainsKey(templateId))
                    throw new InvalidOperationException($"Template ID '{templateId}' already exists");

                _unitTemplates[templateId] = template;
            }
            catch (Exception e)
            {
                AppService.HandleException(nameof(CombatUnitDatabase), nameof(AddTemplate), e);
                throw;
            }
        }

        /// <summary>
        /// Creates all unit templates during initialization
        /// </summary>
        private static void CreateAllUnitTemplates()
        {
            try
            {
                // Soviet units
                CreateSovietMotorRifleRegiments();
                CreateSovietTankRegiments();
                CreateSovietArtilleryRegiments();
                CreateSovietRocketRegiments();
                CreateSovietAirAssaultRegiments();
                CreateSovietAirborneRegiments();
                CreateSovietNavalInfantryRegiments();
                CreateSovietInfantryForces();
                CreateSovietReconForces();
                CreateSovietAirDefenseRegiments();
                CreateSovietSAMRegiments();
                CreateSovietHelicopterRegiments();
                CreateSovietFighterRegiments();
                CreateSovietAttackAviationRegiments();
                CreateSovietBomberRegiments();
                CreateSovietStrategicReconRegiments();

                // Mujahedin units

            }
            catch (Exception e)
            {
                AppService.HandleException(nameof(CombatUnitDatabase), nameof(CreateAllUnitTemplates), e);
                throw;
            }
        }

        #endregion //Private Methods

        #region Soviet Motor Rifle Regiments

        /// <summary>
        /// Creates and initializes Soviet motor rifle regiments.
        /// </summary>
        public static void CreateSovietMotorRifleRegiments()
        {

            #region BTR70 MRR

            var btr70Regiment = new CombatUnit(
                unitName: "Motor Rifle Regiment (BTR-70)",
                classification: UnitClassification.MECH,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_MRR_BTR70,
                deployedProfileID: WeaponSystems.INF_REG,
                isMountable: true,
                mobileProfileID: WeaponSystems.APC_BTR70,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            btr70Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            btr70Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_MRR_BTR70", btr70Regiment);

            #endregion //BTR70 MRR

            #region BTR80 MRR

            var btr80Regiment = new CombatUnit(
                unitName: "Motor Rifle Regiment (BTR-80)",
                classification: UnitClassification.MOT,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_MRR_BTR80,
                deployedProfileID: WeaponSystems.INF_REG,
                isMountable: true,
                mobileProfileID: WeaponSystems.APC_BTR80,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            btr80Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            btr80Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_MRR_BTR80", btr80Regiment);

            #endregion //BTR80 MRR

            #region BMP1 MRR

            var bmp1Regiment = new CombatUnit(
                unitName: "Motor Rifle Regiment (BMP-1)",
                classification: UnitClassification.MECH,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_MRR_BMP1,
                deployedProfileID: WeaponSystems.INF_REG,
                isMountable: true,
                mobileProfileID: WeaponSystems.IFV_BMP1,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            bmp1Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            bmp1Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_MRR_BMP1", bmp1Regiment);

            #endregion //BMP1 MRR

            #region BMP2 MRR

            var bmp2Regiment = new CombatUnit(
                unitName: "Motor Rifle Regiment (BMP-2)",
                classification: UnitClassification.MECH,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_MRR_BMP2,
                deployedProfileID: WeaponSystems.INF_REG,
                isMountable: true,
                mobileProfileID: WeaponSystems.IFV_BMP2,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            bmp2Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            bmp2Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_MRR_BMP2", bmp2Regiment);

            #endregion //BMP2 MRR

            #region BMP3 MRR

            var bmp3Regiment = new CombatUnit(
                unitName: "Motor Rifle Regiment (BMP-3)",
                classification: UnitClassification.MECH,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_MRR_BMP3,
                deployedProfileID: WeaponSystems.INF_REG,
                isMountable: true,
                mobileProfileID: WeaponSystems.IFV_BMP3,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            bmp3Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            bmp3Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_MRR_BMP3", bmp3Regiment);

            #endregion //BMP3 MRR
        }

        #endregion

        #region Soviet Tank Regiments

        /// <summary>
        /// Creates and initializes Soviet tank regiments.
        /// </summary>
        public static void CreateSovietTankRegiments()
        {
            #region T55 Tank Regiment

            var t55Regiment = new CombatUnit(
                unitName: "Tank Regiment (T-55A)",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_TR_T55,
                deployedProfileID: WeaponSystems.TANK_T55A,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            t55Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            t55Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_TR_T55", t55Regiment);

            #endregion //T55 Tank Regiment

            #region T64A Tank Regiment

            var t64aRegiment = new CombatUnit(
                unitName: "Tank Regiment (T-64A)",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_TR_T64A,
                deployedProfileID: WeaponSystems.TANK_T64A,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            t64aRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            t64aRegiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_TR_T64A", t64aRegiment);

            #endregion //T64A Tank Regiment

            #region T64B Tank Regiment

            var t64bRegiment = new CombatUnit(
                unitName: "Tank Regiment (T-64B)",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_TR_T64B,
                deployedProfileID: WeaponSystems.TANK_T64B,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            t64bRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            t64bRegiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_TR_T64B", t64bRegiment);

            #endregion //T64B Tank Regiment

            #region T72A Tank Regiment

            var t72aRegiment = new CombatUnit(
                unitName: "Tank Regiment (T-72A)",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_TR_T72A,
                deployedProfileID: WeaponSystems.TANK_T72A,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            t72aRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            t72aRegiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_TR_T72A", t72aRegiment);

            #endregion //T72A Tank Regiment

            #region T72B Tank Regiment

            var t72bRegiment = new CombatUnit(
                unitName: "Tank Regiment (T-72B)",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_TR_T72B,
                deployedProfileID: WeaponSystems.TANK_T72B,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            t72bRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            t72bRegiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_TR_T72B", t72bRegiment);

            #endregion //T72B Tank Regiment

            #region T80B Tank Regiment

            var t80bRegiment = new CombatUnit(
                unitName: "Tank Regiment (T-80B)",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_TR_T80B,
                deployedProfileID: WeaponSystems.TANK_T80B,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            t80bRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            t80bRegiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_TR_T80B", t80bRegiment);

            #endregion //T80B Tank Regiment

            #region T80U Tank Regiment

            var t80uRegiment = new CombatUnit(
                unitName: "Tank Regiment (T-80U)",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_TR_T80U,
                deployedProfileID: WeaponSystems.TANK_T80U,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            t80uRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            t80uRegiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_TR_T80U", t80uRegiment);

            #endregion //T80U Tank Regiment

            #region T80BV Tank Regiment

            var t80bvRegiment = new CombatUnit(
                unitName: "Tank Regiment (T-80BV)",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_TR_T80BV,
                deployedProfileID: WeaponSystems.TANK_T80BV,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            t80bvRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            t80bvRegiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_TR_T80BV", t80bvRegiment);

            #endregion //T80BV Tank Regiment
        }

        #endregion

        #region Soviet Artillery Regiments

        /// <summary>
        /// Creates and initializes Soviet artillery regiments.
        /// </summary>
        public static void CreateSovietArtilleryRegiments()
        {
            #region Light Artillery Regiment

            var lightArtilleryRegiment = new CombatUnit(
                unitName: "Artillery Regiment (Light)",
                classification: UnitClassification.ART,
                role: UnitRole.GroundCombatIndirect,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_AR_LGT,
                deployedProfileID: WeaponSystems.ART_LIGHT_GENERIC,
                isMountable: true,
                mobileProfileID: WeaponSystems.TRUCK_GENERIC,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            lightArtilleryRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            lightArtilleryRegiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_AR_LIGHT", lightArtilleryRegiment);

            #endregion //Light Artillery Regiment

            #region Heavy Artillery Regiment

            var heavyArtilleryRegiment = new CombatUnit(
                unitName: "Artillery Regiment (Heavy)",
                classification: UnitClassification.ART,
                role: UnitRole.GroundCombatIndirect,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_AR_HVY,
                deployedProfileID: WeaponSystems.ART_HEAVY_GENERIC,
                isMountable: true,
                mobileProfileID: WeaponSystems.TRUCK_GENERIC,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            heavyArtilleryRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            heavyArtilleryRegiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_AR_HEAVY", heavyArtilleryRegiment);

            #endregion //Heavy Artillery Regiment

            #region 2S1 Self-Propelled Artillery Regiment

            var spa2s1Regiment = new CombatUnit(
                unitName: "Self-Propelled Artillery Regiment (2S1)",
                classification: UnitClassification.SPA,
                role: UnitRole.GroundCombatIndirect,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_AR_2S1,
                deployedProfileID: WeaponSystems.SPA_2S1,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            spa2s1Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            spa2s1Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_SPA_2S1", spa2s1Regiment);

            #endregion //2S1 Self-Propelled Artillery Regiment

            #region 2S3 Self-Propelled Artillery Regiment

            var spa2s3Regiment = new CombatUnit(
                unitName: "Self-Propelled Artillery Regiment (2S3)",
                classification: UnitClassification.SPA,
                role: UnitRole.GroundCombatIndirect,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_AR_2S3,
                deployedProfileID: WeaponSystems.SPA_2S3,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            spa2s3Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            spa2s3Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_SPA_2S3", spa2s3Regiment);

            #endregion //2S3 Self-Propelled Artillery Regiment

            #region 2S5 Self-Propelled Artillery Regiment

            var spa2s5Regiment = new CombatUnit(
                unitName: "Self-Propelled Artillery Regiment (2S5)",
                classification: UnitClassification.SPA,
                role: UnitRole.GroundCombatIndirect,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_AR_2S5,
                deployedProfileID: WeaponSystems.SPA_2S5,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            spa2s5Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            spa2s5Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_SPA_2S5", spa2s5Regiment);

            #endregion //2S5 Self-Propelled Artillery Regiment

            #region 2S19 Self-Propelled Artillery Regiment

            var spa2s19Regiment = new CombatUnit(
                unitName: "Self-Propelled Artillery Regiment (2S19)",
                classification: UnitClassification.SPA,
                role: UnitRole.GroundCombatIndirect,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_AR_2S19,
                deployedProfileID: WeaponSystems.SPA_2S19,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            spa2s19Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            spa2s19Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_SPA_2S19", spa2s19Regiment);

            #endregion //2S19 Self-Propelled Artillery Regiment
        }

        #endregion

        #region Soviet Rocket Regiments

        /// <summary>
        /// Creates and initializes Soviet rocket regiments.
        /// </summary>
        public static void CreateSovietRocketRegiments()
        {
            #region BM-21 Rocket Artillery Regiment

            var rocBm21Regiment = new CombatUnit(
                unitName: "Rocket Artillery Regiment (BM-21)",
                classification: UnitClassification.ROC,
                role: UnitRole.GroundCombatIndirect,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_ROC_BM21,
                deployedProfileID: WeaponSystems.ROC_BM21,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            rocBm21Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            rocBm21Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_ROC_BM21", rocBm21Regiment);

            #endregion //BM-21 Rocket Artillery Regiment

            #region BM-27 Rocket Artillery Regiment

            var rocBm27Regiment = new CombatUnit(
                unitName: "Rocket Artillery Regiment (BM-27)",
                classification: UnitClassification.ROC,
                role: UnitRole.GroundCombatIndirect,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_ROC_BM27,
                deployedProfileID: WeaponSystems.ROC_BM27,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            rocBm27Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            rocBm27Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_ROC_BM27", rocBm27Regiment);

            #endregion //BM-27 Rocket Artillery Regiment

            #region BM-30 Rocket Artillery Regiment

            var rocBm30Regiment = new CombatUnit(
                unitName: "Rocket Artillery Regiment (BM-30)",
                classification: UnitClassification.ROC,
                role: UnitRole.GroundCombatIndirect,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_ROC_BM30,
                deployedProfileID: WeaponSystems.ROC_BM30,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            rocBm30Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            rocBm30Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_ROC_BM30", rocBm30Regiment);

            #endregion //BM-30 Rocket Artillery Regiment

            #region SCUD-B Ballistic Missile Regiment

            var bmScudRegiment = new CombatUnit(
                unitName: "Ballistic Missile Regiment (SCUD-B)",
                classification: UnitClassification.BM,
                role: UnitRole.GroundCombatIndirect,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_BM_SCUDB,
                deployedProfileID: WeaponSystems.SSM_SCUD,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            bmScudRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            bmScudRegiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_BM_SCUD", bmScudRegiment);

            #endregion //SCUD-B Ballistic Missile Regiment
        }

        #endregion

        #region Soviet Air Assault Regiments

        /// <summary>
        /// Creates and initializes Soviet air assault regiments.
        /// </summary>
        public static void CreateSovietAirAssaultRegiments()
        {
            #region Air Assault Regiment (MT-LB)

            var aarMtlbRegiment = new CombatUnit(
                unitName: "Air Assault Regiment (MT-LB)",
                classification: UnitClassification.MAM,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_AAR_MTLB,
                deployedProfileID: WeaponSystems.APC_MTLB,
                isMountable: true,
                mobileProfileID: WeaponSystems.TRANHEL_MI8T,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            aarMtlbRegiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            aarMtlbRegiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_AAR_MTLB", aarMtlbRegiment);

            #endregion //Air Assault Regiment (MT-LB)

            #region Air Assault Regiment (BMD-1)

            var aarBmd1Regiment = new CombatUnit(
                unitName: "Air Assault Regiment (BMD-1)",
                classification: UnitClassification.MAM,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_AAR_BMD1,
                deployedProfileID: WeaponSystems.IFV_BMD1,
                isMountable: true,
                mobileProfileID: WeaponSystems.TRANHEL_MI8T,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            aarBmd1Regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            aarBmd1Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_AAR_BMD1", aarBmd1Regiment);

            #endregion //Air Assault Regiment (BMD-1)

            #region Air Assault Regiment (BMD-2)

            var aarBmd2Regiment = new CombatUnit(
                unitName: "Air Assault Regiment (BMD-2)",
                classification: UnitClassification.MAM,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_AAR_BMD2,
                deployedProfileID: WeaponSystems.IFV_BMD2,
                isMountable: true,
                mobileProfileID: WeaponSystems.TRANHEL_MI8T,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            aarBmd2Regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            aarBmd2Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_AAR_BMD2", aarBmd2Regiment);

            #endregion //Air Assault Regiment (BMD-2)

            #region Air Assault Regiment (BMD-3)

            var aarBmd3Regiment = new CombatUnit(
                unitName: "Air Assault Regiment (BMD-3)",
                classification: UnitClassification.MAM,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_AAR_BMD3,
                deployedProfileID: WeaponSystems.IFV_BMD3,
                isMountable: true,
                mobileProfileID: WeaponSystems.TRANHEL_MI8T,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            aarBmd3Regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            aarBmd3Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_AAR_BMD3", aarBmd3Regiment);

            #endregion //Air Assault Regiment (BMD-3)
        }

        #endregion

        #region Soviet Airborne Regiments

        /// <summary>
        /// Creates and initializes Soviet airborne regiments.
        /// </summary>
        public static void CreateSovietAirborneRegiments()
        {
            #region VDV Airborne Regiment (BMD-1)

            var vdvBmd1Regiment = new CombatUnit(
                unitName: "VDV Airborne Regiment (BMD-1)",
                classification: UnitClassification.MAB,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_VDV_BMD1,
                deployedProfileID: WeaponSystems.INF_AB,
                isMountable: true,
                mobileProfileID: WeaponSystems.IFV_BMD1,
                isEmbarkable: true,
                embarkProfileID: WeaponSystems.TRANAIR_AN12,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            vdvBmd1Regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            vdvBmd1Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_VDV_BMD1", vdvBmd1Regiment);

            #endregion //VDV Airborne Regiment (BMD-1)

            #region VDV Airborne Regiment (BMD-2)

            var vdvBmd2Regiment = new CombatUnit(
                unitName: "VDV Airborne Regiment (BMD-2)",
                classification: UnitClassification.MAB,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_VDV_BMD2,
                deployedProfileID: WeaponSystems.INF_AB,
                isMountable: true,
                mobileProfileID: WeaponSystems.IFV_BMD2,
                isEmbarkable: true,
                embarkProfileID: WeaponSystems.TRANAIR_AN12,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            vdvBmd2Regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            vdvBmd2Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_VDV_BMD2", vdvBmd2Regiment);

            #endregion //VDV Airborne Regiment (BMD-2)

            #region VDV Airborne Regiment (BMD-3)

            var vdvBmd3Regiment = new CombatUnit(
                unitName: "VDV Airborne Regiment (BMD-3)",
                classification: UnitClassification.MAB,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_VDV_BMD3,
                deployedProfileID: WeaponSystems.INF_AB,
                isMountable: true,
                mobileProfileID: WeaponSystems.IFV_BMD3,
                isEmbarkable: true,
                embarkProfileID: WeaponSystems.TRANAIR_AN12,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            vdvBmd3Regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            vdvBmd3Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_VDV_BMD3", vdvBmd3Regiment);

            #endregion //VDV Airborne Regiment (BMD-3)

            #region VDV Artillery Regiment

            var vdvArtilleryRegiment = new CombatUnit(
                unitName: "VDV Artillery Regiment",
                classification: UnitClassification.ART,
                role: UnitRole.GroundCombatIndirect,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_VDV_ART,
                deployedProfileID: WeaponSystems.ART_LIGHT_GENERIC,
                isMountable: true,
                mobileProfileID: WeaponSystems.APC_MTLB,
                isEmbarkable: true,
                embarkProfileID: WeaponSystems.TRANAIR_AN12,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            vdvArtilleryRegiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            vdvArtilleryRegiment.SetICM(CUConstants.ICM_SMALL_UNIT);

            // Add the template to the database
            AddTemplate("USSR_VDV_ART", vdvArtilleryRegiment);

            #endregion //VDV Artillery Regiment

            #region VDV Support Regiment

            var vdvSupportRegiment = new CombatUnit(
                unitName: "VDV Support Regiment",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_VDV_SUP,
                deployedProfileID: WeaponSystems.TANK_T55A,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: true,
                embarkProfileID: WeaponSystems.TRANAIR_AN12,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            vdvSupportRegiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            vdvSupportRegiment.SetICM(CUConstants.ICM_SMALL_UNIT);

            // Add the template to the database
            AddTemplate("USSR_VDV_SUP", vdvSupportRegiment);

            #endregion //VDV Support Regiment
        }

        #endregion

        #region Soviet Naval Infantry Regiments

        /// <summary>
        /// Creates and initializes Soviet naval infantry regiments.
        /// </summary>
        public static void CreateSovietNavalInfantryRegiments()
        {

        }

        #endregion

        #region Soviet Infantry Forces

        /// <summary>
        /// Creates and initializes Soviet infantry forces.
        /// </summary>
        public static void CreateSovietInfantryForces()
        {

        }

        #endregion

        #region Soviet Recon Forces

        /// <summary>
        /// Creates and initializes Soviet reconnaissance forces.
        /// </summary>
        public static void CreateSovietReconForces()
        {

        }

        #endregion

        #region Soviet Air Defense Regiments

        /// <summary>
        /// Creates and initializes Soviet air defense regiments.
        /// </summary>
        public static void CreateSovietAirDefenseRegiments()
        {

        }

        #endregion

        #region Soviet SAM Regiments

        /// <summary>
        /// Creates and initializes Soviet surface-to-air missile (SAM) regiments.
        /// </summary>
        public static void CreateSovietSAMRegiments()
        {

        }

        #endregion

        #region Soviet Helicopter Regiments

        /// <summary>
        /// Creates and initializes Soviet helicopter regiments.
        /// </summary>
        public static void CreateSovietHelicopterRegiments()
        {
            
        }

        #endregion

        #region Soviet Fighter Regiments

        /// <summary>
        /// Creates and initializes Soviet fighter regiments.
        /// </summary>
        public static void CreateSovietFighterRegiments()
        {

        }

        #endregion

        #region Soviet Attack Aviation Regiments

        /// <summary>
        /// Creates and initializes Soviet attack aviation regiments.
        /// </summary>
        public static void CreateSovietAttackAviationRegiments()
        {

        }

        #endregion

        #region Soviet Bomber Regiments

        /// <summary>
        /// Creates and initializes Soviet bomber regiments.
        /// </summary>
        public static void CreateSovietBomberRegiments()
        {

        }

        #endregion

        #region Soviet Strategic Recon Regiments

        /// <summary>
        /// Creates and initializes Soviet strategic reconnaissance regiments.
        /// </summary>
        public static void CreateSovietStrategicReconRegiments()
        {

        }

        #endregion
    }
}