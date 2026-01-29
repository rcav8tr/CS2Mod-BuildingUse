using Game;
using Game.Buildings;
using Game.Objects;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BuildingUse
{
    /// <summary>
    /// Partial system to set building colors for Processing infoview.
    /// </summary>
    public partial class BuildingColorSystem : GameSystemBase
    {
        /// <summary>
        /// Partial job struct to set the color of each main building for Processing infoview.
        /// Burst compilation for this partial struct is defined by the main UpdateColorsJobMainBuilding struct.
        /// </summary>
        private partial struct UpdateColorsJobMainBuilding : IJobChunk
        {
            /// <summary>
            /// Do a main building for Processing infoview.
            /// </summary>
            private void DoBuildingProcessing(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Do each building status type in descending order.
                DoBuildingProcessingMailSortingSpeed     (in mainBuildingAndUpgrades, ref color);
                DoBuildingProcessingGarbageProcessing    (in mainBuildingAndUpgrades, ref color);
                DoBuildingProcessingCrematoriumProcessing(in mainBuildingAndUpgrades, ref color);
                DoBuildingProcessingSewageTreatment      (in mainBuildingAndUpgrades, ref color);
                DoBuildingProcessingWaterOutput          (in mainBuildingAndUpgrades, ref color);
                DoBuildingProcessingElectricityProduction(in mainBuildingAndUpgrades, ref color);
            }

            /// <summary>
            /// Do a main building and upgrades for Processing infoview for electricity production.
            /// </summary>
            private void DoBuildingProcessingElectricityProduction(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Do each main building and upgrade.
                long used     = 0L;
                long capacity = 0L;
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Logic adapted from Game.UI.InGame.ElectricitySection.OnProcess().
                    if (ComponentLookupElectricityProducer.TryGetComponent(mainBuildingOrUpgrade.Entity, out ElectricityProducer electricityProducer))
                    {
                        used     += electricityProducer.m_LastProduction;
                        capacity += electricityProducer.m_Capacity;
                    }
                }

                // If main building plus upgrades has capacity, update entity color and total used and capacity.
                if (capacity > 0L)
                {
                    UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.ProcessingElectricityProduction, used, capacity, ref color);
                }
            }

            /// <summary>
            /// Do a main building and upgrades for Processing infoview for water output.
            /// </summary>
            private void DoBuildingProcessingWaterOutput(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Do each main building and upgrade.
                long used     = 0L;
                long capacity = 0L;
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Logic adapted from Game.UI.InGame.WaterSection.OnProcess().
                    if (ComponentLookupWaterPumpingStation.TryGetComponent(mainBuildingOrUpgrade.Entity, out Game.Buildings.WaterPumpingStation waterPumpingStation))
                    {
                        used     += waterPumpingStation.m_LastProduction;
                        capacity += waterPumpingStation.m_Capacity;
                    }
                }

                // If main building plus upgrades has capacity, update entity color and total used and capacity.
                if (capacity > 0L)
                {
                    UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.ProcessingWaterOutput, used, capacity, ref color);
                }
            }

            /// <summary>
            /// Do a main building and upgrades for Processing infoview for sewage treatment.
            /// </summary>
            private void DoBuildingProcessingSewageTreatment(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Do each main building and upgrade.
                long used     = 0L;
                long capacity = 0L;
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Logic adapted from Game.UI.InGame.SewageSection.OnProcess().
                    if (ComponentLookupSewageOutlet.TryGetComponent(mainBuildingOrUpgrade.Entity, out Game.Buildings.SewageOutlet sewageOutlet))
                    {
                        used     += sewageOutlet.m_LastProcessed;
                        capacity += sewageOutlet.m_Capacity;
                    }
                }

                // If main building plus upgrades has capacity, update entity color and total used and capacity.
                if (capacity > 0L)
                {
                    UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.ProcessingSewageTreatment, used, capacity, ref color);
                }
            }

            /// <summary>
            /// Do a main building and upgrades for Processing infoview for crematorium processing.
            /// </summary>
            private void DoBuildingProcessingCrematoriumProcessing(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Do each main building and upgrade.
                long used     = 0L;
                long capacity = 0L;
                int bodyCount = 0;
                float mainBuildingEfficiency = 0f;
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Logic adapted from Game.UI.InGame.DeathcareSection.OnProcess().

                    // Get deathcare facility data.
                    if (BuildingHasDeathcareFacility(mainBuildingOrUpgrade.Prefab, out DeathcareFacilityData deathcareFacilityData))
                    {
                        // If don't yet have main building efficiency, get it now.
                        if (mainBuildingEfficiency == 0f)
                        {
                            if (BufferLookupEfficiency.TryGetBuffer(mainBuildingAndUpgrades[0].Entity, out DynamicBuffer<Efficiency> bufferEfficiency) &&
                                bufferEfficiency.IsCreated)
                            {
                                mainBuildingEfficiency = BuildingUtils.GetEfficiency(bufferEfficiency);
                            }
                        }

                        // Used (i.e. processing speed) is processing rate times efficiency.
                        used += (long)math.round(deathcareFacilityData.m_ProcessingRate * mainBuildingEfficiency);

                        // Capacity is processing rate.
                        capacity += (long)math.round(deathcareFacilityData.m_ProcessingRate);

                        // Get body count.
                        if (ComponentLookupDeathcareFacility.TryGetComponent(mainBuildingOrUpgrade.Entity, out Game.Buildings.DeathcareFacility deathcareFacility))
                        {
                            bodyCount += deathcareFacility.m_LongTermStoredCount;
                        }
                        bodyCount += GetDynamicBufferLength(mainBuildingOrUpgrade.Entity, in BufferLookupPatient);
                    }
                }

                // If main building plus upgrades has capacity, update entity color and total used and capacity.
                if (capacity > 0L)
                {
                    // If main building plus upgrades has no bodies, then used (i.e. processing speed) is zero.
                    if (bodyCount == 0)
                    {
                        used = 0L;
                    }

                    UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.ProcessingCrematoriumProcessing, used, capacity, ref color);
                }
            }

            /// <summary>
            /// Do a main building and upgrades for Processing infoview for garbage processing.
            /// </summary>
            private void DoBuildingProcessingGarbageProcessing(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Do each main building and upgrade.
                long used     = 0L;
                long capacity = 0L;
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Logic adapted from Game.UI.InGame.GarbageSection.OnProcess().
                    if (ComponentLookupGarbageFacility.TryGetComponent(mainBuildingOrUpgrade.Entity, out Game.Buildings.GarbageFacility garbageFacility) &&
                        BuildingHasGarbageFacility(mainBuildingOrUpgrade.Prefab, out GarbageFacilityData garbageFacilityData))
                    {
                        used     += garbageFacility    .m_ProcessingRate;
                        capacity += garbageFacilityData.m_ProcessingSpeed;
                    }
                }

                // If main building plus upgrades has capacity, update entity color and total used and capacity.
                if (capacity > 0L)
                {
                    UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.ProcessingGarbageProcessing, used, capacity, ref color);
                }
            }

            /// <summary>
            /// Do a main building and upgrades for Processing infoview for mail sorting speed.
            /// </summary>
            private void DoBuildingProcessingMailSortingSpeed(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Do each main building and upgrade.
                long used     = 0L;
                long capacity = 0L;
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Logic adapted from Game.UI.InGame.MailSection.OnProcess().
                    if (ComponentLookupPostFacility.TryGetComponent(mainBuildingOrUpgrade.Entity, out Game.Buildings.PostFacility postFacility) &&
                        BuildingHasPostFacility(mainBuildingOrUpgrade.Prefab, out PostFacilityData postFacilityData))
                    {
                        used     += (postFacilityData.m_SortingRate * postFacility.m_ProcessingFactor + 50) / 100;
                        capacity += postFacilityData.m_SortingRate;
                    }
                }

                // If main building plus upgrades has capacity, update entity color and total used and capacity.
                if (capacity > 0L)
                {
                    UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.ProcessingMailSortingSpeed, used, capacity, ref color);
                }
            }
        }
    }
}
