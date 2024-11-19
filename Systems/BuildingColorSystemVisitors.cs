using Unity.Collections;
using Unity.Entities;

namespace BuildingUse
{
    /// <summary>
    /// Partial system to set building colors for Visitors infoview.
    /// </summary>
    public partial class BuildingColorSystem : Game.GameSystemBase
    {
        /// <summary>
        /// Partial job struct to set the color of each main building for Visitors infoview.
        /// Burst compilation for this partial struct is defined by the main UpdateColorsJobMainBuilding struct.
        /// </summary>
        private partial struct UpdateColorsJobMainBuilding : IJobChunk
        {
            /// <summary>
            /// Get building status types applicable to the building chunk for Visitors infoview.
            /// </summary>
            private void GetApplicableBuildingStatusTypesVisitors(ArchetypeChunk buildingChunk, ref NativeList<BUBuildingStatusType> applicableBuildingStatusTypes)
            {
                // Add all building status types that apply to this building chunk.
                if (buildingChunk.Has(ref ComponentTypeHandleHospital))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VisitorsHealthcare);
                if (buildingChunk.Has(ref ComponentTypeHandleDeathcareFacility))
                {
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VisitorsCemetery);
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VisitorsCrematorium);
                }
                if (buildingChunk.Has(ref ComponentTypeHandleSchool))
                {
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VisitorsElementarySchool);
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VisitorsHighSchool);
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VisitorsCollege);
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VisitorsUniversity);
                }
                if (buildingChunk.Has(ref ComponentTypeHandleEmergencyShelter))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VisitorsEmergencyShelter);
                if (buildingChunk.Has(ref ComponentTypeHandlePoliceStation))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VisitorsPoliceStation);
                if (buildingChunk.Has(ref ComponentTypeHandlePrison))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VisitorsPrison);
            }

            /// <summary>
            /// Set building colors for Visitors infoview.
            /// </summary>
            private void SetBuildingColorsVisitors
            (
                ArchetypeChunk buildingChunk,
                bool infomodeActive,
                int infomodeIndex,
                NativeArray<Game.Objects.Color> colors,
                BUBuildingStatusType buildingStatusType
            )
            {
                // Check which building status type.
                switch (buildingStatusType)
                {
                    case BUBuildingStatusType.VisitorsHealthcare:
                        SetBuildingColorsVisitorsHealthcare(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType);
                        break;

                    case BUBuildingStatusType.VisitorsCemetery:
                        SetBuildingColorsVisitorsDeathcare(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType, true);
                        break;
                    case BUBuildingStatusType.VisitorsCrematorium:
                        SetBuildingColorsVisitorsDeathcare(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType, false);
                        break;

                    case BUBuildingStatusType.VisitorsElementarySchool:
                        SetBuildingColorsVisitorsEducation(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType, 1);
                        break;
                    case BUBuildingStatusType.VisitorsHighSchool:
                        SetBuildingColorsVisitorsEducation(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType, 2);
                        break;
                    case BUBuildingStatusType.VisitorsCollege:
                        SetBuildingColorsVisitorsEducation(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType, 3);
                        break;
                    case BUBuildingStatusType.VisitorsUniversity:
                        SetBuildingColorsVisitorsEducation(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType, 4);
                        break;

                    case BUBuildingStatusType.VisitorsEmergencyShelter:
                        SetBuildingColorsVisitorsEmergencyShelter(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType);
                        break;

                    case BUBuildingStatusType.VisitorsPoliceStation:
                        SetBuildingColorsVisitorsPoliceStation(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType);
                        break;

                    case BUBuildingStatusType.VisitorsPrison:
                        SetBuildingColorsVisitorsPrison(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType);
                        break;

                    default:
                        break;
                }
            }
            
            /// <summary>
            /// Set building colors for Visitors infoview for healthcare.
            /// Logic adapted from HealthcareInfoviewUISystem.UpdateHealthcareJob.
            /// </summary>
            private void SetBuildingColorsVisitorsHealthcare
            (
                ArchetypeChunk buildingChunk,
                bool infomodeActive,
                int infomodeIndex,
                NativeArray<Game.Objects.Color> colors,
                BUBuildingStatusType buildingStatusType
            )
            {
                // Initialize total used and capacity.
                long totalUsed     = 0L;
                long totalCapacity = 0L;

                // Do each entity (i.e. building).
                NativeArray<Entity                > entities   = buildingChunk.GetNativeArray(EntityTypeHandle);
                NativeArray<Game.Prefabs.PrefabRef> prefabRefs = buildingChunk.GetNativeArray(ref ComponentTypeHandlePrefabRef);
                for (int i = 0; i < entities.Length; i++)
                {
                    // Get component data with upgrades.
                    if (TryGetComponentDataWithUpgrades(entities[i], prefabRefs[i].m_Prefab, ref ComponentLookupHospitalData, out Game.Prefabs.HospitalData hospitalData))
                    {
                        // Must have capacity.
                        long capacity = hospitalData.m_PatientCapacity;
                        if (capacity > 0)
                        {
                            // Get used from dynamic buffer length.
                            long used = GetDynamicBufferLength(entities[i], ref BufferLookupPatient);

                            // Update entity color and accumulate totals.
                            UpdateEntityColor(used, capacity, infomodeActive, infomodeIndex, colors, i);
                            totalUsed     += used;
                            totalCapacity += capacity;
                        }
                    }
                }

                // Update total used and capacity data arrays.
                UpdateTotalUsedCapacity(buildingStatusType, totalUsed, totalCapacity);
            }
            
            /// <summary>
            /// Set building colors for Visitors infoview for deathcare (handles both cemetery and crematorium).
            /// Logic adapted from HealthcareInfoviewUISystem.UpdateDeathcareJob.
            /// </summary>
            private void SetBuildingColorsVisitorsDeathcare
            (
                ArchetypeChunk buildingChunk,
                bool infomodeActive,
                int infomodeIndex,
                NativeArray<Game.Objects.Color> colors,
                BUBuildingStatusType buildingStatusType,
                bool longTermStorage
            )
            {
                // Initialize total used and capacity.
                long totalUsed     = 0L;
                long totalCapacity = 0L;

                // Get deathcare facilities.
			    NativeArray<Game.Buildings.DeathcareFacility> deathcareFacilities = buildingChunk.GetNativeArray(ref ComponentTypeHandleDeathcareFacility);

                // Do each entity (i.e. building).
                NativeArray<Entity                > entities   = buildingChunk.GetNativeArray(EntityTypeHandle);
                NativeArray<Game.Prefabs.PrefabRef> prefabRefs = buildingChunk.GetNativeArray(ref ComponentTypeHandlePrefabRef);
                for (int i = 0; i < entities.Length; i++)
                {
                    // Get component data with upgrades.
                    if (TryGetComponentDataWithUpgrades(entities[i], prefabRefs[i].m_Prefab, ref ComponentLookupDeathcareFacilityData, out Game.Prefabs.DeathcareFacilityData deathcareFacilityData))
                    {
                        // Check for cemetery vs crematorium.
                        // Only cemetery has long term storage.
                        if (deathcareFacilityData.m_LongTermStorage == longTermStorage)
                        {
                            // Used is long term stored count, for both cemetery and crematorium.
                            long used = deathcareFacilities[i].m_LongTermStoredCount;

                            // Get capacity.
                            long capacity = deathcareFacilityData.m_StorageCapacity;
                        
                            // Update entity color and accumulate totals.
                            UpdateEntityColor(used, capacity, infomodeActive, infomodeIndex, colors, i);
                            totalUsed     += used;
                            totalCapacity += capacity;
                        }
                    }
                }

                // Update total used and capacity data arrays.
                UpdateTotalUsedCapacity(buildingStatusType, totalUsed, totalCapacity);
            }
            
            /// <summary>
            /// Set building colors for Visitors infoview for education (handles all four education levels).
            /// Logic adapted from EducationInfoviewUISystem.UpdateStudentCountsJob.
            /// </summary>
            private void SetBuildingColorsVisitorsEducation
            (
                ArchetypeChunk buildingChunk,
                bool infomodeActive,
                int infomodeIndex,
                NativeArray<Game.Objects.Color> colors,
                BUBuildingStatusType buildingStatusType,
                byte educationLevel
            )
            {
                // Initialize total used and capacity.
                long totalUsed     = 0L;
                long totalCapacity = 0L;

                // Do each entity (i.e. building).
                NativeArray<Entity                > entities   = buildingChunk.GetNativeArray(EntityTypeHandle);
                NativeArray<Game.Prefabs.PrefabRef> prefabRefs = buildingChunk.GetNativeArray(ref ComponentTypeHandlePrefabRef);
                for (int i = 0; i < entities.Length; i++)
                {
                    // Get component data with upgrades.
                    if (TryGetComponentDataWithUpgrades(entities[i], prefabRefs[i].m_Prefab, ref ComponentLookupSchoolData, out Game.Prefabs.SchoolData schoolData))
                    {
                        // Check education level.
                        if (schoolData.m_EducationLevel == educationLevel)
                        {
                            // Get used from dynamic buffer length.
                            long used = GetDynamicBufferLength(entities[i], ref BufferLookupStudent);

                            // Get capacity.
                            long capacity = schoolData.m_StudentCapacity;

                            // Update entity color and accumulate totals.
                            UpdateEntityColor(used, capacity, infomodeActive, infomodeIndex, colors, i);
                            totalUsed     += used;
                            totalCapacity += capacity;
                        }
                    }
                }

                // Update total used and capacity data arrays.
                UpdateTotalUsedCapacity(buildingStatusType, totalUsed, totalCapacity);
            }
            
            /// <summary>
            /// Set building colors for Visitors infoview for emergency shelter.
            /// Logic adapted from Game.UI.InGame.ShelterSection.
            /// </summary>
            private void SetBuildingColorsVisitorsEmergencyShelter
            (
                ArchetypeChunk buildingChunk,
                bool infomodeActive,
                int infomodeIndex,
                NativeArray<Game.Objects.Color> colors,
                BUBuildingStatusType buildingStatusType
            )
            {
                // Initialize total used and capacity.
                long totalUsed     = 0L;
                long totalCapacity = 0L;

                // Do each entity (i.e. building).
                NativeArray<Entity                > entities   = buildingChunk.GetNativeArray(EntityTypeHandle);
                NativeArray<Game.Prefabs.PrefabRef> prefabRefs = buildingChunk.GetNativeArray(ref ComponentTypeHandlePrefabRef);
                for (int i = 0; i < entities.Length; i++)
                {
                    // Get component data with upgrades.
                    if (TryGetComponentDataWithUpgrades(entities[i], prefabRefs[i].m_Prefab, ref ComponentLookupEmergencyShelterData, out Game.Prefabs.EmergencyShelterData emergencyShelterData))
                    {
                        // Get used from dynamic buffer length.
                        long used = GetDynamicBufferLength(entities[i], ref BufferLookupOccupant);

                        // Get capacity
                        long capacity = emergencyShelterData.m_ShelterCapacity;

                        // Update entity color and accumulate totals.
                        UpdateEntityColor(used, capacity, infomodeActive, infomodeIndex, colors, i);
                        totalUsed     += used;
                        totalCapacity += capacity;
                    }
                }

                // Update total used and capacity data arrays.
                UpdateTotalUsedCapacity(buildingStatusType, totalUsed, totalCapacity);
            }
            
            /// <summary>
            /// Set building colors for Visitors infoview for police station.
            /// Logic adapted from Game.UI.InGame.PoliceSection.
            /// </summary>
            private void SetBuildingColorsVisitorsPoliceStation
            (
                ArchetypeChunk buildingChunk,
                bool infomodeActive,
                int infomodeIndex,
                NativeArray<Game.Objects.Color> colors,
                BUBuildingStatusType buildingStatusType
            )
            {
                // Initialize total used and capacity.
                long totalUsed     = 0L;
                long totalCapacity = 0L;

                // Do each entity (i.e. building).
                NativeArray<Entity                > entities   = buildingChunk.GetNativeArray(EntityTypeHandle);
                NativeArray<Game.Prefabs.PrefabRef> prefabRefs = buildingChunk.GetNativeArray(ref ComponentTypeHandlePrefabRef);
                for (int i = 0; i < entities.Length; i++)
                {
                    // Get component data with upgrades.
                    if (TryGetComponentDataWithUpgrades(entities[i], prefabRefs[i].m_Prefab, ref ComponentLookupPoliceStationData, out Game.Prefabs.PoliceStationData policeStationData))
                    {
                        // Get used from dynamic buffer length.
                        long used = GetDynamicBufferLength(entities[i], ref BufferLookupOccupant);

                        // Get capacity.
                        long capacity = policeStationData.m_JailCapacity;
                    
                        // Update entity color and accumulate totals.
                        UpdateEntityColor(used, capacity, infomodeActive, infomodeIndex, colors, i);
                        totalUsed     += used;
                        totalCapacity += capacity;
                    }
                }

                // Update total used and capacity data arrays.
                UpdateTotalUsedCapacity(buildingStatusType, totalUsed, totalCapacity);
            }
            
            /// <summary>
            /// Set building colors for Visitors infoview for prison.
            /// Logic adapted from Game.UI.InGame.PrisonSection.
            /// </summary>
            private void SetBuildingColorsVisitorsPrison
            (
                ArchetypeChunk buildingChunk,
                bool infomodeActive,
                int infomodeIndex,
                NativeArray<Game.Objects.Color> colors,
                BUBuildingStatusType buildingStatusType
            )
            {
                // Initialize total used and capacity.
                long totalUsed     = 0L;
                long totalCapacity = 0L;

                // Do each entity (i.e. building).
                NativeArray<Entity                > entities   = buildingChunk.GetNativeArray(EntityTypeHandle);
                NativeArray<Game.Prefabs.PrefabRef> prefabRefs = buildingChunk.GetNativeArray(ref ComponentTypeHandlePrefabRef);
                for (int i = 0; i < entities.Length; i++)
                {
                    // Get component data with upgrades.
                    if (TryGetComponentDataWithUpgrades(entities[i], prefabRefs[i].m_Prefab, ref ComponentLookupPrisonData, out Game.Prefabs.PrisonData prisonData))
                    {
                        // Get used from dynamic buffer length.
                        long used = GetDynamicBufferLength(entities[i], ref BufferLookupOccupant);

                        // Get capacity.
                        long capacity = prisonData.m_PrisonerCapacity;

                        // Update entity color and accumulate totals.
                        UpdateEntityColor(used, capacity, infomodeActive, infomodeIndex, colors, i);
                        totalUsed     += used;
                        totalCapacity += capacity;
                    }
                }

                // Update total used and capacity data arrays.
                UpdateTotalUsedCapacity(buildingStatusType, totalUsed, totalCapacity);
            }
        }
    }
}
