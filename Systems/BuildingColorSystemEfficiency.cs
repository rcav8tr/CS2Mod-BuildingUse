using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Objects;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BuildingUse
{
    /// <summary>
    /// Partial system to set building colors for Employees infoview.
    /// </summary>
    public partial class BuildingColorSystem : GameSystemBase
    {
        /// <summary>
        /// Partial job struct to set the color of each main building for Efficiency infoview.
        /// Burst compilation for this partial struct is defined by the main UpdateColorsJobMainBuilding struct.
        /// </summary>
        private partial struct UpdateColorsJobMainBuilding : IJobChunk
        {
            /// <summary>
            /// Do a main building for Efficiency infoview.
            /// </summary>
            private void DoBuildingEfficiency(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Do building status types for non-residential first.
                DoBuildingEfficiencyNonResidential(in mainBuildingAndUpgrades, ref color);

                // Do building status types for residential last.
                DoBuildingEfficiencyResidential(in mainBuildingAndUpgrades, ref color);
            }

            /// <summary>
            /// Do a main building and upgrades for Efficiency infoview for a residential building.
            /// </summary>
            private void DoBuildingEfficiencyResidential(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Get total happiness and citizen count from main building and upgrades.
                int totalHappiness = 0;
                int citizenCount   = 0;
                bool hasResidential = false;
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    hasResidential |= DoBuildingEfficiencyResidentialDetail(mainBuildingOrUpgrade.Entity, ref totalHappiness, ref citizenCount);
                }

                // Main building plus upgrades must have residential.
                if (hasResidential)
                {
                    // Compute average happiness percent.
                    // Citizen happiness neutral is 50.
                    // Convert citizen happiness to percent where 100% is neutral.
                    int happinessPercent = 0;
                    if (citizenCount > 0)
                    {
                        happinessPercent = (int)math.round(2f * totalHappiness / citizenCount);
                    }

                    // Update entity color according to the efficiency max color setting.
                    UpdateEntityColor(BUBuildingStatusType.EfficiencyResidential, happinessPercent, (EfficiencyMaxColor200Percent ? 200L : 100L), ref color);

                    // Update total used and capacity.
                    // Capacity is always 100% even if entity color is based on 200%.
                    UpdateTotalUsedCapacity(BUBuildingStatusType.EfficiencyResidential, happinessPercent, 100L);
                }
            }

            /// <summary>
            /// Do a main building or upgrade for Efficiency infoview for a residential building to get details.
            /// </summary>
            private bool DoBuildingEfficiencyResidentialDetail(Entity entity, ref int totalHappiness, ref int citizenCount)
            {
                // Building must have residential and Renter buffer.
                if (BuildingHasResidential(entity) &&
                    BufferLookupRenter.TryGetBuffer(entity, out DynamicBuffer<Renter> renters) &&
                    renters.IsCreated)
                {
                    // Do each renter (household).
                    for (int i = 0; i < renters.Length; i++)
                    {
                        // Get citizens in the renter (household), if any.
                        if (BufferLookupHouseholdCitizen.TryGetBuffer(renters[i].m_Renter, out DynamicBuffer<HouseholdCitizen> householdCitizens) &&
                            householdCitizens.IsCreated)
                        {
                            // Do each citizen.
                            for (int j = 0; j < householdCitizens.Length; j++)
                            {
                                // Citizen component must exist on the citizen and citizen must not be dead.
                                Entity citizenEntity = householdCitizens[j].m_Citizen;
                                if (ComponentLookupCitizen.TryGetComponent(citizenEntity, out Citizen citizen) &&
                                    !CitizenUtils.IsDead(citizenEntity, ref ComponentLookupHealthProblem))
                                {
                                    totalHappiness += citizen.Happiness;
                                    citizenCount++;
                                }
                            }
                        }
                    }

                    // Building has residential, even if renters buffer is empty.
                    return true;
                }

                // Building does not have residential.
                return false;
            }

            /// <summary>
            /// Do a main building and upgrades for Efficiency infoview for a non-residential building.
            /// </summary>
            private void DoBuildingEfficiencyNonResidential(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Main building is always the first entry.
                Entity mainBuildingEntity = mainBuildingAndUpgrades[0].Entity;

                // Main building must have an Efficiency buffer.
                if (BufferLookupEfficiency.TryGetBuffer(mainBuildingEntity, out DynamicBuffer<Efficiency> bufferEfficiency) &&
                    bufferEfficiency.IsCreated)
                {
                    // Main building has an Efficiency buffer.

                    // Used is efficiency percent from the buffer.
                    // Start with 100% as the default efficiency.
                    // A building with no efficiency buffer entries will have the default efficiency of 100%, like in the game.
                    float tempEfficiency = 1f;
                    foreach (Efficiency efficiency in bufferEfficiency)
                    {
                        // Exclude negative efficiencies.
                        // Note that the efficiency entry for Disabled has an efficiency value of zero.
                        // So disabled buildings will still be included, but will have 0% efficiency, like in the game.
                        if (efficiency.m_Efficiency >= 0f)
                        {
                            // Efficiency is multiplicative.
                            tempEfficiency *= efficiency.m_Efficiency;
                        }
                    }
                    long mainBuildingEfficiency = (int)math.round(100f * tempEfficiency);

                    // Get building status types for the main building and upgrades.
                    NativeList<BUBuildingStatusType> buildingStatusTypes = new(4, Allocator.Temp);
                    foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                    {
                        DoBuildingEfficiencyNonResidentialDetail(mainBuildingOrUpgrade.Entity, mainBuildingOrUpgrade.Prefab, buildingStatusTypes);
                    }

                    // Use nested loops to check for duplicate building status types.
                    for (int i = 0; i < buildingStatusTypes.Length - 1; i++)
                    {
                        for (int j = i + 1; j < buildingStatusTypes.Length; j++)
                        {
                            if (buildingStatusTypes[i] == buildingStatusTypes[j])
                            {
                                // Remove the duplicate.
                                buildingStatusTypes.RemoveAtSwapBack(j);

                                // Check the entry that was swapped in.
                                j--;
                            }
                        }
                    }

                    // Use nested loops to sort building status types in descending order.
                    for (int i = 0; i < buildingStatusTypes.Length - 1; i++)
                    {
                        for (int j = i + 1; j < buildingStatusTypes.Length; j++)
                        {
                            if (buildingStatusTypes[i] < buildingStatusTypes[j])
                            {
                                (buildingStatusTypes[i], buildingStatusTypes[j]) = (buildingStatusTypes[j], buildingStatusTypes[i]);
                            }
                        }
                    }

                    // Get whether or not main building has a company.
                    // Only the main building is checked for company, not upgrades.
                    bool mainBuildingHasCompany = TryGetCompany(mainBuildingEntity, out Entity _);

                    // Do each building status type.
                    foreach (BUBuildingStatusType buildingStatusType in buildingStatusTypes)
                    {
                        // Check for commercial, industrial, or office with no company.
                        long efficiency;
                        if ((buildingStatusType == BUBuildingStatusType.EfficiencyCommercial ||
                             buildingStatusType == BUBuildingStatusType.EfficiencyIndustrial ||
                             buildingStatusType == BUBuildingStatusType.EfficiencyOffice) &&
                            !mainBuildingHasCompany)
                        {
                            // Use 0% for efficiency.
                            efficiency = 0L;
                        }
                        else
                        {
                            // Use main building efficiency.
                            efficiency = mainBuildingEfficiency;
                        }

                        // Update entity color according to the efficiency max color setting.
                        UpdateEntityColor(buildingStatusType, efficiency, (EfficiencyMaxColor200Percent ? 200L : 100L), ref color);

                        // Update total used and capacity.
                        // Capacity is always 100% even if entity color is based on 200%.
                        UpdateTotalUsedCapacity(buildingStatusType, efficiency, 100L);
                    }
                }
            }

            /// <summary>
            /// Do a main building or upgrade for Efficiency infoview for a non-residential building to get details.
            /// </summary>
            private void DoBuildingEfficiencyNonResidentialDetail(
                Entity entity,
                Entity prefab,
                NativeList<BUBuildingStatusType> buildingStatusTypes)
            {
                // Get building status types for zoned non-residential.
                if (BuildingHasCommercial           (entity))           { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyCommercial       ); }
                if (BuildingHasIndustrial           (entity))           { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyIndustrial       ); }
                if (BuildingHasOffice               (entity))           { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyOffice           ); }

                // Get all building status types based on the services that apply to this building.
                // These are exactly the same as the Employees infoview.
                if (BuildingHasParkingFacility    (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyParking          ); }
                if (BuildingHasRoadMaintenance    (prefab, entity)) { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyRoadMaintenance  ); }
                if (BuildingHasElectricity        (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyElectricity      ); }
                if (BuildingHasWaterPumpingStation(prefab, entity)) { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyWater            ); }
                if (BuildingHasSewageOutlet       (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencySewage           ); }
                if (BuildingHasHospital           (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyHealthcare       ); }
                if (BuildingHasDeathcareFacility  (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyDeathcare        ); }
                if (BuildingHasGarbageFacility    (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyGarbageManagement); }
                if (BuildingHasSchool             (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyEducation        ); }
                if (BuildingHasResearchFacility   (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyResearch         ); }
                if (BuildingHasFireRescue         (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyFireRescue       ); }
                if (BuildingHasDisasterControl    (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyDisasterControl  ); }
                if (BuildingHasPolice             (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyPolice           ); }
                if (BuildingHasAdministration     (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyAdministration   ); }
                if (BuildingHasTransportation     (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyTransportation   ); }
                if (BuildingHasParkMaintenance    (prefab, entity)) { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyParkMaintenance  ); }
                if (BuildingHasPark               (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyParksRecreation  ); }
                if (BuildingHasPostFacility       (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyPost             ); }
                if (BuildingHasTelecomFacility    (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EfficiencyTelecom          ); }
            }
        }
    }
}
