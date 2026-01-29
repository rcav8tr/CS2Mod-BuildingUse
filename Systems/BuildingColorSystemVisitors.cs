using Game;
using Game.Objects;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace BuildingUse
{
    /// <summary>
    /// Partial system to set building colors for Visitors infoview.
    /// </summary>
    public partial class BuildingColorSystem : GameSystemBase
    {
        /// <summary>
        /// Partial job struct to set the color of each main building for Visitors infoview.
        /// Burst compilation for this partial struct is defined by the main UpdateColorsJobMainBuilding struct.
        /// </summary>
        private partial struct UpdateColorsJobMainBuilding : IJobChunk
        {
            /// <summary>
            /// Do a main building for Visitors infoview.
            /// </summary>
            private void DoBuildingVisitors(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Do each Visitors building status type in descending order.
                DoBuildingVisitorsPrison          (in mainBuildingAndUpgrades, ref color);
                DoBuildingVisitorsPoliceStation   (in mainBuildingAndUpgrades, ref color);
                DoBuildingVisitorsEmergencyShelter(in mainBuildingAndUpgrades, ref color);
                DoBuildingVisitorsEducation       (in mainBuildingAndUpgrades, ref color, SchoolLevel.University);
                DoBuildingVisitorsEducation       (in mainBuildingAndUpgrades, ref color, SchoolLevel.College);
                DoBuildingVisitorsEducation       (in mainBuildingAndUpgrades, ref color, SchoolLevel.HighSchool);
                DoBuildingVisitorsEducation       (in mainBuildingAndUpgrades, ref color, SchoolLevel.Elementary);
                DoBuildingVisitorsDeathcare       (in mainBuildingAndUpgrades, ref color, false);
                DoBuildingVisitorsDeathcare       (in mainBuildingAndUpgrades, ref color, true);
                DoBuildingVisitorsHealthcare      (in mainBuildingAndUpgrades, ref color);
            }

            /// <summary>
            /// Do a main building and upgrades for Visitors infoview for Healthcare.
            /// </summary>
            private void DoBuildingVisitorsHealthcare(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Get used and capacity from main building and upgrades.
                int used     = 0;
                int capacity = 0;
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Logic adapted from Game.UI.InGame.HealthcareSection .OnUpdate() and .OnProcess().
                    // Building (main or upgrade) must have hospital.
                    if (BuildingHasHospital(mainBuildingOrUpgrade.Prefab, out HospitalData hospitalData))
                    {
                        // Accumulate used and capacity.
                        used     += GetDynamicBufferLength(mainBuildingOrUpgrade.Entity, in BufferLookupPatient);
                        capacity += hospitalData.m_PatientCapacity;
                    }
                }

                // If main building plus upgrades has capacity, update entity color and total used and capacity.
                if (capacity > 0)
                {
                    UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VisitorsHealthcare, used, capacity, ref color);
                }
            }

            /// <summary>
            /// Do a main building and upgrades for Visitors infoview for Deathcare.
            /// </summary>
            private void DoBuildingVisitorsDeathcare(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color, bool longTermStorage)
            {
                // Get used and capacity from main building and upgrades.
                int used     = 0;
                int capacity = 0;
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Logic adapted from Game.UI.InGame.DeathcareSection.OnProcess().
                    // Building (main or upgrade) must have deathcare.
                    if (BuildingHasDeathcareFacility(mainBuildingOrUpgrade.Prefab, out DeathcareFacilityData deathcareFacilityData))
                    {
                        // Check for cemetery vs crematorium.
                        // Only cemetery has long term storage.
                        if (deathcareFacilityData.m_LongTermStorage == longTermStorage)
                        {
                            // Accumulate used.
                            // For both cemetery and crematorium, used is patient count plus long term stored count.
                            used += GetDynamicBufferLength(mainBuildingOrUpgrade.Entity, in BufferLookupPatient);
                            if (ComponentLookupDeathcareFacility.TryGetComponent(mainBuildingOrUpgrade.Entity, out Game.Buildings.DeathcareFacility deathcareFacility))
                            {
                                used += deathcareFacility.m_LongTermStoredCount;
                            }

                            // Accumulate capacity.
                            capacity += deathcareFacilityData.m_StorageCapacity;
                        }
                    }
                }

                // If main building plus upgrades has capacity, update entity color and total used and capacity.
                if (capacity > 0)
                {
                    BUBuildingStatusType buildingStatusType =
                        longTermStorage ? BUBuildingStatusType.VisitorsCemetery : BUBuildingStatusType.VisitorsCrematorium;
                    UpdateEntityColorAndTotalUsedCapacity(buildingStatusType, used, capacity, ref color);
                }
            }

            /// <summary>
            /// Do a main building and upgrades for Visitors infoview for Education.
            /// </summary>
            private void DoBuildingVisitorsEducation(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color, SchoolLevel schoolLevel)
            {
                // Get used and capacity from main building and upgrades.
                int used     = 0;
                int capacity = 0;
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Logic adapted from Game.UI.InGame.EducationSection.OnProcess().
                    // Building (main or upgrade) must have school.
                    if (BuildingHasSchool(mainBuildingOrUpgrade.Prefab, out SchoolData schoolData))
                    {
                        // School must be correct level.
                        if ((SchoolLevel)schoolData.m_EducationLevel == schoolLevel)
                        {
                            // Accumulate used and capacity.
                            used     += GetDynamicBufferLength(mainBuildingOrUpgrade.Entity, in BufferLookupStudent);
                            capacity += schoolData.m_StudentCapacity;
                        }
                    }
                }

                // If main building plus upgrades has capacity, update entity color and total used and capacity.
                if (capacity > 0)
                {
                    BUBuildingStatusType buildingStatusType = BUBuildingStatusType.None;
                    if      (schoolLevel == SchoolLevel.Elementary) { buildingStatusType = BUBuildingStatusType.VisitorsElementarySchool; }
                    else if (schoolLevel == SchoolLevel.HighSchool) { buildingStatusType = BUBuildingStatusType.VisitorsHighSchool;       }
                    else if (schoolLevel == SchoolLevel.College   ) { buildingStatusType = BUBuildingStatusType.VisitorsCollege;          }
                    else if (schoolLevel == SchoolLevel.University) { buildingStatusType = BUBuildingStatusType.VisitorsUniversity;       }
                    UpdateEntityColorAndTotalUsedCapacity(buildingStatusType, used, capacity, ref color);
                }
            }

            /// <summary>
            /// Do a main building and upgrades for Visitors infoview for Emergency Shelter.
            /// </summary>
            private void DoBuildingVisitorsEmergencyShelter(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Get used and capacity from main building and upgrades.
                int used     = 0;
                int capacity = 0;
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Logic adapted from Game.UI.InGame.ShelterSection.OnProcess().
                    // Building (main or upgrade) must have emergency shelter.
                    if (BuildingHasEmergencyShelter(mainBuildingOrUpgrade.Prefab, out EmergencyShelterData emergencyShelterData))
                    {
                        // Accumulate used and capacity.
                        used     += GetDynamicBufferLength(mainBuildingOrUpgrade.Entity, in BufferLookupOccupant);
                        capacity += emergencyShelterData.m_ShelterCapacity;
                    }
                }

                // If main building plus upgrades has capacity, update entity color and total used and capacity.
                if (capacity > 0)
                {
                    UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VisitorsEmergencyShelter, used, capacity, ref color);
                }
            }

            /// <summary>
            /// Do a main building and upgrades for Visitors infoview for Police Station.
            /// </summary>
            private void DoBuildingVisitorsPoliceStation(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Get used and capacity from main building and upgrades.
                int used     = 0;
                int capacity = 0;
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Logic adapted from Game.UI.InGame.PoliceSection.OnProcess().
                    // Building (main or upgrade) must have police station.
                    if (BuildingHasPoliceStation(mainBuildingOrUpgrade.Prefab, out PoliceStationData policeStationData))
                    {
                        // Accumulate used and capacity.
                        used     += GetDynamicBufferLength(mainBuildingOrUpgrade.Entity, in BufferLookupOccupant);
                        capacity += policeStationData.m_JailCapacity;
                    }
                }

                // If main building plus upgrades has capacity, update entity color and total used and capacity.
                if (capacity > 0)
                {
                    UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VisitorsPoliceStation, used, capacity, ref color);
                }
            }

            /// <summary>
            /// Do a main building and upgrades for Visitors infoview for Prison.
            /// </summary>
            private void DoBuildingVisitorsPrison(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Get used and capacity from main building and upgrades.
                int used     = 0;
                int capacity = 0;
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Logic adapted from Game.UI.InGame.PrisonSection.OnProcess().
                    // Building (main or upgrade) must have prison.
                    if (BuildingHasPrison(mainBuildingOrUpgrade.Prefab, out PrisonData prisonData))
                    {
                        // Accumulate used and capacity.
                        used     += GetDynamicBufferLength(mainBuildingOrUpgrade.Entity, in BufferLookupOccupant);
                        capacity += prisonData.m_PrisonerCapacity;
                    }
                }

                // If main building plus upgradeshas capacity, update entity color and total used and capacity.
                if (capacity > 0)
                {
                    UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VisitorsPrison, used, capacity, ref color);
                }
            }
        }
    }
}
