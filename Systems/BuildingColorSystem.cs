using Colossal.Collections;
using Game;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Creatures;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Game.Vehicles;
using HarmonyLib;
using System.Reflection;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Ambulance             = Game.Vehicles.    Ambulance;
using Battery               = Game.Buildings.   Battery;
using BuildingFlags         = Game.Prefabs.     BuildingFlags;
using Color                 = Game.Objects.     Color;
using DeathcareFacility     = Game.Buildings.   DeathcareFacility;
using DeliveryTruck         = Game.Vehicles.    DeliveryTruck;
using Elevation             = Game.Objects.     Elevation;
using ExtractorCompany      = Game.Companies.   ExtractorCompany;
using FireEngine            = Game.Vehicles.    FireEngine;
using GarbageFacility       = Game.Buildings.   GarbageFacility;
using GarbageTruck          = Game.Vehicles.    GarbageTruck;
using Hearse                = Game.Vehicles.    Hearse;
using MailBox               = Game.Routes.      MailBox;
using MaintenanceVehicle    = Game.Vehicles.    MaintenanceVehicle;
using Object                = Game.Objects.     Object;
using ParkingLane           = Game.Net.         ParkingLane;
using PoliceCar             = Game.Vehicles.    PoliceCar;
using PostFacility          = Game.Buildings.   PostFacility;
using PostVan               = Game.Vehicles.    PostVan;
using Resources             = Game.Economy.     Resources;
using SewageOutlet          = Game.Buildings.   SewageOutlet;
using Student               = Game.Buildings.   Student;
using SubArea               = Game.Areas.       SubArea;
using SubLane               = Game.Net.         SubLane;
using SubNet                = Game.Net.         SubNet;
using SubObject             = Game.Objects.     SubObject;
using UtilityObject         = Game.Objects.     UtilityObject;
using Watercraft            = Game.Vehicles.    Watercraft;
using WaterPumpingStation   = Game.Buildings.   WaterPumpingStation;

namespace BuildingUse
{
    /// <summary>
    /// System to set building colors.
    /// Adapted from Game.Rendering.ObjectColorSystem.
    /// This system replaces the game's ObjectColorSystem logic when one of this mod's infoviews is selected.
    /// </summary>
    public partial class BuildingColorSystem : GameSystemBase
    {
        /// <summary>
        /// Building status type and infomode index for an active infomode.
        /// </summary>
        private struct ActiveBuildingStatusType
        {
            public BUBuildingStatusType BuildingStatusType;
            public int InfomodeIndex;

            public ActiveBuildingStatusType(BUBuildingStatusType buildingStatusType, int infomodeIndex)
            {
                BuildingStatusType = buildingStatusType;
                InfomodeIndex = infomodeIndex;
            }
        }

        /// <summary>
        /// Building capacity for a building status type.
        /// </summary>
        private struct BuildingCapacity
        {
            public BUBuildingStatusType BuildingStatusType;
            public long Capacity;

            public BuildingCapacity(BUBuildingStatusType buildingStatusType, long capacity)
            {
                BuildingStatusType = buildingStatusType;
                Capacity = capacity;
            }
        }

        /// <summary>
        /// Entity and prefab for a main building or upgrade.
        /// </summary>
        private struct EntityPrefab
        {
            public Entity Entity;
            public Entity Prefab;

            public EntityPrefab(Entity entity, Entity prefab)
            {
                Entity = entity;
                Prefab = prefab;
            }
        }

        /// <summary>
        /// Used and capacity amounts for a building status type.
        /// </summary>
        private struct UsedCapacity
        {
            public BUBuildingStatusType BuildingStatusType;
            public long Used;
            public long Capacity;

            public UsedCapacity(BUBuildingStatusType buildingStatusType, long used, long capacity)
            {
                BuildingStatusType = buildingStatusType;
                Used = used;
                Capacity = capacity;
            }
        }

        /// <summary>
        /// Job to set the color to default on all objects that have a color.
        /// In this way, any object not set by subsequent jobs is assured to be the default color.
        /// </summary>
        [BurstCompile]
        private partial struct UpdateColorsJobDefault : IJobChunk
        {
            // Color component type to update.
            public ComponentTypeHandle<Color> ComponentTypeHandleColor;

            /// <summary>
            /// Job execution.
            /// </summary>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // Do all objects.
                NativeArray<Color> colors = chunk.GetNativeArray(ref ComponentTypeHandleColor);
                for (int i = 0; i < colors.Length; i++)
                {
                    // Set color index and value to default.
                    // SubColor remains unchanged.
                    Color color = colors[i];
                    color.m_Index = 0;
                    color.m_Value = 0;
                    colors[i] = color;
                }
            }
        }

        /// <summary>
        /// Job to set the color of each main building.
        /// See also other partial structs in files BuildingColorSystem* for each infoview.
        /// Burst compilation for this entire struct (including other partials) is handled here.
        /// </summary>
        [BurstCompile]
        private partial struct UpdateColorsJobMainBuilding : IJobChunk
        {
            // Color component type to update (not ReadOnly).
            public ComponentTypeHandle<Color> ComponentTypeHandleColor;

            // Buffer lookups.
            [ReadOnly] public BufferLookup<Efficiency       > BufferLookupEfficiency;
            [ReadOnly] public BufferLookup<Employee         > BufferLookupEmployee;
            [ReadOnly] public BufferLookup<HouseholdCitizen > BufferLookupHouseholdCitizen;
            [ReadOnly] public BufferLookup<InstalledUpgrade > BufferLookupInstalledUpgrade;
            [ReadOnly] public BufferLookup<LaneObject       > BufferLookupLaneObject;
            [ReadOnly] public BufferLookup<Occupant         > BufferLookupOccupant;
            [ReadOnly] public BufferLookup<OwnedVehicle     > BufferLookupOwnedVehicle;
            [ReadOnly] public BufferLookup<Patient          > BufferLookupPatient;
            [ReadOnly] public BufferLookup<Renter           > BufferLookupRenter;
            [ReadOnly] public BufferLookup<Resources        > BufferLookupResources;
            [ReadOnly] public BufferLookup<Student          > BufferLookupStudent;
            [ReadOnly] public BufferLookup<SubArea          > BufferLookupSubArea;
            [ReadOnly] public BufferLookup<SubLane          > BufferLookupSubLane;
            [ReadOnly] public BufferLookup<SubNet           > BufferLookupSubNet;
            [ReadOnly] public BufferLookup<SubObject        > BufferLookupSubObject;

            // Component lookups.
            [ReadOnly] public ComponentLookup<AdminBuildingData         > ComponentLookupAdminBuildingData;
            [ReadOnly] public ComponentLookup<Ambulance                 > ComponentLookupAmbulance;
            [ReadOnly] public ComponentLookup<Battery                   > ComponentLookupBattery;
            [ReadOnly] public ComponentLookup<BatteryData               > ComponentLookupBatteryData;
            [ReadOnly] public ComponentLookup<Bicycle                   > ComponentLookupBicycle;
            [ReadOnly] public ComponentLookup<BuildingData              > ComponentLookupBuildingData;
            [ReadOnly] public ComponentLookup<BuildingPropertyData      > ComponentLookupBuildingPropertyData;
            [ReadOnly] public ComponentLookup<Car                       > ComponentLookupCar;
            [ReadOnly] public ComponentLookup<CargoTransportStationData > ComponentLookupCargoTransportStationData;
            [ReadOnly] public ComponentLookup<Citizen                   > ComponentLookupCitizen;
            [ReadOnly] public ComponentLookup<CommercialProperty        > ComponentLookupCommercialProperty;
            [ReadOnly] public ComponentLookup<CompanyData               > ComponentLookupCompanyData;
            [ReadOnly] public ComponentLookup<CompanyStatisticData      > ComponentLookupCompanyStatisticData;
            [ReadOnly] public ComponentLookup<Curve                     > ComponentLookupCurve;
            [ReadOnly] public ComponentLookup<DeathcareFacility         > ComponentLookupDeathcareFacility;
            [ReadOnly] public ComponentLookup<DeathcareFacilityData     > ComponentLookupDeathcareFacilityData;
            [ReadOnly] public ComponentLookup<DeliveryTruck             > ComponentLookupDeliveryTruck;
            [ReadOnly] public ComponentLookup<DisasterFacilityData      > ComponentLookupDisasterFacilityData;
            [ReadOnly] public ComponentLookup<ElectricityProducer       > ComponentLookupElectricityProducer;
            [ReadOnly] public ComponentLookup<EmergencyGeneratorData    > ComponentLookupEmergencyGeneratorData;
            [ReadOnly] public ComponentLookup<EmergencyShelterData      > ComponentLookupEmergencyShelterData;
            [ReadOnly] public ComponentLookup<EvacuatingTransport       > ComponentLookupEvacuatingTransport;
            [ReadOnly] public ComponentLookup<ExtractorCompany          > ComponentLookupExtractorCompany;
            [ReadOnly] public ComponentLookup<ExtractorProperty         > ComponentLookupExtractorProperty;
            [ReadOnly] public ComponentLookup<FireEngine                > ComponentLookupFireEngine;
            [ReadOnly] public ComponentLookup<FireStationData           > ComponentLookupFireStationData;
            [ReadOnly] public ComponentLookup<FirewatchTowerData        > ComponentLookupFirewatchTowerData;
            [ReadOnly] public ComponentLookup<GarageLane                > ComponentLookupGarageLane;
            [ReadOnly] public ComponentLookup<GarbageFacility           > ComponentLookupGarbageFacility;
            [ReadOnly] public ComponentLookup<GarbageFacilityData       > ComponentLookupGarbageFacilityData;
            [ReadOnly] public ComponentLookup<GarbageTruck              > ComponentLookupGarbageTruck;
            [ReadOnly] public ComponentLookup<Geometry                  > ComponentLookupGeometry;
            [ReadOnly] public ComponentLookup<GroundWaterPoweredData    > ComponentLookupGroundWaterPoweredData;
            [ReadOnly] public ComponentLookup<HealthProblem             > ComponentLookupHealthProblem;
            [ReadOnly] public ComponentLookup<Hearse                    > ComponentLookupHearse;
            [ReadOnly] public ComponentLookup<Helicopter                > ComponentLookupHelicopter;
            [ReadOnly] public ComponentLookup<HospitalData              > ComponentLookupHospitalData;
            [ReadOnly] public ComponentLookup<Household                 > ComponentLookupHousehold;
            [ReadOnly] public ComponentLookup<IndustrialProcessData     > ComponentLookupIndustrialProcessData;
            [ReadOnly] public ComponentLookup<IndustrialProperty        > ComponentLookupIndustrialProperty;
            [ReadOnly] public ComponentLookup<MailBox                   > ComponentLookupMailBox;
            [ReadOnly] public ComponentLookup<MailBoxData               > ComponentLookupMailBoxData;
            [ReadOnly] public ComponentLookup<MaintenanceDepotData      > ComponentLookupMaintenanceDepotData;
            [ReadOnly] public ComponentLookup<MaintenanceVehicle        > ComponentLookupMaintenanceVehicle;
            [ReadOnly] public ComponentLookup<OfficeProperty            > ComponentLookupOfficeProperty;
            [ReadOnly] public ComponentLookup<Owner                     > ComponentLookupOwner;
            [ReadOnly] public ComponentLookup<ParkData                  > ComponentLookupParkData;
            [ReadOnly] public ComponentLookup<ParkedCar                 > ComponentLookupParkedCar;
            [ReadOnly] public ComponentLookup<ParkedTrain               > ComponentLookupParkedTrain;
            [ReadOnly] public ComponentLookup<ParkingFacilityData       > ComponentLookupParkingFacilityData;
            [ReadOnly] public ComponentLookup<ParkingLane               > ComponentLookupParkingLane;
            [ReadOnly] public ComponentLookup<ParkingLaneData           > ComponentLookupParkingLaneData;
            [ReadOnly] public ComponentLookup<ParkMaintenance           > ComponentLookupParkMaintenance;
            [ReadOnly] public ComponentLookup<PoliceCar                 > ComponentLookupPoliceCar;
            [ReadOnly] public ComponentLookup<PoliceStationData         > ComponentLookupPoliceStationData;
            [ReadOnly] public ComponentLookup<PostFacility              > ComponentLookupPostFacility;
            [ReadOnly] public ComponentLookup<PostFacilityData          > ComponentLookupPostFacilityData;
            [ReadOnly] public ComponentLookup<PostVan                   > ComponentLookupPostVan;
            [ReadOnly] public ComponentLookup<PowerPlantData            > ComponentLookupPowerPlantData;
            [ReadOnly] public ComponentLookup<PrefabRef                 > ComponentLookupPrefabRef;
            [ReadOnly] public ComponentLookup<PrisonData                > ComponentLookupPrisonData;
            [ReadOnly] public ComponentLookup<PrisonerTransport         > ComponentLookupPrisonerTransport;
            [ReadOnly] public ComponentLookup<ResearchFacilityData      > ComponentLookupResearchFacilityData;
            [ReadOnly] public ComponentLookup<ResidentialProperty       > ComponentLookupResidentialProperty;
            [ReadOnly] public ComponentLookup<ResourceData              > ComponentLookupResourceData;
            [ReadOnly] public ComponentLookup<RoadMaintenance           > ComponentLookupRoadMaintenance;
            [ReadOnly] public ComponentLookup<SchoolData                > ComponentLookupSchoolData;
            [ReadOnly] public ComponentLookup<SewageOutlet              > ComponentLookupSewageOutlet;
            [ReadOnly] public ComponentLookup<SewageOutletData          > ComponentLookupSewageOutletData;
            [ReadOnly] public ComponentLookup<SolarPoweredData          > ComponentLookupSolarPoweredData;
            [ReadOnly] public ComponentLookup<SpawnableBuildingData     > ComponentLookupSpawnableBuildingData;
            [ReadOnly] public ComponentLookup<Storage                   > ComponentLookupStorage;
            [ReadOnly] public ComponentLookup<StorageAreaData           > ComponentLookupStorageAreaData;
            [ReadOnly] public ComponentLookup<StorageLimitData          > ComponentLookupStorageLimitData;
            [ReadOnly] public ComponentLookup<StorageProperty           > ComponentLookupStorageProperty;
            [ReadOnly] public ComponentLookup<TelecomFacilityData       > ComponentLookupTelecomFacilityData;
            [ReadOnly] public ComponentLookup<Train                     > ComponentLookupTrain;
            [ReadOnly] public ComponentLookup<TransformerData           > ComponentLookupTransformerData;
            [ReadOnly] public ComponentLookup<TransportCompanyData      > ComponentLookupTransportCompanyData;
            [ReadOnly] public ComponentLookup<TransportDepotData        > ComponentLookupTransportDepotData;
            [ReadOnly] public ComponentLookup<TransportStationData      > ComponentLookupTransportStationData;
            [ReadOnly] public ComponentLookup<Watercraft                > ComponentLookupWatercraft;
            [ReadOnly] public ComponentLookup<WaterPoweredData          > ComponentLookupWaterPoweredData;
            [ReadOnly] public ComponentLookup<WaterPumpingStation       > ComponentLookupWaterPumpingStation;
            [ReadOnly] public ComponentLookup<WaterPumpingStationData   > ComponentLookupWaterPumpingStationData;
            [ReadOnly] public ComponentLookup<WelfareOfficeData         > ComponentLookupWelfareOfficeData;
            [ReadOnly] public ComponentLookup<WindPoweredData           > ComponentLookupWindPoweredData;
            [ReadOnly] public ComponentLookup<WorkplaceData             > ComponentLookupWorkplaceData;
            [ReadOnly] public ComponentLookup<WorkProvider              > ComponentLookupWorkProvider;

            // Component type handles.
            [ReadOnly] public ComponentTypeHandle<CurrentDistrict       > ComponentTypeHandleCurrentDistrict;
            [ReadOnly] public ComponentTypeHandle<Destroyed             > ComponentTypeHandleDestroyed;
            [ReadOnly] public ComponentTypeHandle<PrefabRef             > ComponentTypeHandlePrefabRef;
            [ReadOnly] public ComponentTypeHandle<UnderConstruction     > ComponentTypeHandleUnderConstruction;

            // Entity type handle.
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;

            // Miscellaneous data.
            [ReadOnly] public EconomyParameterData EconomyParameters;
			[ReadOnly] public ResourcePrefabs ResourcePrefabs;

            // Active infoview and corresponding active building status types.
            [ReadOnly] public BUInfoview ActiveInfoview;
            [ReadOnly] public NativeList<ActiveBuildingStatusType> ActiveBuildingStatusTypes;

            // Mod settings used in the job.
            [ReadOnly] public bool CountVehiclesInUse;
            [ReadOnly] public bool CountVehiclesInMaintenance;
            [ReadOnly] public bool EfficiencyMaxColor200Percent;
            [ReadOnly] public bool ProductionMaxColor200Percent;

            // Selected district.
            [ReadOnly] public Entity SelectedDistrict;
            [ReadOnly] public bool SelectedDistrictIsEntireCity;

            // Array of lists to return used and capacity to the BuildingColorSystem.
            // The outer array is one for each possible thread.
            // The inner list is one for each used and capacity computed in that thread.
            // Even though the outer array is read only, entries can still be added to the inner lists.
            [ReadOnly] public NativeArray<NativeList<UsedCapacity>> TotalUsedCapacity;

            /// <summary>
            /// Job execution.
            /// </summary>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // Get colors to set.
                NativeArray<Color            > colors             = chunk.GetNativeArray(ref ComponentTypeHandleColor);

                // Get arrays from chunk.
                NativeArray<Entity           > entities           = chunk.GetNativeArray(EntityTypeHandle);
                NativeArray<PrefabRef        > prefabRefs         = chunk.GetNativeArray(ref ComponentTypeHandlePrefabRef);
                NativeArray<CurrentDistrict  > currentDistricts   = chunk.GetNativeArray(ref ComponentTypeHandleCurrentDistrict);
                NativeArray<Destroyed        > destroyeds         = chunk.GetNativeArray(ref ComponentTypeHandleDestroyed);
                NativeArray<UnderConstruction> underConstructions = chunk.GetNativeArray(ref ComponentTypeHandleUnderConstruction);

                // Do each entity (i.e. building).
                for (int i = 0; i < entities.Length; i++)
                {
                    // Main building must be in selected district.
                    if (SelectedDistrictIsEntireCity || (currentDistricts[i].m_District == SelectedDistrict))
                    {
                        // Get the entities and prefabs for the main building and installed upgrades.
                        // This list is created once here and then re-used for each building status type in the active infoview.
                        // Always include the main building as the first entry.
                        NativeList<EntityPrefab> mainBuildingAndUpgrades = new(4, Allocator.Temp)
                        { new EntityPrefab(entities[i], prefabRefs[i].m_Prefab) };

                        // Add activated installed upgrades to the list.
                        if (BufferLookupInstalledUpgrade.TryGetBuffer(entities[i], out DynamicBuffer<InstalledUpgrade> installedUpgrades) &&
                            installedUpgrades.IsCreated)
                        {
                            foreach (InstalledUpgrade installedUpgrade in installedUpgrades)
                            {
                                // Installed upgrade must be active and have a prefab.
                                Entity upgradeEntity = installedUpgrade.m_Upgrade;
                                if (!BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive) &&
                                    ComponentLookupPrefabRef.TryGetComponent(upgradeEntity, out PrefabRef upgradePrefabRef))
                                {
                                    mainBuildingAndUpgrades.Add(new EntityPrefab(upgradeEntity, upgradePrefabRef.m_Prefab));
                                }
                            }
                        }

                        // Do the building according to the active infoview.
                        Color color = colors[i];
                        switch (ActiveInfoview)
                        {
                            case BUInfoview.Employees:  DoBuildingEmployees (in mainBuildingAndUpgrades, ref color); break;
                            case BUInfoview.Visitors:   DoBuildingVisitors  (in mainBuildingAndUpgrades, ref color); break;
                            case BUInfoview.Storage:    DoBuildingStorage   (in mainBuildingAndUpgrades, ref color); break;
                            case BUInfoview.Vehicles:   DoBuildingVehicles  (in mainBuildingAndUpgrades, ref color); break;
                            case BUInfoview.Efficiency: DoBuildingEfficiency(in mainBuildingAndUpgrades, ref color); break;
                            case BUInfoview.Processing: DoBuildingProcessing(in mainBuildingAndUpgrades, ref color); break;
                            case BUInfoview.Production: DoBuildingProduction(in mainBuildingAndUpgrades, ref color); break;
                        }

                        // Set building to color that may or may not have been updated in the DoBuilding* method.
                        colors[i] = color;
                    }

                    // Check if should set SubColor flag on the color.
                    // Logic adapted from Game.Rendering.ObjectColorSystem.CheckColors().
                    if ((ComponentLookupBuildingData[prefabRefs[i].m_Prefab].m_Flags & BuildingFlags.ColorizeLot) != 0 || 
                        (CollectionUtils.TryGet(destroyeds,         i, out Destroyed         destroyed        ) && destroyed.m_Cleared >= 0f) || 
                        (CollectionUtils.TryGet(underConstructions, i, out UnderConstruction underConstruction) && underConstruction.m_NewPrefab == Entity.Null))
                    {
                        // Set SubColor flag on the color.
                        // SubColor determines whether or not the building's lot should be colorized.
                        Color color = colors[i];
                        color.m_SubColor = true;
                        colors[i] = color;
                    }
                }
            }

            /// <summary>
            /// Update entity color.
            /// </summary>
            private void UpdateEntityColor(BUBuildingStatusType buildingStatusType, long used, long capacity, ref Color color)
            {
                // Check if building status type is active.
                foreach (ActiveBuildingStatusType activeBuildingStatusType in ActiveBuildingStatusTypes)
                {
                    if (activeBuildingStatusType.BuildingStatusType == buildingStatusType)
                    {
                        // Building status type is active.

                        // Set color index from the active building status type.
                        color.m_Index = (byte)activeBuildingStatusType.InfomodeIndex;

                        // Set color value according to the building use ratio.
                        // All infomodes for this mod have a range from 0 to 255 which represents 0% to 100%.
                        float useRatio = capacity > 0L ? (float)used / capacity : 0f;
                        color.m_Value = (byte)math.clamp(Mathf.RoundToInt(255f * useRatio), 0, 255);

                        // Stop searching for active building status type.
                        break;
                    }
                }
            }

            /// <summary>
            /// Update total used and capacity data.
            /// </summary>
            private void UpdateTotalUsedCapacity(BUBuildingStatusType buildingStatusType, long used, long capacity)
            {
                // Add an entry even if both values are zero so that the building can be counted.
                // Add the entry for this thread.
                // By having a separate entry for each thread, parallel threads will never access the same inner list at the same time.
                TotalUsedCapacity[JobsUtility.ThreadIndex].Add(new UsedCapacity(buildingStatusType, used, capacity));
            }

            /// <summary>
            /// Update entity color and total used and capacity data.
            /// </summary>
            private void UpdateEntityColorAndTotalUsedCapacity(BUBuildingStatusType buildingStatusType, long used, long capacity, ref Color color)
            {
                // Update entity color and total used and capacity using the same used and capacity values for both.
                UpdateEntityColor(buildingStatusType, used, capacity, ref color);
                UpdateTotalUsedCapacity(buildingStatusType, used, capacity);
            }

            /// <summary>
            /// Get length of dynamic buffer from a buffer lookup.
            /// </summary>
            private int GetDynamicBufferLength<T>(Entity entity, in BufferLookup<T> bufferLookup) where T : unmanaged, IBufferElementData
            {
                // Check if entity has buffer.
                if (bufferLookup.TryGetBuffer(entity, out DynamicBuffer<T> dynamicBuffer) &&
                    dynamicBuffer.IsCreated)
                {
                    // Return length of dynamic buffer.
                    return dynamicBuffer.Length;
                }

                // No buffer length.
                return 0;
            }

            /// <summary>
            /// Check if building has a company and return the company.
            /// </summary>
            private bool TryGetCompany(Entity buildingEntity, out Entity companyEntity)
            {
                // Initialize return company.
                companyEntity = Entity.Null;

                // Logic adapted from Game.UI.InGame.CompanyUIUtils.HasCompany().

                // Building must have a renter buffer.
                // Building must allow sold, manufactured, or stored resources.
                if (BufferLookupRenter.TryGetBuffer(buildingEntity, out DynamicBuffer<Renter> bufferRenters) &&
                    bufferRenters.IsCreated &&
                    ComponentLookupPrefabRef.TryGetComponent(buildingEntity, out PrefabRef prefabRef) &&
                    ComponentLookupBuildingPropertyData.TryGetComponent(prefabRef.m_Prefab, out BuildingPropertyData buildingPropertyData) &&
                    (buildingPropertyData.m_AllowedSold         != Resource.NoResource ||
                     buildingPropertyData.m_AllowedManufactured != Resource.NoResource ||
                     buildingPropertyData.m_AllowedStored       != Resource.NoResource))
                {
                    // Find and return the renter that has company data, if any.
                    foreach (Renter renter in bufferRenters)
                    {
                        if (ComponentLookupCompanyData.HasComponent(renter.m_Renter))
                        {
                            companyEntity = renter.m_Renter;
                            return true;
                        }
                    }
                }

                // Company not found.
                return false;
            }

            // The following methods return whether or not the building (main or upgrade) has residential, commercial, industrial, or office.
            // Check the property of the entity, which determines whether or not the building CAN have RCIO,
            // even if the building does not currently have RCIO.
            // The prefab is not checked.
            private bool BuildingHasResidential(Entity entity) => ComponentLookupResidentialProperty.HasComponent(entity);
            private bool BuildingHasCommercial (Entity entity) => ComponentLookupCommercialProperty.HasComponent(entity);
            private bool BuildingHasIndustrial (Entity entity) => ComponentLookupIndustrialProperty.HasComponent(entity) &&
                                                                 !ComponentLookupOfficeProperty    .HasComponent(entity);
            private bool BuildingHasOffice     (Entity entity) => ComponentLookupOfficeProperty.HasComponent(entity);

            // The following methods return whether or not the building (main or upgrade) currently has a service.
            // The logic uses the prefab data with conditons on the prefab data fields
            // because for some main buildings only an upgrade provides the service.
            // For example:
            //      The Grandview Heights (Skyscrapers DLC) does not really have hospital until one of its upgrades is installed.
            //      The main building entity has Hospital, so the presence of that component alone is not enough to have hospital.
            //      The main building prefab has HospitalData, so the presence of that prefab component alone is not enough to have hospital.
            //      The main building prefab has zeroes for all fields in HospitalData.
            //      Having all zeroes in HospitalData fields determines that the main building alone does not have hospital.
            //      Both of its upgrades have at least one non-zero field in HospitalData.
            //      So once an upgrade is installed, then the entire building is determined to have hospital.
            // For most ordinary healthcare buildings:
            //      The main building has at least one non-zero field in its HospitalData.
            //      So an upgrade is not needed to determine that the entire building has hospital.
            // Methods listed in alphabetical order.
            private bool BuildingHasAdminBuilding(Entity prefab)
            {
                return
                    ComponentLookupAdminBuildingData.HasComponent(prefab);
            }

            private bool BuildingHasAdministration(Entity prefab)
            {
                return
                    BuildingHasAdminBuilding(prefab) ||
                    BuildingHasWelfareOffice(prefab);
            }

            private bool BuildingHasBattery(Entity prefab)
            {
                return
                    BuildingHasBattery(prefab, out BatteryData _);
            }

            private bool BuildingHasBattery(Entity prefab, out BatteryData batteryData)
            {
                return
                    ComponentLookupBatteryData.TryGetComponent(prefab, out batteryData) &&
                    (batteryData.m_Capacity    > 0 ||
                     batteryData.m_PowerOutput > 0);
            }

            private bool BuildingHasCargoTransportStation(Entity prefab)
            {
                // The WorkMultiplier is the only field and sometimes 0, so don't include that field when checking.
                return
                    ComponentLookupCargoTransportStationData.HasComponent(prefab);
            }

            private bool BuildingHasDeathcareFacility(Entity prefab)
            {
                return
                    BuildingHasDeathcareFacility(prefab, out DeathcareFacilityData _);
            }

            private bool BuildingHasDeathcareFacility(Entity prefab, out DeathcareFacilityData deathcareFacilityData)
            {
                return
                    ComponentLookupDeathcareFacilityData.TryGetComponent(prefab, out deathcareFacilityData) &&
                    (deathcareFacilityData.m_HearseCapacity  > 0  ||
                     deathcareFacilityData.m_StorageCapacity > 0  ||
                     deathcareFacilityData.m_ProcessingRate  > 0f);
            }

            private bool BuildingHasDisasterControl(Entity prefab)
            {
                return
                    BuildingHasEmergencyShelter(prefab) ||
                    BuildingHasDisasterFacility(prefab);
            }

            private bool BuildingHasDisasterFacility(Entity prefab)
            {
                return
                    ComponentLookupDisasterFacilityData.HasComponent(prefab);
            }

            private bool BuildingHasElectricity(Entity prefab)
            {
                return
                    BuildingHasPowerPlant        (prefab) ||
                    BuildingHasBattery           (prefab) ||
                    BuildingHasGroundWaterPowered(prefab) ||
                    BuildingHasWaterPowered      (prefab) ||
                    BuildingHasWindPowered       (prefab) ||
                    BuildingHasSolarPowered      (prefab) ||
                    BuildingHasTransformer       (prefab) ||
                    BuildingHasEmergencyGenerator(prefab);
            }

            private bool BuildingHasEmergencyGenerator(Entity prefab)
            {
                return
                    ComponentLookupEmergencyGeneratorData.TryGetComponent(prefab, out EmergencyGeneratorData emergencyGeneratorData) &&
                    (emergencyGeneratorData.m_ElectricityProduction > 0);
            }

            private bool BuildingHasEmergencyShelter(Entity prefab)
            {
                return
                    BuildingHasEmergencyShelter(prefab, out EmergencyShelterData _);
            }

            private bool BuildingHasEmergencyShelter(Entity prefab, out EmergencyShelterData emergencyShelterData)
            {
                return
                    ComponentLookupEmergencyShelterData.TryGetComponent(prefab, out emergencyShelterData) &&
                    (emergencyShelterData.m_ShelterCapacity > 0 ||
                     emergencyShelterData.m_VehicleCapacity > 0);
            }

            private bool BuildingHasFireRescue(Entity prefab)
            {
                return
                    BuildingHasFireStation   (prefab, out FireStationData _) ||
                    BuildingHasFirewatchTower(prefab);
            }

            private bool BuildingHasFireStation(Entity prefab, out FireStationData fireStationData)
            {
                return
                    ComponentLookupFireStationData.TryGetComponent(prefab, out fireStationData) &&
                    (fireStationData.m_FireEngineCapacity       > 0 ||
                     fireStationData.m_FireHelicopterCapacity   > 0 ||
                     fireStationData.m_DisasterResponseCapacity > 0 ||
                     fireStationData.m_VehicleEfficiency        > 0f);
            }

            private bool BuildingHasFirewatchTower(Entity prefab)
            {
                return
                    ComponentLookupFirewatchTowerData.HasComponent(prefab);
            }

            private bool BuildingHasGarbageFacility(Entity prefab)
            {
                return
                    BuildingHasGarbageFacility(prefab, out GarbageFacilityData _);
            }

            private bool BuildingHasGarbageFacility(Entity prefab, out GarbageFacilityData garbageFacilityData)
            {
                // GarbagePoweredData is only on prefabs that already have GarbageFacilityData.
                // So no need to check specially for GarbagePoweredData.
                return
                    ComponentLookupGarbageFacilityData.TryGetComponent(prefab, out garbageFacilityData) &&
                    (garbageFacilityData.m_GarbageCapacity   > 0 ||
                     garbageFacilityData.m_VehicleCapacity   > 0 ||
                     garbageFacilityData.m_TransportCapacity > 0 ||
                     garbageFacilityData.m_ProcessingSpeed   > 0);
            }

            private bool BuildingHasGroundWaterPowered(Entity prefab)
            {
                return
                    ComponentLookupGroundWaterPoweredData.TryGetComponent(prefab, out GroundWaterPoweredData groundWaterPoweredData) &&
                    (groundWaterPoweredData.m_Production         > 0 ||
                     groundWaterPoweredData.m_MaximumGroundWater > 0);
            }

            private bool BuildingHasHospital(Entity prefab)
            {
                return
                    BuildingHasHospital(prefab, out HospitalData _);
            }

            private bool BuildingHasHospital(Entity prefab, out HospitalData hospitalData)
            {
                return
                    ComponentLookupHospitalData.TryGetComponent(prefab, out hospitalData) &&
                    (hospitalData.m_PatientCapacity           > 0 ||
                     hospitalData.m_AmbulanceCapacity         > 0 ||
                     hospitalData.m_MedicalHelicopterCapacity > 0 ||
                     hospitalData.m_TreatmentBonus            > 0 ||
                     hospitalData.m_HealthRange.x             > 0 ||
                     hospitalData.m_HealthRange.y             > 0 ||
                     hospitalData.m_TreatDiseases                 ||
                     hospitalData.m_TreatInjuries);
            }

            private bool BuildingHasMailBox(Entity prefab, out MailBoxData mailBoxData)
            {
                return
                    ComponentLookupMailBoxData.TryGetComponent(prefab, out mailBoxData) &&
                    (mailBoxData.m_MailCapacity > 0);
            }

            private bool BuildingHasPark(Entity prefab)
            {
                return
                    ComponentLookupParkData.TryGetComponent(prefab, out ParkData parkData) &&
                    (parkData.m_MaintenancePool > 0);
            }

            private bool BuildingHasParkingFacility(Entity prefab)
            {
                return
                    BuildingHasParkingFacility(prefab, out ParkingFacilityData _);
            }

            private bool BuildingHasParkingFacility(Entity prefab, out ParkingFacilityData parkingFacilityData)
            {
                // For BicycleParkingHall02 (Large Bicycle Parking Hall):
                //      The BicycleParkingHall02 main building prefab has non-zeroes for both fields of ParkingFacilityData.
                //      The BicycleParkingHall02 Rear Entrance upgrade prefab has zero for both fields of ParkingFacilityData.
                //      Therefore, the BicycleParkingHall02 Rear Entrance upgrade will never add parking
                //      to the building where parking was not already there for the main building.
                // For Pack10-OHSignature05 (Skyscrapers DLC, Arroyo Seco Building):
                //      The Pack10-OHSignature05 main building prefab has zeroes for both fields of ParkingFacilityData.
                //      The Pack10-OHSignature05_Ext01 upgrade prefab has a non-zero for one of the fields of ParkingFacilityData.
                //      Therefore, Pack10-OHSignature05 will have parking only if the upgrade is installed, as desired.
                // For BusStation03 (City Bus Station):
                //      The BusStation03 main building prefab does not have ParkingFacilityData component at all.
                //      The BusStation03 Parking Hall upgrade prefab has zero for both fields of ParkingFacilityData.
                //      Therefore, BusStation03 will never have parking because
                //      both the main building and the upgrade fail the ParkingFacilityData tests below.
                return
                    ComponentLookupParkingFacilityData.TryGetComponent(prefab, out parkingFacilityData) &&
                    (parkingFacilityData.m_GarageMarkerCapacity > 0 ||
                     parkingFacilityData.m_ComfortFactor        > 0f);
            }

            private bool BuildingHasParkMaintenance(Entity prefab, Entity entity)
            {
                return
                    BuildingHasParkMaintenance(prefab, entity, out MaintenanceDepotData _);
            }

            private bool BuildingHasParkMaintenance(Entity prefab, Entity entity, out MaintenanceDepotData maintenanceDepotData)
            {
                return
                    ComponentLookupMaintenanceDepotData.TryGetComponent(prefab, out maintenanceDepotData) &&
                    (maintenanceDepotData.m_VehicleCapacity   > 0 ||
                     maintenanceDepotData.m_VehicleEfficiency > 0f) &&
                    // MaintenanceDepotData.MaintenanceType is not set for upgrades.
                    // So either entity or entity's owner must have ParkMaintenance.
                    (ComponentLookupParkMaintenance.HasComponent(entity) ||
                     (ComponentLookupOwner.TryGetComponent(entity, out Owner owner) && 
                      ComponentLookupParkMaintenance.HasComponent(owner.m_Owner)));
            }

            private bool BuildingHasPolice(Entity prefab)
            {
                return
                    BuildingHasPoliceStation(prefab, out PoliceStationData _) ||
                    BuildingHasPrison       (prefab, out PrisonData _);
            }

            private bool BuildingHasPoliceStation(Entity prefab, out PoliceStationData policeStationData)
            {
                return
                    ComponentLookupPoliceStationData.TryGetComponent(prefab, out policeStationData) &&
                    (policeStationData.m_JailCapacity             > 0 ||
                     policeStationData.m_PatrolCarCapacity        > 0 ||
                     policeStationData.m_PoliceHelicopterCapacity > 0 ||
                     policeStationData.m_PurposeMask             != 0);
            }

            private bool BuildingHasPostFacility(Entity prefab)
            {
                return
                    BuildingHasPostFacility(prefab, out PostFacilityData _);
            }

            private bool BuildingHasPostFacility(Entity prefab, out PostFacilityData postFacilityData)
            {
                return
                    ComponentLookupPostFacilityData.TryGetComponent(prefab, out postFacilityData) &&
                    (postFacilityData.m_PostVanCapacity   > 0 ||
                     postFacilityData.m_PostTruckCapacity > 0 ||
                     postFacilityData.m_MailCapacity      > 0 ||
                     postFacilityData.m_SortingRate       > 0);
            }

            private bool BuildingHasPowerPlant(Entity prefab)
            {
                return
                    ComponentLookupPowerPlantData.TryGetComponent(prefab, out PowerPlantData powerPlantData) &&
                    (powerPlantData.m_ElectricityProduction > 0);
            }

            private bool BuildingHasPrison(Entity prefab, out PrisonData prisonData)
            {
                return
                    ComponentLookupPrisonData.TryGetComponent(prefab, out prisonData) &&
                    (prisonData.m_PrisonerCapacity  > 0 ||
                     prisonData.m_PrisonVanCapacity > 0 ||
                     prisonData.m_PrisonerWellbeing > 0 ||
                     prisonData.m_PrisonerHealth    > 0);
            }

            private bool BuildingHasResearchFacility(Entity prefab)
            {
                return
                    ComponentLookupResearchFacilityData.HasComponent(prefab);
            }

            private bool BuildingHasRoadMaintenance(Entity prefab, Entity entity)
            {
                return
                    BuildingHasRoadMaintenance(prefab, entity, out MaintenanceDepotData _);
            }

            private bool BuildingHasRoadMaintenance(Entity prefab, Entity entity, out MaintenanceDepotData maintenanceDepotData)
            {
                return
                    ComponentLookupMaintenanceDepotData.TryGetComponent(prefab, out maintenanceDepotData) &&
                    (maintenanceDepotData.m_VehicleCapacity   > 0 ||
                     maintenanceDepotData.m_VehicleEfficiency > 0f) &&
                    // MaintenanceDepotData.MaintenanceType is not set for upgrades.
                    // So either entity or entity's owner must have RoadMaintenance.
                    (ComponentLookupRoadMaintenance.HasComponent(entity) ||
                     (ComponentLookupOwner.TryGetComponent(entity, out Owner owner) && 
                      ComponentLookupRoadMaintenance.HasComponent(owner.m_Owner)));
            }

            private bool BuildingHasSchool(Entity prefab)
            {
                return
                    BuildingHasSchool(prefab, out SchoolData _);
            }

            private bool BuildingHasSchool(Entity prefab, out SchoolData schoolData)
            {
                return
                    ComponentLookupSchoolData.TryGetComponent(prefab, out schoolData) &&
                    (schoolData.m_StudentCapacity    > 0  ||
                     schoolData.m_GraduationModifier > 0f ||
                     schoolData.m_StudentWellbeing   > 0  ||
                     schoolData.m_StudentHealth      > 0);
            }

            private bool BuildingHasSewageOutlet(Entity prefab)
            {
                return
                    ComponentLookupSewageOutletData.TryGetComponent(prefab, out SewageOutletData sewageOutletData) &&
                    (sewageOutletData.m_Capacity     > 0 ||
                     sewageOutletData.m_Purification > 0f);
            }

            private bool BuildingHasSolarPowered(Entity prefab)
            {
                return
                    ComponentLookupSolarPoweredData.TryGetComponent(prefab, out SolarPoweredData solarPoweredData) &&
                    (solarPoweredData.m_Production > 0);
            }

            private bool BuildingHasTelecomFacility(Entity prefab)
            {
                return
                    ComponentLookupTelecomFacilityData.TryGetComponent(prefab, out TelecomFacilityData telecomFacilityData) &&
                    (telecomFacilityData.m_NetworkCapacity > 0f ||
                     telecomFacilityData.m_Range           > 0f ||
                     telecomFacilityData.m_PenetrateTerrain);
            }

            private bool BuildingHasTransformer(Entity prefab)
            {
                return
                    ComponentLookupTransformerData.HasComponent(prefab);
            }

            private bool BuildingHasTransportation(Entity prefab)
            {
                return
                    BuildingHasTransportDepot       (prefab, out TransportDepotData _) ||
                    BuildingHasTransportStation     (prefab) ||
                    BuildingHasCargoTransportStation(prefab);
            }

            private bool BuildingHasTransportDepot(Entity prefab, out TransportDepotData transportDepotData)
            {
                return
                    ComponentLookupTransportDepotData.TryGetComponent(prefab, out transportDepotData) &&
                    (transportDepotData.m_VehicleCapacity     > 0  ||
                     transportDepotData.m_ProductionDuration  > 0f ||
                     transportDepotData.m_MaintenanceDuration > 0f ||
                     transportDepotData.m_DispatchCenter);
            }

            private bool BuildingHasTransportStation(Entity prefab)
            {
                return
                    ComponentLookupTransportStationData.TryGetComponent(prefab, out TransportStationData transportStationData) &&
                    (transportStationData.m_ComfortFactor         > 0f ||
                     transportStationData.m_LoadingFactor         > 0f ||
                     transportStationData.m_CarRefuelTypes        != 0 ||
                     transportStationData.m_TrainRefuelTypes      != 0 ||
                     transportStationData.m_WatercraftRefuelTypes != 0 ||
                     transportStationData.m_AircraftRefuelTypes   != 0);
            }

            private bool BuildingHasWaterPowered(Entity prefab)
            {
                return
                    ComponentLookupWaterPoweredData.TryGetComponent(prefab, out WaterPoweredData waterPoweredData) &&
                    (waterPoweredData.m_ProductionFactor > 0f ||
                     waterPoweredData.m_CapacityFactor   > 0f);
            }

            private bool BuildingHasWaterPumpingStation(Entity prefab, Entity entity)
            {
                // The Water Treatment Plant has WaterPumpingStationData but with zeroes for data fields.
                // So the Water Treatment Plant does not satisfy the WaterPumpingStationData check below.
                // In order to include the Water Treatment Plant, also check for WaterPumpingStation with capacity.
                // Unfortunately, the Water Treatment Plant does not have capacity until it actually starts pumping water.
                // This delay is acceptable so that the Water Treatment Plant can be included at least when it starts pumping.
                return
                    (ComponentLookupWaterPumpingStationData.TryGetComponent(prefab, out WaterPumpingStationData waterPumpingStationData) &&
                     (waterPumpingStationData.m_Capacity     > 0 ||
                      waterPumpingStationData.m_Purification > 0f))
                     ||
                    (ComponentLookupWaterPumpingStation.TryGetComponent(entity, out WaterPumpingStation waterPumpingStation) &&
                     (waterPumpingStation.m_Capacity > 0));
            }

            private bool BuildingHasWelfareOffice(Entity prefab)
            {
                return
                    ComponentLookupWelfareOfficeData.HasComponent(prefab);
            }

            private bool BuildingHasWindPowered(Entity prefab)
            {
                return
                    ComponentLookupWindPoweredData.TryGetComponent(prefab, out WindPoweredData windPoweredData) &&
                    (windPoweredData.m_Production  > 0 ||
                     windPoweredData.m_MaximumWind > 0f);
            }
        }


        /// <summary>
        /// Job to set the color of each attachment building to the color of the building to which it is attached.
        /// Attachment buildings are the lots attached to specialized industry hubs.
        /// </summary>
        [BurstCompile]
        private struct UpdateColorsJobAttachmentBuilding : IJobChunk
        {
            // Color component lookup to update.
            [NativeDisableParallelForRestriction] public ComponentLookup<Color> ComponentLookupColor;

            // Component type handles.
            [ReadOnly] public ComponentTypeHandle<Attachment> ComponentTypeHandleAttachment;

            // Entity type handle.
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;

            /// <summary>
            /// Job execution.
            /// </summary>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // Do each attachment entity.
                NativeArray<Attachment> attachments = chunk.GetNativeArray(ref ComponentTypeHandleAttachment);
                NativeArray<Entity    > entities    = chunk.GetNativeArray(EntityTypeHandle);
                for (int i = 0; i < entities.Length; i++)
                {
                    // Get the color of the attached entity.
                    if (ComponentLookupColor.TryGetComponent(attachments[i].m_Attached, out Color attachedColor))
                    {

                        // Set color of this attachment entity to the color of the attached entity.
                        Entity entity = entities[i];
                        Color color = ComponentLookupColor[entity];
                        color.m_Index = attachedColor.m_Index;
                        color.m_Value = attachedColor.m_Value;
                        ComponentLookupColor[entity] = color;
                    }
                }
            }
        }


        /// <summary>
        /// Job to set the color of each middle building to the color of its owner.
        /// Middle buildings include sub buildings (i.e. building upgrades placed around the perimeter of the main building).
        /// Logic is adapted from Game.Rendering.ObjectColorSystem.UpdateMiddleObjectColorsJob except:
        ///     Handle only buildings.
        ///     Handle port middle buildings specially.
        ///     Variables are renamed to improve readability.
        /// </summary>
        [BurstCompile]
        private struct UpdateColorsJobMiddleBuilding : IJobChunk
        {
            // Color component lookup to update.
            [NativeDisableParallelForRestriction] public ComponentLookup<Color> ComponentLookupColor;

            // Component lookups.
            [ReadOnly] public ComponentLookup<BuildingData          > ComponentLookupBuildingData;
            [ReadOnly] public ComponentLookup<PrefabRef             > ComponentLookupPrefabRef;

            // Component type handles.
            [ReadOnly] public ComponentTypeHandle<Owner             > ComponentTypeHandleOwner;
            [ReadOnly] public ComponentTypeHandle<PrefabRef         > ComponentTypeHandlePrefabRef;

            // Entity type handle.
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;

            /// <summary>
            /// Job execution.
            /// </summary>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // Do each entity.
                NativeArray<Owner    > owners     = chunk.GetNativeArray(ref ComponentTypeHandleOwner);
                NativeArray<PrefabRef> prefabRefs = chunk.GetNativeArray(ref ComponentTypeHandlePrefabRef);
                NativeArray<Entity   > entities   = chunk.GetNativeArray(EntityTypeHandle);
                for (int i = 0; i < entities.Length; i++)
                {
                    // Get color of owner building.
                    if (ComponentLookupColor.TryGetComponent(owners[i].m_Owner, out Color ownerColor))
                    {
                        // Set color of this middle building to color of owner entity.
                        Entity entity = entities[i];
                        Color color = ComponentLookupColor[entity];
                        color.m_Index = ownerColor.m_Index;
                        color.m_Value = ownerColor.m_Value;

                        // Determine whether or not this middle building's lot should be colorized.
                        // This is specified in Color.m_SubColor.
                        // For unknown reasons, the Bulk Storage Yards in a Port start with m_SubColor = false
                        // when the Bulk Storage Yards should have m_SubColor = true;
                        // So need to set m_SubColor here according to the building prefab's ColorizeLot flag.
                        // For simplicity, check SubColor for all middle buildings, not just Bulk Storage Yards.
                        if (!color.m_SubColor &&
                            ComponentLookupBuildingData.TryGetComponent(prefabRefs[i].m_Prefab, out BuildingData buildingData) &&
                            (buildingData.m_Flags & BuildingFlags.ColorizeLot) != 0)
                        {
                            color.m_SubColor = true;
                        }

                        // Set the updated color.
                        ComponentLookupColor[entity] = color;
                    }
                }
            }
        }


        /// <summary>
        /// Job to set the color of a temp object to the color of its original.
        /// Temp objects are when cursor is hovered over an object.
        /// Logic copied exactly from Game.Rendering.ObjectColorSystem.UpdateTempObjectColorsJob except variables are renamed to improve readability.
        /// </summary>
        [BurstCompile]
        private struct UpdateColorsJobTempObject : IJobChunk
        {
            // Color component lookup to update.
            [NativeDisableParallelForRestriction] public ComponentLookup<Color> ComponentLookupColor;

            // Component type handles.
            [ReadOnly] public ComponentTypeHandle<Temp> ComponentTypeHandleTemp;

            // Entity type handle.
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;

            /// <summary>
            /// Job execution.
            /// </summary>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // Set color of object to color of its original.
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityTypeHandle);
                NativeArray<Temp> temps = chunk.GetNativeArray(ref ComponentTypeHandleTemp);
                for (int i = 0; i < temps.Length; i++)
                {
                    if (ComponentLookupColor.TryGetComponent(temps[i].m_Original, out Color originalColor))
                    {
                        ComponentLookupColor[entities[i]] = originalColor;
                    }
                }
            }
        }


        /// <summary>
        /// Job to set the color of each sub object to the color of its owner.
        /// Sub objects include building extensions (i.e. building upgrades attached to the main building).
        /// Logic copied exactly from Game.Rendering.ObjectColorSystem.UpdateSubObjectColorsJob except
        /// variables are renamed to improve readability and if owner color cannot be found leave default color.
        /// </summary>
        [BurstCompile]
        private struct UpdateColorsJobSubObject : IJobChunk
        {
            // Color component lookup to update.
            [NativeDisableParallelForRestriction] public ComponentLookup<Color> ComponentLookupColor;

            // Component lookups.
            [ReadOnly] public ComponentLookup<Building  > ComponentLookupBuilding;
            [ReadOnly] public ComponentLookup<Elevation > ComponentLookupElevation;
            [ReadOnly] public ComponentLookup<Owner     > ComponentLookupOwner;
            [ReadOnly] public ComponentLookup<Vehicle   > ComponentLookupVehicle;

            // Component type handles.
            [ReadOnly] public ComponentTypeHandle<Elevation > ComponentTypeHandleElevation;
            [ReadOnly] public ComponentTypeHandle<Owner     > ComponentTypeHandleOwner;
            [ReadOnly] public ComponentTypeHandle<Tree      > ComponentTypeHandleTree;

            // Entity type handle.
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;

            /// <summary>
            /// Job execution.
            /// </summary>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Owner> owners = chunk.GetNativeArray(ref ComponentTypeHandleOwner);
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityTypeHandle);
                if (chunk.Has(ref ComponentTypeHandleTree))
                {
                    NativeArray<Elevation> elevations = chunk.GetNativeArray(ref ComponentTypeHandleElevation);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        Entity entity = entities[i];
                        Owner owner = owners[i];
                        Elevation elevation;
                        bool flag = CollectionUtils.TryGet(elevations, i, out elevation) && (elevation.m_Flags & ElevationFlags.OnGround) == 0;
                        bool flag2 = flag && !ComponentLookupColor.HasComponent(owner.m_Owner);
                        Owner newOwner;
                        while (ComponentLookupOwner.TryGetComponent(owner.m_Owner, out newOwner) && !ComponentLookupBuilding.HasComponent(owner.m_Owner) && !ComponentLookupVehicle.HasComponent(owner.m_Owner))
                        {
                            if (flag2)
                            {
                                if (ComponentLookupColor.HasComponent(owner.m_Owner))
                                {
                                    flag2 = false;
                                }
                                else
                                {
                                    flag &= ComponentLookupElevation.TryGetComponent(owner.m_Owner, out elevation) && (elevation.m_Flags & ElevationFlags.OnGround) == 0;
                                }
                            }
                            owner = newOwner;
                        }
                        if (ComponentLookupColor.TryGetComponent(owner.m_Owner, out Color color) && (flag || color.m_SubColor))
                        {
                            ComponentLookupColor[entity] = color;
                        }
                    }
                    return;
                }

                for (int j = 0; j < entities.Length; j++)
                {
                    Owner owner = owners[j];
                    Owner newOwner;
                    while (ComponentLookupOwner.TryGetComponent(owner.m_Owner, out newOwner) && !ComponentLookupBuilding.HasComponent(owner.m_Owner) && !ComponentLookupVehicle.HasComponent(owner.m_Owner))
                    {
                        owner = newOwner;
                    }
                    if (ComponentLookupColor.TryGetComponent(owner.m_Owner, out Color color))
                    {
                        ComponentLookupColor[entities[j]] = color;
                    }
                }
            }
        }



        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////



        // The game's instance of this system.
        private static BuildingColorSystem  _buildingColorSystem;

        // Other systems.
        private BuildingUseUISystem _buildingUseUISystem;
        private ResourceSystem      _resourceSystem;
        private ToolSystem          _toolSystem;
        
        // Harmony ID.
        private const string HarmonyID = "rcav8tr." + ModAssemblyInfo.Name;

        // Active building status types.
        private NativeList<ActiveBuildingStatusType> _activeBuildingStatusTypes;

        // Create nested array of lists for used and capacity data.
        // The outer array is one for each possible thread.
        // The inner list is one for each used and capacity amount.
        private NativeArray<NativeList<UsedCapacity>> _totalUsedCapacity;

        /// <summary>
        /// Initialize this system.
        /// </summary>
        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            Mod.log.Info($"{nameof(BuildingColorSystem)}.{nameof(OnCreate)}");

            // Save the game's instance of this system.
            _buildingColorSystem = this;

            // Get other systems.
            _buildingUseUISystem = base.World.GetOrCreateSystemManaged<BuildingUseUISystem>();
            _resourceSystem      = base.World.GetOrCreateSystemManaged<ResourceSystem>();
            _toolSystem          = base.World.GetOrCreateSystemManaged<ToolSystem>();

            // Create list of active building status types.
            // List is persistent so it does not need to be recreated each frame.
            _activeBuildingStatusTypes = new(32, Allocator.Persistent);

            // Create nested arrays and lists to hold used and capacity amounts.
            // Arrays and lists are persistent so they do not need to be recreated each time a job runs.
            int threadCount = JobsUtility.ThreadIndexCount;
            _totalUsedCapacity = new NativeArray<NativeList<UsedCapacity>>(threadCount, Allocator.Persistent);
            for (int i = 0; i < threadCount; i++)
            {
                // Each thread array entry is a list to hold used and capacity amounts.
                _totalUsedCapacity[i] = new NativeList<UsedCapacity>(32, Allocator.Persistent);
            }

            // Use Harmony to patch ObjectColorSystem.OnUpdate with BuildingColorSystem.OnUpdatePrefix.
            // When one of this mod's infoviews is displayed, it is not necessary to execute ObjectColorSystem.OnUpdate.
            // By using a Harmony prefix, this system can prevent the execution of ObjectColorSystem.OnUpdate.
            // Note that ObjectColorSystem.OnUpdate can be patched, but the jobs in ObjectColorSystem cannot be patched because they are burst compiled.
            MethodInfo originalMethod = typeof(ObjectColorSystem).GetMethod("OnUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            if (originalMethod == null)
            {
                Mod.log.Error($"Unable to find original method {nameof(ObjectColorSystem)}.OnUpdate.");
                return;
            }
            MethodInfo prefixMethod = typeof(BuildingColorSystem).GetMethod(nameof(OnUpdatePrefix), BindingFlags.Static | BindingFlags.NonPublic);
            if (prefixMethod == null)
            {
                Mod.log.Error($"Unable to find patch prefix method {nameof(BuildingColorSystem)}.{nameof(OnUpdatePrefix)}.");
                return;
            }
            new Harmony(HarmonyID).Patch(originalMethod, new HarmonyMethod(prefixMethod), null);
        }

        /// <summary>
        /// One time system destruction.
        /// </summary>
        protected override void OnDestroy()
        {
            // Dispose persistent native collections.
            _activeBuildingStatusTypes.Dispose();
            for (int i = 0; i < _totalUsedCapacity.Length; i++)
            {
                _totalUsedCapacity[i].Dispose();
            }
            _totalUsedCapacity.Dispose();

            base.OnDestroy();
        }

        /// <summary>
        /// Called when a game is done being loaded.
        /// </summary>
        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            // Initialize only for game mode.
            if (mode == GameMode.Game)
            {
                // Initialize infomode colors.
                // Must do this here instead of Mod.OnLoad because some data
                // is obtained from prefabs that are not yet initialized when Mod.OnLoad runs.
                BUInfoviewDatas.instance.SetInfomodeColors();
            }
        }

        /// <summary>
        /// If system is activated, called by the game to update the system.
        /// </summary>
        protected override void OnUpdate()
        {
            // Nothing to do here, but implementation is required.
        }

        /// <summary>
        /// Prefix patch method for ObjectColorSystem.OnUpdate().
        /// </summary>
        private static bool OnUpdatePrefix()
        {
            // Call the implementation of OnUpdate for the game's instance of this system.
            return _buildingColorSystem.OnUpdateImpl();
        }

        /// <summary>
        /// Implementation method that potentially replaces the call to ObjectColorSystem.OnUpdate().
        /// </summary>
        private bool OnUpdateImpl()
        {
            // If no active infoview, then execute original game logic.
            if (_toolSystem.activeInfoview == null)
            {
                return true;
            }

            // If active infoview is not for this mod, then execute original game logic.
            BUInfoview activeInfoview = BUInfoviewData.GetInfoview(_toolSystem.activeInfoview.name);
            if (activeInfoview == BUInfoview.None)
            {
                return true;
            }

            // Active infoview is for this mod.

            // Clear inner lists that hold used and capacity amounts.
            // When a NativeList is cleared, capacity remains the same.
            // So once increased, the capacity never decreases, as desired,
            // so list capacity does not need to be expanded each time OnUpdate runs.
            for (int i = 0; i < _totalUsedCapacity.Length; i++)
            {
                _totalUsedCapacity[i].Clear();
            }

            // Get each query and job.
            GetQueryJobDefault           (out EntityQuery queryDefault,            out UpdateColorsJobDefault            updateColorsJobDefault);
            GetQueryJobMainBuilding      (out EntityQuery queryMainBuilding,       out UpdateColorsJobMainBuilding       updateColorsJobMainBuilding);
            GetQueryJobAttachmentBuilding(out EntityQuery queryAttachmentBuilding, out UpdateColorsJobAttachmentBuilding updateColorsJobAttachmentBuilding);
            GetQueryJobMiddleBuilding    (out EntityQuery queryMiddleBuilding,     out UpdateColorsJobMiddleBuilding     updateColorsJobMiddleBuilding);
            GetQueryJobTempObject        (out EntityQuery queryTempObject,         out UpdateColorsJobTempObject         updateColorsJobTempObject);
            GetQueryJobSubObject         (out EntityQuery querySubObject,          out UpdateColorsJobSubObject          updateColorsJobSubObject);

            // Set a few more parameters for main building job.
            SetActiveBuildingStatusTypes(activeInfoview);
            updateColorsJobMainBuilding.ActiveInfoview            = activeInfoview;
            updateColorsJobMainBuilding.ActiveBuildingStatusTypes = _activeBuildingStatusTypes;

            // Schedule the jobs with dependencies so the jobs run in order.
            // Schedule each job to execute in parallel (i.e. job uses multiple threads, if available).
            // Parallel threads execute much faster than a single thread.
            // Do attachment buildings before middle buildings because some middle buildings have an attachment building as owner.
            JobHandle jobHandleDefault        = JobChunkExtensions.ScheduleParallel(updateColorsJobDefault,            queryDefault,            base.Dependency);
            JobHandle jobHandleMainBuilding   = JobChunkExtensions.ScheduleParallel(updateColorsJobMainBuilding,       queryMainBuilding,       jobHandleDefault);
            JobHandle jobHandleNext = jobHandleMainBuilding;
            if (Mod.ModSettings.ColorSpecializedIndustryLots)
            {
                jobHandleNext                 = JobChunkExtensions.ScheduleParallel(updateColorsJobAttachmentBuilding, queryAttachmentBuilding, jobHandleMainBuilding);
            }
            JobHandle jobHandleMiddleBuilding = JobChunkExtensions.ScheduleParallel(updateColorsJobMiddleBuilding,     queryMiddleBuilding,     jobHandleNext);
            JobHandle jobHandleTempObject     = JobChunkExtensions.ScheduleParallel(updateColorsJobTempObject,         queryTempObject,         jobHandleMiddleBuilding);
            JobHandle jobHandleSubObject      = JobChunkExtensions.ScheduleParallel(updateColorsJobSubObject,          querySubObject,          jobHandleTempObject);

            // Prevent these jobs from running again until last job is complete.
            base.Dependency = jobHandleSubObject;

            // Wait for the main building job to complete before accessing total used and capacity.
            jobHandleMainBuilding.Complete();

            // Jobs scheduled after and dependent on the main building job could still be executing at this point, which is okay.
            // Those jobs only set building color based on main building color.
            // All the data needed to update the UI has been collected by the main building job.
            // So proceed now with updating the UI while those subsequent jobs might still be executing.

            // For the active infoview, get building status type datas and first building status type.
            BUBuildingStatusTypeDatas buildingStatusTypeDatas = BUInfoviewDatas.instance[activeInfoview].buildingStatusTypeDatas;
            BUBuildingStatusType buildingStatusTypeFirst = buildingStatusTypeDatas.BuildingStatusTypeFirst;

            // Compute total used and capacity by building status type.
            // Totals are double because some data values can exceed the max value of an int.
            // Do each thread entry in the total used and capacity array.
            double[] totalUsed     = new double[buildingStatusTypeDatas.Count];
            double[] totalCapacity = new double[buildingStatusTypeDatas.Count];
            int   [] totalCount    = new int   [buildingStatusTypeDatas.Count];
            foreach (NativeList<UsedCapacity> usedCapacities in _totalUsedCapacity)
            {
                // Do each used and capacity entry in the total list.
                foreach (UsedCapacity usedCapacity in usedCapacities)
                {
                    // Add used and capacity from this entry to totals.
                    // Index into the total arrays is the building status type minus the first building status type.
                    // This assumes the building status types are in sequential numerical order in the enum, which they should always be.
                    int totalIndex = usedCapacity.BuildingStatusType - buildingStatusTypeFirst;
                    totalUsed    [totalIndex] += usedCapacity.Used;
                    totalCapacity[totalIndex] += usedCapacity.Capacity;
                    totalCount   [totalIndex] += 1;     // Every entry represents one building.
                }
            }

            // Update building status type data values.
            buildingStatusTypeDatas.UpdateDataValues(totalUsed, totalCapacity, totalCount);

            // Wait for the middle building job to complete to help reduce building flicker.
            jobHandleMiddleBuilding.Complete();

            // This system handled building colors for one of this mod's infoviews.
            // Do not execute the original game logic.
            return false;
        }

        /// <summary>
        /// Set active building status types.
        /// </summary>
        private void SetActiveBuildingStatusTypes(BUInfoview activeInfoview)
        {
            // Clear active building status types.
            _activeBuildingStatusTypes.Clear();

            // Define query to get active building status datas.
            // All infomodes for this mod are BuildingStatusInfomodePrefab which generates InfoviewBuildingStatusData.
            // So there is no need to check for other datas.
            // Get only the active infomodes.
            EntityQuery queryActiveInfoviewBuildingStatusData = SystemAPI.QueryBuilder()
                .WithAll<InfoviewBuildingStatusData, InfomodeActive>()
                .Build();

            // Get infoview building status datas and corresponding infomode actives.
            NativeArray<InfoviewBuildingStatusData> infoviewBuildingStatusDatas =
                queryActiveInfoviewBuildingStatusData.ToComponentDataArray<InfoviewBuildingStatusData>(Allocator.Temp);
            NativeArray<InfomodeActive> infomodeActives =
                queryActiveInfoviewBuildingStatusData.ToComponentDataArray<InfomodeActive>(Allocator.Temp);

            // Do each building status type for the active infoview.
            BUBuildingStatusTypeDatas buildingStatusTypeDatasForActiveInfoview = BUInfoviewDatas.instance[activeInfoview].buildingStatusTypeDatas;
            foreach (BUBuildingStatusTypeData buildingStatusTypeData in buildingStatusTypeDatasForActiveInfoview.Values)
            {
                // Ignore special cases.
                if (!buildingStatusTypeData.IsSpecialCase)
                {
                    // Check if building status type is active.
                    BUBuildingStatusType buildingStatusType = buildingStatusTypeData.BuildingStatusType;
                    for (int i = 0; i < infoviewBuildingStatusDatas.Length; i++)
                    {
                        if ((BUBuildingStatusType)infoviewBuildingStatusDatas[i].m_Type == buildingStatusType)
                        {
                            // The fact that the infoview building status data was returned by the query means it is active.
                            _activeBuildingStatusTypes.Add(new ActiveBuildingStatusType(buildingStatusType, infomodeActives[i].m_Index));
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get query and job for default.
        /// </summary>
        private void GetQueryJobDefault(out EntityQuery queryDefault, out UpdateColorsJobDefault updateColorsJobDefault)
        {
            // Define query to get default objects (i.e. every object that has a color).
            queryDefault = SystemAPI.QueryBuilder()
                .WithAllRW<Color>()
                .WithAll<Object>()
                .WithNone<Hidden, Deleted>()
                .Build();

            // Create a job to update default colors.
            updateColorsJobDefault = new UpdateColorsJobDefault()
            {
                ComponentTypeHandleColor = SystemAPI.GetComponentTypeHandle<Color>(false),
            };
        }

        /// <summary>
        /// Get query and job for main building.
        /// </summary>
        private void GetQueryJobMainBuilding(out EntityQuery queryMainBuilding, out UpdateColorsJobMainBuilding updateColorsJobMainBuilding)
        {
            // Define query to get main buildings.
            // Logic adapted from Game.Rendering.ObjectColorSystem.
            // Do not exclude hidden buildings because they must be included in the total used and capacity data.
            queryMainBuilding = SystemAPI.QueryBuilder()
                .WithAllRW<Color>()
                .WithAll<Object, PrefabRef, CurrentDistrict>()
                .WithAny<Building, MailBox>()
                .WithNone<Abandoned, Condemned, Deleted, Destroyed>()
                .WithNone<Attachment>()     // Exclude attachments (see attachments query below).
                .WithNone<Owner>()          // Exclude subbuildings (see middle buildings query below).
                .WithNone<Temp>()           // Exclude temp (see temp objects query below).
                .Build();

            // Create a job to update main building colors.
            // All of the buffers and components are set even though not all of them will be used for the active infoview.
            updateColorsJobMainBuilding = new UpdateColorsJobMainBuilding()
            {
                ComponentTypeHandleColor                        = SystemAPI.GetComponentTypeHandle<Color>(false),

                BufferLookupEfficiency                          = SystemAPI.GetBufferLookup<Efficiency          >(true),
                BufferLookupEmployee                            = SystemAPI.GetBufferLookup<Employee            >(true),
                BufferLookupHouseholdCitizen                    = SystemAPI.GetBufferLookup<HouseholdCitizen    >(true),
                BufferLookupInstalledUpgrade                    = SystemAPI.GetBufferLookup<InstalledUpgrade    >(true),
                BufferLookupLaneObject                          = SystemAPI.GetBufferLookup<LaneObject          >(true),
                BufferLookupOccupant                            = SystemAPI.GetBufferLookup<Occupant            >(true),
                BufferLookupOwnedVehicle                        = SystemAPI.GetBufferLookup<OwnedVehicle        >(true),
                BufferLookupPatient                             = SystemAPI.GetBufferLookup<Patient             >(true),
                BufferLookupRenter                              = SystemAPI.GetBufferLookup<Renter              >(true),
                BufferLookupResources                           = SystemAPI.GetBufferLookup<Resources           >(true),
                BufferLookupStudent                             = SystemAPI.GetBufferLookup<Student             >(true),
                BufferLookupSubArea                             = SystemAPI.GetBufferLookup<SubArea             >(true),
                BufferLookupSubLane                             = SystemAPI.GetBufferLookup<SubLane             >(true),
                BufferLookupSubNet                              = SystemAPI.GetBufferLookup<SubNet              >(true),
                BufferLookupSubObject                           = SystemAPI.GetBufferLookup<SubObject           >(true),
                
                ComponentLookupAdminBuildingData                = SystemAPI.GetComponentLookup<AdminBuildingData        >(true),
                ComponentLookupAmbulance                        = SystemAPI.GetComponentLookup<Ambulance                >(true),
                ComponentLookupBattery                          = SystemAPI.GetComponentLookup<Battery                  >(true),
                ComponentLookupBatteryData                      = SystemAPI.GetComponentLookup<BatteryData              >(true),
                ComponentLookupBicycle                          = SystemAPI.GetComponentLookup<Bicycle                  >(true),
                ComponentLookupBuildingData                     = SystemAPI.GetComponentLookup<BuildingData             >(true),
                ComponentLookupBuildingPropertyData             = SystemAPI.GetComponentLookup<BuildingPropertyData     >(true),
                ComponentLookupCar                              = SystemAPI.GetComponentLookup<Car                      >(true),
                ComponentLookupCargoTransportStationData        = SystemAPI.GetComponentLookup<CargoTransportStationData>(true),
                ComponentLookupCitizen                          = SystemAPI.GetComponentLookup<Citizen                  >(true),
                ComponentLookupCommercialProperty               = SystemAPI.GetComponentLookup<CommercialProperty       >(true),
                ComponentLookupCompanyData                      = SystemAPI.GetComponentLookup<CompanyData              >(true),
                ComponentLookupCompanyStatisticData             = SystemAPI.GetComponentLookup<CompanyStatisticData     >(true),
                ComponentLookupCurve                            = SystemAPI.GetComponentLookup<Curve                    >(true),
                ComponentLookupDeathcareFacility                = SystemAPI.GetComponentLookup<DeathcareFacility        >(true),
                ComponentLookupDeathcareFacilityData            = SystemAPI.GetComponentLookup<DeathcareFacilityData    >(true),
                ComponentLookupDeliveryTruck                    = SystemAPI.GetComponentLookup<DeliveryTruck            >(true),
                ComponentLookupDisasterFacilityData             = SystemAPI.GetComponentLookup<DisasterFacilityData     >(true),
                ComponentLookupElectricityProducer              = SystemAPI.GetComponentLookup<ElectricityProducer      >(true),
                ComponentLookupEmergencyGeneratorData           = SystemAPI.GetComponentLookup<EmergencyGeneratorData   >(true),
                ComponentLookupEmergencyShelterData             = SystemAPI.GetComponentLookup<EmergencyShelterData     >(true),
                ComponentLookupEvacuatingTransport              = SystemAPI.GetComponentLookup<EvacuatingTransport      >(true),
                ComponentLookupExtractorCompany                 = SystemAPI.GetComponentLookup<ExtractorCompany         >(true),
                ComponentLookupExtractorProperty                = SystemAPI.GetComponentLookup<ExtractorProperty        >(true),
                ComponentLookupFireEngine                       = SystemAPI.GetComponentLookup<FireEngine               >(true),
                ComponentLookupFireStationData                  = SystemAPI.GetComponentLookup<FireStationData          >(true),
                ComponentLookupFirewatchTowerData               = SystemAPI.GetComponentLookup<FirewatchTowerData       >(true),
                ComponentLookupGarageLane                       = SystemAPI.GetComponentLookup<GarageLane               >(true),
                ComponentLookupGarbageFacility                  = SystemAPI.GetComponentLookup<GarbageFacility          >(true),
                ComponentLookupGarbageFacilityData              = SystemAPI.GetComponentLookup<GarbageFacilityData      >(true),
                ComponentLookupGarbageTruck                     = SystemAPI.GetComponentLookup<GarbageTruck             >(true),
                ComponentLookupGeometry                         = SystemAPI.GetComponentLookup<Geometry                 >(true),
                ComponentLookupGroundWaterPoweredData           = SystemAPI.GetComponentLookup<GroundWaterPoweredData   >(true),
                ComponentLookupHealthProblem                    = SystemAPI.GetComponentLookup<HealthProblem            >(true),
                ComponentLookupHearse                           = SystemAPI.GetComponentLookup<Hearse                   >(true),
                ComponentLookupHelicopter                       = SystemAPI.GetComponentLookup<Helicopter               >(true),
                ComponentLookupHospitalData                     = SystemAPI.GetComponentLookup<HospitalData             >(true),
                ComponentLookupHousehold                        = SystemAPI.GetComponentLookup<Household                >(true),
                ComponentLookupIndustrialProcessData            = SystemAPI.GetComponentLookup<IndustrialProcessData    >(true),
                ComponentLookupIndustrialProperty               = SystemAPI.GetComponentLookup<IndustrialProperty       >(true),
                ComponentLookupMailBox                          = SystemAPI.GetComponentLookup<MailBox                  >(true),
                ComponentLookupMailBoxData                      = SystemAPI.GetComponentLookup<MailBoxData              >(true),
                ComponentLookupMaintenanceDepotData             = SystemAPI.GetComponentLookup<MaintenanceDepotData     >(true),
                ComponentLookupMaintenanceVehicle               = SystemAPI.GetComponentLookup<MaintenanceVehicle       >(true),
                ComponentLookupOfficeProperty                   = SystemAPI.GetComponentLookup<OfficeProperty           >(true),
                ComponentLookupOwner                            = SystemAPI.GetComponentLookup<Owner                    >(true),
                ComponentLookupParkData                         = SystemAPI.GetComponentLookup<ParkData                 >(true),
                ComponentLookupParkedCar                        = SystemAPI.GetComponentLookup<ParkedCar                >(true),
                ComponentLookupParkedTrain                      = SystemAPI.GetComponentLookup<ParkedTrain              >(true),
                ComponentLookupParkingFacilityData              = SystemAPI.GetComponentLookup<ParkingFacilityData      >(true),
                ComponentLookupParkingLane                      = SystemAPI.GetComponentLookup<ParkingLane              >(true),
                ComponentLookupParkingLaneData                  = SystemAPI.GetComponentLookup<ParkingLaneData          >(true),
                ComponentLookupParkMaintenance                  = SystemAPI.GetComponentLookup<ParkMaintenance          >(true),
                ComponentLookupPoliceCar                        = SystemAPI.GetComponentLookup<PoliceCar                >(true),
                ComponentLookupPoliceStationData                = SystemAPI.GetComponentLookup<PoliceStationData        >(true),
                ComponentLookupPostFacility                     = SystemAPI.GetComponentLookup<PostFacility             >(true),
                ComponentLookupPostFacilityData                 = SystemAPI.GetComponentLookup<PostFacilityData         >(true),
                ComponentLookupPostVan                          = SystemAPI.GetComponentLookup<PostVan                  >(true),
                ComponentLookupPowerPlantData                   = SystemAPI.GetComponentLookup<PowerPlantData           >(true),
                ComponentLookupPrefabRef                        = SystemAPI.GetComponentLookup<PrefabRef                >(true),
                ComponentLookupPrisonData                       = SystemAPI.GetComponentLookup<PrisonData               >(true),
                ComponentLookupPrisonerTransport                = SystemAPI.GetComponentLookup<PrisonerTransport        >(true),
                ComponentLookupResearchFacilityData             = SystemAPI.GetComponentLookup<ResearchFacilityData     >(true),
                ComponentLookupResidentialProperty              = SystemAPI.GetComponentLookup<ResidentialProperty      >(true),
                ComponentLookupResourceData                     = SystemAPI.GetComponentLookup<ResourceData             >(true),
                ComponentLookupRoadMaintenance                  = SystemAPI.GetComponentLookup<RoadMaintenance          >(true),
                ComponentLookupSchoolData                       = SystemAPI.GetComponentLookup<SchoolData               >(true),
                ComponentLookupSewageOutlet                     = SystemAPI.GetComponentLookup<SewageOutlet             >(true),
                ComponentLookupSewageOutletData                 = SystemAPI.GetComponentLookup<SewageOutletData         >(true),
                ComponentLookupSolarPoweredData                 = SystemAPI.GetComponentLookup<SolarPoweredData         >(true),
                ComponentLookupSpawnableBuildingData            = SystemAPI.GetComponentLookup<SpawnableBuildingData    >(true),
                ComponentLookupStorage                          = SystemAPI.GetComponentLookup<Storage                  >(true),
                ComponentLookupStorageAreaData                  = SystemAPI.GetComponentLookup<StorageAreaData          >(true),
                ComponentLookupStorageLimitData                 = SystemAPI.GetComponentLookup<StorageLimitData         >(true),
                ComponentLookupStorageProperty                  = SystemAPI.GetComponentLookup<StorageProperty          >(true),
                ComponentLookupTelecomFacilityData              = SystemAPI.GetComponentLookup<TelecomFacilityData      >(true),
                ComponentLookupTrain                            = SystemAPI.GetComponentLookup<Train                    >(true),
                ComponentLookupTransformerData                  = SystemAPI.GetComponentLookup<TransformerData          >(true),
                ComponentLookupTransportCompanyData             = SystemAPI.GetComponentLookup<TransportCompanyData     >(true),
                ComponentLookupTransportDepotData               = SystemAPI.GetComponentLookup<TransportDepotData       >(true),
                ComponentLookupTransportStationData             = SystemAPI.GetComponentLookup<TransportStationData     >(true),
                ComponentLookupWatercraft                       = SystemAPI.GetComponentLookup<Watercraft               >(true),
                ComponentLookupWaterPoweredData                 = SystemAPI.GetComponentLookup<WaterPoweredData         >(true),
                ComponentLookupWaterPumpingStation              = SystemAPI.GetComponentLookup<WaterPumpingStation      >(true),
                ComponentLookupWaterPumpingStationData          = SystemAPI.GetComponentLookup<WaterPumpingStationData  >(true),
                ComponentLookupWelfareOfficeData                = SystemAPI.GetComponentLookup<WelfareOfficeData        >(true),
                ComponentLookupWindPoweredData                  = SystemAPI.GetComponentLookup<WindPoweredData          >(true),
                ComponentLookupWorkplaceData                    = SystemAPI.GetComponentLookup<WorkplaceData            >(true),
                ComponentLookupWorkProvider                     = SystemAPI.GetComponentLookup<WorkProvider             >(true),

                ComponentTypeHandleCurrentDistrict              = SystemAPI.GetComponentTypeHandle<CurrentDistrict      >(true),
                ComponentTypeHandleDestroyed                    = SystemAPI.GetComponentTypeHandle<Destroyed            >(true),
                ComponentTypeHandlePrefabRef                    = SystemAPI.GetComponentTypeHandle<PrefabRef            >(true),
                ComponentTypeHandleUnderConstruction            = SystemAPI.GetComponentTypeHandle<UnderConstruction    >(true),
                
                EntityTypeHandle                                = SystemAPI.GetEntityTypeHandle(),

                EconomyParameters                               = SystemAPI.GetSingleton<EconomyParameterData>(),
				ResourcePrefabs                                 = _resourceSystem.GetPrefabs(),

                // These get set later.
                //ActiveInfoview                                  = activeInfoview,
                //ActiveBuildingStatusTypes                       = _activeBuildingStatusTypes,
                
                CountVehiclesInUse                              = Mod.ModSettings.CountVehiclesInUse,
                CountVehiclesInMaintenance                      = Mod.ModSettings.CountVehiclesInMaintenance,
                EfficiencyMaxColor200Percent                    = Mod.ModSettings.EfficiencyMaxColor200Percent,
                ProductionMaxColor200Percent                    = Mod.ModSettings.ProductionMaxColor200Percent,

                SelectedDistrict                                = _buildingUseUISystem.SelectedDistrict,
                SelectedDistrictIsEntireCity                    = _buildingUseUISystem.SelectedDistrict == BuildingUseUISystem.EntireCity,
                
                TotalUsedCapacity                               = _totalUsedCapacity,
            };
        }

        /// <summary>
        /// Get query and job for attachment building.
        /// </summary>
        private void GetQueryJobAttachmentBuilding(out EntityQuery queryAttachmentBuilding, out UpdateColorsJobAttachmentBuilding updateColorsJobAttachmentBuilding)
        {
            // Define query to get attachment buildings.
            // Attachments are the lots attached to specialized industry.
            queryAttachmentBuilding = SystemAPI.QueryBuilder()
                .WithAllRW<Color>()
                .WithAll<Building, Attachment>()
                .WithNone<Hidden, Deleted>()
                .WithNone<Owner>()          // Exclude middle buildings (see middle buildings query below).
                .Build();

            // Create a job to update attachment building colors.
            updateColorsJobAttachmentBuilding = new UpdateColorsJobAttachmentBuilding()
            {
                ComponentLookupColor            = SystemAPI.GetComponentLookup<Color>(false),
                
                ComponentTypeHandleAttachment   = SystemAPI.GetComponentTypeHandle<Attachment>(true),
                
                EntityTypeHandle                = SystemAPI.GetEntityTypeHandle(),
            };
        }

        /// <summary>
        /// Get query and job for middle building.
        /// </summary>
        private void GetQueryJobMiddleBuilding(out EntityQuery queryMiddleBuilding, out UpdateColorsJobMiddleBuilding updateColorsJobMiddleBuilding)
        {
            // Define query to get middle buildings.
            // Middle buildings include sub buildings (i.e. building upgrades placed around the perimeter of the main building).
            // Adapted from Game.Rendering.ObjectColorSystem except Vehicles with Controllers and attachments are excluded.
            queryMiddleBuilding = SystemAPI.QueryBuilder()
                .WithAllRW<Color>()
                .WithAll<Building, Owner>()
                .WithNone<Hidden, Deleted>()
                .WithNone<Attachment>()     // Exclude attachments (see attachment buildings query above).
                .Build();

            // Create a job to update middle building colors.
            updateColorsJobMiddleBuilding = new UpdateColorsJobMiddleBuilding()
            {
                ComponentLookupColor            = SystemAPI.GetComponentLookup<Color        >(false),

                ComponentLookupBuildingData     = SystemAPI.GetComponentLookup<BuildingData >(true),
                ComponentLookupPrefabRef        = SystemAPI.GetComponentLookup<PrefabRef    >(true),
                
                ComponentTypeHandleOwner        = SystemAPI.GetComponentTypeHandle<Owner    >(true),
                ComponentTypeHandlePrefabRef    = SystemAPI.GetComponentTypeHandle<PrefabRef>(true),

                EntityTypeHandle                = SystemAPI.GetEntityTypeHandle(),
            };
        }

        /// <summary>
        /// Get query and job for temp object.
        /// </summary>
        private void GetQueryJobTempObject(out EntityQuery queryTempObject, out UpdateColorsJobTempObject updateColorsJobTempObject)
        {
            // Define query to get Temp objects.
            // Temp objects are when cursor is hovered over an object.
            // The original object gets hidden and a temp object is placed over the original.
            // Adapted from Game.Rendering.ObjectColorSystem.
            queryTempObject = SystemAPI.QueryBuilder()
                .WithAllRW<Color>()
                .WithAll<Object, Temp>()
                .WithNone<Hidden, Deleted>()
                .Build();

            // Create a job to update temp object colors.
            updateColorsJobTempObject = new UpdateColorsJobTempObject()
            {
                ComponentLookupColor            = SystemAPI.GetComponentLookup<Color    >(false),
                
                ComponentTypeHandleTemp         = SystemAPI.GetComponentTypeHandle<Temp >(true),
                
                EntityTypeHandle                = SystemAPI.GetEntityTypeHandle(),
            };
        }

        /// <summary>
        /// Get query and job for sub object.
        /// </summary>
        private void GetQueryJobSubObject(out EntityQuery querySubObject, out UpdateColorsJobSubObject updateColorsJobSubObject)
        {
            // Define query that will get building extensions (i.e. the building upgrades attached to the main building).
            // This query will likely also get other objects.
            // Adapted from Game.Rendering.ObjectColorSystem.
            querySubObject = SystemAPI.QueryBuilder()
                .WithAllRW<Color>()
                .WithAll<Object, Owner>()
                // Exclude all same things as base game logic.
                .WithNone<Hidden, Deleted, Vehicle, Creature, Building, UtilityObject>()
                .Build();

            // Create a job to update sub object colors.
            updateColorsJobSubObject = new UpdateColorsJobSubObject()
            {
                ComponentLookupColor            = SystemAPI.GetComponentLookup<Color        >(false),
                
                ComponentLookupBuilding         = SystemAPI.GetComponentLookup<Building     >(true),
                ComponentLookupElevation        = SystemAPI.GetComponentLookup<Elevation    >(true),
                ComponentLookupOwner            = SystemAPI.GetComponentLookup<Owner        >(true),
                ComponentLookupVehicle          = SystemAPI.GetComponentLookup<Vehicle      >(true),
                
                ComponentTypeHandleElevation    = SystemAPI.GetComponentTypeHandle<Elevation>(true),
                ComponentTypeHandleOwner        = SystemAPI.GetComponentTypeHandle<Owner    >(true),
                ComponentTypeHandleTree         = SystemAPI.GetComponentTypeHandle<Tree     >(true),
                
                EntityTypeHandle                = SystemAPI.GetEntityTypeHandle(),
            };
        }
    }
}
