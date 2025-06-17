using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HammerAndSickle.Models
{
    /// <summary>
    ///  Unified wrapper that plugs LandBaseFacility, AirbaseFacility, and SupplyDepotFacility.
    /// </summary>
    [Serializable]
    public sealed class BaseFacilitySubsystem
    {
        public FacilityType FacilityType { get; private set; } = FacilityType.HQ;

        // runtime reference – non‑null once attached
        private LandBaseFacility _facility;

        // cached down‑casts
        private AirbaseFacility Airbase => _facility as AirbaseFacility;
        private SupplyDepotFacility Depot => _facility as SupplyDepotFacility;

        // back‑pointer for supply & damage propagation
        private readonly CombatUnit _parent;

       

        private void AttachFacility(LandBaseFacility facility)
        {
            _facility = facility;
            FacilityType = facility switch
            {
                AirbaseFacility => FacilityType.Airbase,
                SupplyDepotFacility => FacilityType.SupplyDepot,
                _ => FacilityType.HQ,
            };
        }


        //──────────────────────────────────────────────────────────────────────────
        // High‑level API – CombatUnit delegates through these helpers
        //──────────────────────────────────────────────────────────────────────────

        public float OperationalEfficiency => _facility.GetEfficiencyMultiplier();

        public void ApplyDamage(int amount)
        {
            _facility.AddDamage(amount);

            // Notify the UI about the damage event.
            AppService.CaptureUiMessage($"{_facility.BaseName} has suffered {amount} damage.");
        }

        public void RepairDamage(int amount) => _facility.RepairDamage(amount);

        #region ─── Airbase Wrappers ─────────────────────────────────────────────-
        //public bool CanLaunchAirOps() => Airbase?.CanLaunchAirOperations() ?? false;
        //public bool CanAcceptAircraft() => Airbase.CanAcceptNewAircraft ?? false;
        //public IReadOnlyList<CombatUnit> HostAirUnits => Airbase?.AirUnitsAttachedReadOnly ?? Array.Empty<CombatUnit>();
        //public bool TryAttachAirUnit(CombatUnit airUnit) => Airbase?.TryAttachAirUnit(airUnit) ?? false;
        //public bool TryDetachAirUnit(CombatUnit airUnit) => Airbase?.TryDetachAirUnit(airUnit) ?? false;
        #endregion

        #region ─── Supply Depot Wrappers ────────────────────────────────────────
        //public int CurrentStockpile => Depot?.CurrentStockpile ?? 0;
        //public int StockpileCapacity => Depot?.StockpileCapacity ?? 0;
        //public bool ConsumeSupplies(int days) => Depot?.TryConsumeSupplies(days) ?? false;
        //public void InjectSupplies(int amount) => Depot?.AddSupplies(amount);
        #endregion

        /// <summary>
        /// Called once per strategic turn by <see cref="CombatUnit"/>.
        /// Handles generation, decay, and propagation of resources.
        /// </summary>
        public void EndTurn()
        {
            
        }
    }
}
