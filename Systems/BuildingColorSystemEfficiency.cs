using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BuildingUse
{
    /// <summary>
    /// Partial system to set building colors for Employees infoview.
    /// </summary>
    public partial class BuildingColorSystem : Game.GameSystemBase
    {
        /// <summary>
        /// Partial job struct to set the color of each main building for Efficiency infoview.
        /// Burst compilation for this partial struct is defined by the main UpdateColorsJobMainBuilding struct.
        /// </summary>
        private partial struct UpdateColorsJobMainBuilding : IJobChunk
        {
            /// <summary>
            /// Get building status types applicable to the building chunk for Efficiency infoview.
            /// </summary>
            private void GetApplicableBuildingStatusTypesEfficiency(ArchetypeChunk buildingChunk, ref NativeList<BUBuildingStatusType> applicableBuildingStatusTypes)
            {
                // Add all building status types that apply to this building chunk.
                if (buildingChunk.Has(ref ComponentTypeHandleResidentialProperty))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyResidential);
                if (buildingChunk.Has(ref ComponentTypeHandleCommercialProperty))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyCommercial);
                if (buildingChunk.Has(ref ComponentTypeHandleIndustrialProperty) && !buildingChunk.Has(ref ComponentTypeHandleOfficeProperty))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyIndustrial);
                if (buildingChunk.Has(ref ComponentTypeHandleOfficeProperty))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyOffice);
                if (buildingChunk.Has(ref ComponentTypeHandleParkingFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyParking);
                if (buildingChunk.Has(ref ComponentTypeHandleRoadMaintenance))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyRoadMaintenance);
                if (buildingChunk.Has(ref ComponentTypeHandleElectricityProducer) || buildingChunk.Has(ref ComponentTypeHandleBattery) || buildingChunk.Has(ref ComponentTypeHandleTransformer))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyElectricity);
                if (buildingChunk.Has(ref ComponentTypeHandleWaterPumpingStation))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyWater);
                if (buildingChunk.Has(ref ComponentTypeHandleSewageOutlet))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencySewage);
                if (buildingChunk.Has(ref ComponentTypeHandleHospital))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyHealthcare);
                if (buildingChunk.Has(ref ComponentTypeHandleDeathcareFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyDeathcare);
                if (buildingChunk.Has(ref ComponentTypeHandleGarbageFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyGarbageManagement);
                if (buildingChunk.Has(ref ComponentTypeHandleSchool))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyEducation);
                if (buildingChunk.Has(ref ComponentTypeHandleResearchFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyResearch);
                if (buildingChunk.Has(ref ComponentTypeHandleFireStation))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyFireRescue);
                if (buildingChunk.Has(ref ComponentTypeHandleEmergencyShelter) || buildingChunk.Has(ref ComponentTypeHandleDisasterFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyDisasterControl);
                if (buildingChunk.Has(ref ComponentTypeHandlePoliceStation) || buildingChunk.Has(ref ComponentTypeHandlePrison))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyPolice);
                if (buildingChunk.Has(ref ComponentTypeHandleAdminBuilding) || buildingChunk.Has(ref ComponentTypeHandleWelfareOffice))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyAdministration);
                if (buildingChunk.Has(ref ComponentTypeHandleTransportDepot) || buildingChunk.Has(ref ComponentTypeHandleTransportStation))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyTransportation);
                if (buildingChunk.Has(ref ComponentTypeHandleParkMaintenance))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyParkMaintenance);
                if (buildingChunk.Has(ref ComponentTypeHandlePark))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyParksRecreation);
                if (buildingChunk.Has(ref ComponentTypeHandlePostFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyPost);
                if (buildingChunk.Has(ref ComponentTypeHandleTelecomFacility))
                    applicableBuildingStatusTypes.Add(BUBuildingStatusType.EfficiencyTelecom);
            }

            /// <summary>
            /// Set building colors for Efficiency infoview.
            /// </summary>
            private void SetBuildingColorsEfficiency
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

                    // Special handling for residential happiness.
                    if (buildingStatusType == BUBuildingStatusType.EfficiencyResidential)
                    {
                        // Logic adapted from Game.UI.InGame.AverageHappinessSection.CountHappinessJob.TryAddPropertyHappiness().

                        // Get renters (households) in the building, if any.
                        int totalHappiness = 0;
                        int citizenCount = 0;
		                if (BufferLookupRenter.TryGetBuffer(entity, out DynamicBuffer<Game.Buildings.Renter> renters))
		                {
                            // Do each renter (household).
			                for (int j = 0; j < renters.Length; j++)
			                {
                                // Get citizens in the renter (household), if any.
                                if (BufferLookupHouseholdCitizen.TryGetBuffer(renters[j].m_Renter, out DynamicBuffer<Game.Citizens.HouseholdCitizen> householdCitizens))
                                {
                                    // Do each citizen.
                                    for (int k = 0; k < householdCitizens.Length; k++)
                                    {
                                        // Citizen component must exist on the citizen and citizen must not be dead.
					                    Entity citizen = householdCitizens[k].m_Citizen;
					                    if (ComponentLookupCitizen.HasComponent(citizen) && !Game.Citizens.CitizenUtils.IsDead(citizen, ref ComponentLookupHealthProblem))
					                    {
						                    totalHappiness += ComponentLookupCitizen[citizen].Happiness;
						                    citizenCount++;
					                    }
                                    }
                                }
			                }
		                }

                        // Compute happiness percent.
                        int happinessPercent = 0;
                        if (citizenCount > 0)
                        {
                            // Compute average happiness of citizens in this building.
                            // Citizen happiness neutral is 50.
                            // Convert citizen happiness to percent where 100% is neutral.
                            happinessPercent = (int)math.round(2f * totalHappiness / citizenCount);
                        }

                        // Update entity color and accumulate totals.
                        UpdateEntityColor(happinessPercent, (EfficiencyMaxColor200Percent ? 200L : 100L), infomodeActive, infomodeIndex, colors, i);
                        totalUsed     += happinessPercent;
                        totalCapacity += 100L;   // Capacity is always 100%, even if color is based on 200%.
                    }
                    else
                    {
                        // Logic adapted from Game.UI.InGame.EfficiencySection.OnProcess().

                        // Building must have an Efficiency buffer.
                        if (BufferLookupEfficiency.TryGetBuffer(entity, out DynamicBuffer<Game.Buildings.Efficiency> bufferEfficiency) &&
                            bufferEfficiency.IsCreated)
                        {
                            // Building has an Efficiency buffer.

                            // Zoned buildings must have a renter because zoned buildings can still have efficiency buffer entries without a renter.
                            // Guessing these efficiency buffer entries are left over from when the building previously had a renter.
                            bool hasRenter;
                            if (buildingStatusType == BUBuildingStatusType.EfficiencyCommercial ||
                                buildingStatusType == BUBuildingStatusType.EfficiencyIndustrial ||
                                buildingStatusType == BUBuildingStatusType.EfficiencyOffice)
                            {
                                // Assume no renters.
                                hasRenter = false;

                                // Get renters.
                                if (BufferLookupRenter.TryGetBuffer(entity, out DynamicBuffer<Game.Buildings.Renter> renters) && renters.Length > 0)
                                {
                                    // Check each renter for company data.
                                    // Mixed use buildings (e.g. residential and commercial) can have residential renters without a company renter.
                                    for (int j = 0; j < renters.Length; j++)
                                    {
                                        if (ComponentLookupCompanyData.HasComponent(renters[j].m_Renter))
                                        {
                                            hasRenter = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Other than zoned buildings are considered to always have a renter.
                                hasRenter = true;
                            }

                            // Check if building is not zoned or zoned building has a renter.
                            if (hasRenter)
                            {
                                // Start with 100% as the default efficiency.
                                float tempEfficiency = 1f;

                                // Do each entry in the buffer.
                                // Note that a building with no efficiency entries will have the default efficiency of 100%, like in the game.
                                foreach (Game.Buildings.Efficiency item in bufferEfficiency)
                                {
                                    // Exclude negative efficiencies.
                                    // Note that the efficiency entry for Disabled has an efficiency value of zero.
                                    // So disabled buildings will still be included, but will have 0% efficiency, like in the game.
                                    if (item.m_Efficiency >= 0f)
                                    {
                                        // Efficiency is multiplicative.
                                        tempEfficiency *= item.m_Efficiency;
                                    }
                                }

                                // Convert efficiency to percent.
              		            int efficiency = (int)math.round(100f * tempEfficiency);

                                // Update entity color and accumulate totals.
                                UpdateEntityColor(efficiency, (EfficiencyMaxColor200Percent ? 200L : 100L), infomodeActive, infomodeIndex, colors, i);
                                totalUsed     += efficiency;
                                totalCapacity += 100L;   // Capacity is always 100%, even if color is based on 200%.
                            }
                            else
                            {
                                // Zoned buildings with no renter are dislayed as 0%.
                                UpdateEntityColor(0L, 0L, infomodeActive, infomodeIndex, colors, i);
                                totalUsed     += 0L;
                                totalCapacity += 100L;  // Count the building.
                            }
                        }
                        else
                        {
                            // Building has no Efficiency buffer.

                            // Check for zoned building
                            if (buildingStatusType == BUBuildingStatusType.EfficiencyCommercial ||
                                buildingStatusType == BUBuildingStatusType.EfficiencyIndustrial ||
                                buildingStatusType == BUBuildingStatusType.EfficiencyOffice)
                            {
                                // Update entity color and accumulate totals for 0%.
                                UpdateEntityColor(0L, (EfficiencyMaxColor200Percent ? 200L : 100L), infomodeActive, infomodeIndex, colors, i);
                                totalUsed     += 0L;
                                totalCapacity += 100L;   // Capacity is always 100%, even if color is based on 200%.
                            }
                            else
                            {
                                // Not a zoned building.
                                // Leave default color.
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
