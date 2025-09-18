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

        /// <summary>
        /// Gets all template identifiers currently stored in the database
        /// </summary>
        /// <returns>List of all template IDs</returns>
        public static List<string> GetAllTemplateIds()
        {
            try
            {
                return new List<string>(_unitTemplates.Keys);
            }
            catch (Exception e)
            {
                AppService.HandleException(nameof(CombatUnitDatabase), nameof(GetAllTemplateIds), e);
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
                CreateSovietFacilities();

                // Mujahedin units
                CreateMujahideenForces();

            }
            catch (Exception e)
            {
                AppService.HandleException(nameof(CombatUnitDatabase), nameof(CreateAllUnitTemplates), e);
                throw;
            }
        }

        #endregion //Private Methods

        //-------------------------------------------------------------------------------------------------

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
            #region Naval Infantry Regiment (BTR-70)

            var navalInfantryBtr70Regiment = new CombatUnit(
                unitName: "Naval Infantry Regiment (BTR-70)",
                classification: UnitClassification.MMAR,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_NAV_BTR70,
                deployedProfileID: WeaponSystems.INF_MAR,
                isMountable: true,
                mobileProfileID: WeaponSystems.APC_BTR70,
                isEmbarkable: true,
                embarkProfileID: WeaponSystems.TRANNAV_NAVAL,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            navalInfantryBtr70Regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            navalInfantryBtr70Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_NAV_BTR70", navalInfantryBtr70Regiment);

            #endregion //Naval Infantry Regiment (BTR-70)

            #region Naval Infantry Regiment (BTR-80)

            var navalInfantryBtr80Regiment = new CombatUnit(
                unitName: "Naval Infantry Regiment (BTR-80)",
                classification: UnitClassification.MMAR,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_NAV_BTR80,
                deployedProfileID: WeaponSystems.INF_MAR,
                isMountable: true,
                mobileProfileID: WeaponSystems.APC_BTR80,
                isEmbarkable: true,
                embarkProfileID: WeaponSystems.TRANNAV_NAVAL,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            navalInfantryBtr80Regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            navalInfantryBtr80Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_NAV_BTR80", navalInfantryBtr80Regiment);

            #endregion //Naval Infantry Regiment (BTR-80)
        }

        #endregion

        #region Soviet Infantry Forces

        /// <summary>
        /// Creates and initializes Soviet infantry forces.
        /// </summary>
        public static void CreateSovietInfantryForces()
        {
            #region Engineer Regiment

            var engineerRegiment = new CombatUnit(
                unitName: "Engineer Regiment",
                classification: UnitClassification.ENG,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_ENG,
                deployedProfileID: WeaponSystems.INF_ENG,
                isMountable: true,
                mobileProfileID: WeaponSystems.APC_MTLB,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            engineerRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            engineerRegiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_ENG", engineerRegiment);

            #endregion //Engineer Regiment

            #region Spetsnaz Regiment (GRU)

            var spetsnazRegiment = new CombatUnit(
                unitName: "Spetsnaz Regiment (GRU)",
                classification: UnitClassification.SPECF,
                role: UnitRole.GroundCombatRecon,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_GRU,
                deployedProfileID: WeaponSystems.INF_SPEC,
                isMountable: true,
                mobileProfileID: WeaponSystems.TRANHEL_MI8T,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            spetsnazRegiment.SetExperienceLevel(ExperienceLevel.Veteran);

            // Set the ICM
            spetsnazRegiment.SetICM(CUConstants.ICM_SMALL_UNIT);

            // Add the template to the database
            AddTemplate("USSR_GRU", spetsnazRegiment);

            #endregion //Spetsnaz Regiment (GRU)
        }

        #endregion

        #region Soviet Recon Forces

        /// <summary>
        /// Creates and initializes Soviet reconnaissance forces.
        /// </summary>
        public static void CreateSovietReconForces()
        {
            #region Reconnaissance Regiment

            var reconRegiment = new CombatUnit(
                unitName: "Reconnaissance Regiment",
                classification: UnitClassification.RECON,
                role: UnitRole.GroundCombatRecon,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_RCR,
                deployedProfileID: WeaponSystems.RCN_BRDM2,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            reconRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            reconRegiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_RCR", reconRegiment);

            #endregion //Reconnaissance Regiment

            #region Anti-Tank Regiment

            var antiTankRegiment = new CombatUnit(
                unitName: "Recon Regiment AT",
                classification: UnitClassification.RECON,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_RCR_AT,
                deployedProfileID: WeaponSystems.RCN_BRDM2AT,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            antiTankRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            antiTankRegiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_RCR_AT", antiTankRegiment);

            #endregion //Anti-Tank Regiment
        }

        #endregion

        #region Soviet Air Defense Regiments

        /// <summary>
        /// Creates and initializes Soviet air defense regiments.
        /// </summary>
        public static void CreateSovietAirDefenseRegiments()
        {
            #region Air Defense Regiment (Generic AAA)

            var adrGenericRegiment = new CombatUnit(
                unitName: "Air Defense Regiment (Generic AAA)",
                classification: UnitClassification.AAA,
                role: UnitRole.AirDefenseArea,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_ADR_AAA,
                deployedProfileID: WeaponSystems.AAA_GENERIC,
                isMountable: true,
                mobileProfileID: WeaponSystems.TRUCK_GENERIC,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            adrGenericRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            adrGenericRegiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_ADR_AAA", adrGenericRegiment);

            #endregion //Air Defense Regiment (Generic AAA)

            #region Air Defense Regiment (ZSU-57)

            var adrZsu57Regiment = new CombatUnit(
                unitName: "Air Defense Regiment (ZSU-57)",
                classification: UnitClassification.SPAAA,
                role: UnitRole.AirDefenseArea,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_ADR_ZSU57,
                deployedProfileID: WeaponSystems.SPAAA_ZSU57,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            adrZsu57Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            adrZsu57Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_ADR_ZSU57", adrZsu57Regiment);

            #endregion //Air Defense Regiment (ZSU-57)

            #region Air Defense Regiment (ZSU-23)

            var adrZsu23Regiment = new CombatUnit(
                unitName: "Air Defense Regiment (ZSU-23)",
                classification: UnitClassification.SPAAA,
                role: UnitRole.AirDefenseArea,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_ADR_ZSU23,
                deployedProfileID: WeaponSystems.SPAAA_ZSU23,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            adrZsu23Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            adrZsu23Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_ADR_ZSU23", adrZsu23Regiment);

            #endregion //Air Defense Regiment (ZSU-23)

            #region Air Defense Regiment (2K22 Tunguska)

            var adr2k22Regiment = new CombatUnit(
                unitName: "Air Defense Regiment (2K22 Tunguska)",
                classification: UnitClassification.SPAAA,
                role: UnitRole.AirDefenseArea,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_ADR_2K22,
                deployedProfileID: WeaponSystems.SPAAA_2K22,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            adr2k22Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            adr2k22Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_ADR_2K22", adr2k22Regiment);

            #endregion //Air Defense Regiment (2K22 Tunguska)
        }

        #endregion

        #region Soviet SAM Regiments

        /// <summary>
        /// Creates and initializes Soviet surface-to-air missile (SAM) regiments.
        /// </summary>
        public static void CreateSovietSAMRegiments()
        {
            #region Self-Propelled SAM Regiment (9K31 Strela-1)

            var spsam9k31Regiment = new CombatUnit(
                unitName: "Self-Propelled SAM Regiment (9K31 Strela-1)",
                classification: UnitClassification.SPSAM,
                role: UnitRole.AirDefenseArea,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_SPSAM_9K31,
                deployedProfileID: WeaponSystems.SPSAM_9K31,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            spsam9k31Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            spsam9k31Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_SPSAM_9K31", spsam9k31Regiment);

            #endregion //Self-Propelled SAM Regiment (9K31 Strela-1)

            #region SAM Regiment (S-75 Dvina)

            var samS75Regiment = new CombatUnit(
                unitName: "SAM Regiment (S-75 Dvina)",
                classification: UnitClassification.SAM,
                role: UnitRole.AirDefenseArea,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_SAM_S75,
                deployedProfileID: WeaponSystems.SAM_S75,
                isMountable: true,
                mobileProfileID: WeaponSystems.TRUCK_GENERIC,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            samS75Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            samS75Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_SAM_S75", samS75Regiment);

            #endregion //SAM Regiment (S-75 Dvina)

            #region SAM Regiment (S-125 Neva)

            var samS125Regiment = new CombatUnit(
                unitName: "SAM Regiment (S-125 Neva)",
                classification: UnitClassification.SAM,
                role: UnitRole.AirDefenseArea,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_SAM_S125,
                deployedProfileID: WeaponSystems.SAM_S125,
                isMountable: true,
                mobileProfileID: WeaponSystems.TRUCK_GENERIC,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            samS125Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            samS125Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_SAM_S125", samS125Regiment);

            #endregion //SAM Regiment (S-125 Neva)

            #region SAM Regiment (S-300)

            var samS300Regiment = new CombatUnit(
                unitName: "SAM Regiment (S-300)",
                classification: UnitClassification.SAM,
                role: UnitRole.AirDefenseArea,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_SAM_S300,
                deployedProfileID: WeaponSystems.SAM_S300,
                isMountable: true,
                mobileProfileID: WeaponSystems.TRUCK_GENERIC,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            samS300Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            samS300Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_SAM_S300", samS300Regiment);

            #endregion //SAM Regiment (S-300)
        }

        #endregion

        #region Soviet Helicopter Regiments

        /// <summary>
        /// Creates and initializes Soviet helicopter regiments.
        /// </summary>
        public static void CreateSovietHelicopterRegiments()
        {
            #region Attack Helicopter Regiment (Mi-8AT)

            var helMi8atRegiment = new CombatUnit(
                unitName: "Attack Helicopter Regiment (Mi-8AT)",
                classification: UnitClassification.HELO,
                role: UnitRole.AirGroundAttack,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_HEL_MI8AT,
                deployedProfileID: WeaponSystems.HEL_MI8AT,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            helMi8atRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            helMi8atRegiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_HEL_MI8AT", helMi8atRegiment);

            #endregion //Attack Helicopter Regiment (Mi-8AT)

            #region Attack Helicopter Regiment (Mi-24D)

            var helMi24dRegiment = new CombatUnit(
                unitName: "Attack Helicopter Regiment (Mi-24D)",
                classification: UnitClassification.HELO,
                role: UnitRole.AirGroundAttack,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_HEL_MI24D,
                deployedProfileID: WeaponSystems.HEL_MI24D,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            helMi24dRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            helMi24dRegiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_HEL_MI24D", helMi24dRegiment);

            #endregion //Attack Helicopter Regiment (Mi-24D)

            #region Attack Helicopter Regiment (Mi-24V)

            var helMi24vRegiment = new CombatUnit(
                unitName: "Attack Helicopter Regiment (Mi-24V)",
                classification: UnitClassification.HELO,
                role: UnitRole.AirGroundAttack,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_HEL_MI24V,
                deployedProfileID: WeaponSystems.HEL_MI24V,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            helMi24vRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            helMi24vRegiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_HEL_MI24V", helMi24vRegiment);

            #endregion //Attack Helicopter Regiment (Mi-24V)

            #region Attack Helicopter Regiment (Mi-28)

            var helMi28Regiment = new CombatUnit(
                unitName: "Attack Helicopter Regiment (Mi-28)",
                classification: UnitClassification.HELO,
                role: UnitRole.AirGroundAttack,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_HEL_MI28,
                deployedProfileID: WeaponSystems.HEL_MI28,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            helMi28Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            helMi28Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_HEL_MI28", helMi28Regiment);

            #endregion //Attack Helicopter Regiment (Mi-28)
        }

        #endregion

        #region Soviet Fighter Regiments

        /// <summary>
        /// Creates and initializes Soviet fighter regiments.
        /// </summary>
        public static void CreateSovietFighterRegiments()
        {
            #region Fighter Regiment (MiG-21)

            var fgtMig21Regiment = new CombatUnit(
                unitName: "Fighter Regiment (MiG-21)",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_FR_MIG21,
                deployedProfileID: WeaponSystems.FGT_MIG21,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fgtMig21Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            fgtMig21Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_FGT_MIG21", fgtMig21Regiment);

            #endregion //Fighter Regiment (MiG-21)

            #region Fighter Regiment (MiG-23)

            var fgtMig23Regiment = new CombatUnit(
                unitName: "Fighter Regiment (MiG-23)",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_FR_MIG23,
                deployedProfileID: WeaponSystems.FGT_MIG23,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fgtMig23Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            fgtMig23Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_FGT_MIG23", fgtMig23Regiment);

            #endregion //Fighter Regiment (MiG-23)

            #region Fighter Regiment (MiG-25)

            var fgtMig25Regiment = new CombatUnit(
                unitName: "Fighter Regiment (MiG-25)",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_FR_MIG25,
                deployedProfileID: WeaponSystems.FGT_MIG25,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fgtMig25Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            fgtMig25Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_FGT_MIG25", fgtMig25Regiment);

            #endregion //Fighter Regiment (MiG-25)

            #region Fighter Regiment (MiG-29)

            var fgtMig29Regiment = new CombatUnit(
                unitName: "Fighter Regiment (MiG-29)",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_FR_MIG29,
                deployedProfileID: WeaponSystems.FGT_MIG29,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fgtMig29Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            fgtMig29Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_FGT_MIG29", fgtMig29Regiment);

            #endregion //Fighter Regiment (MiG-29)

            #region Fighter Regiment (MiG-31)

            var fgtMig31Regiment = new CombatUnit(
                unitName: "Fighter Regiment (MiG-31)",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_FR_MIG31,
                deployedProfileID: WeaponSystems.FGT_MIG31,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fgtMig31Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            fgtMig31Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_FGT_MIG31", fgtMig31Regiment);

            #endregion //Fighter Regiment (MiG-31)

            #region Fighter Regiment (Su-27)

            var fgtSu27Regiment = new CombatUnit(
                unitName: "Fighter Regiment (Su-27)",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_FR_SU27,
                deployedProfileID: WeaponSystems.FGT_SU27,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fgtSu27Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            fgtSu27Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_FGT_SU27", fgtSu27Regiment);

            #endregion //Fighter Regiment (Su-27)

            #region Fighter Regiment (Su-47)

            var fgtSu47Regiment = new CombatUnit(
                unitName: "Fighter Regiment (Su-47)",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_FR_SU47,
                deployedProfileID: WeaponSystems.FGT_SU47,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fgtSu47Regiment.SetExperienceLevel(ExperienceLevel.Elite);

            // Set the ICM
            fgtSu47Regiment.SetICM(CUConstants.ICM_LARGE_UNIT);

            // Add the template to the database
            AddTemplate("USSR_FGT_SU47", fgtSu47Regiment);

            #endregion //Fighter Regiment (Su-47)

            #region Multirole Fighter Regiment (MiG-27)

            var mrfMig27Regiment = new CombatUnit(
                unitName: "Multirole Fighter Regiment (MiG-27)",
                classification: UnitClassification.FGT,
                role: UnitRole.AirMultirole,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_MR_MIG27,
                deployedProfileID: WeaponSystems.FGT_MIG27,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            mrfMig27Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            mrfMig27Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_MRF_MIG27", mrfMig27Regiment);

            #endregion //Multirole Fighter Regiment (MiG-27)
        }

        #endregion

        #region Soviet Attack Aviation Regiments

        /// <summary>
        /// Creates and initializes Soviet attack aviation regiments.
        /// </summary>
        public static void CreateSovietAttackAviationRegiments()
        {
            #region Attack Regiment (Su-25)

            var attSu25Regiment = new CombatUnit(
                unitName: "Attack Regiment (Su-25)",
                classification: UnitClassification.ATT,
                role: UnitRole.AirGroundAttack,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_AR_SU25,
                deployedProfileID: WeaponSystems.ATT_SU25,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            attSu25Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            attSu25Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_ATT_SU25", attSu25Regiment);

            #endregion //Attack Regiment (Su-25)

            #region Attack Regiment (Su-25B)

            var attSu25bRegiment = new CombatUnit(
                unitName: "Attack Regiment (Su-25B)",
                classification: UnitClassification.ATT,
                role: UnitRole.AirGroundAttack,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_AR_SU25B,
                deployedProfileID: WeaponSystems.ATT_SU25B,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            attSu25bRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            attSu25bRegiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_ATT_SU25B", attSu25bRegiment);

            #endregion //Attack Regiment (Su-25B)
        }

        #endregion

        #region Soviet Bomber Regiments

        /// <summary>
        /// Creates and initializes Soviet bomber regiments.
        /// </summary>
        public static void CreateSovietBomberRegiments()
        {
            #region AWACS Regiment (A-50)

            var awacsA50Regiment = new CombatUnit(
                unitName: "AWACS Regiment (A-50)",
                classification: UnitClassification.AWACS,
                role: UnitRole.AirborneEarlyWarning,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_AWACS_A50,
                deployedProfileID: WeaponSystems.AWACS_A50,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            awacsA50Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            awacsA50Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_AWACS_A50", awacsA50Regiment);

            #endregion //AWACS Regiment (A-50)

            #region Bomber Regiment (Su-24)

            var bmbSu24Regiment = new CombatUnit(
                unitName: "Bomber Regiment (Su-24)",
                classification: UnitClassification.BMB,
                role: UnitRole.AirStrategicAttack,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_BR_SU24,
                deployedProfileID: WeaponSystems.BMB_SU24,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            bmbSu24Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            bmbSu24Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_BMB_SU24", bmbSu24Regiment);

            #endregion //Bomber Regiment (Su-24)

            #region Bomber Regiment (Tu-16)

            var bmbTu16Regiment = new CombatUnit(
                unitName: "Bomber Regiment (Tu-16)",
                classification: UnitClassification.BMB,
                role: UnitRole.AirStrategicAttack,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_BR_TU16,
                deployedProfileID: WeaponSystems.BMB_TU16,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            bmbTu16Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            bmbTu16Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_BMB_TU16", bmbTu16Regiment);

            #endregion //Bomber Regiment (Tu-16)

            #region Bomber Regiment (Tu-22)

            var bmbTu22Regiment = new CombatUnit(
                unitName: "Bomber Regiment (Tu-22)",
                classification: UnitClassification.BMB,
                role: UnitRole.AirStrategicAttack,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_BR_TU22,
                deployedProfileID: WeaponSystems.BMB_TU22,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            bmbTu22Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            bmbTu22Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_BMB_TU22", bmbTu22Regiment);

            #endregion //Bomber Regiment (Tu-22)

            #region Bomber Regiment (Tu-22M3)

            var bmbTu22m3Regiment = new CombatUnit(
                unitName: "Bomber Regiment (Tu-22M3)",
                classification: UnitClassification.BMB,
                role: UnitRole.AirStrategicAttack,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_BR_TU22M3,
                deployedProfileID: WeaponSystems.BMB_TU22M3,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            bmbTu22m3Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            bmbTu22m3Regiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_BMB_TU22M3", bmbTu22m3Regiment);

            #endregion //Bomber Regiment (Tu-22M3)
        }

        #endregion

        #region Soviet Strategic Recon Regiments

        /// <summary>
        /// Creates and initializes Soviet strategic reconnaissance regiments.
        /// </summary>
        public static void CreateSovietStrategicReconRegiments()
        {
            #region Strategic Reconnaissance Regiment (MiG-25R)

            var rcnMig25rRegiment = new CombatUnit(
                unitName: "Strategic Reconnaissance Regiment (MiG-25R)",
                classification: UnitClassification.RECONA,
                role: UnitRole.AirRecon,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_RR_MIG25R,
                deployedProfileID: WeaponSystems.RCNA_MIG25R,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            rcnMig25rRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            rcnMig25rRegiment.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_RCN_MIG25R", rcnMig25rRegiment);

            #endregion //Strategic Reconnaissance Regiment (MiG-25R)
        }

        #endregion

        #region Soviet Facilities

        /// <summary>
        /// Creates and initializes Soviet facility units.
        /// </summary>
        public static void CreateSovietFacilities()
        {
            #region Soviet Supply Depot

            var sovSupplyDepot = new CombatUnit(
                unitName: "Soviet Supply Depot",
                classification: UnitClassification.DEPOT,
                role: UnitRole.GroundCombatStatic,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_DEPOT,
                deployedProfileID: WeaponSystems.SUPPLYDEPOT_GENERIC,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Main,
                size: DepotSize.Large
            );

            // Set experience level - facilities don't gain experience
            sovSupplyDepot.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM - standard facility
            sovSupplyDepot.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_DEPOT", sovSupplyDepot);

            #endregion //Soviet Supply Depot

            #region Soviet Airbase

            var sovAirbase = new CombatUnit(
                unitName: "Soviet Airbase",
                classification: UnitClassification.AIRB,
                role: UnitRole.GroundCombatStatic,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_AIRB,
                deployedProfileID: WeaponSystems.AIRBASE_GENERIC,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Main,
                size: DepotSize.Large
            );

            // Set experience level - facilities don't gain experience
            sovAirbase.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM - standard facility
            sovAirbase.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_AIRBASE", sovAirbase);

            #endregion //Soviet Airbase

            #region Soviet HQ

            var sovHQ = new CombatUnit(
                unitName: "Soviet Headquarters",
                classification: UnitClassification.HQ,
                role: UnitRole.GroundCombatStatic,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_BASE,
                deployedProfileID: WeaponSystems.LANDBASE_GENERIC,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Main,
                size: DepotSize.Medium
            );

            // Set experience level - facilities don't gain experience
            sovHQ.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM - command facility
            sovHQ.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_HQ", sovHQ);

            #endregion //Soviet HQ

            #region Soviet Intelligence Base

            var sovIntelBase = new CombatUnit(
                unitName: "Soviet Intelligence Base",
                classification: UnitClassification.HQ,
                role: UnitRole.GroundCombatStatic,
                side: Side.Player,
                nationality: Nationality.USSR,
                intelProfileType: IntelProfileTypes.SV_BASE,
                deployedProfileID: WeaponSystems.LANDBASE_GENERIC,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Main,
                size: DepotSize.Small
            );

            // Set experience level - facilities don't gain experience
            sovIntelBase.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM - specialized intelligence facility
            sovIntelBase.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_INTEL_BASE", sovIntelBase);

            #endregion //Soviet Intelligence Base
        }

        #endregion

        //-------------------------------------------------------------------------------------------------

        #region Mujahideen Units

        public static void CreateMujahideenForces()
        {
            #region Mujahideen Guerrilla Infantry

            var mjGuerrillaInf = new CombatUnit(
                unitName: "Mujahideen Guerrilla Infantry",
                classification: UnitClassification.INF,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.MJ,
                intelProfileType: IntelProfileTypes.MJ_INF_GUERRILLA,
                deployedProfileID: WeaponSystems.INF_REG,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            mjGuerrillaInf.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM - guerrillas are tough fighters on home terrain
            mjGuerrillaInf.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("MJ_INF_GUERRILLA", mjGuerrillaInf);

            #endregion //Mujahideen Guerrilla Infantry

            #region Mujahideen Special Forces Commando

            var mjSpecCommando = new CombatUnit(
                unitName: "Mujahideen Special Forces Commando",
                classification: UnitClassification.SPECF,
                role: UnitRole.GroundCombatRecon,
                side: Side.AI,
                nationality: Nationality.MJ,
                intelProfileType: IntelProfileTypes.MJ_SPEC_COMMANDO,
                deployedProfileID: WeaponSystems.INF_SPEC,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level - elite fighters
            mjSpecCommando.SetExperienceLevel(ExperienceLevel.Veteran);

            // Set the ICM - small elite unit
            mjSpecCommando.SetICM(CUConstants.ICM_SMALL_UNIT);

            // Add the template to the database
            AddTemplate("MJ_SPEC_COMMANDO", mjSpecCommando);

            #endregion //Mujahideen Special Forces Commando

            #region Mujahideen Horse Cavalry

            var mjHorseCavalry = new CombatUnit(
                unitName: "Mujahideen Horse Cavalry",
                classification: UnitClassification.CAV,
                role: UnitRole.GroundCombatRecon,
                side: Side.AI,
                nationality: Nationality.MJ,
                intelProfileType: IntelProfileTypes.MJ_CAV_HORSE,
                deployedProfileID: WeaponSystems.CAVALRY_GENERIC,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level - experienced in mobile warfare
            mjHorseCavalry.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM - traditional mobile fighters
            mjHorseCavalry.SetICM(CUConstants.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("MJ_CAV_HORSE", mjHorseCavalry);

            #endregion //Mujahideen Horse Cavalry

            #region Mujahideen Anti-Aircraft Unit

            var mjAntiAircraft = new CombatUnit(
                unitName: "Mujahideen Anti-Aircraft Unit",
                classification: UnitClassification.AAA,
                role: UnitRole.AirDefenseArea,
                side: Side.AI,
                nationality: Nationality.MJ,
                intelProfileType: IntelProfileTypes.MJ_AA,
                deployedProfileID: WeaponSystems.MANPAD_GENERIC,
                isMountable: true,
                mobileProfileID: WeaponSystems.TRUCK_GENERIC,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            mjAntiAircraft.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM - portable air defense
            mjAntiAircraft.SetICM(CUConstants.ICM_SMALL_UNIT);

            // Add the template to the database
            AddTemplate("MJ_AA", mjAntiAircraft);

            #endregion //Mujahideen Anti-Aircraft Unit

            #region Mujahideen Light Mortar Unit

            var mjLightMortar = new CombatUnit(
                unitName: "Mujahideen Light Mortar Unit",
                classification: UnitClassification.ART,
                role: UnitRole.GroundCombatIndirect,
                side: Side.AI,
                nationality: Nationality.MJ,
                intelProfileType: IntelProfileTypes.MJ_ART_LIGHT_MORTAR,
                deployedProfileID: WeaponSystems.MORTAR_81MM,
                isMountable: true,
                mobileProfileID: WeaponSystems.TRUCK_GENERIC,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            mjLightMortar.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM - light artillery support
            mjLightMortar.SetICM(CUConstants.ICM_SMALL_UNIT);

            // Add the template to the database
            AddTemplate("MJ_ART_LIGHT_MORTAR", mjLightMortar);

            #endregion //Mujahideen Light Mortar Unit

            #region Mujahideen Heavy Mortar Unit

            var mjHeavyMortar = new CombatUnit(
                unitName: "Mujahideen Heavy Mortar Unit",
                classification: UnitClassification.ART,
                role: UnitRole.GroundCombatIndirect,
                side: Side.AI,
                nationality: Nationality.MJ,
                intelProfileType: IntelProfileTypes.MJ_ART_HEAVY_MORTAR,
                deployedProfileID: WeaponSystems.MORTAR_120MM,
                isMountable: true,
                mobileProfileID: WeaponSystems.TRUCK_GENERIC,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            mjHeavyMortar.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM - heavier artillery support
            mjHeavyMortar.SetICM(CUConstants.ICM_SMALL_UNIT);

            // Add the template to the database
            AddTemplate("MJ_ART_HEAVY_MORTAR", mjHeavyMortar);

            #endregion //Mujahideen Heavy Mortar Unit

            #region Mujahideen Supply Cache

            var mjSupplyCache = new CombatUnit(
                unitName: "Mujahideen Supply Cache",
                classification: UnitClassification.DEPOT,
                role: UnitRole.GroundCombatStatic,
                side: Side.AI,
                nationality: Nationality.MJ,
                intelProfileType: IntelProfileTypes.SV_DEPOT, // Using generic depot intel profile
                deployedProfileID: WeaponSystems.SUPPLYDEPOT_GENERIC,
                isMountable: false,
                mobileProfileID: WeaponSystems.DEFAULT,
                isEmbarkable: false,
                embarkProfileID: WeaponSystems.DEFAULT,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level - facilities don't gain experience
            mjSupplyCache.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM - small hidden supply cache
            mjSupplyCache.SetICM(CUConstants.ICM_SMALL_UNIT);

            // Add the template to the database
            AddTemplate("MJ_DEPOT", mjSupplyCache);

            #endregion //Mujahideen Supply Cache
        }

        #endregion

        //-------------------------------------------------------------------------------------------------
    }
}