using Colossal.UI.Binding;
using System;

namespace BuildingUse
{
    // This mod's building status types.
    // The "BU" (i.e. Building Use) prefix differentiates this enum from Game.Prefabs.BuildingStatusType.
    // Start at an arbitrary large number to avoid overlap with the game's BuildingStatusType and
    // hopefully avoid conflicts with any other mod's building status types.
    // This mod has logic that assumes these are numbered sequentially.
    public enum BUBuildingStatusType
    {
        None = 123456700,

        EmployeesSelectDeselect,
        EmployeesResidential,
        EmployeesCommercial,
        EmployeesIndustrial,
        EmployeesOffice,
        EmployeesParking,
        EmployeesRoadMaintenance,
        EmployeesElectricity,
        EmployeesWater,
        EmployeesSewage,
        EmployeesHealthcare,
        EmployeesDeathcare,
        EmployeesGarbageManagement,
        EmployeesEducation,
        EmployeesResearch,
        EmployeesFireRescue,
        EmployeesDisasterControl,
        EmployeesPolice,
        EmployeesAdministration,
        EmployeesTransportation,
        EmployeesParkMaintenance,
        EmployeesParksRecreation,
        EmployeesPost,
        EmployeesTelecom,

        VisitorsSelectDeselect,
        VisitorsHealthcare,
        VisitorsCemetery,
        VisitorsCrematorium,
        VisitorsElementarySchool,
        VisitorsHighSchool,
        VisitorsCollege,
        VisitorsUniversity,
        VisitorsEmergencyShelter,
        VisitorsPoliceStation,
        VisitorsPrison,

        StorageSelectDeselect,
        StorageCommercial,
        StorageIndustrial,
        StorageOffice,
        StorageBatteryCharge,
        StoragePowerPlantFuel,
        StorageHealthcare,
        StorageLandfill,
        StorageGarbageManagement,
        StorageEmergencyShelter,
        StorageCargoTransportation,
        StoragePost,

        VehiclesSelectDeselect,
        VehiclesInUseInMaintenance,
        VehiclesCommercialTruck,
        VehiclesIndustrialTruck,
        VehiclesOfficeTruck,
        VehiclesParked,
        VehiclesRoadMaintenance,
        VehiclesAmbulance,
        VehiclesMedicalHelicopter,
        VehiclesHearse,
        VehiclesGarbageTruck,
        VehiclesFireEngine,
        VehiclesFireHelicopter,
        VehiclesDisasterResponse,
        VehiclesEvacuationBus,
        VehiclesPoliceCar,
        VehiclesPoliceHelicopter,
        VehiclesPrisonVan,
        VehiclesBus,
        VehiclesTaxi,
        VehiclesTrain,
        VehiclesTram,
        VehiclesSubway,
        VehiclesParkMaintenance,
        VehiclesPost,
        VehiclesCargoStationTruck,

        EfficiencySelectDeselect,
        EfficiencyMaxColor,
        EfficiencyCommercial,
        EfficiencyIndustrial,
        EfficiencyOffice,
        EfficiencyParking,
        EfficiencyRoadMaintenance,
        EfficiencyElectricity,
        EfficiencyWater,
        EfficiencySewage,
        EfficiencyHealthcare,
        EfficiencyDeathcare,
        EfficiencyGarbageManagement,
        EfficiencyEducation,
        EfficiencyResearch,
        EfficiencyFireRescue,
        EfficiencyDisasterControl,
        EfficiencyPolice,
        EfficiencyAdministration,
        EfficiencyTransportation,
        EfficiencyParkMaintenance,
        EfficiencyParksRecreation,
        EfficiencyPost,
        EfficiencyTelecom,

        ProcessingSelectDeselect,
        ProcessingElectricityProduction,
        ProcessingWaterOutput,
        ProcessingSewageTreatment,
        ProcessingCrematoriumProcessing,
        ProcessingGarbageProcessing,
        ProcessingMailSortingSpeed,
    }

    /// <summary>
    /// Class to hold data for one of this mod's building status types.
    /// The "BU" (i.e. Building Use) prefix differentiates this class from any game objects.
    /// </summary>
    public class BUBuildingStatusTypeData
    {
        // The building status type this data is for.
        public BUBuildingStatusType buildingStatusType { get; set; }
        public string buildingStatusTypeName { get; set; }

        // Whether or not this is a special case.
        public bool isSpecialCase { get; set; }

        // Data bindings.
        private ValueBinding<double> bindingUsed;
        private ValueBinding<double> bindingCapacity;

        // Total city-wide data values.
        // City-wide data values are double because:
        //      An int max value is exceeded by some city-wide totals.
        //      A long is not supported by ValueBinding.
        //      A float does not have enough precision.
        private double used;
        private double capacity;

        // Lock for accessing data values.
        private readonly object _dataValuesLock = new object();

        // Can construct only with parameters.
        private BUBuildingStatusTypeData() { }

        /// <summary>
        /// Constructor for new instance.
        /// </summary>
        public BUBuildingStatusTypeData(BUBuildingStatusType type)
        {
            // Save building status type.
            buildingStatusType = type;
            buildingStatusTypeName = GetBuildingStatusTypeName(buildingStatusType);

            // Determine whether or not building status type is a special case.
            isSpecialCase =
                buildingStatusType == BUBuildingStatusType.None ||
                buildingStatusType == BUBuildingStatusType.VehiclesInUseInMaintenance ||
                buildingStatusType == BUBuildingStatusType.EfficiencyMaxColor ||
                buildingStatusTypeName.EndsWith("SelectDeselect");

            // Create data bindings, except for special cases.
            if (!isSpecialCase)
            {
                bindingUsed     = new ValueBinding<double>(ModAssemblyInfo.Name, buildingStatusType + "Used",     0d);
                bindingCapacity = new ValueBinding<double>(ModAssemblyInfo.Name, buildingStatusType + "Capacity", 0d);
            }
        }

        /// <summary>
        /// Convert building status type enum to building status type name.
        /// </summary>
        public static string GetBuildingStatusTypeName(BUBuildingStatusType buildingStatusType)
        {
            // Simply prefix the building status type with the mod name.
            return ModAssemblyInfo.Name + buildingStatusType.ToString();
        }

        /// <summary>
        /// Convert building status type name to building status type enum.
        /// </summary>
        public static BUBuildingStatusType GetBuildingStatusType(string buildingStatusTypeName)
        {
            // Name must start with the mod name.
            if (buildingStatusTypeName.StartsWith(ModAssemblyInfo.Name))
            {
                // Get the enum value from the name after the mod name prefix.
                string nameSuffix = buildingStatusTypeName.Substring(ModAssemblyInfo.Name.Length);
                if (Enum.TryParse(nameSuffix, out BUBuildingStatusType buildingStatusType))
                {
                    return buildingStatusType;
                }
            }

            // Name is not for a building status type in this mod.
            return BUBuildingStatusType.None;
        }

        /// <summary>
        /// Get the data bindings.
        /// </summary>
        public void GetDataBindings(out ValueBinding<double> bindingUsed, out ValueBinding<double> bindingCapacity)
        {
            bindingUsed     = this.bindingUsed;
            bindingCapacity = this.bindingCapacity;
        }

        /// <summary>
        /// Update data bindings with data values.
        /// </summary>
        public void UpdateDataBindings()
        {
            // Do not update special cases.
            if (!isSpecialCase)
            {
                // Allow only one thread at a time to access data values.
                lock (_dataValuesLock)
                {
                    bindingUsed    .Update(used);
                    bindingCapacity.Update(capacity);
                }
            }
        }

        /// <summary>
        /// Update data values.
        /// </summary>
        public void UpdateDataValues(double used, double capacity)
        {
            // Allow only one thread at a time to access data values.
            lock (_dataValuesLock)
            {
                this.used     = used;
                this.capacity = capacity;
            }
        }

        /// <summary>
        /// Reset data values.
        /// </summary>
        public void ResetDataValues()
        {
            // Allow only one thread at a time to access data values.
            lock (_dataValuesLock)
            {
                used     = 0d;
                capacity = 0d;
            }
        }
    }
}
