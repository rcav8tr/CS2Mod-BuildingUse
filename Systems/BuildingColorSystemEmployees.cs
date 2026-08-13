using Game;
using Game.Buildings;
using Game.Companies;
using Game.Objects;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace BuildingUse
{
    /// <summary>
    /// Partial system to set building colors for Employees infoview.
    /// </summary>
    public partial class BuildingColorSystem : GameSystemBase
    {
        /// <summary>
        /// Partial job struct to set the color of each main building for Employees infoview.
        /// Burst compilation for this partial struct is defined by the main UpdateColorsJobMainBuilding struct.
        /// </summary>
        private partial struct UpdateColorsJobMainBuilding : IJobChunk
        {
            /// <summary>
            /// Do a main building for Employees infoview.
            /// </summary>
            private void DoBuildingEmployees(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Do building status types for service first.
                DoBuildingEmployeesService(in mainBuildingAndUpgrades, ref color);

                // Do building status types for zoned, which need different handling than service.
                // Do in descending order by building status type.
                DoBuildingEmployeesCompany(in mainBuildingAndUpgrades, ref color, BUBuildingStatusType.EmployeesOffice);
                DoBuildingEmployeesCompany(in mainBuildingAndUpgrades, ref color, BUBuildingStatusType.EmployeesIndustrial);
                DoBuildingEmployeesCompany(in mainBuildingAndUpgrades, ref color, BUBuildingStatusType.EmployeesExtractor);
                DoBuildingEmployeesCompany(in mainBuildingAndUpgrades, ref color, BUBuildingStatusType.EmployeesCommercial);
                DoBuildingEmployeesResidential(in mainBuildingAndUpgrades, ref color);
            }

            /// <summary>
            /// Do a main building and upgrades for Employees infoview for a residential building.
            /// </summary>
            private void DoBuildingEmployeesResidential(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Get used and capacity from main building and upgrades.
                long used     = 0L;
                long capacity = 0L;
                bool hasResidential = false;
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    hasResidential |= DoBuildingEmployeesResidentialDetail(mainBuildingOrUpgrade.Entity, ref used, ref capacity);
                }

                // If main building plus upgrades has residential, update entity color and total used and capacity.
                if (hasResidential)
                {
                    UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType.EmployeesResidential, used, capacity, ref color);
                }
            }

            /// <summary>
            /// Do a main building or upgrade for Employees infoview for a residential building to get details.
            /// </summary>
            private bool DoBuildingEmployeesResidentialDetail(Entity entity, ref long used, ref long capacity)
            {
                // Building must have residential.
                bool hasResidential = BuildingHasResidential(entity);

                // Building must have building property data.
                if (hasResidential &&
                    ComponentLookupPrefabRef.TryGetComponent(entity, out PrefabRef prefabRef) &&
                    ComponentLookupBuildingPropertyData.TryGetComponent(prefabRef.m_Prefab, out BuildingPropertyData buildingPropertyData) &&
                    buildingPropertyData.m_ResidentialProperties > 0)
                {
                    // Accumulate capacity.
                    capacity += buildingPropertyData.m_ResidentialProperties;

                    // Do each renter (i.e. potential household).
                    if (BufferLookupRenter.TryGetBuffer(entity, out DynamicBuffer<Renter> renters) &&
                        renters.IsCreated)
                    {
                        for (int i = 0; i < renters.Length; i++)
                        {
                            // If renter has at least 1 citizen, then count as 1 household for used.
                            if (GetDynamicBufferLength(renters[i].m_Renter, in BufferLookupHouseholdCitizen) > 0)
                            {
                                used++;
                            }
                        }
                    }
                }

                // Return has residential status.
                return hasResidential;
            }

            /// <summary>
            /// Do a main building and upgrades for Employees infoview for a company building.
            /// </summary>
            private void DoBuildingEmployeesCompany(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color, BUBuildingStatusType buildingStatusType)
            {
                // Get used and capacity from main building and upgrades.
                long used     = 0L;
                long capacity = 0L;
                bool hasProperty = false;
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    hasProperty |= DoBuildingEmployeesCompanyDetail(mainBuildingOrUpgrade.Entity, buildingStatusType, ref used, ref capacity);
                }

                // If main building plus upgrades has property, update entity color and total used and capacity.
                if (hasProperty)
                {
                    UpdateEntityColorAndTotalUsedCapacity(buildingStatusType, used, capacity, ref color);
                }
            }

            /// <summary>
            /// Do a main building or upgrade for Employees infoview for a company building to get details.
            /// </summary>
            private bool DoBuildingEmployeesCompanyDetail(Entity entity, BUBuildingStatusType buildingStatusType, ref long used, ref long capacity)
            {
                // Building must have commercial, industrial, or office property.
                bool hasProperty;
                switch (buildingStatusType)
                {
                    case BUBuildingStatusType.EmployeesCommercial: hasProperty = BuildingHasCommercial(entity); break;
                    case BUBuildingStatusType.EmployeesExtractor:  hasProperty = BuildingHasExtractor (entity); break;
                    case BUBuildingStatusType.EmployeesIndustrial: hasProperty = BuildingHasIndustrial(entity); break;
                    case BUBuildingStatusType.EmployeesOffice:     hasProperty = BuildingHasOffice    (entity); break;
                    default:
                        return false;
                }

                // Building must have a company and company must have employee buffer and work provider.
                if (hasProperty &&
                    TryGetCompany(entity, out Entity companyEntity) &&
                    BufferLookupEmployee.TryGetBuffer(companyEntity, out DynamicBuffer<Employee> employeeBuffer) &&
                    employeeBuffer.IsCreated &&
                    ComponentLookupWorkProvider.TryGetComponent(companyEntity, out WorkProvider workProvider))
                {
                    // Used is number of employees in the buffer.
                    used += employeeBuffer.Length;

                    // Capacity is the max workers from the work provider.
                    capacity += workProvider.m_MaxWorkers;
                }

                // Return has property status.
                return hasProperty;
            }

            /// <summary>
            /// Do a main building and upgrades for Employees infoview for a service building.
            /// </summary>
            private void DoBuildingEmployeesService(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Main building is always the first entry.
                Entity mainBuildingEntity = mainBuildingAndUpgrades[0].Entity;

                // The main building must have employee buffer and work provider.
                if (BufferLookupEmployee.TryGetBuffer(mainBuildingEntity, out DynamicBuffer<Employee> mainEmployeeBuffer) &&
                    mainEmployeeBuffer.IsCreated &&
                    ComponentLookupWorkProvider.TryGetComponent(mainBuildingEntity, out WorkProvider mainWorkProvider))
                {
                    // Get number of employees and max workers for the main building.
                    long mainBuildingEmployees = mainEmployeeBuffer.Length;
                    long mainBuildingMaxWorkers = mainWorkProvider.m_MaxWorkers;

                    // Get building status types and building capacities for the main building and upgrades.
                    NativeList<BuildingCapacity> buildingCapacities = new(4, Allocator.Temp);
                    foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                    {
                        DoBuildingEmployeesServiceDetail(mainBuildingOrUpgrade.Entity, mainBuildingOrUpgrade.Prefab, buildingCapacities);

                        // If upgrade has employees or workprovider directly, add to the main building.
                        if (mainBuildingOrUpgrade.Entity != mainBuildingEntity)
                        {
                            mainBuildingEmployees += GetDynamicBufferLength(mainBuildingOrUpgrade.Entity, in BufferLookupEmployee);
                            if (ComponentLookupWorkProvider.TryGetComponent(mainBuildingOrUpgrade.Entity, out WorkProvider upgradeWorkProvider))
                            {
                                mainBuildingMaxWorkers += upgradeWorkProvider.m_MaxWorkers;
                            }
                        }
                    }

                    // Use nested loops to check for duplicate building status types.
                    for (int i = 0; i < buildingCapacities.Length - 1; i++)
                    {
                        for (int j = i + 1; j < buildingCapacities.Length; j++)
                        {
                            if (buildingCapacities[i].BuildingStatusType == buildingCapacities[j].BuildingStatusType)
                            {
                                // Found a duplicate.
                                // Add capacity from the duplicate into the original.
                                BuildingCapacity buildingCapacity = buildingCapacities[i];
                                buildingCapacity.Capacity += buildingCapacities[j].Capacity;
                                buildingCapacities[i] = buildingCapacity;

                                // Remove the duplicate.
                                buildingCapacities.RemoveAtSwapBack(j);

                                // Check the entry that was swapped in.
                                j--;
                            }
                        }
                    }

                    // Allocate capacity for None to all other building capacities.
                    for (int i = 0; i < buildingCapacities.Length; i++)
                    {
                        if (buildingCapacities[i].BuildingStatusType == BUBuildingStatusType.None)
                        {
                            // Must have at least one other building capacity.
                            if (buildingCapacities.Length > 1)
                            {
                                // Compute the None capacity to be added to each other building status type.
                                // Note that this integer division will produce a slight and acceptable rounding error
                                // if the None capacity does not divide evenly by the number of other building status types.
                                long allocatedCapacityNone = buildingCapacities[i].Capacity / (buildingCapacities.Length - 1);
                                for (int j = 0; j < buildingCapacities.Length; j++)
                                {
                                    if (j != i)
                                    {
                                        BuildingCapacity buildingCapacityOther = buildingCapacities[j];
                                        buildingCapacityOther.Capacity += allocatedCapacityNone;
                                        buildingCapacities[j] = buildingCapacityOther;
                                    }
                                }
                            }
                            
                            // Remove the None building capacity.
                            buildingCapacities.RemoveAtSwapBack(i);

                            // Found the None capacity, stop checking.
                            break;
                        }
                    }

                    // Check if there are any building status types.
                    if (buildingCapacities.Length > 0)
                    {
                        // Use nested loops to sort building capacities by building status type in descending order.
                        for (int i = 0; i < buildingCapacities.Length - 1; i++)
                        {
                            for (int j = i + 1; j < buildingCapacities.Length; j++)
                            {
                                if (buildingCapacities[i].BuildingStatusType < buildingCapacities[j].BuildingStatusType)
                                {
                                    (buildingCapacities[i], buildingCapacities[j]) = (buildingCapacities[j], buildingCapacities[i]);
                                }
                            }
                        }

                        // Compute total capacity.
                        long totalCapacity = 0L;
                        for (int i = 0; i < buildingCapacities.Length; i++)
                        {
                            totalCapacity += buildingCapacities[i].Capacity;
                        }

                        // Do each building capacity.
                        foreach (BuildingCapacity buildingCapacity in buildingCapacities)
                        {
                            // Compute used and capacity for this building status type.
                            long used;
                            long capacity;
                            if (totalCapacity > 0)
                            {
                                // Allocate main building's employees and max workers proportionally according to this building status type's capacity.
                                // Note that this integer division will produce a slight and acceptable rounding error
                                // if the employees or max workers does not divide evenly by the total capacity.
                                used     = mainBuildingEmployees  * buildingCapacity.Capacity / totalCapacity;
                                capacity = mainBuildingMaxWorkers * buildingCapacity.Capacity / totalCapacity;
                            }
                            else
                            {
                                // This should never happen.
                                // But if it does, allocate main building employees and max workers evenly to all building status types.
                                used     = mainBuildingEmployees  / buildingCapacities.Length;
                                capacity = mainBuildingMaxWorkers / buildingCapacities.Length;
                            }

                            // Update entity color and total used and capacity.
                            UpdateEntityColorAndTotalUsedCapacity(buildingCapacity.BuildingStatusType, used, capacity, ref color);
                        }
                    }
                }
            }

            /// <summary>
            /// Do a main building or upgrade for Employees infoview for a service building to get details.
            /// </summary>
            private void DoBuildingEmployeesServiceDetail(
                Entity entity,
                Entity prefab,
                NativeList<BuildingCapacity> buildingCapacities)
            {
                // Get all building status types based on the services that apply to this building.
                // These are exactly the same as the Efficiency infoview.
                NativeList<BUBuildingStatusType> buildingStatusTypes = new(4, Allocator.Temp);
                if (BuildingHasParkingFacility    (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EmployeesParking          ); }
                if (BuildingHasRoadMaintenance    (prefab, entity)) { buildingStatusTypes.Add(BUBuildingStatusType.EmployeesRoadMaintenance  ); }
                if (BuildingHasElectricity        (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EmployeesElectricity      ); }
                if (BuildingHasWaterPumpingStation(prefab, entity)) { buildingStatusTypes.Add(BUBuildingStatusType.EmployeesWater            ); }
                if (BuildingHasSewageOutlet       (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EmployeesSewage           ); }
                if (BuildingHasHospital           (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EmployeesHealthcare       ); }
                if (BuildingHasDeathcareFacility  (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EmployeesDeathcare        ); }
                if (BuildingHasGarbageFacility    (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EmployeesGarbageManagement); }
                if (BuildingHasSchool             (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EmployeesEducation        ); }
                if (BuildingHasResearchFacility   (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EmployeesResearch         ); }
                if (BuildingHasFireRescue         (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EmployeesFireRescue       ); }
                if (BuildingHasDisasterControl    (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EmployeesDisasterControl  ); }
                if (BuildingHasPolice             (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EmployeesPolice           ); }
                if (BuildingHasAdministration     (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EmployeesAdministration   ); }
                if (BuildingHasTransportation     (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EmployeesTransportation   ); }
                if (BuildingHasParkMaintenance    (prefab, entity)) { buildingStatusTypes.Add(BUBuildingStatusType.EmployeesParkMaintenance  ); }
                if (BuildingHasPark               (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EmployeesParksRecreation  ); }
                if (BuildingHasPostFacility       (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EmployeesPost             ); }
                if (BuildingHasTelecomFacility    (prefab))         { buildingStatusTypes.Add(BUBuildingStatusType.EmployeesTelecom          ); }

                // Get max workers from workplace data of prefab, if any.
                int maxWorkers = 0;
                if (ComponentLookupWorkplaceData.TryGetComponent(prefab, out WorkplaceData workplaceData))
                {
                    maxWorkers = workplaceData.m_MaxWorkers;
                }

                // If this building has more than one building status type,
                // then distribute the max workers evenly across the building status types.
                if (buildingStatusTypes.Length > 1)
                {
                    // Note that this integer division will produce a slight and acceptable rounding error
                    // if the max workers does not divide evenly by the number of building status types.
                    maxWorkers /= buildingStatusTypes.Length;
                }

                // Return the capacities for this building.
                // All the buildings status types get the same capacity.
                foreach (BUBuildingStatusType buildingStatusType in buildingStatusTypes)
                {
                    buildingCapacities.Add(new BuildingCapacity(buildingStatusType, maxWorkers));
                }

                // If this building has no building status types and there are workers, return a None building capacity for those workers.
                if (buildingStatusTypes.Length == 0 && maxWorkers > 0)
                {
                    buildingCapacities.Add(new BuildingCapacity(BUBuildingStatusType.None, maxWorkers));
                }
            }
        }
    }
}
