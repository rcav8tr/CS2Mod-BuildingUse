using Game;
using Game.Areas;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace BuildingUse
{
    /// <summary>
    /// Partial system to set building colors for Storage infoview.
    /// </summary>
    public partial class BuildingColorSystem : GameSystemBase
    {
        /// <summary>
        /// Partial job struct to set the color of each main building for Storage infoview.
        /// Burst compilation for this partial struct is defined by the main UpdateColorsJobMainBuilding struct.
        /// </summary>
        private partial struct UpdateColorsJobMainBuilding : IJobChunk
        {
            /// <summary>
            /// Do a main building for Storage infoview.
            /// </summary>
            private void DoBuildingStorage(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Do service building status types first.
                DoBuildingStorageService(in mainBuildingAndUpgrades, ref color);

                // Do the company building status types in descending order.
                DoBuildingStorageCompany(in mainBuildingAndUpgrades, ref color, BUBuildingStatusType.StorageOffice);
                DoBuildingStorageCompany(in mainBuildingAndUpgrades, ref color, BUBuildingStatusType.StorageIndustrial);
                DoBuildingStorageCompany(in mainBuildingAndUpgrades, ref color, BUBuildingStatusType.StorageCommercial);
            }

            /// <summary>
            /// Do a main building and upgrades for Storage infoview for a company.
            /// </summary>
            private void DoBuildingStorageCompany(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color, BUBuildingStatusType buildingStatusType)
            {
                // Do each main building and upgrade.
                long used     = 0L;
                long capacity = 0L;
                bool hasCorrectProperty = false;
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Check if building has correct property.
                    Entity entity = mainBuildingOrUpgrade.Entity;
                    if ((buildingStatusType == BUBuildingStatusType.StorageCommercial && BuildingHasCommercial(entity)) ||
                        (buildingStatusType == BUBuildingStatusType.StorageIndustrial && BuildingHasIndustrial(entity)) ||
                        (buildingStatusType == BUBuildingStatusType.StorageOffice     && BuildingHasOffice    (entity)))
                    {
                        // Building has the correct property.
                        hasCorrectProperty = true;

                        // Get the company, if any.
                        if (TryGetCompany(entity, out Entity companyEntity))
                        {
                            // Get used from the company resources buffer, not from the building.
                            if (BufferLookupResources.TryGetBuffer(companyEntity, out DynamicBuffer<Resources> resourcesBuffer) &&
                                resourcesBuffer.IsCreated)
                            {
                                // Do each resources in the buffer.
                                foreach (Resources resources in resourcesBuffer)
                                {
                                    // Exclude NoResource, Money, and Last.
                                    Resource resource = resources.m_Resource;
                                    if (resource != Resource.NoResource && resource != Resource.Money && resource != Resource.Last)
                                    {
                                        used += resources.m_Amount;
                                    }
                                }
                            }

                            // Get storage capacity from the company prefab, not from the building prefab.
                            if (ComponentLookupPrefabRef.TryGetComponent(companyEntity, out PrefabRef prefabRef) &&
                                ComponentLookupStorageLimitData.TryGetComponent(prefabRef.m_Prefab, out  StorageLimitData storageLimitData))
                            {
                                // Check for warehouse.
                                if (ComponentLookupBuildingPropertyData .TryGetComponent(prefabRef.m_Prefab, out BuildingPropertyData buildingPropertyData) &&
                                    ComponentLookupSpawnableBuildingData.TryGetComponent(prefabRef.m_Prefab, out SpawnableBuildingData spawnableBuildingData) &&
                                    ComponentLookupBuildingData         .TryGetComponent(prefabRef.m_Prefab, out BuildingData buildingData) &&
                                    buildingPropertyData.m_AllowedStored != Resource.NoResource)
                                {
                                    // For warehouse, storage capacity is computed.
                                    capacity += storageLimitData.GetAdjustedLimitForWarehouse(spawnableBuildingData, buildingData);
                                }
                                else
                                {
                                    // For other than warehouse, storage capacity is obtained directly.
                                    capacity += storageLimitData.m_Limit;
                                }
                            }
                        }
                    }
                }

                // If main building plus upgrades has the correct property,
                // update building color and total used and capacity even if no company.
                if (hasCorrectProperty)
                {
                    UpdateEntityColorAndTotalUsedCapacity(buildingStatusType, used, capacity, ref color);
                }
            }

            /// <summary>
            /// Do a main building and upgrades for Storage infoview for Service.
            /// </summary>
            private void DoBuildingStorageService(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Do mail box as the first service.
                // Used and capacity for mail box are completely separate
                // from the building's other resources and storage capacity.
                DoBuildingStorageMailbox(in mainBuildingAndUpgrades, ref color);

                // Building Status Type     Capacity Is From
                // ------------------------ -------------------------------
                // StoragePowerPlantFuel    StorageLimitData
                // StorageHealthcare        StorageLimitData
                // StorageLandfill          GarbageFacilityData and SubArea
                // StorageGarbageManagement GarbageFacilityData
                // StorageEmergencyShelter  StorageLimitData
                // StorageTransportation    StorageLimitData
                // StoragePost              PostFacilityData

                // Determine if the building has the building status type.
                bool hasPowerPlantFuel         = false;
                bool hasHealthcare             = false;
                bool hasLandfill               = false;
                bool hasGarbageManagement      = false;
                bool hasEmergencyShelter       = false;
                bool hasCargoTransportation    = false;
                bool hasPost                   = false;

                // At the same time get storage capacities.
                long capacityStorageLimit      = 0L;
                long capacityLandfill          = 0L;
                long capacityGarbageManagement = 0L;
                long capacityPost              = 0L;

                // Do each main building and upgrade.
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Get main building or upgrade prefab.
                    Entity mainBuildingOrUpgradePrefab = mainBuildingOrUpgrade.Prefab;

                    // Try to get StorageLimitData for the building prefab.
                    bool hasStorageLimitData = ComponentLookupStorageLimitData.TryGetComponent(mainBuildingOrUpgradePrefab, out StorageLimitData storageLimitData);

                    // Accumulate capacity from StorageLimitData.
                    if (hasStorageLimitData)
                    {
                        capacityStorageLimit += storageLimitData.m_Limit;
                    }

                    // For PowerPlantFuel, building must have electricity producer with storage.
                    // There are elictricity producers without storage which should not be included.
                    if (ComponentLookupElectricityProducer.HasComponent(mainBuildingOrUpgrade.Entity) &&
                        hasStorageLimitData && storageLimitData.m_Limit > 0)
                    {
                        hasPowerPlantFuel = true;
                    }

                    // For Healthcare, building must have hospital.
                    if (BuildingHasHospital(mainBuildingOrUpgradePrefab))
                    {
                        hasHealthcare = true;
                    }

                    // For Landfill, building must have garbage facility WITH long term storage.
                    bool hasGarbageFacility = BuildingHasGarbageFacility(mainBuildingOrUpgradePrefab, out GarbageFacilityData garbageFacilityData);
                    if (hasGarbageFacility && garbageFacilityData.m_LongTermStorage)
                    {
                        hasLandfill = true;
                        capacityLandfill += garbageFacilityData.m_GarbageCapacity;
                    }

                    // For GarbageManagement, building must have garbage facility WITHOUT long term storage.
                    if (hasGarbageFacility && !garbageFacilityData.m_LongTermStorage)
                    {
                        hasGarbageManagement = true;
                        capacityGarbageManagement += garbageFacilityData.m_GarbageCapacity;
                    }

                    // For EmergencyShelter, building must have emergency shelter.
                    if (BuildingHasEmergencyShelter(mainBuildingOrUpgradePrefab))
                    {
                        hasEmergencyShelter = true;
                    }

                    // For CargoTransportation, building must have cargo transport station.
                    if (BuildingHasCargoTransportStation(mainBuildingOrUpgradePrefab))
                    {
                        hasCargoTransportation = true;
                    }

                    // For Post, building must have post facility.
                    // Logic adapted from Game.UI.InGame.MailSection.OnProcess().
                    // A post facility can also have a mail box.
                    // But for reasons unknown, the used and capacity from the mailbox are not included for the post facility.
                    if (BuildingHasPostFacility(mainBuildingOrUpgradePrefab, out PostFacilityData postFacilityData))
                    {
                        hasPost = true;
                        capacityPost += postFacilityData.m_MailCapacity;
                    }
                }

                // Get used amount for each building status type.
                long usedPowerPlantFuel      = 0L;
                long usedHealthcare          = 0L;
                long usedLandfill            = 0L;
                long usedGarbageManagement   = 0L;
                long usedEmergencyShelter    = 0L;
                long usedCargoTransportation = 0L;
                long usedPost                = 0L;

                // Do each main building and upgrade.
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Get resources buffer, if any.
                    if (BufferLookupResources.TryGetBuffer(mainBuildingOrUpgrade.Entity, out DynamicBuffer<Resources> resourcesBuffer) &&
                        resourcesBuffer.IsCreated)
                    {
                        // Do each resources entry in the buffer.
                        foreach (Resources resources in resourcesBuffer)
                        {
                            // For each specially checked resource,
                            // the resource amount gets assigned first to one of the building status types if needed,
                            // and if not so assigned then the resource amount gets assigned to CargoTransportion if needed.
                            switch (resources.m_Resource)
                            {
                                case Resource.NoResource:
                                case Resource.Money:
                                case Resource.Last:
                                    // Skip these.
                                    break;

                                case Resource.Petrochemicals:
                                case Resource.Coal:
                                    // Petrochemicals and/or Coal can be stored by PowerPlantFuel or CargoTransportation.
                                    if      (hasPowerPlantFuel     ) { usedPowerPlantFuel      += resources.m_Amount; }
                                    else if (hasCargoTransportation) { usedCargoTransportation += resources.m_Amount; }
                                    break;

                                case Resource.Pharmaceuticals:
                                    // Pharmaceuticals can be stored by Healthcare or CargoTransportation.
                                    if      (hasHealthcare         ) { usedHealthcare          += resources.m_Amount; }
                                    else if (hasCargoTransportation) { usedCargoTransportation += resources.m_Amount; }
                                    break;

                                case Resource.Garbage:
                                    // Garbage can be stored by Landfill, GarbageManagement, or CargoTransportation.
                                    if      ( hasLandfill && !hasGarbageManagement) { usedLandfill          += resources.m_Amount; }
                                    else if (!hasLandfill &&  hasGarbageManagement) { usedGarbageManagement += resources.m_Amount; }
                                    else if ( hasLandfill &&  hasGarbageManagement)
                                    {
                                        // Building has both Landfill and GarbageManagement.
                                        // This should never happen, but if it does, then each gets half.
                                        usedLandfill          += resources.m_Amount / 2L;
                                        usedGarbageManagement += resources.m_Amount / 2L;
                                    }
                                    else if (hasCargoTransportation) { usedCargoTransportation += resources.m_Amount; }
                                    break;

                                case Resource.Food:
                                    // Food can be stored by EmergencyShelter or CargoTransportation.
                                    if      (hasEmergencyShelter   ) { usedEmergencyShelter    += resources.m_Amount; }
                                    else if (hasCargoTransportation) { usedCargoTransportation += resources.m_Amount; }
                                    break;

                                case Resource.LocalMail:
                                case Resource.OutgoingMail:
                                case Resource.UnsortedMail:
                                    // Mail of any type can be stored by Post or CargoTransportation.
                                    if      (hasPost               ) { usedPost                += resources.m_Amount; }
                                    else if (hasCargoTransportation) { usedCargoTransportation += resources.m_Amount; }
                                    break;

                                default:
                                    // All other resources can only be stored by CargoTransportation.
                                    if (hasCargoTransportation) { usedCargoTransportation += resources.m_Amount; }
                                    break;
                            }
                        }
                    }

                    // For Landfill, include Garbage used and capacity from sub areas that store garbage.
                    // Logic adapted from Game.UI.InGame.GarbageSection.OnProcess().
                    if (hasLandfill &&
                        BufferLookupSubArea.TryGetBuffer(mainBuildingOrUpgrade.Entity, out DynamicBuffer<Game.Areas.SubArea> bufferSubAreas) &&
                        bufferSubAreas.IsCreated)
                    {
                        foreach (Game.Areas.SubArea subArea in bufferSubAreas)
                        {
                            Entity subAreaEntity = subArea.m_Area;
                            if (ComponentLookupStorage.TryGetComponent(subAreaEntity, out Storage subAreaStorage) &&
                                ComponentLookupGeometry.TryGetComponent(subAreaEntity, out Geometry subAreaGeometry) &&
                                ComponentLookupPrefabRef.TryGetComponent(subAreaEntity, out PrefabRef subAreaPrefabRef) &&
                                ComponentLookupStorageAreaData.TryGetComponent(subAreaPrefabRef.m_Prefab, out StorageAreaData subAreaStorageAreaData) &&
                                (subAreaStorageAreaData.m_Resources & Resource.Garbage) != 0)
                            {
                                usedLandfill     += subAreaStorage.m_Amount;
                                capacityLandfill += AreaUtils.CalculateStorageCapacity(subAreaGeometry, subAreaStorageAreaData);
                            }
                        }
                    }
                }

                // For the building status types that share the capacity of the building (i.e. capacity is from StorageLimitData):
                // if more than one is present, divide the capacity evenly between them.
                // This is not ideal, but it is better than letting the full capacity remain with all of them.
                int count = 0;
                if (hasPowerPlantFuel     ) { count++; }
                if (hasHealthcare         ) { count++; }
                if (hasEmergencyShelter   ) { count++; }
                if (hasCargoTransportation) { count++; }
                if (count > 1)
                {
                    capacityStorageLimit /= count;
                }

                // For each building status type that has capacity, update entity color and total used and capacity.
                // Do in descending order by building status type.
                if (hasPost                && capacityPost              > 0L) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.StoragePost,                usedPost,                capacityPost,              ref color); }
                if (hasCargoTransportation && capacityStorageLimit      > 0L) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.StorageCargoTransportation, usedCargoTransportation, capacityStorageLimit,      ref color); }
                if (hasEmergencyShelter    && capacityStorageLimit      > 0L) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.StorageEmergencyShelter,    usedEmergencyShelter,    capacityStorageLimit,      ref color); }
                if (hasGarbageManagement   && capacityGarbageManagement > 0L) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.StorageGarbageManagement,   usedGarbageManagement,   capacityGarbageManagement, ref color); }
                if (hasLandfill            && capacityLandfill          > 0L) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.StorageLandfill,            usedLandfill,            capacityLandfill,          ref color); }
                if (hasHealthcare          && capacityStorageLimit      > 0L) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.StorageHealthcare,          usedHealthcare,          capacityStorageLimit,      ref color); }
                if (hasPowerPlantFuel      && capacityStorageLimit      > 0L) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.StoragePowerPlantFuel,      usedPowerPlantFuel,      capacityStorageLimit,      ref color); }

                // Do battery charge as the last service.
                // Used and capacity for battery charge are completely separate
                // from the building's other resources and storage capacity.
                DoBuildingStorageBatteryCharge(in mainBuildingAndUpgrades, ref color);
            }

            /// <summary>
            /// Do a main building and upgrades for Storage infoview for Battery Charge.
            /// </summary>
            private void DoBuildingStorageBatteryCharge(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Get used and capacity from main building and upgrades.
                long used     = 0;
                long capacity = 0;
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Logic adapted from Game.UI.InGame.BatterySection.OnProcess().

                    // Accumulate used from energy stored in the battery.
                    if (ComponentLookupBattery.TryGetComponent(mainBuildingOrUpgrade.Entity, out Game.Buildings.Battery battery))
                    {
                        used += battery.storedEnergyHours;
                    }

                    // Accumulate capacity from the building.
                    if (BuildingHasBattery(mainBuildingOrUpgrade.Prefab, out BatteryData batteryData))
                    {
                        capacity += batteryData.m_Capacity;
                    }
                }

                // If main building plus upgrades has capacity, update entity color and total used and capacity.
                if (capacity > 0)
                {
                    UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.StorageBatteryCharge, used, capacity, ref color);
                }
            }

            /// <summary>
            /// Do a main building and upgrades for Storage infoview for Mail Box.
            /// </summary>
            private void DoBuildingStorageMailbox(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Get used and capacity from main building and upgrades.
                long used     = 0L;
                long capacity = 0L;
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Logic adapted from Game.UI.InGame.MailSection.OnProcess().

                    // Building must not have post facility.
                    if (!BuildingHasPostFacility(mainBuildingOrUpgrade.Prefab))
                    {
                        // Accumulate used from the mail box.
                        if (ComponentLookupMailBox.TryGetComponent(mainBuildingOrUpgrade.Entity, out Game.Routes.MailBox mailBox))
                        {
                            used += mailBox.m_MailAmount;
                        }

                        // Accumulate capacity from the mail box data.
                        if (BuildingHasMailBox(mainBuildingOrUpgrade.Prefab, out MailBoxData mailBoxData))
                        {
                            capacity += mailBoxData.m_MailCapacity;
                        }
                    }
                }

                // If main building plus upgrades has capacity, update entity color and total used and capacity.
                if (capacity > 0)
                {
                    UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.StorageMailbox, used, capacity, ref color);
                }
            }
        }
    }
}
