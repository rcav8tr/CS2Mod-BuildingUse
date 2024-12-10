using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BuildingUse
{
    /// <summary>
    /// Partial system to set building colors for Processing infoview.
    /// </summary>
    public partial class BuildingColorSystem : Game.GameSystemBase
    {
        /// <summary>
        /// Partial job struct to set the color of each main building for Processing infoview.
        /// Burst compilation for this partial struct is defined by the main UpdateColorsJobMainBuilding struct.
        /// </summary>
        private partial struct UpdateColorsJobMainBuilding : IJobChunk
        {
            /// <summary>
            /// Get building status types applicable to the building chunk for Processing infoview.
            /// </summary>
            private void GetApplicableBuildingStatusTypesProcessing(ArchetypeChunk buildingChunk, ref NativeList<BUBuildingStatusType> applicableBuildingStatusTypes)
            {
                // Add all building status types that apply to this building chunk.
                if (buildingChunk.Has(ref ComponentTypeHandleElectricityProducer))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.ProcessingElectricityProduction);
                if (buildingChunk.Has(ref ComponentTypeHandleWaterPumpingStation))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.ProcessingWaterOutput);
                if (buildingChunk.Has(ref ComponentTypeHandleSewageOutlet))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.ProcessingSewageTreatment);
                if (buildingChunk.Has(ref ComponentTypeHandleDeathcareFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.ProcessingCrematoriumProcessing);
                if (buildingChunk.Has(ref ComponentTypeHandleGarbageFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.ProcessingGarbageProcessing);
                if (buildingChunk.Has(ref ComponentTypeHandlePostFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.ProcessingMailSortingSpeed);
            }

            /// <summary>
            /// Set building colors for Visitors infoview.
            /// </summary>
            private void SetBuildingColorsProcessing
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
                    case BUBuildingStatusType.ProcessingElectricityProduction:
                        SetBuildingColorsProcessingElectricityProduction(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType);
                        break;

                    case BUBuildingStatusType.ProcessingWaterOutput:
                        SetBuildingColorsProcessingWaterOutput(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType);
                        break;

                    case BUBuildingStatusType.ProcessingSewageTreatment:
                        SetBuildingColorsProcessingSewageTreatment(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType);
                        break;

                    case BUBuildingStatusType.ProcessingCrematoriumProcessing:
                        SetBuildingColorsProcessingCrematoriumProcessing(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType);
                        break;

                    case BUBuildingStatusType.ProcessingGarbageProcessing:
                        SetBuildingColorsProcessingGarbageProcessing(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType);
                        break;

                    case BUBuildingStatusType.ProcessingMailSortingSpeed:
                        SetBuildingColorsProcessingMailSortingSpeed(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType);
                        break;

                    default:
                        break;
                }
            }

            /// <summary>
            /// Set building colors for Processing infoview for electricity production
            /// </summary>
            private void SetBuildingColorsProcessingElectricityProduction
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

                // Do each electricity producer (i.e. building).
                NativeArray<Game.Areas.CurrentDistrict        > districts            = buildingChunk.GetNativeArray(ref ComponentTypeHandleCurrentDistrict);
                NativeArray<Game.Buildings.ElectricityProducer> electricityProducers = buildingChunk.GetNativeArray(ref ComponentTypeHandleElectricityProducer);
                for (int i = 0; i < electricityProducers.Length; i++)
                {
                    // Building must be in selected district.
                    if (BuildingInSelectedDistrict(districts[i].m_District))
                    {
                        // Logic adapted from Game.UI.InGame.ElectricitySection.
                        long capacity = electricityProducers[i].m_Capacity;
                        long used     = electricityProducers[i].m_LastProduction;

                        // Building must have capacity.
                        if (capacity > 0L)
                        {
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
            /// Set building colors for Processing infoview for water output.
            /// </summary>
            private void SetBuildingColorsProcessingWaterOutput
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

                // Do each water pumping station (i.e. building).
                NativeArray<Game.Areas.CurrentDistrict        > districts            = buildingChunk.GetNativeArray(ref ComponentTypeHandleCurrentDistrict);
                NativeArray<Game.Buildings.WaterPumpingStation> waterPumpingStations = buildingChunk.GetNativeArray(ref ComponentTypeHandleWaterPumpingStation);
                for (int i = 0; i < waterPumpingStations.Length; i++)
                {
                    // Building must be in selected district.
                    if (BuildingInSelectedDistrict(districts[i].m_District))
                    {
                        // Logic adapted from Game.UI.InGame.WaterSection.
                        long capacity = waterPumpingStations[i].m_Capacity;
                        long used     = waterPumpingStations[i].m_LastProduction;

                        // Building must have capacity.
                        if (capacity > 0L)
                        {
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
            /// Set building colors for Processing infoview for sewage treatment.
            /// </summary>
            private void SetBuildingColorsProcessingSewageTreatment
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

                // Do each sewage outlet (i.e. building).
                NativeArray<Game.Areas.CurrentDistrict > districts     = buildingChunk.GetNativeArray(ref ComponentTypeHandleCurrentDistrict);
                NativeArray<Game.Buildings.SewageOutlet> sewageOutlets = buildingChunk.GetNativeArray(ref ComponentTypeHandleSewageOutlet);
                for (int i = 0; i < sewageOutlets.Length; i++)
                {
                    // Building must be in selected district.
                    if (BuildingInSelectedDistrict(districts[i].m_District))
                    {
                        // Logic adapted from Game.UI.InGame.SewageSection.
                        long capacity = sewageOutlets[i].m_Capacity;
                        long used     = sewageOutlets[i].m_LastProcessed;

                        // Building must have capacity.
                        if (capacity > 0L)
                        {
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
            /// Set building colors for Processing infoview for creamtorium processing.
            /// </summary>
            private void SetBuildingColorsProcessingCrematoriumProcessing
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

                // Do each deathcare facility (i.e. building).
                NativeArray<Game.Areas.CurrentDistrict      > districts           = buildingChunk.GetNativeArray(ref ComponentTypeHandleCurrentDistrict);
                NativeArray<Entity                          > entities            = buildingChunk.GetNativeArray(EntityTypeHandle);
                NativeArray<Game.Prefabs.PrefabRef          > prefabRefs          = buildingChunk.GetNativeArray(ref ComponentTypeHandlePrefabRef);
                NativeArray<Game.Buildings.DeathcareFacility> deathcareFacilities = buildingChunk.GetNativeArray(ref ComponentTypeHandleDeathcareFacility);
                for (int i = 0; i < deathcareFacilities.Length; i++)
                {
                    // Building must be in selected district.
                    if (BuildingInSelectedDistrict(districts[i].m_District))
                    {
                        // Get entity and prefab.
                        Entity entity = entities[i];
                        Entity prefab = prefabRefs[i].m_Prefab;

                        // Logic adapted from Game.UI.InGame.DeathcareSection.
                        // Get deathcare facility data.
                        if (TryGetComponentDataWithUpgrades(entity, prefab, ref ComponentLookupDeathcareFacilityData, out Game.Prefabs.DeathcareFacilityData deathcareFacilityData))
                        {
                            // Capacity (i.e. processing capacity) is processing rate.
                            long capacity = Mathf.RoundToInt(deathcareFacilityData.m_ProcessingRate);

                            // Building must have capacity.
                            if (capacity > 0L)
                            {
                                // Building has used (i.e. processing speed) only when there are bodies to process.
                                long used = 0L;
                                int bodyCount = deathcareFacilities[i].m_LongTermStoredCount + GetDynamicBufferLength(entity, ref BufferLookupPatient);
                                if (bodyCount > 0)
                                {
                                    // Used (i.e. processing speed) is processing rate times efficiency.
                                    if (BufferLookupEfficiency.TryGetBuffer(entity, out DynamicBuffer<Game.Buildings.Efficiency> bufferEfficiency))
                                    {
                                        used = Mathf.RoundToInt(deathcareFacilityData.m_ProcessingRate * GetBuildingEfficiency(bufferEfficiency));
                                    }
                                }

                                // Update entity color and accumulate totals.
                                UpdateEntityColor(used, capacity, infomodeActive, infomodeIndex, colors, i);
                                totalUsed     += used;
                                totalCapacity += capacity;
                            }
                        }
                    }
                }

                // Update total used and capacity data arrays.
                UpdateTotalUsedCapacity(buildingStatusType, totalUsed, totalCapacity);
            }

            /// <summary>
            /// Set building colors for Processing infoview for garbage processing.
            /// </summary>
            private void SetBuildingColorsProcessingGarbageProcessing
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

                // Do each garbage facility (i.e. building).
                NativeArray<Game.Areas.CurrentDistrict    > districts         = buildingChunk.GetNativeArray(ref ComponentTypeHandleCurrentDistrict);
                NativeArray<Entity                        > entities          = buildingChunk.GetNativeArray(EntityTypeHandle);
                NativeArray<Game.Prefabs.PrefabRef        > prefabRefs        = buildingChunk.GetNativeArray(ref ComponentTypeHandlePrefabRef);
                NativeArray<Game.Buildings.GarbageFacility> garbageFacilities = buildingChunk.GetNativeArray(ref ComponentTypeHandleGarbageFacility);
                for (int i = 0; i < garbageFacilities.Length; i++)
                {
                    // Building must be in selected district.
                    if (BuildingInSelectedDistrict(districts[i].m_District))
                    {
                        // Get entity and prefab.
                        Entity entity = entities[i];
                        Entity prefab = prefabRefs[i].m_Prefab;

                        // Logic adapted from Game.UI.InGame.GarbageSection.
                        // Get processing garbage facility data.
                        if (TryGetComponentDataWithUpgrades(entity, prefab, ref ComponentLookupGarbageFacilityData, out Game.Prefabs.GarbageFacilityData garbageFacilityData))
                        {
                            // Building must have capacity.
                            long capacity = garbageFacilityData.m_ProcessingSpeed;
                            if (capacity > 0L)
                            {
                                // Used is processing rate.
                                long used = garbageFacilities[i].m_ProcessingRate;

                                // Update entity color and accumulate totals.
                                UpdateEntityColor(used, capacity, infomodeActive, infomodeIndex, colors, i);
                                totalUsed     += used;
                                totalCapacity += capacity;
                            }
                        }
                    }
                }

                // Update total used and capacity data arrays.
                UpdateTotalUsedCapacity(buildingStatusType, totalUsed, totalCapacity);
            }

            /// <summary>
            /// Set building colors for Processing infoview for mail sorting speed.
            /// </summary>
            private void SetBuildingColorsProcessingMailSortingSpeed
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

                // Do each post facility (i.e. building).
                NativeArray<Game.Areas.CurrentDistrict > districts      = buildingChunk.GetNativeArray(ref ComponentTypeHandleCurrentDistrict);
                NativeArray<Entity                     > entities       = buildingChunk.GetNativeArray(EntityTypeHandle);
                NativeArray<Game.Prefabs.PrefabRef     > prefabRefs     = buildingChunk.GetNativeArray(ref ComponentTypeHandlePrefabRef);
                NativeArray<Game.Buildings.PostFacility> postFacilities = buildingChunk.GetNativeArray(ref ComponentTypeHandlePostFacility);
                for (int i = 0; i < postFacilities.Length; i++)
                {
                    // Building must be in selected district.
                    if (BuildingInSelectedDistrict(districts[i].m_District))
                    {
                        // Get entity and prefab.
                        Entity entity = entities[i];
                        Entity prefab = prefabRefs[i].m_Prefab;

                        // Logic adapted from Game.UI.InGame.MailSection.
                        // Get processing post facility data.
                        if (TryGetComponentDataWithUpgrades(entity, prefab, ref ComponentLookupPostFacilityData, out Game.Prefabs.PostFacilityData postFacilityData))
                        {
                            // Building must have capacity.
                            long capacity = postFacilityData.m_SortingRate;
                            if (capacity > 0L)
                            {
                                // Used is sorting rate times processing factor
                                long used = (postFacilityData.m_SortingRate * postFacilities[i].m_ProcessingFactor + 50) / 100;

                                // Update entity color and accumulate totals.
                                UpdateEntityColor(used, capacity, infomodeActive, infomodeIndex, colors, i);
                                totalUsed     += used;
                                totalCapacity += capacity;
                            }
                        }
                    }
                }

                // Update total used and capacity data arrays.
                UpdateTotalUsedCapacity(buildingStatusType, totalUsed, totalCapacity);
            }

        }
    }
}
