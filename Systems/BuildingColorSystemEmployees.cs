using Unity.Collections;
using Unity.Entities;

namespace BuildingUse
{
    /// <summary>
    /// Partial system to set building colors for Employees infoview.
    /// </summary>
    public partial class BuildingColorSystem : Game.GameSystemBase
    {
        /// <summary>
        /// Partial job struct to set the color of each main building for Employees infoview.
        /// Burst compilation for this partial struct is defined by the main UpdateColorsJobMainBuilding struct.
        /// </summary>
        private partial struct UpdateColorsJobMainBuilding : IJobChunk
        {
            /// <summary>
            /// Get building status types applicable to the building chunk for Employees infoview.
            /// </summary>
            private void GetApplicableBuildingStatusTypesEmployees(ArchetypeChunk buildingChunk, ref NativeList<BUBuildingStatusType> applicableBuildingStatusTypes)
            {
                // Add all building status types that apply to this building chunk.
                if (buildingChunk.Has(ref ComponentTypeHandleResidentialProperty))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesResidential);
                if (buildingChunk.Has(ref ComponentTypeHandleCommercialProperty))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesCommercial);
                if (buildingChunk.Has(ref ComponentTypeHandleIndustrialProperty) && !buildingChunk.Has(ref ComponentTypeHandleOfficeProperty))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesIndustrial);
                if (buildingChunk.Has(ref ComponentTypeHandleOfficeProperty))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesOffice);
                if (buildingChunk.Has(ref ComponentTypeHandleParkingFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesParking);
                if (buildingChunk.Has(ref ComponentTypeHandleRoadMaintenance))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesRoadMaintenance);
                if (buildingChunk.Has(ref ComponentTypeHandleElectricityProducer) || buildingChunk.Has(ref ComponentTypeHandleBattery))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesElectricity);
                if (buildingChunk.Has(ref ComponentTypeHandleWaterPumpingStation))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesWater);
                if (buildingChunk.Has(ref ComponentTypeHandleSewageOutlet))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesSewage);
                if (buildingChunk.Has(ref ComponentTypeHandleHospital))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesHealthcare);
                if (buildingChunk.Has(ref ComponentTypeHandleDeathcareFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesDeathcare);
                if (buildingChunk.Has(ref ComponentTypeHandleGarbageFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesGarbageManagement);
                if (buildingChunk.Has(ref ComponentTypeHandleSchool))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesEducation);
                if (buildingChunk.Has(ref ComponentTypeHandleResearchFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesResearch);
                if (buildingChunk.Has(ref ComponentTypeHandleFireStation))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesFireRescue);
                if (buildingChunk.Has(ref ComponentTypeHandleEmergencyShelter) || buildingChunk.Has(ref ComponentTypeHandleDisasterFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesDisasterControl);
                if (buildingChunk.Has(ref ComponentTypeHandlePoliceStation) || buildingChunk.Has(ref ComponentTypeHandlePrison))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesPolice);
                if (buildingChunk.Has(ref ComponentTypeHandleAdminBuilding) || buildingChunk.Has(ref ComponentTypeHandleWelfareOffice))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesAdministration);
                if (buildingChunk.Has(ref ComponentTypeHandleTransportDepot) || buildingChunk.Has(ref ComponentTypeHandleTransportStation))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesTransportation);
                if (buildingChunk.Has(ref ComponentTypeHandleParkMaintenance))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesParkMaintenance);
                if (buildingChunk.Has(ref ComponentTypeHandlePark))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesParksRecreation);
                if (buildingChunk.Has(ref ComponentTypeHandlePostFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesPost);
                if (buildingChunk.Has(ref ComponentTypeHandleTelecomFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EmployeesTelecom);
            }

            /// <summary>
            /// Set building colors for Employees infoview.
            /// </summary>
            private void SetBuildingColorsEmployees
            (
                ArchetypeChunk buildingChunk,
                bool infomodeActive,
                int infomodeIndex,
                NativeArray<Game.Objects.Color> colors,
                BUBuildingStatusType buildingStatusType
            )
            {
                // Set building colors based on building status type.
                switch (buildingStatusType)
                {
                    // Do residential.
                    case BUBuildingStatusType.EmployeesResidential:
                        SetBuildingColorsEmployeesResidential(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType);
                        break;

                    // Do companies.
                    case BUBuildingStatusType.EmployeesCommercial:
                    case BUBuildingStatusType.EmployeesIndustrial:
                    case BUBuildingStatusType.EmployeesOffice:
                        SetBuildingColorsEmployeesCompany(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType);
                        break;

                    // Everything else is a service building.
                    default:
                        SetBuildingColorsEmployeesService(buildingChunk, infomodeActive, infomodeIndex, colors, buildingStatusType);
                        break;
                }
            }

            /// <summary>
            /// Set building colors for Employees infoview for a residential building.
            /// </summary>
            private void SetBuildingColorsEmployeesResidential
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
                    // Get entity and prefab.
                    Entity entity = entities[i];
                    Entity prefab = prefabRefs[i].m_Prefab;

                    // Logic adapted from Game.UI.InGame.HouseholdSidebarSection.CheckVisibilityJob.HasResidentialProperties().

                    // Building must have building property data.
                    if (ComponentLookupBuildingPropertyData.TryGetComponent(prefab, out Game.Prefabs.BuildingPropertyData buildingPropertyData))
                    {
                        // Building must have capacity.
                        long capacity = buildingPropertyData.m_ResidentialProperties;
                        if (capacity > 0L)
                        {
                            // Do each renter (i.e. potential household).
                            long used = 0L;
                            DynamicBuffer<Game.Buildings.Renter> renters = BufferLookupRenter[entity];
                            for (int j = 0; j < renters.Length; j++)
                            {
                                // If renter has at least 1 citizen, then count as 1 household for used.
                                if (BufferLookupHouseholdCitizen.TryGetBuffer(renters[j].m_Renter, out DynamicBuffer<Game.Citizens.HouseholdCitizen> householdCitizens))
                                {
                                    if (householdCitizens.Length > 0)
                                    {
                                        used++;
                                    }
                                }
                            }

                            // Household used and capacity are valid.
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
            /// Set building colors for Employees infoview for a company building.
            /// Logic adapted from ExtendedTooltip mod, EmployeesTooltipBuilder class.
            /// </summary>
            private void SetBuildingColorsEmployeesCompany
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
                    // Get entity and prefab.
                    Entity entity = entities[i];
                    Entity prefab = prefabRefs[i].m_Prefab;

                    // Get company, if any.
                    bool found = false;
                    if (Game.UI.InGame.CompanyUIUtils.HasCompany(entity, prefab, ref BufferLookupRenter, ref ComponentLookupBuildingPropertyData, ref ComponentLookupCompanyData, out Entity companyEntity))
                    {
                        // Employee data can be computed only if company has employee buffer and work provider component.
                        if (BufferLookupEmployee.TryGetBuffer(companyEntity, out DynamicBuffer<Game.Companies.Employee> employeeBuffer) &&
                            ComponentLookupWorkProvider.TryGetComponent(companyEntity, out Game.Companies.WorkProvider workProvider))
                        {
                            // Found a company and company has employee buffer and work provider.
                            found = true;

                            // Get used employees.
                            long used = employeeBuffer.Length;

                            // Get building level.
                            int buildingLevel = 1;
                            if (ComponentLookupSpawnableBuildingData.TryGetComponent(prefab, out Game.Prefabs.SpawnableBuildingData spawnableBuildingData1))
                            {
                                buildingLevel = spawnableBuildingData1.m_Level;
                            }
                            else if (ComponentLookupPropertyRenter.TryGetComponent(entity, out Game.Buildings.PropertyRenter propertyRenter) &&
                                     ComponentLookupPrefabRef.TryGetComponent(propertyRenter.m_Property, out Game.Prefabs.PrefabRef propertyRenterPrefabRef) &&
                                     ComponentLookupSpawnableBuildingData.TryGetComponent(propertyRenterPrefabRef.m_Prefab, out Game.Prefabs.SpawnableBuildingData spawnableBuildingData2))
                            {
                                buildingLevel = spawnableBuildingData2.m_Level;
                            }

                            // Get employee capacity.
                            Entity companyPrefab = ComponentLookupPrefabRef[companyEntity].m_Prefab;
                            Game.Prefabs.WorkplaceComplexity complexity = ComponentLookupWorkplaceData[companyPrefab].m_Complexity;
                            Game.UI.InGame.EmploymentData workplacesData = Game.UI.InGame.EmploymentData.GetWorkplacesData(workProvider.m_MaxWorkers, buildingLevel, complexity);
                            long capacity = workplacesData.total;

                            // Employee used and capacity are valid.
                            // Update entity color and accumulate totals.
                            UpdateEntityColor(used, capacity, infomodeActive, infomodeIndex, colors, i);
                            totalUsed     += used;
                            totalCapacity += capacity;
                        }
                    }

                    // If no company or the company has no employee buffer and work provider, building is available to rent.
                    // Show buildings available to rent with 0%.
                    if (!found)
                    {
                        UpdateEntityColor(0L, 0L, infomodeActive, infomodeIndex, colors, i);
                    }
                }

                // Update total used and capacity data arrays.
                UpdateTotalUsedCapacity(buildingStatusType, totalUsed, totalCapacity);
            }

            /// <summary>
            /// Set building colors for Employees infoview for a service building.
            /// </summary>
            private void SetBuildingColorsEmployeesService
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
                    // Get entity and prefab.
                    Entity entity = entities[i];
                    Entity prefab = prefabRefs[i].m_Prefab;

                    // Employee data for service can be computed only if entity has employee buffer and work provider component.
                    if (BufferLookupEmployee.TryGetBuffer(entity, out DynamicBuffer<Game.Companies.Employee> employeeBuffer) &&
                        ComponentLookupWorkProvider.TryGetComponent(entity, out Game.Companies.WorkProvider workProvider))
                    {
                        // Get used employees.
                        long used = employeeBuffer.Length;

                        // Building level is always 1 for service buildings.
                        int buildingLevel = 1;

                        // Get employee capacity.
                        Game.Prefabs.WorkplaceComplexity complexity = ComponentLookupWorkplaceData[prefab].m_Complexity;
                        Game.UI.InGame.EmploymentData workplacesData = Game.UI.InGame.EmploymentData.GetWorkplacesData(workProvider.m_MaxWorkers, buildingLevel, complexity);
                        long capacity = workplacesData.total;

                        // Employee used and capacity are valid.
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
