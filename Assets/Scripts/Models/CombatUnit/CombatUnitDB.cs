using System;
using System.Collections.Generic;
using HammerAndSickle.Services;
using HammerAndSickle.Core.GameData;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Static repository of CombatUnit templates for scenario OOB generation.
    /// Templates are immutable once created and provide baseline configurations
    /// for spawning actual combat units in scenarios.
    /// </summary>
    public static class CombatUnitDB
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
                AppService.HandleException(nameof(CombatUnitDB), nameof(Initialize), e);
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
                AppService.HandleException(nameof(CombatUnitDB), nameof(GetUnitTemplate), e);
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
                AppService.HandleException(nameof(CombatUnitDB), nameof(HasUnitTemplate), e);
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
                AppService.HandleException(nameof(CombatUnitDB), nameof(CreateUnitFromTemplate), e);
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
                AppService.HandleException(nameof(CombatUnitDB), nameof(GetTemplatesByNationality), e);
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
                AppService.HandleException(nameof(CombatUnitDB), nameof(GetTemplatesByClassification), e);
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
                AppService.HandleException(nameof(CombatUnitDB), nameof(GetAllTemplateIds), e);
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
                AppService.HandleException(nameof(CombatUnitDB), nameof(AddTemplate), e);
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

                // Create Western units
                CreateUSForces();
                CreateGermanForces();
                CreateBritishForces();
                CreateFrenchForces();

                // Create Arab Forces
                CreateArabForces();

                // Create Chinese Forces
                CreateChineseForces();
            }
            catch (Exception e)
            {
                AppService.HandleException(nameof(CombatUnitDB), nameof(CreateAllUnitTemplates), e);
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
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_REG_SV,
                isMountable: true,
                mobileProfile: WeaponType.APC_BTR70_SV,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            btr70Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            btr70Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_REG_SV,
                isMountable: true,
                mobileProfile: WeaponType.APC_BTR80_SV,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            btr80Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            btr80Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_REG_SV,
                isMountable: true,
                mobileProfile: WeaponType.IFV_BMP1_SV,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            bmp1Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            bmp1Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_REG_SV,
                isMountable: true,
                mobileProfile: WeaponType.IFV_BMP2_SV,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            bmp2Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            bmp2Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_REG_SV,
                isMountable: true,
                mobileProfile: WeaponType.IFV_BMP3_SV,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            bmp3Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            bmp3Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_T55A_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            t55Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            t55Regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_TR_T55", t55Regiment);

            #endregion //T55 Tank Regiment

            #region T62A Tank Regiment

            var t62aRegiment = new CombatUnit(
                unitName: "Tank Regiment (T-62A)",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_T62A_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            t62aRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            t62aRegiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_TR_T62A", t62aRegiment);

            #endregion //T62A Tank Regiment

            #region T64A Tank Regiment

            var t64aRegiment = new CombatUnit(
                unitName: "Tank Regiment (T-64A)",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_T64A_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            t64aRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            t64aRegiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_T64B_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            t64bRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            t64bRegiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_T72A_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            t72aRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            t72aRegiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_T72B_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            t72bRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            t72bRegiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_T80B_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            t80bRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            t80bRegiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_T80U_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            t80uRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            t80uRegiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_T80BV_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            t80bvRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            t80bvRegiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.ART_LIGHT_SV,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_SV,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            lightArtilleryRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            lightArtilleryRegiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.ART_HEAVY_SV,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_SV,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            heavyArtilleryRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            heavyArtilleryRegiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPA_2S1_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            spa2s1Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            spa2s1Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPA_2S3_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            spa2s3Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            spa2s3Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPA_2S5_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            spa2s5Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            spa2s5Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPA_2S19_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            spa2s19Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            spa2s19Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.ROC_BM21_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            rocBm21Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            rocBm21Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.ROC_BM27_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            rocBm27Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            rocBm27Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.ROC_BM30_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            rocBm30Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            rocBm30Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.ROC_SCUD_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            bmScudRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            bmScudRegiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP_MOB_EMB_HELO,
                deployedProfile: WeaponType.INF_AM_SV,
                isMountable: true,
                mobileProfile: WeaponType.APC_MTLB_SV,
                isEmbarkable: true,
                embarkedProfile: WeaponType.HEL_MI8T_SV,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            aarMtlbRegiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            aarMtlbRegiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_AAR_MTLB", aarMtlbRegiment);

            #endregion //Air Assault Regiment (MT-LB)

            #region Air Assault Regiment (BMD-2)

            var aarBmd2Regiment = new CombatUnit(
                unitName: "Air Assault Regiment (BMD-2)",
                classification: UnitClassification.MAM,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                profileType: RegimentProfileType.DEP_MOB_EMB_HELO,
                deployedProfile: WeaponType.INF_AM_SV,
                isMountable: true,
                mobileProfile: WeaponType.IFV_BMD2_SV,
                isEmbarkable: true,
                embarkedProfile: WeaponType.HEL_MI8T_SV,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            aarBmd2Regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            aarBmd2Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP_MOB_EMB_HELO,
                deployedProfile: WeaponType.INF_AM_SV,
                isMountable: true,
                mobileProfile: WeaponType.IFV_BMD3_SV,
                isEmbarkable: true,
                embarkedProfile: WeaponType.HEL_MI8T_SV,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            aarBmd3Regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            aarBmd3Regiment.SetICM(GameData.ICM_DEFAULT);

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
            #region VDV Airborne Regiment (BMD-2)

            var vdvBmd2Regiment = new CombatUnit(
                unitName: "VDV Airborne Regiment (BMD-2)",
                classification: UnitClassification.MAB,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                profileType: RegimentProfileType.DEP_MOB_EMB_AIR,
                deployedProfile: WeaponType.INF_AB_SV,
                isMountable: true,
                mobileProfile: WeaponType.IFV_BMD2_SV,
                isEmbarkable: true,
                embarkedProfile: WeaponType.TRN_AN8_SV,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            vdvBmd2Regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            vdvBmd2Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP_MOB_EMB_AIR,
                deployedProfile: WeaponType.INF_AB_SV,
                isMountable: true,
                mobileProfile: WeaponType.IFV_BMD3_SV,
                isEmbarkable: true,
                embarkedProfile: WeaponType.TRN_AN8_SV,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            vdvBmd3Regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            vdvBmd3Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP_MOB_EMB_AIR,
                deployedProfile: WeaponType.ART_LIGHT_SV,
                isMountable: true,
                mobileProfile: WeaponType.APC_MTLB_SV,
                isEmbarkable: true,
                embarkedProfile: WeaponType.TRN_AN8_SV,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            vdvArtilleryRegiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            vdvArtilleryRegiment.SetICM(GameData.ICM_SMALL_UNIT);

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
                profileType: RegimentProfileType.DEP_MOB_EMB_AIR,
                deployedProfile: WeaponType.RCN_BRDM2AT_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.TRN_AN8_SV,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            vdvSupportRegiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            vdvSupportRegiment.SetICM(GameData.ICM_SMALL_UNIT);

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
                profileType: RegimentProfileType.DEP_MOB_EMB_NAVAL,
                deployedProfile: WeaponType.INF_MAR_SV,
                isMountable: true,
                mobileProfile: WeaponType.APC_BTR70_SV,
                isEmbarkable: true,
                embarkedProfile: WeaponType.TRN_NAVAL,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            navalInfantryBtr70Regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            navalInfantryBtr70Regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_NAV_BTR70", navalInfantryBtr70Regiment);

            #endregion //Naval Personnel Regiment (BTR-70)

            #region Naval Infantry Regiment (BTR-80)

            var navalInfantryBtr80Regiment = new CombatUnit(
                unitName: "Naval Infantry Regiment (BTR-80)",
                classification: UnitClassification.MMAR,
                role: UnitRole.GroundCombat,
                side: Side.Player,
                nationality: Nationality.USSR,
                profileType: RegimentProfileType.DEP_MOB_EMB_NAVAL,
                deployedProfile: WeaponType.INF_MAR_SV,
                isMountable: true,
                mobileProfile: WeaponType.APC_BTR80_SV,
                isEmbarkable: true,
                embarkedProfile: WeaponType.TRN_NAVAL,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            navalInfantryBtr80Regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            navalInfantryBtr80Regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_NAV_BTR80", navalInfantryBtr80Regiment);

            #endregion //Naval Personnel Regiment (BTR-80)
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
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_ENG_SV,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_SV,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            engineerRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            engineerRegiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_SPEC_SV,
                isMountable: true,
                mobileProfile: WeaponType.HEL_MI8T_SV,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            spetsnazRegiment.SetExperienceLevel(ExperienceLevel.Veteran);

            // Set the ICM
            spetsnazRegiment.SetICM(GameData.ICM_SMALL_UNIT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.RCN_BRDM2_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            reconRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            reconRegiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_RCR", reconRegiment);

            #endregion //Reconnaissance Regiment

            #region Anti-Tank Regiment

            var antiTankRegiment = new CombatUnit(
                unitName: "Recon Regiment AT",
                classification: UnitClassification.RECON,
                role: UnitRole.GroundCombatRecon,
                side: Side.Player,
                nationality: Nationality.USSR,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.RCN_BRDM2AT_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            antiTankRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            antiTankRegiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.AAA_GEN_SV,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_SV,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            adrGenericRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            adrGenericRegiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPAAA_ZSU57_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            adrZsu57Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            adrZsu57Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPAAA_ZSU23_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            adrZsu23Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            adrZsu23Regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_ADR_ZSU23", adrZsu23Regiment);

            #endregion //Air Defense Regiment (ZSU-23)

            #region Air Defense Regiment (2K12 Kub)

            var adr2k12Regiment = new CombatUnit(
                unitName: "Air Defense Regiment (2K12 Kub)",
                classification: UnitClassification.SPSAM,
                role: UnitRole.AirDefenseArea,
                side: Side.Player,
                nationality: Nationality.USSR,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPSAM_2K12_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            adr2k12Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            adr2k12Regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_ADR_2K12", adr2k12Regiment);

            #endregion //Air Defense Regiment (2K12 Kub)

            #region Air Defense Regiment (2K22 Tunguska)

            var adr2k22Regiment = new CombatUnit(
                unitName: "Air Defense Regiment (2K22 Tunguska)",
                classification: UnitClassification.SPSAM,
                role: UnitRole.AirDefenseArea,
                side: Side.Player,
                nationality: Nationality.USSR,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPSAM_2K22_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            adr2k22Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            adr2k22Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPSAM_9K31_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            spsam9k31Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            spsam9k31Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.SAM_S75_SV,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_SV,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            samS75Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            samS75Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.SAM_S125_SV,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_SV,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            samS125Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            samS125Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.SAM_S300_SV,
                isMountable: true,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: true,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            samS300Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            samS300Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.HEL_MI8AT_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            helMi8atRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            helMi8atRegiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.HEL_MI24D_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            helMi24dRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            helMi24dRegiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.HEL_MI24V_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            helMi24vRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            helMi24vRegiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.HEL_MI28_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            helMi28Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            helMi28Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_MIG21_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fgtMig21Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            fgtMig21Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_MIG23_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fgtMig23Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            fgtMig23Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,    
                deployedProfile: WeaponType.FGT_MIG25_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fgtMig25Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            fgtMig25Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_MIG29_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fgtMig29Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            fgtMig29Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_MIG31_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fgtMig31Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            fgtMig31Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_SU27_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fgtSu27Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            fgtSu27Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_SU47_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fgtSu47Regiment.SetExperienceLevel(ExperienceLevel.Elite);

            // Set the ICM
            fgtSu47Regiment.SetICM(GameData.ICM_LARGE_UNIT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_MIG27_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            mrfMig27Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            mrfMig27Regiment.SetICM(GameData.ICM_DEFAULT);

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
            #region Attack Regiment (Su-17)

             var attSu17Regiment = new CombatUnit(
                unitName: "Attack Regiment (Su-17)",
                classification: UnitClassification.ATT,
                role: UnitRole.AirGroundAttack,
                side: Side.Player,
                nationality: Nationality.USSR,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.ATT_SU17_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            attSu17Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            attSu17Regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_ATT_SU17", attSu17Regiment);

            #endregion //Attack Regiment (Su-17)

            #region Attack Regiment (Su-25)

            var attSu25Regiment = new CombatUnit(
                unitName: "Attack Regiment (Su-25)",
                classification: UnitClassification.ATT,
                role: UnitRole.AirGroundAttack,
                side: Side.Player,
                nationality: Nationality.USSR,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.ATT_SU25_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            attSu25Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            attSu25Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.ATT_SU25B_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            attSu25bRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            attSu25bRegiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.AWACS_A50_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            awacsA50Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            awacsA50Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.BMB_SU24_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            bmbSu24Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            bmbSu24Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.BMB_TU16_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            bmbTu16Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            bmbTu16Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.BMB_TU22_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            bmbTu22Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            bmbTu22Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.BMB_TU22M3_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            bmbTu22m3Regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            bmbTu22m3Regiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.RCNA_MIG25R_SV,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            rcnMig25rRegiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            rcnMig25rRegiment.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.BASE_LARGE,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Main,
                size: DepotSize.Large
            );

            // Set experience level - facilities don't gain experience
            sovSupplyDepot.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM - standard facility
            sovSupplyDepot.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.BASE_LARGE,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Main,
                size: DepotSize.Large
            );

            // Set experience level - facilities don't gain experience
            sovAirbase.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM - standard facility
            sovAirbase.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.BASE_MEDIUM,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Main,
                size: DepotSize.Medium
            );

            // Set experience level - facilities don't gain experience
            sovHQ.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM - command facility
            sovHQ.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.BASE_SMALL,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Main,
                size: DepotSize.Small
            );

            // Set experience level - facilities don't gain experience
            sovIntelBase.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM - specialized intelligence facility
            sovIntelBase.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("USSR_INTEL_BASE", sovIntelBase);

            #endregion //Soviet Intelligence Base
        }

        #endregion

        //-------------------------------------------------------------------------------------------------

        #region Mujahideen Units

        public static void CreateMujahideenForces()
        {
            #region Mujahideen Infantry

            var mjInf = new CombatUnit(
                unitName: "Mujahideen Infantry",
                classification: UnitClassification.INF,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.MJ,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.INF_REG_MJ,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            mjInf.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM - guerrillas are tough fighters on home terrain
            mjInf.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("MJ_INF", mjInf);

            #endregion //Mujahideen Infantry

            #region Mujahideen RPG Unit

            var mjRPGUnit = new CombatUnit(
                unitName: "Mujahideen RPG Unit",
                classification: UnitClassification.INF,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.MJ,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.INF_RPG_MJ,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level - guerrillas are tough fighters on home terrain
            mjRPGUnit.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM - specialized anti-armor unit
            mjRPGUnit.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("MJ_RPG_UNIT", mjRPGUnit);

            #endregion //Mujahideen RPG Unit

            #region Mujahideen Special Forces Commando

            var mjSpecCommando = new CombatUnit(
                unitName: "Mujahideen Commandos",
                classification: UnitClassification.SPECF,
                role: UnitRole.GroundCombatRecon,
                side: Side.AI,
                nationality: Nationality.MJ,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.INF_SPEC_MJ,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level - elite fighters
            mjSpecCommando.SetExperienceLevel(ExperienceLevel.Veteran);

            // Set the ICM - small elite unit
            mjSpecCommando.SetICM(GameData.ICM_SMALL_UNIT);

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
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.INF_CAV_MJ,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level - experienced in mobile warfare
            mjHorseCavalry.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM - traditional mobile fighters
            mjHorseCavalry.SetICM(GameData.ICM_DEFAULT);

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
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.AAA_GEN_MJ,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_ARAB,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            mjAntiAircraft.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM - portable air defense
            mjAntiAircraft.SetICM(GameData.ICM_SMALL_UNIT);

            // Add the template to the database
            AddTemplate("MJ_AAA", mjAntiAircraft);

            #endregion //Mujahideen Anti-Aircraft Unit

            #region Mujahideen SAM Unit

            var mjSAMUnit = new CombatUnit(
                unitName: "Mujahideen SAM Unit",
                classification: UnitClassification.SAM,
                role: UnitRole.AirDefenseArea,
                side: Side.AI,
                nationality: Nationality.MJ,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SAM_GEN_MJ,
                isMountable: true,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            mjSAMUnit.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM - specialized air defense unit
            mjSAMUnit.SetICM(GameData.ICM_SMALL_UNIT);

            // Add the template to the database
            AddTemplate("MJ_SAM_UNIT", mjSAMUnit);

            #endregion // Mujahideen SAM Unit

            #region Mujahideen Mortar Unit

            var mjLightMortar = new CombatUnit(
                unitName: "Mujahideen Mortar Unit",
                classification: UnitClassification.ART,
                role: UnitRole.GroundCombatIndirect,
                side: Side.AI,
                nationality: Nationality.MJ,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.ART_MORTAR_MJ,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_ARAB,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            mjLightMortar.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM - light artillery support
            mjLightMortar.SetICM(GameData.ICM_SMALL_UNIT);

            // Add the template to the database
            AddTemplate("MJ_ART_LIGHT_MORTAR", mjLightMortar);

            #endregion //Mujahideen Mortar Unit

            #region Mujahideen Light Artillery

            var mjLightArt = new CombatUnit(
                unitName: "Mujahideen Light Artillery",
                classification: UnitClassification.ART,
                role: UnitRole.GroundCombatIndirect,
                side: Side.AI,
                nationality: Nationality.MJ,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.ART_LIGHT_MJ,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_ARAB,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            mjLightArt.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM - heavier artillery support
            mjLightArt.SetICM(GameData.ICM_SMALL_UNIT);

            // Add the template to the database
            AddTemplate("MJ_ART_LIGHT", mjLightArt);

            #endregion //Mujahideen Heavy Mortar Unit

            #region Mujahideen Supply Cache

            var mjSupplyCache = new CombatUnit(
                unitName: "Mujahideen Supply Cache",
                classification: UnitClassification.DEPOT,
                role: UnitRole.GroundCombatStatic,
                side: Side.AI,
                nationality: Nationality.MJ,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.BASE_LARGE,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level - facilities don't gain experience
            mjSupplyCache.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM - small hidden supply cache
            mjSupplyCache.SetICM(GameData.ICM_SMALL_UNIT);

            // Add the template to the database
            AddTemplate("MJ_DEPOT", mjSupplyCache);

            #endregion //Mujahideen Supply Cache
        }

        #endregion // Mujahideen Units

        #region Western Units

        public static void CreateUSForces()
        {
            #region US Armored Brigade

            var us_armored_brigade = new CombatUnit(
                unitName: "US Armored Brigade",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_M1_US,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_armored_brigade.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            us_armored_brigade.SetICM(1.3f);

            // Add the template to the database
            AddTemplate("US_ARMOR_BRIGADE", us_armored_brigade);

            #endregion // US Armored Brigade

            #region US Mechanized Infantry Brigade

            var us_mech_brigade = new CombatUnit(
                unitName: "US Mechanized Infantry Brigade",
                classification: UnitClassification.MECH,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_REG_US,
                isMountable: true,
                mobileProfile: WeaponType.IFV_M2_US,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_mech_brigade.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            us_mech_brigade.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("US_MECH_BRIGADE", us_mech_brigade);

            #endregion // US Mechanized Infantry Brigade

            #region US Armored Cavalry Squadron

            var us_armored_cavalry_squadron = new CombatUnit(
                unitName: "US Armored Cavalry Squadron",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_M60_US,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_armored_cavalry_squadron.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            us_armored_cavalry_squadron.SetICM(1.2f);

            // Add the template to the database
            AddTemplate("US_ARMORED_CAVALRY_SQUADRON", us_armored_cavalry_squadron);

            #endregion // US Armored Cavalry Squadron

            #region US Artillery Regiment

            var us_artillery_regiment = new CombatUnit(
                unitName: "US Artillery Regiment",
                classification: UnitClassification.ART,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPA_M109_US,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_artillery_regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            us_artillery_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("US_ARTILLERY_REGIMENT", us_artillery_regiment);

            #endregion // US Artillery Regiment

            #region US Rocket Artillery Regiment

            var us_rocket_artillery_regiment = new CombatUnit(
                unitName: "US Rocket Artillery Regiment",
                classification: UnitClassification.ROC,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.ROC_MLRS_US,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_rocket_artillery_regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            us_rocket_artillery_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("US_ROCKET_ARTILLERY_REGIMENT", us_rocket_artillery_regiment);

            #endregion // US Rocket Artillery Regiment

            #region US Air Defense Regiment

            var us_air_defense_regiment = new CombatUnit(
                unitName: "US Air Defense Regiment",
                classification: UnitClassification.SPAAA,
                role: UnitRole.AirDefenseArea,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPAAA_M163_US,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_air_defense_regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            us_air_defense_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("US_AIR_DEFENSE_REGIMENT", us_air_defense_regiment);

            #endregion // US Air Defense Regiment

            #region US Hawk Regiment

            var us_hawk_regiment = new CombatUnit(
                unitName: "US Hawk Regiment",
                classification: UnitClassification.SAM,
                role: UnitRole.AirDefenseArea,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.SAM_HAWK_US,
                isMountable: true,
                mobileProfile: WeaponType.TRK_WEST,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_hawk_regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            us_hawk_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("US_HAWK_REGIMENT", us_hawk_regiment);

            #endregion // US Hawk Regiment

            #region US Chaparral Regiment

            var us_chaparral_regiment = new CombatUnit(
                unitName: "US Chaparral Regiment",
                classification: UnitClassification.SPSAM,
                role: UnitRole.AirDefenseArea,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPSAM_CHAP_US,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_chaparral_regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            us_chaparral_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("US_CHAPARRAL_REGIMENT", us_chaparral_regiment);

            #endregion // US Chaparral Regiment

            #region US Marine Expeditionary Unit

            var us_marine_expeditionary_unit = new CombatUnit(
                unitName: "US Marine Expeditionary Unit",
                classification: UnitClassification.MAR,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_MAR_US,
                isMountable: true,
                mobileProfile: WeaponType.APC_LVTP7_US,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_marine_expeditionary_unit.SetExperienceLevel(ExperienceLevel.Veteran);
            // Set the ICM
            us_marine_expeditionary_unit.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("US_MARINE_EXPEDITIONARY_UNIT", us_marine_expeditionary_unit);

            #endregion // US Marine Expeditionary Unit

            #region US Airborne Brigade

            var us_airborne_brigade = new CombatUnit(
                unitName: "US Airborne Brigade",
                classification: UnitClassification.AB,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_AB_US,
                isMountable: true,
                mobileProfile: WeaponType.APC_HUMVEE_US,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_airborne_brigade.SetExperienceLevel(ExperienceLevel.Veteran);
            // Set the ICM
            us_airborne_brigade.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("US_AIRBORNE_BRIGADE", us_airborne_brigade);

            #endregion // US Airborne Brigade

            #region US Airmobile Brigade

            var us_airmobile_brigade = new CombatUnit(
                unitName: "US Airmobile Brigade",
                classification: UnitClassification.AM,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP_MOB_EMB_HELO,
                deployedProfile: WeaponType.INF_AM_US,
                isMountable: true,
                mobileProfile: WeaponType.APC_HUMVEE_US,
                isEmbarkable: true,
                embarkedProfile: WeaponType.HEL_UH60_US,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_airmobile_brigade.SetExperienceLevel(ExperienceLevel.Veteran);
            // Set the ICM
            us_airmobile_brigade.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("US_AIRMOBILE_BRIGADE", us_airmobile_brigade);

            #endregion // US Airmobile Brigade

            #region US Recon Detachment

            var us_recon_detachment = new CombatUnit(
                unitName: "US Recon Detachment",
                classification: UnitClassification.RECON,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.RCN_M3_US,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_recon_detachment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            us_recon_detachment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("US_RECON_DETACHMENT", us_recon_detachment);

            #endregion // US Recon Detachment

            #region US Aviation Brigade

            var us_aviation_brigade = new CombatUnit(
                unitName: "US Aviation Brigade",
                classification: UnitClassification.HELO,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.HEL_AH64_US,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_aviation_brigade.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            us_aviation_brigade.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("US_AVIATION_BRIGADE", us_aviation_brigade);

            #endregion // US Aviation Brigade

            #region US AWACS Squadron

            var us_awacs_squadron = new CombatUnit(
                unitName: "US AWACS Squadron",
                classification: UnitClassification.AWACS,
                role: UnitRole.AirborneEarlyWarning,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.AWACS_E3_US,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_awacs_squadron.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            us_awacs_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("US_AWACS_SQUADRON", us_awacs_squadron);

            #endregion // US AWACS Squadron

            #region US F-15 Fighter Wing

            var us_f15_squadron = new CombatUnit(
                unitName: "US F-15 Fighter Squadron",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_F15_US,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_f15_squadron.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            us_f15_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("US_F15_FIGHTER_SQUADRON", us_f15_squadron);

            #endregion // US F-15 Fighter Wing

            #region US F-16 Fighter Wing

            var us_f16_squadron = new CombatUnit(
                unitName: "US F-16 Fighter Squadron",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_F16_US,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_f16_squadron.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            us_f16_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("US_F16_FIGHTER_SQUADRON", us_f16_squadron);

            #endregion // US F-16 Fighter Wing

            #region US F4 Phantom Fighter Wing

            var us_f4_squadron = new CombatUnit(
                unitName: "US F-4 Phantom Fighter Squadron",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_F4_US,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_f4_squadron.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            us_f4_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("US_F4_PHANTOM_FIGHTER_SQUADRON", us_f4_squadron);

            #endregion // US F4 Phantom Fighter Wing

            #region US Navy F-14 Fighter Squadron

            var us_f14_squadron = new CombatUnit(
                unitName: "US F-14 Fighter Squadron",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_F14_US,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_f14_squadron.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            us_f14_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("US_F14_FIGHTER_SQUADRON", us_f14_squadron);

            #endregion // US Navy F-14 Fighter Squadron

            #region US A-10 Attack Wing

            var us_a10_squadron = new CombatUnit(
                unitName: "US A-10 Attack Squadron",
                classification: UnitClassification.ATT,
                role: UnitRole.AirGroundAttack,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.ATT_A10_US,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_a10_squadron.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            us_a10_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("US_A10_ATTACK_SQUADRON", us_a10_squadron);

            #endregion // US A-10 Attack Wing

            #region US F-111 Strike Wing

            var us_f111_squadron = new CombatUnit(
                unitName: "US F-111 Strike Squadron",
                classification: UnitClassification.BMB,
                role: UnitRole.AirStrategicAttack,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.BMB_F111_US,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_f111_squadron.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            us_f111_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("US_F111_STRIKE_SQUADRON", us_f111_squadron);

            #endregion // US F-111 Strike Wing

            #region US F-117 Stealth Fighter Wing

            var us_f117_squadron = new CombatUnit(
                unitName: "US F-117 Stealth Fighter Squadron",
                classification: UnitClassification.ATT,
                role: UnitRole.AirStrategicAttack,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.ATT_F117_US,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_f117_squadron.SetExperienceLevel(ExperienceLevel.Elite);

            // Set the ICM
            us_f111_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("US_F117_STEALTH_FIGHTER_SQUADRON", us_f117_squadron);

            #endregion // US F-117 Stealth Fighter Wing

            #region US Recon Squadron

            var us_SR71_squadron = new CombatUnit(
                unitName: "US SR-71 Recon Squadron",
                classification: UnitClassification.RECONA,
                role: UnitRole.AirRecon,
                side: Side.AI,
                nationality: Nationality.USA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.RCNA_SR71_US,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            us_SR71_squadron.SetExperienceLevel(ExperienceLevel.Elite);

            // Set the ICM
            us_SR71_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("US_SR71_RECON_SQUADRON", us_SR71_squadron);

            #endregion // US Recon Squadron
        }

        public static void CreateGermanForces()
        {
            #region GE Panzer Regiment Leopard 1

            var ge_panzer_regiment_leo1 = new CombatUnit(
                unitName: "GE Panzer Regiment (Leopard 1)",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.FRG,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_LEOPARD1_GE,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ge_panzer_regiment_leo1.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            ge_panzer_regiment_leo1.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("GE_PANZER_REGIMENT_LEO1", ge_panzer_regiment_leo1);

            #endregion // GE Panzer Regiment

            #region GE Panzer Regiment Leopard 2

            var ge_panzer_regiment_leo2 = new CombatUnit(
                unitName: "GE Panzer Regiment (Leopard 2)",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.FRG,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_LEOPARD2_GE,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ge_panzer_regiment_leo2.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            ge_panzer_regiment_leo2.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("GE_PANZER_REGIMENT_LEO2", ge_panzer_regiment_leo2);

            #endregion // GE Panzer Regiment

            #region GE Panzergrenadier Regiment

            var ge_panzergrenadier_regiment = new CombatUnit(
                unitName: "GE Panzergrenadier Regiment",
                classification: UnitClassification.MECH,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.FRG,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_REG_GE,
                isMountable: true,
                mobileProfile: WeaponType.IFV_MARDER_GE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ge_panzergrenadier_regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            ge_panzergrenadier_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("GE_PANZERGRENADIER_REGIMENT", ge_panzergrenadier_regiment);

            #endregion // GE Panzergrenadier Regiment

            #region GE Airborne Regiment

            var ge_airborne_regiment = new CombatUnit(
                unitName: "GE Airborne Regiment",
                classification: UnitClassification.INF,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.FRG,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.INF_AB_GE,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ge_airborne_regiment.SetExperienceLevel(ExperienceLevel.Veteran);

            // Set the ICM
            ge_airborne_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("GE_AIRBORNE_REGIMENT", ge_airborne_regiment);

            #endregion // GE Airborne Regiment

            #region GE Recon Unit

            var ge_recon_unit = new CombatUnit(
                unitName: "GE Recon Unit",
                classification: UnitClassification.RECON,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.FRG,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.RCN_LUCHS_GE,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ge_recon_unit.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            ge_recon_unit.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("GE_RECON_UNIT", ge_recon_unit);

            #endregion // GE Recon Unit

            #region GE Self-Propelled Artillery Regiment

            var ge_sp_artillery_regiment = new CombatUnit(
                unitName: "GE Self-Propelled Artillery Regiment",
                classification: UnitClassification.ART,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.FRG,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPA_M109_GE,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ge_sp_artillery_regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            ge_sp_artillery_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("GE_SP_ARTILLERY_REGIMENT", ge_sp_artillery_regiment);

            #endregion // GE Self-Propelled Artillery Regiment

            #region GE Air Defense Regiment (Gepard)

            var ge_air_defense_regiment = new CombatUnit(
                unitName: "GE Air Defense Regiment (Gepard)",
                classification: UnitClassification.SPAAA,
                role: UnitRole.AirDefenseArea,
                side: Side.AI,
                nationality: Nationality.FRG,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPSAM_GEPARD_GE,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ge_air_defense_regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            ge_air_defense_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("GE_AIR_DEFENSE_REGIMENT", ge_air_defense_regiment);

            #endregion // GE Air Defense Regiment (Gepard)

            #region GE SAM Regiment (Hawk)

            var ge_hawk_regiment = new CombatUnit(
                unitName: "GE SAM Regiment (Hawk)",
                classification: UnitClassification.SAM,
                role: UnitRole.AirDefenseArea,
                side: Side.AI,
                nationality: Nationality.FRG,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.SAM_HAWK_US,
                isMountable: true,
                mobileProfile: WeaponType.TRK_WEST,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ge_hawk_regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            ge_hawk_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("GE_HAWK_REGIMENT", ge_hawk_regiment);

            #endregion // GE SAM Regiment (Hawk)

            #region GE Aviation Regiment (BO105)

            var ge_aviation_regiment = new CombatUnit(
                unitName: "GE Aviation Regiment (BO105)",
                classification: UnitClassification.HELO,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.FRG,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.HEL_BO105_GE,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ge_aviation_regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            ge_aviation_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("GE_AVIATION_REGIMENT", ge_aviation_regiment);

            #endregion // GE Aviation Regiment (BO105)

            #region GE F-4 Phantom Fighter Squadron

            var ge_f4_squadron = new CombatUnit(
                unitName: "GE F-4 Phantom Fighter Squadron",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.AI,
                nationality: Nationality.FRG,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_F4_GE,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ge_f4_squadron.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            ge_f4_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("GE_F4_PHANTOM_FIGHTER_SQUADRON", ge_f4_squadron);

            #endregion // GE F-4 Phantom Fighter Squadron

            #region GE Tornado Fighter Squadron

            var ge_tornado_squadron = new CombatUnit(
                unitName: "GE Tornado Fighter Squadron",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.AI,
                nationality: Nationality.FRG,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_TORNADO_GR1_US,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ge_tornado_squadron.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            ge_tornado_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("GE_TORNADO_FIGHTER_SQUADRON", ge_tornado_squadron);

            #endregion // GE Tornado Fighter Squadron
        }

        public static void CreateBritishForces()
        {
            #region UK Challenger 1 Armored Regiment

            var uk_armoured_regiment = new CombatUnit(
                unitName: "UK Armoured Regiment (Challenger 1)",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.UK,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_CHALLENGER1_UK,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            uk_armoured_regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            uk_armoured_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("UK_ARMOURED_REGIMENT", uk_armoured_regiment);

            #endregion // UK Challenger 1 Armored Regiment

            #region UK Mechanized Infantry Regiment

            var uk_mech_infantry_regiment = new CombatUnit(
                unitName: "UK Mechanized Infantry Regiment",
                classification: UnitClassification.MECH,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.UK,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_REG_UK,
                isMountable: true,
                mobileProfile: WeaponType.IFV_WARRIOR_UK,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            uk_mech_infantry_regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            uk_mech_infantry_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("UK_MECH_INFANTRY_REGIMENT", uk_mech_infantry_regiment);

            #endregion // UK Mechanized Infantry Regiment

            #region UK Airborne Regiment

            var uk_airborne_regiment = new CombatUnit(
                unitName: "UK Airborne Regiment",
                classification: UnitClassification.AB,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.UK,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_AB_UK,
                isMountable: true,
                mobileProfile: WeaponType.TRK_WEST,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            uk_airborne_regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            uk_airborne_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("UK_AIRBORNE_REGIMENT", uk_airborne_regiment);

            #endregion // UK Airborne Regiment

            #region UK Recon Unit

            var uk_recon_unit = new CombatUnit(
                unitName: "UK Recon Unit",
                classification: UnitClassification.RECON,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.UK,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.RCN_FV105_UK,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            uk_recon_unit.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            uk_recon_unit.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("UK_RECON_UNIT", uk_recon_unit);

            #endregion // UK Recon Unit

            #region UK Self-Propelled Artillery Regiment

            var uk_sp_artillery_regiment = new CombatUnit(
                unitName: "UK Self-Propelled Artillery Regiment",
                classification: UnitClassification.ART,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.UK,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPA_M109_UK,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            uk_sp_artillery_regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            uk_sp_artillery_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("UK_SP_ARTILLERY_REGIMENT", uk_sp_artillery_regiment);

            #endregion // UK Self-Propelled Artillery Regiment

            #region UK Air Defense Regiment (US M163)

            var uk_air_defense_regiment = new CombatUnit(
                unitName: "UK Air Defense Regiment (M163)",
                classification: UnitClassification.SPAAA,
                role: UnitRole.AirDefenseArea,
                side: Side.AI,
                nationality: Nationality.UK,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPAAA_M163_US,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            uk_air_defense_regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            uk_air_defense_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("UK_AIR_DEFENSE_REGIMENT", uk_air_defense_regiment);

            #endregion // UK Air Defense Regiment (US M163)

            #region UK SAM Regiment (Rapier)

            var uk_rapier_regiment = new CombatUnit(
                unitName: "UK SAM Regiment (Rapier)",
                classification: UnitClassification.SPSAM,
                role: UnitRole.AirDefenseArea,
                side: Side.AI,
                nationality: Nationality.UK,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPSAM_RAPIER_UK,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            uk_rapier_regiment.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            uk_rapier_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("UK_RAPIER_REGIMENT", uk_rapier_regiment);

            #endregion // UK SAM Regiment (Rapier)

            #region UK Tornado Fighter Squadron

            var uk_tornado_squadron = new CombatUnit(
                unitName: "UK Tornado Fighter Squadron",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.AI,
                nationality: Nationality.UK,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_TORNADO_IDS_UK,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            uk_tornado_squadron.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            uk_tornado_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("UK_TORNADO_FIGHTER_SQUADRON", uk_tornado_squadron);

            #endregion // UK Tornado Fighter Squadron
        }

        public static void CreateFrenchForces()
        {
            #region FR Armored Brigade

            var fr_armored_brigade = new CombatUnit(
                unitName: "FR Armored Brigade (AMX-30)",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.FRA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_AMX30_FR,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fr_armored_brigade.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            fr_armored_brigade.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("FR_ARMORED_BRIGADE", fr_armored_brigade);

            #endregion // FR Armored Brigade

            #region FR Mechanized Infantry Brigade

            var fr_mech_infantry_brigade = new CombatUnit(
                unitName: "FR Mechanized Infantry Brigade",
                classification: UnitClassification.MECH,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.FRA,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_REG_FR,
                isMountable: true,
                mobileProfile: WeaponType.APC_VAB_FR,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fr_mech_infantry_brigade.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            fr_mech_infantry_brigade.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("FR_MECH_INFANTRY_BRIGADE", fr_mech_infantry_brigade);

            #endregion // FR Mechanized Infantry Brigade

            #region FR Airborne Brigade

            var fr_airborne_brigade = new CombatUnit(
                unitName: "FR Airborne Brigade",
                classification: UnitClassification.AB,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.FRA,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_AB_FR,
                isMountable: true,
                mobileProfile: WeaponType.TRK_WEST,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fr_airborne_brigade.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            fr_airborne_brigade.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("FR_AIRBORNE_BRIGADE", fr_airborne_brigade);

            #endregion // FR Airborne Brigade

            #region FR Recon Unit

            var fr_recon_unit = new CombatUnit(
                unitName: "FR Recon Unit (ERC-90)",
                classification: UnitClassification.RECON,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.FRA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.RCN_ERC90_FR,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fr_recon_unit.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            fr_recon_unit.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("FR_RECON_UNIT", fr_recon_unit);

            #endregion // FR Recon Unit

            #region FR Self-Propelled Artillery Regiment

            var fr_sp_artillery_regiment = new CombatUnit(
                unitName: "FR Self-Propelled Artillery Regiment",
                classification: UnitClassification.ART,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.FRA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPA_M109_FR,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fr_sp_artillery_regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            fr_sp_artillery_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("FR_SP_ARTILLERY_REGIMENT", fr_sp_artillery_regiment);

            #endregion // FR Self-Propelled Artillery Regiment

            #region FR Air Defense Regiment (Roland)

            var fr_air_defense_regiment = new CombatUnit(
                unitName: "FR Air Defense Regiment (Roland)",
                classification: UnitClassification.SPAAA,
                role: UnitRole.AirDefenseArea,
                side: Side.AI,
                nationality: Nationality.FRA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPAAA_ROLAND_FR,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fr_air_defense_regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            fr_air_defense_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("FR_AIR_DEFENSE_REGIMENT", fr_air_defense_regiment);

            #endregion // FR Air Defense Regiment (Roland)

            #region FR SAM Regiment (Crotale)

            var fr_crotale_regiment = new CombatUnit(
                unitName: "FR SAM Regiment (Crotale)",
                classification: UnitClassification.SPSAM,
                role: UnitRole.AirDefenseArea,
                side: Side.AI,
                nationality: Nationality.FRA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPSAM_CROTALE_FR,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fr_crotale_regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            fr_crotale_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("FR_CROTALE_REGIMENT", fr_crotale_regiment);

            #endregion // FR SAM Regiment (Crotale)

            #region FR Mirage F1 Fighter Squadron

            var fr_mirage_f1_squadron = new CombatUnit(
                unitName: "FR Mirage F1 Fighter Squadron",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.AI,
                nationality: Nationality.FRA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_MIRAGEF1_FR,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fr_mirage_f1_squadron.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            fr_mirage_f1_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("FR_MIRAGE_F1_FIGHTER_SQUADRON", fr_mirage_f1_squadron);

            #endregion // FR Mirage F1 Fighter Squadron

            #region FR Mirage 2000 Fighter Squadron

            var fr_mirage_2000_squadron = new CombatUnit(
                unitName: "FR Mirage 2000 Fighter Squadron",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.AI,
                nationality: Nationality.FRA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_MIRAGE2000_FR,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fr_mirage_2000_squadron.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            fr_mirage_2000_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("FR_MIRAGE_2000_FIGHTER_SQUADRON", fr_mirage_2000_squadron);

            #endregion // FR Mirage 2000 Fighter Squadron

            #region FR Jaguar Attack Squadron

            var fr_jaguar_squadron = new CombatUnit(
                unitName: "FR Jaguar Attack Squadron",
                classification: UnitClassification.ATT,
                role: UnitRole.AirGroundAttack,
                side: Side.AI,
                nationality: Nationality.FRA,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.ATT_JAGUAR_FR,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            fr_jaguar_squadron.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            fr_jaguar_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("FR_JAGUAR_ATTACK_SQUADRON", fr_jaguar_squadron);

            #endregion // FR Jaguar Attack Squadron
        }

        #endregion // Western Units

        //-------------------------------------------------------------------------------------------------

        #region Arab Units

        public static void CreateArabForces()
        {
            #region Iraqi Tank Regiment (T-55)

            var iq_tank_regiment_t55 = new CombatUnit(
                unitName: "Iraqi Tank Regiment (T-55)",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.IQ,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_T55A_IQ,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            iq_tank_regiment_t55.SetExperienceLevel(ExperienceLevel.Green);

            // Set the ICM
            iq_tank_regiment_t55.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IQ_TANK_REGIMENT_T55", iq_tank_regiment_t55);

            #endregion // Iraqi Tank Regiment (T-55)

            #region Iraqi Tank Regiment (T-62)

            var iq_tank_regiment_t62 = new CombatUnit(
                unitName: "Iraqi Tank Regiment (T-62)",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.IQ,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_T62A_IQ,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            iq_tank_regiment_t62.SetExperienceLevel(ExperienceLevel.Green);

            // Set the ICM
            iq_tank_regiment_t62.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IQ_TANK_REGIMENT_T62", iq_tank_regiment_t62);

            #endregion // Iraqi Tank Regiment (T-62)

            #region Iraqi Armored Infantry Regiment

            var iq_armored_infantry_regiment = new CombatUnit(
                unitName: "Iraqi Armored Infantry Regiment (BMP-1)",
                classification: UnitClassification.MECH,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.IQ,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_REG_IQ,
                isMountable: true,
                mobileProfile: WeaponType.IFV_BMP1_IQ,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            iq_armored_infantry_regiment.SetExperienceLevel(ExperienceLevel.Green);

            // Set the ICM
            iq_armored_infantry_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IQ_ARMORED_INFANTRY_REGIMENT", iq_armored_infantry_regiment);

            #endregion // Iraqi Armored Infantry Regiment (BMP-1)

            #region Iraqi Infantry Regiment

            var iq_infantry_regiment = new CombatUnit(
                unitName: "Iraqi Infantry Regiment",
                classification: UnitClassification.INF,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.IQ,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_REG_IQ,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_ARAB,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            iq_infantry_regiment.SetExperienceLevel(ExperienceLevel.Raw);

            // Set the ICM
            iq_infantry_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IQ_INFANTRY_REGIMENT", iq_infantry_regiment);

            #endregion // Iraqi Infantry Regiment

            #region Iraqi Self-Propelled Artillery Regiment (2S1)

            var iq_sp_artillery_regiment = new CombatUnit(
                unitName: "Iraqi Self-Propelled Artillery Regiment (2S1)",
                classification: UnitClassification.ART,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.IQ,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPA_2S1_IQ,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            iq_sp_artillery_regiment.SetExperienceLevel(ExperienceLevel.Green);

            // Set the ICM
            iq_sp_artillery_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IQ_SP_ARTILLERY_REGIMENT", iq_sp_artillery_regiment);

            #endregion // Iraqi Self-Propelled Artillery Regiment

            #region Iraqi Towed Artillery Regiment (ART_HEAVY_ARAB)

            var iq_towed_artillery_regiment = new CombatUnit(
                unitName: "Iraqi Towed Artillery Regiment",
                classification: UnitClassification.ART,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.IQ,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.ART_HEAVY_ARAB,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_ARAB,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            iq_towed_artillery_regiment.SetExperienceLevel(ExperienceLevel.Green);

            // Set the ICM
            iq_towed_artillery_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IQ_TOWED_ARTILLERY_REGIMENT", iq_towed_artillery_regiment);

            #endregion // Iraqi Towed Artillery Regiment

            #region Iraqi Air Defense Regiment (ZSU-57)

            var iq_air_defense_regiment = new CombatUnit(
                unitName: "Iraqi Air Defense Regiment (ZSU-57)",
                classification: UnitClassification.SPAAA,
                role: UnitRole.AirDefenseArea,
                side: Side.AI,
                nationality: Nationality.IQ,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPAAA_ZSU57_IQ,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            iq_air_defense_regiment.SetExperienceLevel(ExperienceLevel.Green);

            // Set the ICM
            iq_air_defense_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IQ_AIR_DEFENSE_REGIMENT", iq_air_defense_regiment);

            #endregion // Iraqi Air Defense Regiment (ZSU-57)

            #region Iraqi Self Propelled SAM Regiment (2K12)

            var iq_spsam_regiment = new CombatUnit(
                unitName: "Iraqi Self-Propelled SAM Regiment (2K12)",
                classification: UnitClassification.SPSAM,
                role: UnitRole.AirDefenseArea,
                side: Side.AI,
                nationality: Nationality.IQ,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPSAM_2K12_IQ,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            iq_spsam_regiment.SetExperienceLevel(ExperienceLevel.Green);

            // Set the ICM
            iq_spsam_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IQ_SPSAM_REGIMENT", iq_spsam_regiment);

            #endregion // Iraqi Self Propelled SAM Regiment (2K12)

            #region Iraqi SAM Regiment (SAM_S75_SV)

            var iq_sam_regiment = new CombatUnit(
                unitName: "Iraqi SAM Regiment (S-75)",
                classification: UnitClassification.SAM,
                role: UnitRole.AirDefenseArea,
                side: Side.AI,
                nationality: Nationality.IQ,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.SAM_S75_SV,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_ARAB,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            iq_sam_regiment.SetExperienceLevel(ExperienceLevel.Green);

            // Set the ICM
            iq_sam_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IQ_SAM_REGIMENT", iq_sam_regiment);

            #endregion // Iraqi SAM Regiment (SAM_S75_SV)

            #region Iraqi Fighter Squadron (MiG-21)

            var iq_mig21_squadron = new CombatUnit(
                unitName: "Iraqi Fighter Squadron (MiG-21)",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.AI,
                nationality: Nationality.IQ,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_MIG21_IQ,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            iq_mig21_squadron.SetExperienceLevel(ExperienceLevel.Green);

            // Set the ICM
            iq_mig21_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IQ_MIG21_FIGHTER_SQUADRON", iq_mig21_squadron);

            #endregion // Iraqi Fighter Squadron (MiG-21)

            #region Iraqi Fighter Squadron (MiG-23)

            var iq_mig23_squadron = new CombatUnit(
                unitName: "Iraqi Fighter Squadron (MiG-23)",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.AI,
                nationality: Nationality.IQ,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_MIG23_IQ,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            iq_mig23_squadron.SetExperienceLevel(ExperienceLevel.Green);

            // Set the ICM
            iq_mig23_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IQ_MIG23_FIGHTER_SQUADRON", iq_mig23_squadron);

            #endregion // Iraqi Fighter Squadron (MiG-23)

            #region Iraqi Attack Squadron (Su-17)

            var iq_su17_squadron = new CombatUnit(
                unitName: "Iraqi Attack Squadron (Su-17)",
                classification: UnitClassification.ATT,
                role: UnitRole.AirGroundAttack,
                side: Side.AI,
                nationality: Nationality.IQ,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.ATT_SU17_IQ,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            iq_su17_squadron.SetExperienceLevel(ExperienceLevel.Green);

            // Set the ICM
            iq_su17_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IQ_SU17_ATTACK_SQUADRON", iq_su17_squadron);

            #endregion // Iraqi Attack Squadron (Su-17)


            #region Iranian Tank Regiment (M-60)

            var ir_tank_regiment = new CombatUnit(
                unitName: "Iranian Tank Regiment (M-60)",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.IR,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_M60A3_IR,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ir_tank_regiment.SetExperienceLevel(ExperienceLevel.Green);

            // Set the ICM
            ir_tank_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IR_TANK_REGIMENT", ir_tank_regiment);

            #endregion // Iranian Tank Regiment (M-60)

            #region Iranian Armored Infantry Regiment (M-113)

            var ir_armored_infantry_regiment = new CombatUnit(
                unitName: "Iranian Armored Infantry Regiment (M-113)",
                classification: UnitClassification.MECH,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.IR,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_REG_IR,
                isMountable: true,
                mobileProfile: WeaponType.APC_M113_IR,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ir_armored_infantry_regiment.SetExperienceLevel(ExperienceLevel.Green);

            // Set the ICM
            ir_armored_infantry_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IR_ARMORED_INFANTRY_REGIMENT", ir_armored_infantry_regiment);

            #endregion // Iranian Armored Infantry Regiment (M-113)

            #region Iranian Infantry Regiment

            var ir_infantry_regiment = new CombatUnit(
                unitName: "Iranian Infantry Regiment",
                classification: UnitClassification.INF,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.IR,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_REG_IR,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_ARAB,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ir_infantry_regiment.SetExperienceLevel(ExperienceLevel.Raw);

            // Set the ICM
            ir_infantry_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IR_INFANTRY_REGIMENT", ir_infantry_regiment);

            #endregion // Iranian Infantry Regiment

            #region Iranian Towed Heavy Artillery Regiment (ART_HEAVY_ARAB)

            var ir_heavy_artillery_regiment = new CombatUnit(
                unitName: "Iranian Towed Heavy Artillery Regiment",
                classification: UnitClassification.ART,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.IR,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.ART_HEAVY_ARAB,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_ARAB,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ir_heavy_artillery_regiment.SetExperienceLevel(ExperienceLevel.Green);

            // Set the ICM
            ir_heavy_artillery_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IR_HEAVY_ARTILLERY_REGIMENT", ir_heavy_artillery_regiment);

            #endregion // Iranian Heavy Artillery Regiment

            #region Iranian Towed Light Artillery Regiment (ART_LIGHT_ARAB)

            var ir_light_artillery_regiment = new CombatUnit(
                unitName: "Iranian Towed Light Artillery Regiment",
                classification: UnitClassification.ART,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.IR,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.ART_LIGHT_ARAB,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_ARAB,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ir_light_artillery_regiment.SetExperienceLevel(ExperienceLevel.Green);

            // Set the ICM
            ir_light_artillery_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IR_LIGHT_ARTILLERY_REGIMENT", ir_light_artillery_regiment);

            #endregion // Iranian Light Artillery Regiment

            #region Iranian Towed Air Defense Regiment (AAA_GEN_SV)

            var ir_air_defense_regiment = new CombatUnit(
                unitName: "Iranian Air Defense Regiment",
                classification: UnitClassification.AAA,
                role: UnitRole.AirDefenseArea,
                side: Side.AI,
                nationality: Nationality.IR,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.AAA_GEN_SV,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_ARAB,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ir_air_defense_regiment.SetExperienceLevel(ExperienceLevel.Green);

            // Set the ICM
            ir_air_defense_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IR_AIR_DEFENSE_REGIMENT", ir_air_defense_regiment);

            #endregion // Iranian Towed Air Defense Regiment (AAA_GEN_SV)

            #region Iranian Towed SAM Regiment (SAM_S75_SV)

            var ir_sam_regiment = new CombatUnit(
                unitName: "Iranian SAM Regiment (S-75)",
                classification: UnitClassification.SAM,
                role: UnitRole.AirDefenseArea,
                side: Side.AI,
                nationality: Nationality.IR,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.SAM_S75_SV,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_ARAB,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ir_sam_regiment.SetExperienceLevel(ExperienceLevel.Green);

            // Set the ICM
            ir_sam_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IR_SAM_REGIMENT", ir_sam_regiment);

            #endregion // Iranian Towed SAM Regiment (SAM_S75_SV)

            #region Iranian Fighter Squadron (F-14)

            var ir_f14_squadron = new CombatUnit(
                unitName: "Iranian Fighter Squadron (F-14)",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.AI,
                nationality: Nationality.IR,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_F14_IR,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ir_f14_squadron.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            ir_f14_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IR_F14_FIGHTER_SQUADRON", ir_f14_squadron);

            #endregion // Iranian Fighter Squadron (F-14)

            #region Iranian Fighter Squadron (F4)

            var ir_f4_squadron = new CombatUnit(
                unitName: "Iranian Fighter Squadron (F-4)",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.AI,
                nationality: Nationality.IR,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_F4_IR,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ir_f4_squadron.SetExperienceLevel(ExperienceLevel.Experienced);

            // Set the ICM
            ir_f4_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("IR_F4_FIGHTER_SQUADRON", ir_f4_squadron);

            #endregion // Iranian Fighter Squadron (F4)
        }

        #endregion // Arab Units

        //-------------------------------------------------------------------------------------------------

        #region Chines Units

        public static void CreateChineseForces()
        {
            #region Chinese Tank Regiment (Type59)

            var ch_tank_regiment_type59 = new CombatUnit(
                unitName: "Chinese Tank Regiment (Type 59)",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.China,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_TYPE59,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ch_tank_regiment_type59.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            ch_tank_regiment_type59.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("CH_TANK_REGIMENT_TYPE59", ch_tank_regiment_type59);

            #endregion // Chinese Tank Regiment (Type59)

            #region Chinese Tank Regiment (Type80)

            var ch_tank_regiment_type80 = new CombatUnit(
                unitName: "Chinese Tank Regiment (Type 80)",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.China,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_TYPE80,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ch_tank_regiment_type80.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            ch_tank_regiment_type80.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("CH_TANK_REGIMENT_TYPE80", ch_tank_regiment_type80);

            #endregion // Chinese Tank Regiment (Type80)

            #region Chinese Tank Regiment (Type95)

            var ch_tank_regiment_type95 = new CombatUnit(
                unitName: "Chinese Tank Regiment (Type 95)",
                classification: UnitClassification.TANK,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.China,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.TANK_TYPE95,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ch_tank_regiment_type95.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            ch_tank_regiment_type95.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("CH_TANK_REGIMENT_TYPE95", ch_tank_regiment_type95);

            #endregion // Chinese Tank Regiment (Type95)

            #region Chinese Mechanized Infantry Regiment (Type86)

            var ch_mech_infantry_regiment = new CombatUnit(
                unitName: "Chinese Mechanized Infantry Regiment (Type 86)",
                classification: UnitClassification.MECH,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.China,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_REG_CH,
                isMountable: true,
                mobileProfile: WeaponType.IFV_TYPE86,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ch_mech_infantry_regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            ch_mech_infantry_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("CH_MECH_INFANTRY_REGIMENT", ch_mech_infantry_regiment);

            #endregion // Chinese Mechanized Infantry Regiment (Type86)

            #region Chinese Infantry Regiment

            var ch_infantry_regiment = new CombatUnit(
                unitName: "Chinese Infantry Regiment",
                classification: UnitClassification.INF,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.China,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_REG_CH,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_SV,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ch_infantry_regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            ch_infantry_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("CH_INFANTRY_REGIMENT", ch_infantry_regiment);

            #endregion // Chinese Infantry Regiment

            #region Chinese Airborne Regiment

            var ch_airborne_regiment = new CombatUnit(
                unitName: "Chinese Airborne Regiment",
                classification: UnitClassification.AB,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.China,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.INF_AB_CH,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_SV,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ch_airborne_regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            ch_airborne_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("CH_AIRBORNE_REGIMENT", ch_airborne_regiment);

            #endregion // Chinese Airborne Regiment

            #region Chinese Towed Artillery Regiment (ART_HEAVY_CH)

            var ch_heavy_artillery_regiment = new CombatUnit(
                unitName: "Chinese Towed Heavy Artillery Regiment",
                classification: UnitClassification.ART,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.China,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.ART_HEAVY_CH,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_SV,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ch_heavy_artillery_regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            ch_heavy_artillery_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("CH_HEAVY_ARTILLERY_REGIMENT", ch_heavy_artillery_regiment);

            #endregion // Chinese Towed Artillery Regiment (ART_HEAVY_CH)

            #region Chinese Towed Artillery Regiment (ART_LIGHT_CH)

            var ch_light_artillery_regiment = new CombatUnit(
                unitName: "Chinese Towed Light Artillery Regiment",
                classification: UnitClassification.ART,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.China,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.ART_LIGHT_CH,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_SV,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ch_light_artillery_regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            ch_light_artillery_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("CH_LIGHT_ARTILLERY_REGIMENT", ch_light_artillery_regiment);

            #endregion // Chinese Towed Artillery Regiment (ART_LIGHT_CH)

            #region Chinese Self-Propelled Artillery Regiment (PHZ89)

            var ch_sp_artillery_regiment = new CombatUnit(
                unitName: "Chinese Self-Propelled Artillery Regiment (PHZ-89)",
                classification: UnitClassification.ROC,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.China,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.ROC_PHZ89,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ch_sp_artillery_regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            ch_sp_artillery_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("CH_SP_ARTILLERY_REGIMENT", ch_sp_artillery_regiment);

            #endregion // Chinese Self-Propelled Artillery Regiment (PHZ89)

            #region Chinese Air Defense Regiment (Type53)

            var ch_air_defense_regiment = new CombatUnit(
                unitName: "Chinese Air Defense Regiment (Type 53)",
                classification: UnitClassification.SPAAA,
                role: UnitRole.AirDefenseArea,
                side: Side.AI,
                nationality: Nationality.China,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPAAA_TYPE53,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ch_air_defense_regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            ch_air_defense_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("CH_AIR_DEFENSE_REGIMENT", ch_air_defense_regiment);

            #endregion // Chinese Air Defense Regiment (Type53)

            #region Chinese SAM Regiment (HQ-7)

            var ch_hq7_regiment = new CombatUnit(
                unitName: "Chinese SAM Regiment (HQ-7)",
                classification: UnitClassification.SPSAM,
                role: UnitRole.AirDefenseArea,
                side: Side.AI,
                nationality: Nationality.China,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.SPSAM_HQ7,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ch_hq7_regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            ch_hq7_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("CH_HQ7_REGIMENT", ch_hq7_regiment);

            #endregion // Chinese SAM Regiment (HQ-7)

            #region Chinese SAM Regiment (SAM_S125_SV)

            var ch_sam_regiment = new CombatUnit(
                unitName: "Chinese SAM Regiment (S-125)",
                classification: UnitClassification.SAM,
                role: UnitRole.AirDefenseArea,
                side: Side.AI,
                nationality: Nationality.China,
                profileType: RegimentProfileType.DEP_MOB,
                deployedProfile: WeaponType.SAM_S125_SV,
                isMountable: true,
                mobileProfile: WeaponType.TRK_GEN_SV,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ch_sam_regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            ch_sam_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("CH_SAM_REGIMENT", ch_sam_regiment);

            #endregion // Chinese SAM Regiment (SAM_S125_SV)

            #region Chinese Fighter Squadron (J-7)

            var ch_j7_squadron = new CombatUnit(
                unitName: "Chinese Fighter Squadron (J-7)",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.AI,
                nationality: Nationality.China,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_J7,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ch_j7_squadron.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            ch_j7_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("CH_J7_FIGHTER_SQUADRON", ch_j7_squadron);

            #endregion // Chinese Fighter Squadron (J-7)

            #region Chinese Fighter Squadron (J-8)

            var ch_j8_squadron = new CombatUnit(
                unitName: "Chinese Fighter Squadron (J-8)",
                classification: UnitClassification.FGT,
                role: UnitRole.AirSuperiority,
                side: Side.AI,
                nationality: Nationality.China,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.FGT_J8,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ch_j8_squadron.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            ch_j8_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("CH_J8_FIGHTER_SQUADRON", ch_j8_squadron);

            #endregion // Chinese Fighter Squadron (J-8)

            #region Chinese Attack Squadron (Q-5)

            var ch_q5_squadron = new CombatUnit(
                unitName: "Chinese Attack Squadron (Q-5)",
                classification: UnitClassification.ATT,
                role: UnitRole.AirGroundAttack,
                side: Side.AI,
                nationality: Nationality.China,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.ATT_Q5,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ch_q5_squadron.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            ch_q5_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("CH_Q5_ATTACK_SQUADRON", ch_q5_squadron);

            #endregion // Chinese Attack Squadron (Q-5)

            #region Chinese Bomber Squadron (H-6)

            var ch_h6_squadron = new CombatUnit(
                unitName: "Chinese Bomber Squadron (H-6)",
                classification: UnitClassification.BMB,
                role: UnitRole.AirStrategicAttack,
                side: Side.AI,
                nationality: Nationality.China,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.BMB_H6,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ch_h6_squadron.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            ch_h6_squadron.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("CH_H6_BOMBER_SQUADRON", ch_h6_squadron);

            #endregion // Chinese Bomber Squadron (H-6)

            #region Chinese Aviation Regiment (H9)

            var ch_aviation_regiment = new CombatUnit(
                unitName: "Chinese Aviation Regiment (H-9)",
                classification: UnitClassification.HELO,
                role: UnitRole.GroundCombat,
                side: Side.AI,
                nationality: Nationality.China,
                profileType: RegimentProfileType.DEP,
                deployedProfile: WeaponType.HEL_H9,
                isMountable: false,
                mobileProfile: WeaponType.NONE,
                isEmbarkable: false,
                embarkedProfile: WeaponType.NONE,
                category: DepotCategory.Secondary,
                size: DepotSize.Small
            );

            // Set experience level
            ch_aviation_regiment.SetExperienceLevel(ExperienceLevel.Trained);

            // Set the ICM
            ch_aviation_regiment.SetICM(GameData.ICM_DEFAULT);

            // Add the template to the database
            AddTemplate("CH_AVIATION_REGIMENT", ch_aviation_regiment);

            #endregion // Chinese Aviation Regiment
        }

        #endregion // Chinese Units
    }
}
