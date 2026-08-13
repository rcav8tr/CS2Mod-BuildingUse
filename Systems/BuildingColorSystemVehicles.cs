using Game;
using Game.Buildings;
using Game.Companies;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BuildingUse
{
    /// <summary>
    /// Partial system to set building colors for Vehicles infoview.
    /// </summary>
    public partial class BuildingColorSystem : GameSystemBase
    {
        /// <summary>
        /// Partial job struct to set the color of each main building for Vehicles infoview.
        /// Burst compilation for this partial struct is defined by the main UpdateColorsJobMainBuilding struct.
        /// </summary>
        private partial struct UpdateColorsJobMainBuilding : IJobChunk
        {
            /// <summary>
            /// Do a main building for Vehicles infoview.
            /// </summary>
            private void DoBuildingVehicles(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Do building status types for service first.
                DoBuildingVehiclesService(in mainBuildingAndUpgrades, ref color);

                // Do building status types for companies, which need different handling than service.
                // Do in descending order by building status type.
                DoBuildingVehiclesCompany(in mainBuildingAndUpgrades, ref color, BUBuildingStatusType.VehiclesOfficeTruck);
                DoBuildingVehiclesCompany(in mainBuildingAndUpgrades, ref color, BUBuildingStatusType.VehiclesWarehouseTruck);
                DoBuildingVehiclesCompany(in mainBuildingAndUpgrades, ref color, BUBuildingStatusType.VehiclesIndustrialTruck);
                DoBuildingVehiclesCompany(in mainBuildingAndUpgrades, ref color, BUBuildingStatusType.VehiclesExtractorTruck);
                DoBuildingVehiclesCompany(in mainBuildingAndUpgrades, ref color, BUBuildingStatusType.VehiclesCommercialTruck);

                // Do residential last, which needs different handling than the above.
                DoBuildingVehiclesResidential(in mainBuildingAndUpgrades, ref color);
            }

            /// <summary>
            /// Get used and capacity of parked vehicles.
            /// </summary>
            private void DoBuildingVehiclesResidential(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Do each main building and upgrade.
                int used     = 0;
                int capacity = 0;
                bool hasCorrectProperty = false;
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Check if building has correct property.
                    Entity entity = mainBuildingOrUpgrade.Entity;
                    if (BuildingHasResidential(entity))
                    {
                        // Building has the correct property.
                        hasCorrectProperty = true;

                        // Get the renters.
                        if (BufferLookupRenter.TryGetBuffer(entity, out DynamicBuffer<Renter> renters) &&
                            renters.IsCreated)
                        {
                            // Do each renter.
                            foreach (Renter renter in renters)
                            {
                                // Renter must be a household.
                                // Residential vehicles are owned by households.
                                if (ComponentLookupHousehold.HasComponent(renter.m_Renter))
                                {
                                    // Get the owned vehicles for the household.
                                    // A household will usually have 0 or 1 cars, infrequently 2, very rarely 3, and never seen 4 or more.
                                    // Note that bicycles are not stored in the OwnedVehicle buffer.
                                    if (BufferLookupOwnedVehicle.TryGetBuffer(renter.m_Renter, out DynamicBuffer<OwnedVehicle> vehicles) &&
                                        vehicles.IsCreated)
                                    {
                                        // Residential cars are present in the buffer even if not in use.
                                        // So capacity is buffer length.
                                        capacity += vehicles.Length;

                                        // Do each owned vehicle.
                                        foreach (OwnedVehicle vehicle in vehicles)
                                        {
                                            // A vehicle not parked is in use.
                                            if (!ComponentLookupParkedCar.HasComponent(vehicle.m_Vehicle))
                                            {
                                                used++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // If main building plus upgrades has correct property,
                // update entity color and total used and capacity, even if no houseshold.
                if (hasCorrectProperty)
                {
                    UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesResidentialCar, used, capacity, ref color);
                }
            }

            /// <summary>
            /// Do a main building and upgrades for Vehicles infoview for a company.
            /// </summary>
            private void DoBuildingVehiclesCompany(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color, BUBuildingStatusType buildingStatusType)
            {
                // Do each main building and upgrade.
                int used     = 0;
                int capacity = 0;
                bool hasCorrectProperty = false;
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Check if building has correct property.
                    Entity entity = mainBuildingOrUpgrade.Entity;
                    if ((buildingStatusType == BUBuildingStatusType.VehiclesCommercialTruck && BuildingHasCommercial(entity)) ||
                        (buildingStatusType == BUBuildingStatusType.VehiclesExtractorTruck  && BuildingHasExtractor (entity)) ||
                        (buildingStatusType == BUBuildingStatusType.VehiclesIndustrialTruck && BuildingHasIndustrial(entity)) ||
                        (buildingStatusType == BUBuildingStatusType.VehiclesWarehouseTruck  && BuildingHasStorage   (entity)) ||
                        (buildingStatusType == BUBuildingStatusType.VehiclesOfficeTruck     && BuildingHasOffice    (entity)))
                    {
                        // Building has the correct property.
                        hasCorrectProperty = true;

                        // Get transport company data from company prefab.
                        if (TryGetCompany(entity, out Entity companyEntity) &&
                            ComponentLookupPrefabRef.TryGetComponent(companyEntity, out PrefabRef companyPrefabRef) &&
                            ComponentLookupTransportCompanyData.TryGetComponent(companyPrefabRef.m_Prefab, out TransportCompanyData transportCompanyData))
                        {
                            // Get capacity.
                            capacity += transportCompanyData.m_MaxTransports;

                            // If needed, get vehicle buffer from company, not from building.
                            if (CountVehiclesInUse &&
                                BufferLookupOwnedVehicle.TryGetBuffer(companyEntity, out DynamicBuffer<OwnedVehicle> ownedVehicles) &&
                                ownedVehicles.IsCreated)
                            {
                                // Do each owned vehicle.
                                foreach (OwnedVehicle ownedVehicle in ownedVehicles)
                                {
                                    // Owned vehicle must not be parked.
                                    Entity vehicle = ownedVehicle.m_Vehicle;
                                    if (!ComponentLookupParkedCar.HasComponent(vehicle))
                                    {
                                        // Companies use only delivery trucks.
                                        // Check if owned vehicle is a delivery truck.
                                        if (ComponentLookupDeliveryTruck.HasComponent(vehicle))
                                        {
                                            used++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Companies do not have vehicles in maintenance.

                // If main building plus upgrades has the correct property,
                // update building color and total used and capacity even if no company.
                if (hasCorrectProperty)
                {
                    UpdateEntityColorAndTotalUsedCapacity(buildingStatusType, used, capacity, ref color);
                }
            }

            /// <summary>
            /// Do a main building and upgrades for Vehicles infoview for Service.
            /// </summary>
            private void DoBuildingVehiclesService(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Logic adapted from Game.UI.InGame.VehiclesSection.OnProcess().

                // Get main building entity and prefab.
                // The first building in the list is always the main building.
                Entity mainBuildingEntity = mainBuildingAndUpgrades[0].Entity;
                Entity mainBuildingPrefab = mainBuildingAndUpgrades[0].Prefab;

                // Determine vehicle capacity for each service building status type except transport depots.
                int capacityRoadMaintenance   = 0;
                int capacityAmbulance         = 0;
                int capacityMedicalHelicopter = 0;
                int capacityHearse            = 0;
                int capacityGarbageTruck      = 0;
                int capacityFireEngine        = 0;
                int capacityFireHelicopter    = 0;
                int capacityDisasterResponse  = 0;
                int capacityEvacuationBus     = 0;
                int capacityPoliceCar         = 0;
                int capacityPoliceHelicopter  = 0;
                int capacityPrisonVan         = 0;
                int capacityParkMaintenance   = 0;
                int capacityPost              = 0;
                int capacityCargoStationTruck = 0;

                // Capacity is obtained from main building and upgrades.
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Get main building or upgrade entity and prefab.
                    Entity entity = mainBuildingOrUpgrade.Entity;
                    Entity prefab = mainBuildingOrUpgrade.Prefab;

                    // For RoadMaintenance building must have RoadMaintenance.
                    if (BuildingHasRoadMaintenance(prefab, entity, out MaintenanceDepotData roadMaintenanceDepotData))
                    {
                        capacityRoadMaintenance += roadMaintenanceDepotData.m_VehicleCapacity;
                    }

                    // For Ambulance, building must have Hospital.
                    bool buildingHasHospital = BuildingHasHospital(prefab, out HospitalData hospitalData);
                    if (buildingHasHospital)
                    {
                        capacityAmbulance += hospitalData.m_AmbulanceCapacity;
                    }

                    // For MedicalHelicopter, building must have Hospital.
                    if (buildingHasHospital)
                    {
                        capacityMedicalHelicopter += hospitalData.m_MedicalHelicopterCapacity;
                    }

                    // For Hearse, building must have DeathcareFacility.
                    if (BuildingHasDeathcareFacility(prefab, out DeathcareFacilityData deathcareFacilityData))
                    {
                        capacityHearse += deathcareFacilityData.m_HearseCapacity;
                    }

                    // For GarbageTruck, building must have GarbageFacility.
                    if (BuildingHasGarbageFacility(prefab, out GarbageFacilityData garbageFacilityData))
                    {
                        capacityGarbageTruck += garbageFacilityData.m_VehicleCapacity;
                    }

                    // For FireEngine, building must have FireStation.
                    bool buildingHasFireStation = BuildingHasFireStation(prefab, out FireStationData fireStationData);
                    if (buildingHasFireStation)
                    {
                        capacityFireEngine += fireStationData.m_FireEngineCapacity;
                    }

                    // For FireHelicopter, building must have FireStation.
                    if (buildingHasFireStation)
                    {
                        capacityFireHelicopter += fireStationData.m_FireHelicopterCapacity;
                    }

                    // For DisasterResponse, building must have FireStation.
                    if (buildingHasFireStation)
                    {
                        // Note that Game.UI.InGame.VehiclesSection.OnProcess() does NOT include disaster response capacity.
                        // Only fire engine and fire helicopter capacities are included for a building.
                        // But disaster response IS included here for this mod.
                        // So the total vehicle capacity for fire stations in the game will be different than
                        // the sum of the fire engine, fire helicopter, and disaster response capacities in this mod.
                        // This error in the game logic also adversely affects the in maintenance calculations below.
                        capacityDisasterResponse += fireStationData.m_DisasterResponseCapacity;
                    }

                    // For EvacuationBus, building must have EmergencyShelter.
                    if (BuildingHasEmergencyShelter(prefab, out EmergencyShelterData emergencyShelterData))
                    {
                        capacityEvacuationBus += emergencyShelterData.m_VehicleCapacity;
                    }

                    // For PoliceCar, building must have PoliceStation.
                    bool buildingHasPoliceStation = BuildingHasPoliceStation(prefab, out PoliceStationData policeStationData);
                    if (buildingHasPoliceStation)
                    {
                        capacityPoliceCar += policeStationData.m_PatrolCarCapacity;
                    }

                    // For PoliceHelicopter, building must have PoliceStation.
                    if (buildingHasPoliceStation)
                    {
                        capacityPoliceHelicopter += policeStationData.m_PoliceHelicopterCapacity;
                    }

                    // For PrisonVan, building must have Prison.
                    if (BuildingHasPrison(prefab, out PrisonData prisonData))
                    {
                        capacityPrisonVan += prisonData.m_PrisonVanCapacity;
                    }

                    // For ParkMaintenance, building must have ParkMaintenance.
                    if (BuildingHasParkMaintenance(prefab, entity, out MaintenanceDepotData parkMaintenanceDepotData))
                    {
                        capacityParkMaintenance += parkMaintenanceDepotData.m_VehicleCapacity;
                    }

                    // For Post, building must have PostFacility.
                    if (BuildingHasPostFacility(prefab, out PostFacilityData postFacilityData))
                    {
                        capacityPost += postFacilityData.m_PostVanCapacity + postFacilityData.m_PostTruckCapacity;
                    }

                    // For CargoStationTruck, building must have TransportCompanyData.
                    // Companies have TransportCompanyData on the company.
                    // Service buildings have TransportCompanyData directly on the building.
                    if (ComponentLookupTransportCompanyData.TryGetComponent(prefab, out TransportCompanyData transportCompanyData))
                    {
                        capacityCargoStationTruck += transportCompanyData.m_MaxTransports;
                    }
                }

                // For transport depot buildings, the main building must have TransportDepotData to define the single transport type.
                // Upgrades can only add capacity for that single transport type.
                // Upgrades cannot change the transport type or specify new or additional transport types.
                // This is necessary because the TransportDepotData.m_TransportType is set to None on upgrades.
                // This means upgrades cannot be used to have more than one transport type in a transport depot building.
                int capacityBus    = 0;
                int capacityTaxi   = 0;
                int capacityTrain  = 0;
                int capacityTram   = 0;
                int capacitySubway = 0;
                int capacityFerry  = 0;
                bool mainBuildingHasTransportDepot = BuildingHasTransportDepot(mainBuildingPrefab, out TransportDepotData mainTransportDepotData);
                if (mainBuildingHasTransportDepot)
                {
                    // Get vehicle capacity from main building and upgrades and assign to the transport type defined by the main building.
                    TransportType mainBuildingTransportType = mainTransportDepotData.m_TransportType;
                    foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                    {
                        if (BuildingHasTransportDepot(mainBuildingOrUpgrade.Prefab, out TransportDepotData transportDepotData))
                        {
                            switch (mainBuildingTransportType)
                            {
                                case TransportType.Bus:    capacityBus    += transportDepotData.m_VehicleCapacity; break;
                                case TransportType.Taxi:   capacityTaxi   += transportDepotData.m_VehicleCapacity; break;
                                case TransportType.Train:  capacityTrain  += transportDepotData.m_VehicleCapacity; break;
                                case TransportType.Tram:   capacityTram   += transportDepotData.m_VehicleCapacity; break;
                                case TransportType.Subway: capacitySubway += transportDepotData.m_VehicleCapacity; break;
                                case TransportType.Ferry:  capacityFerry  += transportDepotData.m_VehicleCapacity; break;
                            }
                        }
                    }
                }

                // Having capacity is the indication that the building has the building status type.
                bool hasRoadMaintenance   = capacityRoadMaintenance   > 0;
                bool hasAmbulance         = capacityAmbulance         > 0;
                bool hasMedicalHelicopter = capacityMedicalHelicopter > 0;
                bool hasHearse            = capacityHearse            > 0;
                bool hasGarbageTruck      = capacityGarbageTruck      > 0;
                bool hasFireEngine        = capacityFireEngine        > 0;
                bool hasFireHelicopter    = capacityFireHelicopter    > 0;
                bool hasDisasterResponse  = capacityDisasterResponse  > 0;
                bool hasEvacuationBus     = capacityEvacuationBus     > 0;
                bool hasPoliceCar         = capacityPoliceCar         > 0;
                bool hasPoliceHelicopter  = capacityPoliceHelicopter  > 0;
                bool hasPrisonVan         = capacityPrisonVan         > 0;
                bool hasBus               = capacityBus               > 0;
                bool hasTaxi              = capacityTaxi              > 0;
                bool hasTrain             = capacityTrain             > 0;
                bool hasTram              = capacityTram              > 0;
                bool hasSubway            = capacitySubway            > 0;
                bool hasFerry             = capacityFerry             > 0;
                bool hasParkMaintenance   = capacityParkMaintenance   > 0;
                bool hasPost              = capacityPost              > 0;
                bool hasCargoStationTruck = capacityCargoStationTruck > 0;

                // Determine vehicles in maintenance for each building status type.
                int inMaintRoadMaintenance   = 0;
                int inMaintAmbulance         = 0;
                int inMaintMedicalHelicopter = 0;
                int inMaintHearse            = 0;
                int inMaintGarbageTruck      = 0;
                int inMaintFireEngine        = 0;
                int inMaintFireHelicopter    = 0;
                int inMaintDisasterResponse  = 0;
                int inMaintEvacuationBus     = 0;
                int inMaintPoliceCar         = 0;
                int inMaintPoliceHelicopter  = 0;
                int inMaintPrisonVan         = 0;
                int inMaintBus               = 0;
                int inMaintTaxi              = 0;
                int inMaintTrain             = 0;
                int inMaintTram              = 0;
                int inMaintSubway            = 0;
                int inMaintFerry             = 0;
                int inMaintParkMaintenance   = 0;
                int inMaintPost              = 0;
                int inMaintCargoStationTruck = 0;

                // Include vehicles in maintenance only if being counted.
                if (CountVehiclesInMaintenance)
                {
                    // Logic to compute vehicles in maintenance is adapted from VehiclesSection in index.js
                    // where vehicles in maintenance is vehicle capacity minus available vehicles.
                    // Vehicles in maintenance is basically vehicles NOT available.

                    // Logic to get available vehicles is adapted from Game.UI.InGame.VehicleUIUtils.GetAvailableVehicles(),
                    // which calls BuildingUtils.GetVehicleCapacity() with the building efficiency and the specific vehicle capacity.
                    // BuildingUtils.GetVehicleCapacity is basically efficiency times capacity clamped to a range of 1 to capacity.
                    
                    // When efficiency <  100%, then available vehicles <  capacity and the building will have some vehicles in maintenance.
                    // When efficiency >= 100%, then available vehicles == capacity and the building will have zero vehicles in maintenance.

                    // Get main building efficiency.
                    // Logic adapted from Game.UI.InGame.VehicleUIUtils.GetAvailableVehicles()
                    float efficiency = 0f;
                    if ((BufferLookupEfficiency.TryGetBuffer(mainBuildingEntity, out DynamicBuffer<Efficiency> bufferEfficiency) &&
                         bufferEfficiency.IsCreated))
                    {
                        efficiency = math.min(BuildingUtils.GetEfficiency(bufferEfficiency), 1f);
                        efficiency = math.min(BuildingUtils.GetImmediateEfficiency(bufferEfficiency), efficiency);
                    }

                    // Do each building status type separately.
                    inMaintRoadMaintenance   = capacityRoadMaintenance   - BuildingUtils.GetVehicleCapacity(efficiency, capacityRoadMaintenance  );
                    inMaintAmbulance         = capacityAmbulance         - BuildingUtils.GetVehicleCapacity(efficiency, capacityAmbulance        );
                    inMaintMedicalHelicopter = capacityMedicalHelicopter - BuildingUtils.GetVehicleCapacity(efficiency, capacityMedicalHelicopter);
                    inMaintHearse            = capacityHearse            - BuildingUtils.GetVehicleCapacity(efficiency, capacityHearse           );
                    inMaintGarbageTruck      = capacityGarbageTruck      - BuildingUtils.GetVehicleCapacity(efficiency, capacityGarbageTruck     );
                    inMaintFireEngine        = capacityFireEngine        - BuildingUtils.GetVehicleCapacity(efficiency, capacityFireEngine       );
                    inMaintFireHelicopter    = capacityFireHelicopter    - BuildingUtils.GetVehicleCapacity(efficiency, capacityFireHelicopter   );
                    inMaintDisasterResponse  = capacityDisasterResponse  - BuildingUtils.GetVehicleCapacity(efficiency, capacityDisasterResponse );
                    inMaintEvacuationBus     = capacityEvacuationBus     - BuildingUtils.GetVehicleCapacity(efficiency, capacityEvacuationBus    );
                    inMaintPoliceCar         = capacityPoliceCar         - BuildingUtils.GetVehicleCapacity(efficiency, capacityPoliceCar        );
                    inMaintPoliceHelicopter  = capacityPoliceHelicopter  - BuildingUtils.GetVehicleCapacity(efficiency, capacityPoliceHelicopter );
                    inMaintPrisonVan         = capacityPrisonVan         - BuildingUtils.GetVehicleCapacity(efficiency, capacityPrisonVan        );
                    inMaintBus               = capacityBus               - BuildingUtils.GetVehicleCapacity(efficiency, capacityBus              );
                    inMaintTaxi              = capacityTaxi              - BuildingUtils.GetVehicleCapacity(efficiency, capacityTaxi             );
                    inMaintTrain             = capacityTrain             - BuildingUtils.GetVehicleCapacity(efficiency, capacityTrain            );
                    inMaintTram              = capacityTram              - BuildingUtils.GetVehicleCapacity(efficiency, capacityTram             );
                    inMaintSubway            = capacitySubway            - BuildingUtils.GetVehicleCapacity(efficiency, capacitySubway           );
                    inMaintFerry             = capacityFerry             - BuildingUtils.GetVehicleCapacity(efficiency, capacityFerry            );
                    inMaintParkMaintenance   = capacityParkMaintenance   - BuildingUtils.GetVehicleCapacity(efficiency, capacityParkMaintenance  );
                    inMaintPost              = capacityPost              - BuildingUtils.GetVehicleCapacity(efficiency, capacityPost             );
                    inMaintCargoStationTruck = capacityCargoStationTruck - BuildingUtils.GetVehicleCapacity(efficiency, capacityCargoStationTruck);
                }

                // Determine vehicles used for each service building status type.
                // Start with vehicles in maintenance, which may or may not have been set above.
                int usedRoadMaintenance   = inMaintRoadMaintenance;
                int usedAmbulance         = inMaintAmbulance;
                int usedMedicalHelicopter = inMaintMedicalHelicopter;
                int usedHearse            = inMaintHearse;
                int usedGarbageTruck      = inMaintGarbageTruck;
                int usedFireEngine        = inMaintFireEngine;
                int usedFireHelicopter    = inMaintFireHelicopter;
                int usedDisasterResponse  = inMaintDisasterResponse;
                int usedEvacuationBus     = inMaintEvacuationBus;
                int usedPoliceCar         = inMaintPoliceCar;
                int usedPoliceHelicopter  = inMaintPoliceHelicopter;
                int usedPrisonVan         = inMaintPrisonVan;
                int usedBus               = inMaintBus;
                int usedTaxi              = inMaintTaxi;
                int usedTrain             = inMaintTrain;
                int usedTram              = inMaintTram;
                int usedSubway            = inMaintSubway;
                int usedFerry             = inMaintFerry;
                int usedParkMaintenance   = inMaintParkMaintenance;
                int usedPost              = inMaintPost;
                int usedCargoStationTruck = inMaintCargoStationTruck;

                // Include vehicles in use only if being counted.
                if (CountVehiclesInUse)
                {
                    // Used is obtained only from the main building, not from upgrades.
                    if (BufferLookupOwnedVehicle.TryGetBuffer(mainBuildingAndUpgrades[0].Entity, out DynamicBuffer<OwnedVehicle> ownedVehicles) &&
                        ownedVehicles.IsCreated)
                    {
                        // Do each owned vehicle.
                        foreach (OwnedVehicle ownedVehicle in ownedVehicles)
                        {
                            // Vehicle must not be parked.
                            Entity vehicleEntity = ownedVehicle.m_Vehicle;
                            if (!ComponentLookupParkedCar  .HasComponent(vehicleEntity) &&
                                !ComponentLookupParkedTrain.HasComponent(vehicleEntity))
                            {
                                // Logic adapted from Game.UI.InGame.VehiclesSection.AddVehicle().

                                // Check for car/train vs helicopter vs watercraft.
                                if (ComponentLookupCar  .HasComponent(vehicleEntity) ||
                                    ComponentLookupTrain.HasComponent(vehicleEntity))
                                {
                                    // Check vehicle type.
                                    if      (ComponentLookupAmbulance          .HasComponent(vehicleEntity)) { usedAmbulance++;     }
                                    else if (ComponentLookupHearse             .HasComponent(vehicleEntity)) { usedHearse++;        }
                                    else if (ComponentLookupGarbageTruck       .HasComponent(vehicleEntity)) { usedGarbageTruck++;  }
                                    else if (ComponentLookupEvacuatingTransport.HasComponent(vehicleEntity)) { usedEvacuationBus++; }
                                    else if (ComponentLookupPoliceCar          .HasComponent(vehicleEntity)) { usedPoliceCar++;     }
                                    else if (ComponentLookupPrisonerTransport  .HasComponent(vehicleEntity)) { usedPrisonVan++;     }
                                    else if (ComponentLookupPostVan            .HasComponent(vehicleEntity)) { usedPost++;          }
                                    else if (ComponentLookupDeliveryTruck      .HasComponent(vehicleEntity))
                                    {
                                        // A delivery truck can be used by any building except transport depot.
                                        // For example, a delivery truck can be dispatched to buy
                                        // Pharmaceuticals for a medical building or Food for an emergency shelter.
                                        // Even though the delivery truck is not an ambulance or evacuation bus,
                                        // the delivery truck is still used by the building and counts against the building's capacity.
                                        // So count the delivery truck as if it is one of those vehicles.
                                        // Count the delivery truck according to which ever one is found first.
                                        if      (hasRoadMaintenance  ) { usedRoadMaintenance++;   }
                                        else if (hasAmbulance        ) { usedAmbulance++;         }
                                        else if (hasHearse           ) { usedHearse++;            }
                                        else if (hasGarbageTruck     ) { usedGarbageTruck++;      }
                                        else if (hasFireEngine       ) { usedFireEngine++;        }
                                        else if (hasDisasterResponse ) { usedDisasterResponse++;  }
                                        else if (hasEvacuationBus    ) { usedEvacuationBus++;     }
                                        else if (hasPoliceCar        ) { usedPoliceCar++;         }
                                        else if (hasPrisonVan        ) { usedPrisonVan++;         }
                                        else if (hasParkMaintenance  ) { usedParkMaintenance++;   }
                                        else if (hasPost             ) { usedPost++;              }
                                        else if (hasCargoStationTruck) { usedCargoStationTruck++; }
                                    }
                                    else if (ComponentLookupMaintenanceVehicle.HasComponent(vehicleEntity))
                                    {
                                        // Determine road vs park maintenance vehicle.
                                        if      (hasRoadMaintenance) { usedRoadMaintenance++; }
                                        else if (hasParkMaintenance) { usedParkMaintenance++; }
                                    }
                                    else if (ComponentLookupFireEngine.TryGetComponent(vehicleEntity, out Game.Vehicles.FireEngine fireEngine))
                                    {
                                        // Determine if fire engine is for disaster reponse or ordinary fire engine.
                                        if ((fireEngine.m_State & FireEngineFlags.DisasterResponse) != 0) { usedDisasterResponse++; }
                                        else                                                              { usedFireEngine++;       }
                                    }
                                    else if (mainBuildingHasTransportDepot)
                                    {
                                        // Every vehicle in a transport depot is a transport vehicle.
                                        // Count every transport vehicle according to the transport type of the main building.
                                        if      (hasBus   ) { usedBus++;    }
                                        else if (hasTaxi  ) { usedTaxi++;   }
                                        else if (hasTrain ) { usedTrain++;  }
                                        else if (hasTram  ) { usedTram++;   }
                                        else if (hasSubway) { usedSubway++; }
                                    }
                                }
                                else if (ComponentLookupHelicopter.HasComponent(vehicleEntity))
                                {
                                    // Check helicopter type.
                                    if      (ComponentLookupAmbulance .HasComponent(vehicleEntity)) { usedMedicalHelicopter++; }
                                    else if (ComponentLookupFireEngine.HasComponent(vehicleEntity)) { usedFireHelicopter++;    }
                                    else if (ComponentLookupPoliceCar .HasComponent(vehicleEntity)) { usedPoliceHelicopter++;  }
                                }
                                else if (ComponentLookupWatercraft.HasComponent(vehicleEntity))
                                {
                                    // Currently, ferry depot is only transport depot with watercraft.
                                    if (mainBuildingHasTransportDepot)
                                    {
                                        if (hasFerry) { usedFerry++; }
                                    }
                                }
                            }
                        }
                    }
                }

                // For each building status type, update entity color and total used and capacity.
                // Do in descending order by building status type.
                if (hasCargoStationTruck) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesCargoStationTruck, usedCargoStationTruck, capacityCargoStationTruck, ref color); }
                if (hasPost             ) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesPost,              usedPost,              capacityPost,              ref color); }
                if (hasParkMaintenance  ) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesParkMaintenance,   usedParkMaintenance,   capacityParkMaintenance,   ref color); }
                if (hasFerry            ) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesFerry,             usedFerry,             capacityFerry,             ref color); }
                if (hasSubway           ) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesSubway,            usedSubway,            capacitySubway,            ref color); }
                if (hasTram             ) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesTram,              usedTram,              capacityTram,              ref color); }
                if (hasTrain            ) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesTrain,             usedTrain,             capacityTrain,             ref color); }
                if (hasTaxi             ) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesTaxi,              usedTaxi,              capacityTaxi,              ref color); }
                if (hasBus              ) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesBus,               usedBus,               capacityBus,               ref color); }
                if (hasPrisonVan        ) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesPrisonVan,         usedPrisonVan,         capacityPrisonVan,         ref color); }
                if (hasPoliceHelicopter ) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesPoliceHelicopter,  usedPoliceHelicopter,  capacityPoliceHelicopter,  ref color); }
                if (hasPoliceCar        ) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesPoliceCar,         usedPoliceCar,         capacityPoliceCar,         ref color); }
                if (hasEvacuationBus    ) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesEvacuationBus,     usedEvacuationBus,     capacityEvacuationBus,     ref color); }
                if (hasDisasterResponse ) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesDisasterResponse,  usedDisasterResponse,  capacityDisasterResponse,  ref color); }
                if (hasFireHelicopter   ) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesFireHelicopter,    usedFireHelicopter,    capacityFireHelicopter,    ref color); }
                if (hasFireEngine       ) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesFireEngine,        usedFireEngine,        capacityFireEngine,        ref color); }
                if (hasGarbageTruck     ) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesGarbageTruck,      usedGarbageTruck,      capacityGarbageTruck,      ref color); }
                if (hasHearse           ) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesHearse,            usedHearse,            capacityHearse,            ref color); }
                if (hasMedicalHelicopter) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesMedicalHelicopter, usedMedicalHelicopter, capacityMedicalHelicopter, ref color); }
                if (hasAmbulance        ) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesAmbulance,         usedAmbulance,         capacityAmbulance,         ref color); }
                if (hasRoadMaintenance  ) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesRoadMaintenance,   usedRoadMaintenance,   capacityRoadMaintenance,   ref color); }

                // Do parked vehicles, which need different handling than all the service above.
                DoBuildingVehiclesParked(in mainBuildingAndUpgrades, ref color);
            }

            /// <summary>
            /// Get used and capacity of parked vehicles.
            /// </summary>
            private void DoBuildingVehiclesParked(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Define used and capacity for each parked building status type.
                int usedCar       = 0;
                int usedBike      = 0;
                int usedOther     = 0;
                int capacityCar   = 0;
                int capacityBike  = 0;
                int capacityOther = 0;

                // Do each main building and upgrade.
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Logic adapted from Game.UI.InGame.ParkingSection.OnProcess().

                    // Get parking data for the entity.
                    int laneCount          = 0;
                    int parkingCapacity    = 0;
                    int parkedVehicleCount = 0;
                    int parkingFee         = 0;
                    VehicleUtils.GetParkingData(
                        mainBuildingOrUpgrade.Entity,
                        ref laneCount,
                        ref parkingCapacity,
                        ref parkedVehicleCount,
                        ref parkingFee,
                        ref ComponentLookupParkingLane,
                        ref ComponentLookupPrefabRef,
                        ref ComponentLookupCurve,
                        ref ComponentLookupParkingLaneData,
                        ref ComponentLookupParkedCar,
                        ref ComponentLookupGarageLane,
                        ref BufferLookupLaneObject,
                        ref BufferLookupSubLane,
                        ref BufferLookupSubNet,
                        ref BufferLookupSubObject);
                    if (parkingCapacity < 0)
                    {
                        parkingCapacity = 0;
                    }

                    // Check for parking facility.
                    if (BuildingHasParkingFacility(mainBuildingOrUpgrade.Prefab, out ParkingFacilityData parkingFacilityData))
                    {
                        // Check for car vs bike vs other.
                        if (parkingFacilityData.m_RoadTypes == RoadTypes.Car)
                        {
                            usedCar     += parkedVehicleCount;
                            capacityCar += parkingCapacity;
                        }
                        else if (parkingFacilityData.m_RoadTypes == RoadTypes.Bicycle)
                        {
                            usedBike     += parkedVehicleCount;
                            capacityBike += parkingCapacity;
                        }
                        else
                        {
                            usedOther     += parkedVehicleCount;
                            capacityOther += parkingCapacity;
                        }
                    }
                    else
                    {
                        // Building without parking facility is other.
                        usedOther     += parkedVehicleCount;
                        capacityOther += parkingCapacity;
                    }
                }

                // If not counting vehicles in use, set used to 0.
                if (!CountVehiclesInUse)
                {
                    usedCar   = 0;
                    usedBike  = 0;
                    usedOther = 0;
                }

                // Parked building status types do not have vehicles in maintenance.

                // If main building plus upgrades has capacity, update entity color and total used and capacity.
                // Do in descending order by building status type.
                if (capacityOther > 0) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesParkedOther, usedOther, capacityOther, ref color); }
                if (capacityBike  > 0) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesParkedBike,  usedBike,  capacityBike,  ref color); }
                if (capacityCar   > 0) { UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.VehiclesParkedCar,   usedCar,   capacityCar,   ref color); }
            }
        }
    }
}
