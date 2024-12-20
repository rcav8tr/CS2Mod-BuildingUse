using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BuildingUse
{
    /// <summary>
    /// Partial system to set building colors for Vehicles infoview.
    /// </summary>
    public partial class BuildingColorSystem : Game.GameSystemBase
    {
        /// <summary>
        /// Partial job struct to set the color of each main building for Vehicles infoview.
        /// Burst compilation for this partial struct is defined by the main UpdateColorsJobMainBuilding struct.
        /// </summary>
        private partial struct UpdateColorsJobMainBuilding : IJobChunk
        {
            // The different fire engine types.
            private enum FireEngineType
            {
                FireEngine,
                FireHelicopter,
                DisasterResponse
            }

            /// <summary>
            /// Get building status types applicable to the building chunk for Vehicles infoview.
            /// </summary>
            private void GetApplicableBuildingStatusTypesVehicles(ArchetypeChunk buildingChunk, ref NativeList<BUBuildingStatusType> applicableBuildingStatusTypes)
            {
                // Add all building status types that apply to this building chunk.
                if (buildingChunk.Has(ref ComponentTypeHandleCommercialProperty))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesCommercialTruck);
                if (buildingChunk.Has(ref ComponentTypeHandleIndustrialProperty) && !buildingChunk.Has(ref ComponentTypeHandleOfficeProperty))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesIndustrialTruck);
                if (buildingChunk.Has(ref ComponentTypeHandleOfficeProperty))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesOfficeTruck);
                if (buildingChunk.Has(ref ComponentTypeHandleParkingFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesParked);
                if (buildingChunk.Has(ref ComponentTypeHandleRoadMaintenance))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesRoadMaintenance);
                if (buildingChunk.Has(ref ComponentTypeHandleHospital))
                {
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesAmbulance);
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesMedicalHelicopter);
                }
                if (buildingChunk.Has(ref ComponentTypeHandleDeathcareFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesHearse);
                if (buildingChunk.Has(ref ComponentTypeHandleGarbageFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesGarbageTruck);
                if (buildingChunk.Has(ref ComponentTypeHandleFireStation))
                {
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesFireEngine);
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesFireHelicopter);
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesDisasterResponse);
                }
                if (buildingChunk.Has(ref ComponentTypeHandleEmergencyShelter))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesEvacuationBus);
                if (buildingChunk.Has(ref ComponentTypeHandlePoliceStation))
                {
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesPoliceCar);
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesPoliceHelicopter);
                }
                if (buildingChunk.Has(ref ComponentTypeHandlePrison))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesPrisonVan);
                if (buildingChunk.Has(ref ComponentTypeHandleTransportDepot))
                {
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesBus);
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesTaxi);
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesTrain);
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesTram);
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesSubway);
                }
                if (buildingChunk.Has(ref ComponentTypeHandleParkMaintenance))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesParkMaintenance);
                if (buildingChunk.Has(ref ComponentTypeHandlePostFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesPost);
                if (buildingChunk.Has(ref ComponentTypeHandleTransportCompany))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.VehiclesCargoStationTruck);
            }

            /// <summary>
            /// Set building colors for Vehicles infoview.
            /// </summary>
            private void SetBuildingColorsVehicles
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
                for (int i = 0; i < entities.Length; i++)
                {
                    // Building must be in selected district.
                    if (!BuildingInSelectedDistrict(districts[i].m_District))
                    {
                        continue;
                    }

                    // Get entity and prefab.
                    Entity entity = entities[i];
                    Entity prefab = prefabRefs[i].m_Prefab;

                    // For most building status types:
                    //      If can get component data with upgrades, then:
                    //          Get capacity from component data.
                    //          If counting vehicles in use, get used from owned vehicles based on vehicle type(s) that correspond to the building status type.
                    int capacity = 0;
                    int used     = 0;
                    switch(buildingStatusType)
                    {
                        case BUBuildingStatusType.VehiclesCommercialTruck:
                        case BUBuildingStatusType.VehiclesIndustrialTruck:
                        case BUBuildingStatusType.VehiclesOfficeTruck:
                            // Check for a company (i.e. renter).
                            if (Game.UI.InGame.CompanyUIUtils.HasCompany(entity, prefab, ref BufferLookupRenter, ref ComponentLookupBuildingPropertyData, ref ComponentLookupCompanyData, out Entity companyEntity))
                            {
                                // Companies have no upgrades to combine.
                                // Get transport company data directly, but from company prefab.
                                Entity companyPrefab = ComponentLookupPrefabRef[companyEntity].m_Prefab;
                                if (ComponentLookupTransportCompanyData.TryGetComponent(companyPrefab, out Game.Companies.TransportCompanyData transportCompanyData1))
                                {
                                    capacity = transportCompanyData1.m_MaxTransports;
                                    if (CountVehiclesInUse) { used = GetOwnedVehicleCountOneType(companyEntity, ref ComponentLookupDeliveryTruck); }
                                }
                            }
                            break;

                        case BUBuildingStatusType.VehiclesParked:
                            GetParkedVehicles(entity, out int usedParked, out int capacityParked);
                            capacity = capacityParked;
                            if (CountVehiclesInUse) { used = usedParked; }
                            break;

                        case BUBuildingStatusType.VehiclesRoadMaintenance:
                        case BUBuildingStatusType.VehiclesParkMaintenance:
                            if (TryGetComponentDataWithUpgrades(entity, prefab, ref ComponentLookupMaintenanceDepotData, out Game.Prefabs.MaintenanceDepotData maintenanceDepotData))
                            {
                                capacity = maintenanceDepotData.m_VehicleCapacity;
                                if (CountVehiclesInUse) { used = GetOwnedVehicleCountOneType(entity, ref ComponentLookupMaintenanceVehicle); }
                            }
                            break;
                        
                        case BUBuildingStatusType.VehiclesAmbulance:
                            if (TryGetComponentDataWithUpgrades(entity, prefab, ref ComponentLookupHospitalData, out Game.Prefabs.HospitalData hospitalData1))
                            {
                                capacity = hospitalData1.m_AmbulanceCapacity;
                                if (CountVehiclesInUse) { used = GetOwnedVehicleCountOneTypeNotType(entity, ref ComponentLookupAmbulance, ref ComponentLookupHelicopter); }
                            }
                            break;
                        
                        case BUBuildingStatusType.VehiclesMedicalHelicopter:
                            if (TryGetComponentDataWithUpgrades(entity, prefab, ref ComponentLookupHospitalData, out Game.Prefabs.HospitalData hospitalData2))
                            { 
                                capacity = hospitalData2.m_MedicalHelicopterCapacity;
                                if (CountVehiclesInUse) { used = GetOwnedVehicleCountTwoTypes(entity, ref ComponentLookupAmbulance, ref ComponentLookupHelicopter); }
                            }
                            break;
                        
                        case BUBuildingStatusType.VehiclesHearse:
                            if (TryGetComponentDataWithUpgrades(entity, prefab, ref ComponentLookupDeathcareFacilityData, out Game.Prefabs.DeathcareFacilityData deathcareFacilityData))
                            {
                                capacity = deathcareFacilityData.m_HearseCapacity;
                                if (CountVehiclesInUse) { used = GetOwnedVehicleCountOneType(entity, ref ComponentLookupHearse); }
                            }
                            break;
                        
                        case BUBuildingStatusType.VehiclesGarbageTruck:
                            if (TryGetComponentDataWithUpgrades(entity, prefab, ref ComponentLookupGarbageFacilityData, out Game.Prefabs.GarbageFacilityData garbageFacilityData))
                            {
                                capacity = garbageFacilityData.m_VehicleCapacity;
                                if (CountVehiclesInUse) { used = GetOwnedVehicleCountOneType(entity, ref ComponentLookupGarbageTruck) +
                                                                 GetOwnedVehicleCountOneType(entity, ref ComponentLookupDeliveryTruck); }
                            }
                            break;
                        
                        case BUBuildingStatusType.VehiclesFireEngine:
                            if (TryGetComponentDataWithUpgrades(entity, prefab, ref ComponentLookupFireStationData, out Game.Prefabs.FireStationData fireStationData1))
                            {
                                capacity = fireStationData1.m_FireEngineCapacity;
                                if (CountVehiclesInUse) { used = GetOwnedVehicleCountFireEngine(entity, FireEngineType.FireEngine); }
                            }
                            break;
                        
                        case BUBuildingStatusType.VehiclesFireHelicopter:
                            if (TryGetComponentDataWithUpgrades(entity, prefab, ref ComponentLookupFireStationData, out Game.Prefabs.FireStationData fireStationData2))
                            {
                                capacity = fireStationData2.m_FireHelicopterCapacity;
                                if (CountVehiclesInUse) { used = GetOwnedVehicleCountFireEngine(entity, FireEngineType.FireHelicopter); }
                            }
                            break;
                        
                        case BUBuildingStatusType.VehiclesDisasterResponse:
                            if (TryGetComponentDataWithUpgrades(entity, prefab, ref ComponentLookupFireStationData, out Game.Prefabs.FireStationData fireStationData3))
                            {
                                capacity = fireStationData3.m_DisasterResponseCapacity;
                                if (CountVehiclesInUse) { used = GetOwnedVehicleCountFireEngine(entity, FireEngineType.DisasterResponse); }
                            }
                            break;

                        case BUBuildingStatusType.VehiclesEvacuationBus:
                            if (TryGetComponentDataWithUpgrades(entity, prefab, ref ComponentLookupEmergencyShelterData, out Game.Prefabs.EmergencyShelterData emergencyShelterData))
                            {
                                capacity = emergencyShelterData.m_VehicleCapacity;
                                if (CountVehiclesInUse) { used = GetOwnedVehicleCountOneType(entity, ref ComponentLookupEvacuatingTransport); }
                            }
                            break;

                        case BUBuildingStatusType.VehiclesPoliceCar:
                            if (TryGetComponentDataWithUpgrades(entity, prefab, ref ComponentLookupPoliceStationData, out Game.Prefabs.PoliceStationData policeStationData1))
                            {
                                capacity = policeStationData1.m_PatrolCarCapacity;
                                if (CountVehiclesInUse) { used = GetOwnedVehicleCountOneTypeNotType(entity, ref ComponentLookupPoliceCar, ref ComponentLookupHelicopter); }
                            }
                            break;
                        
                        case BUBuildingStatusType.VehiclesPoliceHelicopter:
                            if (TryGetComponentDataWithUpgrades(entity, prefab, ref ComponentLookupPoliceStationData, out Game.Prefabs.PoliceStationData policeStationData2))
                            {
                                capacity = policeStationData2.m_PoliceHelicopterCapacity;
                                if (CountVehiclesInUse) { used = GetOwnedVehicleCountTwoTypes(entity, ref ComponentLookupPoliceCar, ref ComponentLookupHelicopter); }
                            }
                            break;
                        
                        case BUBuildingStatusType.VehiclesPrisonVan:
                            if (TryGetComponentDataWithUpgrades(entity, prefab, ref ComponentLookupPrisonData, out Game.Prefabs.PrisonData prisonData))
                            {
                                capacity = prisonData.m_PrisonVanCapacity;
                                if (CountVehiclesInUse) { used = GetOwnedVehicleCountOneType(entity, ref ComponentLookupPrisonerTransport); }
                            }
                            break;

                        case BUBuildingStatusType.VehiclesBus:
                            if (TryGetTransportDepotDataWithUpgrades(entity, prefab, Game.Prefabs.TransportType.Bus, out Game.Prefabs.TransportDepotData transportDepotData1))
                            {
                                capacity = transportDepotData1.m_VehicleCapacity;
                                if (CountVehiclesInUse) { used = GetOwnedVehicleCountAll(entity); }
                            }
                            break;

                        case BUBuildingStatusType.VehiclesTaxi:
                            if (TryGetTransportDepotDataWithUpgrades(entity, prefab, Game.Prefabs.TransportType.Taxi, out Game.Prefabs.TransportDepotData transportDepotData2))
                            {
                                capacity = transportDepotData2.m_VehicleCapacity;
                                if (CountVehiclesInUse) { used = GetOwnedVehicleCountAll(entity); }
                            }
                            break;

                        case BUBuildingStatusType.VehiclesTrain:
                            if (TryGetTransportDepotDataWithUpgrades(entity, prefab, Game.Prefabs.TransportType.Train, out Game.Prefabs.TransportDepotData transportDepotData3))
                            {
                                capacity = transportDepotData3.m_VehicleCapacity;
                                if (CountVehiclesInUse) { used = GetOwnedVehicleCountAll(entity); }
                            }
                            break;

                        case BUBuildingStatusType.VehiclesTram:
                            if (TryGetTransportDepotDataWithUpgrades(entity, prefab, Game.Prefabs.TransportType.Tram, out Game.Prefabs.TransportDepotData transportDepotData4))
                            {
                                capacity = transportDepotData4.m_VehicleCapacity;
                                if (CountVehiclesInUse) { used = GetOwnedVehicleCountAll(entity); }
                            }
                            break;

                        case BUBuildingStatusType.VehiclesSubway:
                            if (TryGetTransportDepotDataWithUpgrades(entity, prefab, Game.Prefabs.TransportType.Subway, out Game.Prefabs.TransportDepotData transportDepotData5))
                            {
                                capacity = transportDepotData5.m_VehicleCapacity;
                                if (CountVehiclesInUse) { used = GetOwnedVehicleCountAll(entity); }
                            }
                            break;

                        case BUBuildingStatusType.VehiclesPost:
                            if (TryGetComponentDataWithUpgrades(entity, prefab, ref ComponentLookupPostFacilityData, out Game.Prefabs.PostFacilityData postFacilityData))
                            {
                                capacity = postFacilityData.m_PostVanCapacity + postFacilityData.m_PostTruckCapacity;
                                if (CountVehiclesInUse) { used = GetOwnedVehicleCountOneType(entity, ref ComponentLookupPostVan); }
                            }
                            break;

                        case BUBuildingStatusType.VehiclesCargoStationTruck:
                            // Cargo stations have no upgrades to combine.
                            // Get transport company data directly.
                            if (ComponentLookupTransportCompanyData.TryGetComponent(prefab, out Game.Companies.TransportCompanyData transportCompanyData2))
                            {
                                capacity = transportCompanyData2.m_MaxTransports;
                                if (CountVehiclesInUse) { used = GetOwnedVehicleCountOneType(entity, ref ComponentLookupDeliveryTruck); }
                            }
                            break;
                    }

                    // Must have capacity.
                    if (capacity > 0L)
                    {
                        // For all but zoned buildings and parked vehicles, check if should add vehicles in maintenance.
                        if (buildingStatusType != BUBuildingStatusType.VehiclesCommercialTruck &&
                            buildingStatusType != BUBuildingStatusType.VehiclesIndustrialTruck &&
                            buildingStatusType != BUBuildingStatusType.VehiclesOfficeTruck &&
                            buildingStatusType != BUBuildingStatusType.VehiclesParked &&
                            CountVehiclesInMaintenance)
                        {
                            used += GetInMaintenanceVehicleCount(entity, prefab, capacity);
                        }

                        // Update entity color and accumulate totals.
                        UpdateEntityColor(used, capacity, infomodeActive, infomodeIndex, colors, i);
                        totalUsed     += used;
                        totalCapacity += capacity;
                    }
                    else
                    {
                        // Building has no capacity.
                        // Display zoned buildings with no capacity as 0%.
                        if (buildingStatusType == BUBuildingStatusType.VehiclesCommercialTruck ||
                            buildingStatusType == BUBuildingStatusType.VehiclesIndustrialTruck ||
                            buildingStatusType == BUBuildingStatusType.VehiclesOfficeTruck)
                        {
                            UpdateEntityColor(0L, 0L, infomodeActive, infomodeIndex, colors, i);
                        }
                    }
                }

                // Update total used and capacity data arrays.
                UpdateTotalUsedCapacity(buildingStatusType, totalUsed, totalCapacity);
            }

            /// <summary>
            /// Get count of owned vehicles that have one vehicle type.
            /// </summary>
            private int GetOwnedVehicleCountOneType<T>(Entity entity, ref ComponentLookup<T> vehicleType) where T : unmanaged, IComponentData
            {
                // Check if entity has owned vehicle buffer.
                if (BufferLookupOwnedVehicle.TryGetBuffer(entity, out DynamicBuffer<Game.Vehicles.OwnedVehicle> dynamicBuffer))
                {
                    // Dynamic buffer must be created.
                    if (dynamicBuffer.IsCreated)
                    {
                        // Do each owned vehicle.
                        int count = 0;
                        foreach (Game.Vehicles.OwnedVehicle ownedVehicle in dynamicBuffer)
                        {
                            // Owned vehicle must not be parked.
                            Entity vehicle = ownedVehicle.m_Vehicle;
                            if (!ComponentLookupParkedCar.HasComponent(vehicle))
                            {
                                // Check if owned vehicle is the vehicle type.
                                if (vehicleType.HasComponent(vehicle))
                                {
                                    count++;
                                }
                            }
                        }

                        // Return counted vehicles.
                        return count;
                    }
                }

                // No owned vehicles.
                return 0;
            }

            /// <summary>
            /// Get count of owned vehicles that have two vehicle types.
            /// </summary>
            private int GetOwnedVehicleCountTwoTypes<T1, T2>(Entity entity, ref ComponentLookup<T1> vehicleType1, ref ComponentLookup<T2> vehicleType2) where T1 : unmanaged, IComponentData where T2 : unmanaged, IComponentData
            {
                // Check if entity has owned vehicle buffer.
                if (BufferLookupOwnedVehicle.TryGetBuffer(entity, out DynamicBuffer<Game.Vehicles.OwnedVehicle> dynamicBuffer))
                {
                    // Dynamic buffer must be created.
                    if (dynamicBuffer.IsCreated)
                    {
                        // Do each owned vehicle.
                        int count = 0;
                        foreach (Game.Vehicles.OwnedVehicle ownedVehicle in dynamicBuffer)
                        {
                            // Owned vehicle must not be parked.
                            Entity vehicle = ownedVehicle.m_Vehicle;
                            if (!ComponentLookupParkedCar.HasComponent(vehicle))
                            {
                                // Check if owned vehicle is both vehicle types.
                                if (vehicleType1.HasComponent(vehicle) && vehicleType2.HasComponent(vehicle))
                                {
                                    count++;
                                }
                            }
                        }

                        // Return counted vehicles.
                        return count;
                    }
                }

                // No owned vehicles.
                return 0;
            }

            /// <summary>
            /// Get count of owned vehicles that have one vehicle type and not another vehicle type.
            /// </summary>
            private int GetOwnedVehicleCountOneTypeNotType<T1, T2>(Entity entity, ref ComponentLookup<T1> vehicleType, ref ComponentLookup<T2> notVehicleType) where T1 : unmanaged, IComponentData where T2 : unmanaged, IComponentData
            {
                // Check if entity has owned vehicle buffer.
                if (BufferLookupOwnedVehicle.TryGetBuffer(entity, out DynamicBuffer<Game.Vehicles.OwnedVehicle> dynamicBuffer))
                {
                    // Dynamic buffer must be created.
                    if (dynamicBuffer.IsCreated)
                    {
                        // Do each owned vehicle.
                        int count = 0;
                        foreach (Game.Vehicles.OwnedVehicle ownedVehicle in dynamicBuffer)
                        {
                            // Owned vehicle must not be parked.
                            Entity vehicle = ownedVehicle.m_Vehicle;
                            if (!ComponentLookupParkedCar.HasComponent(vehicle))
                            {
                                // Check if owned vehicle is vehicle type and not vehicle type.
                                if (vehicleType.HasComponent(vehicle) && !notVehicleType.HasComponent(vehicle))
                                {
                                    count++;
                                }
                            }
                        }

                        // Return counted vehicles.
                        return count;
                    }
                }

                // No owned vehicles.
                return 0;
            }

            /// <summary>
            /// Get count of owned vehicles for fire engine.
            /// </summary>
            private int GetOwnedVehicleCountFireEngine(Entity entity, FireEngineType fireEngineType)
            {
                // Check if entity has owned vehicle buffer.
                if (BufferLookupOwnedVehicle.TryGetBuffer(entity, out DynamicBuffer<Game.Vehicles.OwnedVehicle> dynamicBuffer))
                {
                    // Dynamic buffer must be created.
                    if (dynamicBuffer.IsCreated)
                    {
                        // Do each owned vehicle.
                        int count = 0;
                        foreach (Game.Vehicles.OwnedVehicle ownedVehicle in dynamicBuffer)
                        {
                            // Owned vehicle must not be parked.
                            Entity vehicle = ownedVehicle.m_Vehicle;
                            if (!ComponentLookupParkedCar.HasComponent(vehicle))
                            {
                                // Fire engine type to check for.
                                switch (fireEngineType)
                                {
                                    case FireEngineType.FireEngine:
                                        // Fire engine has owned vehicle is fire engine, and is not disaster response, and is not helicopter.
                                        if (ComponentLookupFireEngine.TryGetComponent(vehicle, out Game.Vehicles.FireEngine fireEngine1) &&
                                            (fireEngine1.m_State & Game.Vehicles.FireEngineFlags.DisasterResponse) == 0 &&
                                            !ComponentLookupHelicopter.HasComponent(vehicle))
                                        {
                                            count++;
                                        }
                                        break;

                                    case FireEngineType.FireHelicopter:
                                        // Fire helicopter has owned vehicle is fire engine, and is not disaster response, and is helicopter.
                                        if (ComponentLookupFireEngine.TryGetComponent(vehicle, out Game.Vehicles.FireEngine fireEngine2) &&
                                            (fireEngine2.m_State & Game.Vehicles.FireEngineFlags.DisasterResponse) == 0 &&
                                            ComponentLookupHelicopter.HasComponent(vehicle))
                                        {
                                            count++;
                                        }
                                        break;

                                    case FireEngineType.DisasterResponse:
                                        // Disaster response vehicle has owned vehicle is fire engine, and is disaster response, and is not helicopter.
                                        if (ComponentLookupFireEngine.TryGetComponent(vehicle, out Game.Vehicles.FireEngine fireEngine3) &&
                                            (fireEngine3.m_State & Game.Vehicles.FireEngineFlags.DisasterResponse) != 0 &&
                                            !ComponentLookupHelicopter.HasComponent(vehicle))
                                        {
                                            count++;
                                        }
                                        break;
                                }
                            }
                        }

                        // Return counted vehicles.
                        return count;
                    }
                }

                // No owned vehicles.
                return 0;
            }

            /// <summary>
            /// Get count of owned vehicles.
            /// </summary>
            private int GetOwnedVehicleCountAll(Entity entity)
            {
                // Check if entity has owned vehicle buffer.
                if (BufferLookupOwnedVehicle.TryGetBuffer(entity, out DynamicBuffer<Game.Vehicles.OwnedVehicle> dynamicBuffer))
                {
                    // Dynamic buffer must be created.
                    if (dynamicBuffer.IsCreated)
                    {
                        // Do each owned vehicle.
                        int count = 0;
                        foreach (Game.Vehicles.OwnedVehicle ownedVehicle in dynamicBuffer)
                        {
                            // Owned vehicle must not be parked.
                            Entity vehicle = ownedVehicle.m_Vehicle;
                            if (!ComponentLookupParkedCar.HasComponent(vehicle) && !ComponentLookupParkedTrain.HasComponent(vehicle))
                            {
                                count++;
                            }
                        }

                        // Return counted vehicles.
                        return count;
                    }
                }

                // No owned vehicles.
                return 0;
            }

            /// <summary>
            /// Get transport depot data from prefab (if any) with upgrades from entity (if any).
            /// </summary>
            private bool TryGetTransportDepotDataWithUpgrades(Entity entity, Entity prefab, Game.Prefabs.TransportType transportType, out Game.Prefabs.TransportDepotData transportDepotData)
            {
                // Try to get the transport depot data directly from the prefab.
		        if (ComponentLookupTransportDepotData.TryGetComponent(prefab, out Game.Prefabs.TransportDepotData tempTransportDepotData))
                {
                    // Check if transport depot is correct type.
                    if (tempTransportDepotData.m_TransportType == transportType)
                    {
                        // Use the transport depot data.
                        transportDepotData = tempTransportDepotData;

		                // Check if entity has any installed upgrades.
                        // Logic adapted from Game.Prefabs.UpgradeUtils.TryCombineData.
        		        if (BufferLookupInstalledUpgrade.TryGetBuffer(entity, out DynamicBuffer<Game.Buildings.InstalledUpgrade> installedUpgrades))
                        {
                            // Do each installed upgrade.
		                    for (int i = 0; i < installedUpgrades.Length; i++)
		                    {
                                // Installed upgrade must not be inactive and prefab of installed upgrade must have transport depot data.
			                    Game.Buildings.InstalledUpgrade installedUpgrade = installedUpgrades[i];
			                    if (!Game.Buildings.BuildingUtils.CheckOption(installedUpgrade, Game.Buildings.BuildingOption.Inactive) && 
                                    ComponentLookupPrefabRef.TryGetComponent(installedUpgrade.m_Upgrade, out Game.Prefabs.PrefabRef installedUpgradePrefabRef) && 
                                    ComponentLookupTransportDepotData.TryGetComponent(installedUpgradePrefabRef.m_Prefab, out Game.Prefabs.TransportDepotData installedUpgradeComponentData))
			                    {
                                    // Combine previous component data with component data from the installed upgrade.
				                    transportDepotData.Combine(installedUpgradeComponentData);
			                    }
		                    }
                        }

                        // Transport depot has correct transport type.
                        return true;
                    }
                }
                
                // Transport depot does not have correct transport type.
                transportDepotData = default;
                return false;
            }

            /// <summary>
            /// Get count of vehicles in maintenance.
            /// </summary>
            private int GetInMaintenanceVehicleCount(Entity entity, Entity prefab, int capacity)
            {
                // Get building efficiency.
                // Logic to get building efficiency adapted from Game.UI.InGame.VehicleUIUtils.GetAvailableVehicles()
                // which if an efficiency buffer is available calls Game.Buildings.BuildingUtils.GetEfficiency().
                float efficiency = 0f;
		        if (BufferLookupEfficiency.TryGetBuffer(entity, out DynamicBuffer<Game.Buildings.Efficiency> bufferEfficiency) && bufferEfficiency.IsCreated)
		        {
                    efficiency = Mathf.Min(GetBuildingEfficiency(bufferEfficiency), 1f);
		        }

                // Logic to get available vehicles is adapted from Game.UI.InGame.VehicleUIUtils.GetAvailableVehicles(),
                // which calls BuildingUtils.GetVehicleCapacity().
                // BuildingUtils.GetVehicleCapacity is basically efficiency times capacity with a range of 1 to capacity.
                // Therefore, when efficiency is at least 100%, available vehicles is equal to capacity.
                int availableVehicles = Game.Buildings.BuildingUtils.GetVehicleCapacity(efficiency, capacity);

                // Logic to compute vehicles in maintenance is adapted from VehiclesSection in index.js
                // where vehicles in maintenance is simply vehicle capacity minus available vehicles.
                // It is unclear why this is vehicles in maintenance.
                // Note than when efficiency is at least 100%, in maintenance vehicles will be zero.
                return capacity - availableVehicles;
            }

            /// <summary>
            /// Get building efficiency from buffer.
            /// </summary>
            private float GetBuildingEfficiency(DynamicBuffer<Game.Buildings.Efficiency> bufferEfficiency)
            {
                // Logic to get building efficiency is adapted from overloaded method BuildingUtils.GetEfficiency() for a buffer of Efficiency.
                // BuildingUtils.GetEfficiency() cannot be used directly because one of the overloads references Span<float>,
                // which for unknown reasons cannot be resolved by Visual Studio.
                // This unresolved overload causes a compile error, even though that is not the overload which would actually be used.

                // Do each efficiency in the buffer.
                float efficiency = 1f;
                foreach (Game.Buildings.Efficiency item in bufferEfficiency)
                {
                    // Efficiency is multiplicative.
                    efficiency *= math.max(0f, item.m_Efficiency);
                }

                // If efficiency is zero, return zero.
                if (efficiency == 0f)
                {
                    return 0f;
                }

                // Round efficiency to 2 decimal places and make sure it is at least 0.01 (i.e. 1%).
                return math.max(0.01f, math.round(100f * efficiency) / 100f);
            }

            /// <summary>
            /// Get used and capacity of parked vehicles.
            /// </summary>
            private void GetParkedVehicles(Entity entity, out int used, out int capacity)
            {
                // Logic adapted from Game.UI.InGame.ParkingSection.OnProcess().

                // Set defaults
                used = 0;
                capacity = 0;

                // Get parked vehicles from sublanes, subnets, and subobjects.
                if (BufferLookupSubLane.TryGetBuffer(entity, out DynamicBuffer<Game.Net.SubLane> bufferSubLanes))
		        {
			        GetParkedVehiclesFromSubLanes(bufferSubLanes, ref used, ref capacity);
		        }
                if (BufferLookupSubNet.TryGetBuffer(entity, out DynamicBuffer<Game.Net.SubNet> bufferSubNets))
		        {
			        GetParkedVehiclesFromSubNets(bufferSubNets, ref used, ref capacity);
		        }
                if (BufferLookupSubObject.TryGetBuffer(entity, out DynamicBuffer<Game.Objects.SubObject> bufferSubObjects))
		        {
			        GetParkedVehiclesFromSubObjects(bufferSubObjects, ref used, ref capacity);
		        }
            }

            /// <summary>
            /// Get used and capacity of parked vehicles from sublanes.
            /// </summary>
	        private void GetParkedVehiclesFromSubLanes(DynamicBuffer<Game.Net.SubLane> subLanes, ref int used, ref int capacity)
	        {
                // Logic adapted from Game.UI.InGame.ParkingSection.CheckParkingLanes() for a sublanes buffer.

                // Do each sublane.
		        for (int i = 0; i < subLanes.Length; i++)
		        {
                    // Get the sublane.
			        Entity subLane = subLanes[i].m_SubLane;

                    // Check if sublane has a parking lane.
                    if (ComponentLookupParkingLane.TryGetComponent(subLane, out Game.Net.ParkingLane parkingLane))
			        {
                        // Skip virtual parking lane.
				        if ((parkingLane.m_Flags & Game.Net.ParkingLaneFlags.VirtualLane) != 0)
				        {
					        continue;
				        }

                        // Get sublane prefab.
                        if (ComponentLookupPrefabRef.TryGetComponent(subLane, out Game.Prefabs.PrefabRef subLanePrefabRef))
                        {
                            // Get parking lane data from sublane prefab.
                            if (ComponentLookupParkingLaneData.TryGetComponent(subLanePrefabRef.m_Prefab, out Game.Prefabs.ParkingLaneData parkingLaneData))
                            {
                                // Check if parking lane has a valid slot interval.
				                if (parkingLaneData.m_SlotInterval != 0f)
				                {
                                    // Get sublane curve.
                                    if (ComponentLookupCurve.TryGetComponent(subLane, out Game.Net.Curve curve))
                                    {
                                        // Use sublane curve length and slot interval to compute capacity.
					                    capacity += (int)math.floor((curve.m_Length + 0.01f) / parkingLaneData.m_SlotInterval);
                                    }
				                }
				                else
				                {
                                    // For invalid slot interval, don't wreck capacity counter
                                    // like game's calculation does here (commented out) for single a entity.
					                // capacity = -1000000;
				                }

                                // Get lane objects in sublane.
                                if (BufferLookupLaneObject.TryGetBuffer(subLane, out DynamicBuffer<Game.Net.LaneObject> laneObjects))
                                {
                                    // Do each lane object.
				                    for (int j = 0; j < laneObjects.Length; j++)
				                    {
                                        // Count lane objects that are parked car.
                                        if (ComponentLookupParkedCar.HasComponent(laneObjects[j].m_LaneObject))
					                    {
                                            used++;
					                    }
				                    }
                                }
                            }
                        }
			        }

                    // Check if sublane has a garage lane.
                    else if (ComponentLookupGarageLane.TryGetComponent(subLane, out Game.Net.GarageLane garageLane))
			        {
                        // Get used and capacity from garage lane.
				        capacity += garageLane.m_VehicleCapacity;
                        used     += garageLane.m_VehicleCount; 
			        }
		        }
	        }

            /// <summary>
            /// Get used and capacity of parked vehicles from subnets.
            /// </summary>
	        private void GetParkedVehiclesFromSubNets(DynamicBuffer<Game.Net.SubNet> subNets, ref int used, ref int capacity)
	        {
                // Logic adapted from Game.UI.InGame.ParkingSection.CheckParkingLanes() for a subnets buffer.

                // Do each subnet.
		        for (int i = 0; i < subNets.Length; i++)
		        {
                    // Do any sublanes of the subnet.
                    if (BufferLookupSubLane.TryGetBuffer(subNets[i].m_SubNet, out DynamicBuffer<Game.Net.SubLane> bufferSubLanes))
			        {
				        GetParkedVehiclesFromSubLanes(bufferSubLanes, ref used, ref capacity);
			        }
		        }
	        }

            /// <summary>
            /// Get used and capacity of parked vehicles from subobjects.
            /// </summary>
	        private void GetParkedVehiclesFromSubObjects(DynamicBuffer<Game.Objects.SubObject> subObjects, ref int used, ref int capacity)
	        {
                // Logic adapted from Game.UI.InGame.ParkingSection.CheckParkingLanes() for a subobjects buffer.

                // Do each subobject.
		        for (int i = 0; i < subObjects.Length; i++)
		        {
                    // Do any sublanes of the subobject.
			        Entity subObject = subObjects[i].m_SubObject;
                    if (BufferLookupSubLane.TryGetBuffer(subObject, out DynamicBuffer<Game.Net.SubLane> bufferSubLanes))
			        {
				        GetParkedVehiclesFromSubLanes(bufferSubLanes, ref used, ref capacity);
			        }

                    // Do any subobjects of the subobject.
                    // NOTICE:  This is a RECURSIVE call!
                    if (BufferLookupSubObject.TryGetBuffer(subObject, out DynamicBuffer<Game.Objects.SubObject> bufferSubObjects))
			        {
				        GetParkedVehiclesFromSubObjects(bufferSubObjects, ref used, ref capacity);
			        }
		        }
	        }
        }
    }
}
