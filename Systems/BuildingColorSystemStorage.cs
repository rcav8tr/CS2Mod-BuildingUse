using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BuildingUse
{
    /// <summary>
    /// Partial system to set building colors for Storage infoview.
    /// </summary>
    public partial class BuildingColorSystem : Game.GameSystemBase
    {
        /// <summary>
        /// Partial job struct to set the color of each main building for Storage infoview.
        /// Burst compilation for this partial struct is defined by the main UpdateColorsJobMainBuilding struct.
        /// </summary>
        private partial struct UpdateColorsJobMainBuilding : IJobChunk
        {
            /// <summary>
            /// Get building status types applicable to the building chunk for Storage infoview.
            /// </summary>
            private void GetApplicableBuildingStatusTypesStorage(ArchetypeChunk buildingChunk, ref NativeList<BUBuildingStatusType> applicableBuildingStatusTypes)
            {
                // Add all building status types that apply to this building chunk.
                if (buildingChunk.Has(ref ComponentTypeHandleCommercialProperty))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.StorageCommercial);
                if (buildingChunk.Has(ref ComponentTypeHandleIndustrialProperty) && !buildingChunk.Has(ref ComponentTypeHandleOfficeProperty))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.StorageIndustrial);
                if (buildingChunk.Has(ref ComponentTypeHandleOfficeProperty))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.StorageOffice);
                if (buildingChunk.Has(ref ComponentTypeHandleBattery))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.StorageBatteryCharge);
                if (buildingChunk.Has(ref ComponentTypeHandleElectricityProducer))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.StoragePowerPlantFuel);
                if (buildingChunk.Has(ref ComponentTypeHandleHospital))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.StorageHealthcare);
                if (buildingChunk.Has(ref ComponentTypeHandleGarbageFacility))
                {
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.StorageLandfill);
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.StorageGarbageManagement);
                }
                if (buildingChunk.Has(ref ComponentTypeHandleEmergencyShelter))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.StorageEmergencyShelter);
                if (buildingChunk.Has(ref ComponentTypeHandleTransportStation))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.StorageCargoTransportation);
                if (buildingChunk.Has(ref ComponentTypeHandlePostFacility) || buildingChunk.Has(ref ComponentTypeHandleMailBox))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.StoragePost);
            }

            /// <summary>
            /// Set building colors for Storage infoview.
            /// </summary>
            private void SetBuildingColorsStorage
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
                    case BUBuildingStatusType.StorageCommercial:
                    case BUBuildingStatusType.StorageIndustrial:
                    case BUBuildingStatusType.StorageOffice:
                        SetBuildingColorsStorageResource(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType, true);
                        break;

                    case BUBuildingStatusType.StorageBatteryCharge:
                        SetBuildingColorsStorageBatteryCharge(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType);
                        break;
                    
                    case BUBuildingStatusType.StorageLandfill:
                        SetBuildingColorsStorageGarbageManagement(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType, true);
                        break;
                    case BUBuildingStatusType.StorageGarbageManagement:
                        SetBuildingColorsStorageGarbageManagement(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType, false);
                        break;

                    case BUBuildingStatusType.StoragePost:
                        SetBuildingColorsStoragePost(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType);
                        break;

                    case BUBuildingStatusType.StoragePowerPlantFuel:
                        if (buildingChunk.Has(ref ComponentTypeHandleGarbageFacility))
                        {
                            // Incinerator stored garbage is power plant fuel.
                            SetBuildingColorsStorageGarbageManagement(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType, false);
                        }
                        else
                        {
                            // Other power plants use general resource storage.
                            SetBuildingColorsStorageResource(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType, false);
                        }
                        break;

                    // Everything else is resource storage.
                    default:
                        SetBuildingColorsStorageResource(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType, false);
                        break;
                }
            }
            
            /// <summary>
            /// Set building colors for Storage infoview for battery charge.
            /// Logic adapted from Game.UI.InGame.BatterySection.
            /// </summary>
            private void SetBuildingColorsStorageBatteryCharge
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

                // Get batteries.
			    NativeArray<Game.Buildings.Battery> batteries = buildingChunk.GetNativeArray(ref ComponentTypeHandleBattery);

                // Do each entity (i.e. building).
                NativeArray<Game.Areas.CurrentDistrict> districts  = buildingChunk.GetNativeArray(ref ComponentTypeHandleCurrentDistrict);
                NativeArray<Entity                    > entities   = buildingChunk.GetNativeArray(EntityTypeHandle);
                NativeArray<Game.Prefabs.PrefabRef    > prefabRefs = buildingChunk.GetNativeArray(ref ComponentTypeHandlePrefabRef);
                for (int i = 0; i < entities.Length; i++)
                {
                    // Building must be in selected district.
                    if (BuildingInSelectedDistrict(districts[i].m_District))
                    {
                        // Get component data with upgrades.
                        if (TryGetComponentDataWithUpgrades(entities[i], prefabRefs[i].m_Prefab, ref ComponentLookupBatteryData, out Game.Prefabs.BatteryData batteryData))
                        {
                            // Get used from battery.
                            long used = batteries[i].storedEnergyHours;

                            // Get capacity.
                            long capacity = batteryData.m_Capacity;

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
            /// Set building colors for Storage infoview for garbage management.
            /// Logic adapted from Game.UI.InGame.GarbageSection.
            /// </summary>
            private void SetBuildingColorsStorageGarbageManagement
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

                // Do each entity (i.e. building).
                NativeArray<Game.Areas.CurrentDistrict> districts  = buildingChunk.GetNativeArray(ref ComponentTypeHandleCurrentDistrict);
                NativeArray<Entity                    > entities   = buildingChunk.GetNativeArray(EntityTypeHandle);
                NativeArray<Game.Prefabs.PrefabRef    > prefabRefs = buildingChunk.GetNativeArray(ref ComponentTypeHandlePrefabRef);
                for (int i = 0; i < entities.Length; i++)
                {
                    // Building must be in selected district.
                    if (BuildingInSelectedDistrict(districts[i].m_District))
                    {
                        // Get component data with upgrades.
                        Entity entity = entities[i];
                        if (TryGetComponentDataWithUpgrades(entity, prefabRefs[i].m_Prefab, ref ComponentLookupGarbageFacilityData, out Game.Prefabs.GarbageFacilityData garbageFacilityData))
                        {
                            // Check for landfill vs other garbage management.
                            // Only landfill has long term storage.
                            if (garbageFacilityData.m_LongTermStorage == longTermStorage)
                            {
                                // Get used from resources buffer.
                                long used = 0;
			                    if (BufferLookupResources.TryGetBuffer(entity, out DynamicBuffer<Game.Economy.Resources> bufferResources))
			                    {
				                    used = Game.Economy.EconomyUtils.GetResources(Game.Economy.Resource.Garbage, bufferResources);
			                    }

                                // Get capacity.
                                long capacity = garbageFacilityData.m_GarbageCapacity;

                                // Add in any sub areas.
		                        if (BufferLookupSubArea.TryGetBuffer(entity, out DynamicBuffer<Game.Areas.SubArea> subAreas))
		                        {
                                    // Do each sub area.
		                            for (int j = 0; j < subAreas.Length; j++)
		                            {
                                        // Get storage information.
			                            Entity subArea = subAreas[j].m_Area;
			                            if (ComponentLookupStorage        .TryGetComponent(subArea, out Game.Areas.Storage subAreaStorage) &&
                                            ComponentLookupGeometry       .TryGetComponent(subArea, out Game.Areas.Geometry subAreaGeometry) &&
                                            ComponentLookupPrefabRef      .TryGetComponent(subArea, out Game.Prefabs.PrefabRef subAreaPrefabRef) &&
                                            ComponentLookupStorageAreaData.TryGetComponent(subAreaPrefabRef.m_Prefab, out Game.Prefabs.StorageAreaData storageAreaData))
			                            {
					                        used     += subAreaStorage.m_Amount;
					                        capacity += Game.Areas.AreaUtils.CalculateStorageCapacity(subAreaGeometry, storageAreaData);
			                            }
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
            /// Set building colors for Storage infoview for post.
            /// Logic adapted from Game.UI.InGame.MailSection.
            /// </summary>
            private void SetBuildingColorsStoragePost
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
                NativeArray<Game.Areas.CurrentDistrict> districts  = buildingChunk.GetNativeArray(ref ComponentTypeHandleCurrentDistrict);
                NativeArray<Entity                    > entities   = buildingChunk.GetNativeArray(EntityTypeHandle);
                NativeArray<Game.Prefabs.PrefabRef    > prefabRefs = buildingChunk.GetNativeArray(ref ComponentTypeHandlePrefabRef);
                NativeArray<Game.Routes.MailBox       > mailboxes  = buildingChunk.GetNativeArray(ref ComponentTypeHandleMailBox);
                for (int i = 0; i < entities.Length; i++)
                {
                    // Building must be in selected district.
                    if (BuildingInSelectedDistrict(districts[i].m_District))
                    {
                        // Get entity and prefab.
                        Entity entity = entities[i];
                        Entity prefab = prefabRefs[i].m_Prefab;

                        // Get the post facility data.
                        if (TryGetComponentDataWithUpgrades(entity, prefab, ref ComponentLookupPostFacilityData, out Game.Prefabs.PostFacilityData postFacilityData))
                        {
                            // Building must have capacity.
                            long capacity = postFacilityData.m_MailCapacity;
                            if (capacity > 0L)
                            {
                                // Get resources.
                                long used = 0L;
                                if (BufferLookupResources.TryGetBuffer(entity, out DynamicBuffer<Game.Economy.Resources> bufferResources))
                                {
                                    // Mail stored is sum of mail amounts.
                                    used =
                                        Game.Economy.EconomyUtils.GetResources(Game.Economy.Resource.UnsortedMail, bufferResources) +
                                        Game.Economy.EconomyUtils.GetResources(Game.Economy.Resource.LocalMail, bufferResources) +
                                        Game.Economy.EconomyUtils.GetResources(Game.Economy.Resource.OutgoingMail, bufferResources);

                                    // If post facility has a mailbox, add in its amount.
                                    // It is unknown why the capacity of the mailbox is not included in the logic of Game.UI.InGame.MailSection.
                                    if (mailboxes.Length > 0)
                                    {
                                        used += mailboxes[i].m_MailAmount;
                                    }
                                }

                                // Update entity color and accumulate totals.
                                UpdateEntityColor(used, capacity, infomodeActive, infomodeIndex, colors, i);
                                totalUsed     += used;
                                totalCapacity += capacity;
                            }
                        }
                        // Get the mailbox data.
                        else if (mailboxes.Length > 0 && ComponentLookupMailBoxData.TryGetComponent(prefab, out Game.Prefabs.MailBoxData mailBoxData))
                        {
                            // There must be capacity.
                            long capacity = mailBoxData.m_MailCapacity;
                            if (capacity > 0L)
                            {
                                // Get used.
                                long used = mailboxes[i].m_MailAmount;

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
            /// Set building colors for Storage infoview for a building with resources.
            /// </summary>
            private void SetBuildingColorsStorageResource
            (
                ArchetypeChunk buildingChunk,
                bool infomodeActive,
                int infomodeIndex,
                NativeArray<Game.Objects.Color> colors,
                BUBuildingStatusType buildingStatusType,
                bool company
            )
            {
                // Logic adapted from Game.UI.InGame.StorageSection.

                // Initialize total used and capacity.
                long totalUsed     = 0L;
                long totalCapacity = 0L;

                // Do each entity (i.e. building).
                NativeArray<Game.Areas.CurrentDistrict> districts  = buildingChunk.GetNativeArray(ref ComponentTypeHandleCurrentDistrict);
                NativeArray<Entity                    > entities   = buildingChunk.GetNativeArray(EntityTypeHandle);
                NativeArray<Game.Prefabs.PrefabRef    > prefabRefs = buildingChunk.GetNativeArray(ref ComponentTypeHandlePrefabRef);
                for (int i = 0; i < entities.Length; i++)
                {
                    // Building must be in selected district.
                    if (BuildingInSelectedDistrict(districts[i].m_District))
                    {
                        // Get entity and prefab.
                        Entity entity = entities[i];
                        Entity prefab = prefabRefs[i].m_Prefab;

                        // Building or company must have storage.
                        bool found = false;
                        Entity companyEntity = Entity.Null;
                        if
                            (
                                (
                                    // Try to get storage limit data from the prefab of the entity.
                                    TryGetComponentDataWithUpgrades(entity, prefab, ref ComponentLookupStorageLimitData, out Game.Companies.StorageLimitData storageLimitData)
                                )
                                ||
                                (
                                    // Try to get storage limit data from the prefab of the company of the entity.
                                    Game.UI.InGame.CompanyUIUtils.HasCompany(entity, prefab, ref BufferLookupRenter, ref ComponentLookupBuildingPropertyData, ref ComponentLookupCompanyData, out companyEntity) &&
                                    ComponentLookupPrefabRef.TryGetComponent(companyEntity, out Game.Prefabs.PrefabRef companyPrefabRef) &&
                                    ComponentLookupStorageLimitData.TryGetComponent(companyPrefabRef.m_Prefab, out storageLimitData)
                                )
                            )
                        {
                            // Storage must have capacity.
                            long capacity = GetStorageCapacity(entity, prefab, storageLimitData);
                            if (capacity > 0L)
                            {
                                // Building or company has storage and storage has capacity.
                                found = true;

                                // Get used from resources.
                                long used = GetTotalResourceAmount(entity, companyEntity);
                                used = math.min(math.max(used, 0L), capacity);

                                // Employee used and capacity are valid.
                                // Update entity color and accumulate totals.
                                UpdateEntityColor(used, capacity, infomodeActive, infomodeIndex, colors, i);
                                totalUsed     += used;
                                totalCapacity += capacity;
                            }
                        }

                        // For a company not found, update entity color for 0%.
                        if (company && !found)
                        {
                            UpdateEntityColor(0L, 0L, infomodeActive, infomodeIndex, colors, i);
                        }
                    }
                }

                // Update total used and capacity data arrays.
                UpdateTotalUsedCapacity(buildingStatusType, totalUsed, totalCapacity);
            }

            /// <summary>
            /// Get storage capacity.
            /// </summary>
            private int GetStorageCapacity(Entity entity, Entity prefab, Game.Companies.StorageLimitData storageLimitData)
            {
                // Logic adapted from Game.UI.InGame.StorageSection.Visible().

                // Check how to compute storage capacity.
                if
                    (
                        (
                            (
                                // Try to get building property data, spawnabale building data, and building data from property renter prefab.
                                ComponentLookupPropertyRenter       .TryGetComponent(entity, out Game.Buildings.PropertyRenter propertyRenter) &&
                                ComponentLookupPrefabRef            .TryGetComponent(propertyRenter.m_Property, out Game.Prefabs.PrefabRef propertyRenterPrefabRef) &&
                                ComponentLookupBuildingPropertyData .TryGetComponent(propertyRenterPrefabRef.m_Prefab, out Game.Prefabs.BuildingPropertyData buildingPropertyData) &&
                                ComponentLookupSpawnableBuildingData.TryGetComponent(propertyRenterPrefabRef.m_Prefab, out Game.Prefabs.SpawnableBuildingData spawnableBuildingData) &&
                                ComponentLookupBuildingData         .TryGetComponent(propertyRenterPrefabRef.m_Prefab, out Game.Prefabs.BuildingData buildingData)
                            )
                            ||
                            (
                                // Try to get building property data, spawnabale building data, and building data from the prefab.
                                ComponentLookupBuildingPropertyData .TryGetComponent(prefab, out buildingPropertyData) &&
                                ComponentLookupSpawnableBuildingData.TryGetComponent(prefab, out spawnableBuildingData) &&
                                ComponentLookupBuildingData         .TryGetComponent(prefab, out buildingData)
                            )
                        )
                        &&
                        (
                            // Must allow resources to be sold, manufactured, or stored.
                            buildingPropertyData.m_AllowedSold         != Game.Economy.Resource.NoResource ||
                            buildingPropertyData.m_AllowedManufactured != Game.Economy.Resource.NoResource ||
                            buildingPropertyData.m_AllowedStored       != Game.Economy.Resource.NoResource 
                        )
                    )
                {
                    // Storage capacity is computed from storage limit, spawnabale building data (i.e. building level), and building data (i.e. lot size).
                    return storageLimitData.GetAdjustedLimit(spawnableBuildingData, buildingData);
                }
                else
                {
                    // Storage capacity is only from storage limit.
                    return storageLimitData.m_Limit;
                }
            }

            /// <summary>
            /// Get total resource amount for entity or company entity.
            /// </summary>
            private int GetTotalResourceAmount(Entity entity, Entity companyEntity)
            {
                // Logic adapted from Game.UI.InGame.StorageSection.OnProcess().

                // Get resources buffer from either the entity or the company entity.
                int amount = 0;
                if (BufferLookupResources.TryGetBuffer(entity, out DynamicBuffer<Game.Economy.Resources> bufferResources) ||
                    BufferLookupResources.TryGetBuffer(companyEntity, out bufferResources))
                {
                    // Do each resource in the buffer.
                    // Note that weighted (e.g. Electronics) and unweighted (e.g. Software) resource amounts
                    // are combined with each other and are generally treated as weighted.
                    for (int i = 0; i < bufferResources.Length; i++)
                    {
                        // Skip no resource and money.
                        Game.Economy.Resources resources = bufferResources[i];
                        if (resources.m_Resource != Game.Economy.Resource.NoResource && resources.m_Resource != Game.Economy.Resource.Money)
                        {
                            // Accumulate resource amount.
                            amount += resources.m_Amount;
                        }
                    }
                }

                // Return the amount.
                return amount;
            }
        }
    }
}
