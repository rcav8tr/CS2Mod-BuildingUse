using Game;
using Game.Buildings;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BuildingUse
{
    /// <summary>
    /// Partial system to set building colors for Production infoview.
    /// </summary>
    public partial class BuildingColorSystem : GameSystemBase
    {
        /// <summary>
        /// Partial job struct to set the color of each main building for Production infoview.
        /// Burst compilation for this partial struct is defined by the main UpdateColorsJobMainBuilding struct.
        /// </summary>
        private partial struct UpdateColorsJobMainBuilding : IJobChunk
        {
            /// <summary>
            /// Do a main building for Production infoview.
            /// </summary>
            private void DoBuildingProduction(in NativeList<EntityPrefab> mainBuildingAndUpgrades, ref Color color)
            {
                // Data for main building.
                int mainBuildingLevel = 0;
                float mainBuildingEfficiency        = 0f;
                float mainBuildingEfficiencyLimited = 0f;

                // Do each main building and upgrade.
                foreach (EntityPrefab mainBuildingOrUpgrade in mainBuildingAndUpgrades)
                {
                    // Building must be an industrial property (which includes extractor, industrial, office, and storage).
                    // Building must not be a storage property.
                    // This is a way to quickly exclude residential, commercial, and service buildings before trying to get a company.
                    Entity buildingEntity = mainBuildingOrUpgrade.Entity;
                    if (ComponentLookupIndustrialProperty.HasComponent(buildingEntity) &&
                       !ComponentLookupStorageProperty   .HasComponent(buildingEntity))
                    {
                        // Building must have a company that has workplace data and industrial process data.
                        if (TryGetCompany(buildingEntity, out Entity companyEntity) &&
                            ComponentLookupPrefabRef            .TryGetComponent(companyEntity, out PrefabRef companyPrefabRef) &&
                            ComponentLookupWorkplaceData        .TryGetComponent(companyPrefabRef.m_Prefab, out WorkplaceData workplaceData) &&
                            ComponentLookupIndustrialProcessData.TryGetComponent(companyPrefabRef.m_Prefab, out IndustrialProcessData industrialProcessData))
                        {
                            // It would be highly unusual for a main building to have more than one company (e.g. thru upgrades).
                            // But if a main building has more than one company,
                            // the main building color will be set according to the last company processed.
                            // This is acceptable to avoid the need to save the data for all the companies and then sort them by resource.

                            // If main building level is not yet obtained, get it now.
                            if (mainBuildingLevel == 0)
                            {
                                mainBuildingLevel = 1;
                                if (ComponentLookupSpawnableBuildingData.TryGetComponent(mainBuildingAndUpgrades[0].Prefab, out SpawnableBuildingData spawnableBuildingData))
                                {
                                    mainBuildingLevel = spawnableBuildingData.m_Level;
                                }
                            }

                            // If main building efficiency was not yet obtained, get it now.
                            if (mainBuildingEfficiency == 0f)
                            {
                                mainBuildingEfficiency = 1f;
                                if (BufferLookupEfficiency.TryGetBuffer(mainBuildingAndUpgrades[0].Entity, out DynamicBuffer<Efficiency> bufferEfficieny) &&
                                    bufferEfficieny.IsCreated)
                                {
                                    mainBuildingEfficiency = BuildingUtils.GetEfficiency(bufferEfficieny);
                                }
                                mainBuildingEfficiencyLimited = math.max(mainBuildingEfficiency, 1f);
                            }

                            // Get building status type based on output resource.
                            BUBuildingStatusType buildingStatusType = BUBuildingStatusType.None;
                            switch (industrialProcessData.m_Output.m_Resource)
                            {
                                case Resource.Wood:            buildingStatusType = BUBuildingStatusType.ProductionWood;            break;
                                case Resource.Grain:           buildingStatusType = BUBuildingStatusType.ProductionGrain;           break;
                                case Resource.Livestock:       buildingStatusType = BUBuildingStatusType.ProductionLivestock;       break;
                                case Resource.Fish:            buildingStatusType = BUBuildingStatusType.ProductionFish;            break;
                                case Resource.Vegetables:      buildingStatusType = BUBuildingStatusType.ProductionVegetables;      break;
                                case Resource.Cotton:          buildingStatusType = BUBuildingStatusType.ProductionCotton;          break;
                                case Resource.Oil:             buildingStatusType = BUBuildingStatusType.ProductionOil;             break;
                                case Resource.Ore:             buildingStatusType = BUBuildingStatusType.ProductionOre;             break;
                                case Resource.Coal:            buildingStatusType = BUBuildingStatusType.ProductionCoal;            break;
                                case Resource.Stone:           buildingStatusType = BUBuildingStatusType.ProductionStone;           break;
                                
                                case Resource.Metals:          buildingStatusType = BUBuildingStatusType.ProductionMetals;          break;
                                case Resource.Steel:           buildingStatusType = BUBuildingStatusType.ProductionSteel;           break;
                                case Resource.Minerals:        buildingStatusType = BUBuildingStatusType.ProductionMinerals;        break;
                                case Resource.Concrete:        buildingStatusType = BUBuildingStatusType.ProductionConcrete;        break;
                                case Resource.Machinery:       buildingStatusType = BUBuildingStatusType.ProductionMachinery;       break;
                                case Resource.Petrochemicals:  buildingStatusType = BUBuildingStatusType.ProductionPetrochemicals;  break;
                                case Resource.Chemicals:       buildingStatusType = BUBuildingStatusType.ProductionChemicals;       break;
                                case Resource.Plastics:        buildingStatusType = BUBuildingStatusType.ProductionPlastics;        break;
                                case Resource.Pharmaceuticals: buildingStatusType = BUBuildingStatusType.ProductionPharmaceuticals; break;
                                case Resource.Electronics:     buildingStatusType = BUBuildingStatusType.ProductionElectronics;     break;
                                case Resource.Vehicles:        buildingStatusType = BUBuildingStatusType.ProductionVehicles;        break;
                                case Resource.Beverages:       buildingStatusType = BUBuildingStatusType.ProductionBeverages;       break;
                                case Resource.ConvenienceFood: buildingStatusType = BUBuildingStatusType.ProductionConvenienceFood; break;
                                case Resource.Food:            buildingStatusType = BUBuildingStatusType.ProductionFood;            break;
                                case Resource.Textiles:        buildingStatusType = BUBuildingStatusType.ProductionTextiles;        break;
                                case Resource.Timber:          buildingStatusType = BUBuildingStatusType.ProductionTimber;          break;
                                case Resource.Paper:           buildingStatusType = BUBuildingStatusType.ProductionPaper;           break;
                                case Resource.Furniture:       buildingStatusType = BUBuildingStatusType.ProductionFurniture;       break;
                                
                                case Resource.Software:        buildingStatusType = BUBuildingStatusType.ProductionSoftware;        break;
                                case Resource.Telecom:         buildingStatusType = BUBuildingStatusType.ProductionTelecom;         break;
                                case Resource.Financial:       buildingStatusType = BUBuildingStatusType.ProductionFinancial;       break;
                                case Resource.Media:           buildingStatusType = BUBuildingStatusType.ProductionMedia;           break;
                            }

                            // Building status type must be valid.
                            if (buildingStatusType != BUBuildingStatusType.None)
                            {
                                // Logic to get current production adapted from Game.UI.InGame.CompanySection.QueryCompanyData().
                                // Logic to get production capacity adapted from
                                //      Game.Simulation.CityProductionCapacityCalculationSystem.UpdateProductionCapacityJob.

                                // Check for extractor company.
                                if (ComponentLookupExtractorCompany.HasComponent(companyEntity))
                                {
                                    // Used and capacity are obtained from CompanyStatisticData.
                                    if (ComponentLookupCompanyStatisticData.TryGetComponent(companyEntity, out CompanyStatisticData companyStatisticData))
                                    {
                                        // Check main building efficiency.
                                        int used     = companyStatisticData.m_LastUpdateProduce;
                                        int capacity = 0;
                                        if (mainBuildingEfficiency > 0f)
                                        {
                                            capacity = (int)math.ceil(companyStatisticData.m_LastUpdateProduce / mainBuildingEfficiency * mainBuildingEfficiencyLimited);
                                        }

                                        // Update entity color according to the production max color setting.
                                        UpdateEntityColor(buildingStatusType, used, capacity * (ProductionMaxColor200Percent ? 2L : 1L), ref color);

                                        // Update total used and capacity.
                                        UpdateTotalUsedCapacity(buildingStatusType, used, capacity);
                                    }
                                }

                                // Company must be industrial/office.
                                else
                                {
                                    // Get buffers and components needed to do calculations.
                                    if (BufferLookupEmployee.TryGetBuffer(companyEntity, out DynamicBuffer<Employee> bufferEmployee) &&
                                        bufferEmployee.IsCreated &&
                                        ComponentLookupWorkProvider.TryGetComponent(companyEntity, out WorkProvider companyWorkProvider))
                                    {
                                        // Get current production.
                                        int used = EconomyUtils.GetCompanyProductionPerDay(
                                            mainBuildingEfficiency,
                                            true,
                                            bufferEmployee,
                                            industrialProcessData,
                                            ResourcePrefabs,
                                            ref ComponentLookupResourceData,
                                            ref ComponentLookupCitizen,
                                            ref EconomyParameters,
                                            default(ServiceAvailable),
                                            default(ServiceCompanyData));

                                        // Get production capacity.
                                        int capacity = EconomyUtils.GetCompanyProductionPerDay(
                                            mainBuildingEfficiencyLimited,
                                            companyWorkProvider.m_MaxWorkers,
                                            mainBuildingLevel,
                                            true,
                                            workplaceData,
                                            industrialProcessData,
                                            ResourcePrefabs,
                                            ref ComponentLookupResourceData,
                                            ref EconomyParameters);

                                        // Update entity color according to the production max color setting.
                                        UpdateEntityColor(buildingStatusType, used, capacity * (ProductionMaxColor200Percent ? 2L : 1L), ref color);

                                        // Update total used and capacity.
                                        UpdateTotalUsedCapacity(buildingStatusType, used, capacity);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
