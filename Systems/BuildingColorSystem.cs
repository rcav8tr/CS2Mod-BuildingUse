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
using DisasterFacility      = Game.Buildings.   DisasterFacility;
using Elevation             = Game.Objects.     Elevation;
using EmergencyShelter      = Game.Buildings.   EmergencyShelter;
using FireEngine            = Game.Vehicles.    FireEngine;
using FireStation           = Game.Buildings.   FireStation;
using GarbageFacility       = Game.Buildings.   GarbageFacility;
using GarbageTruck          = Game.Vehicles.    GarbageTruck;
using Hearse                = Game.Vehicles.    Hearse;
using Hospital              = Game.Buildings.   Hospital;
using MailBox               = Game.Routes.      MailBox;
using MaintenanceVehicle    = Game.Vehicles.    MaintenanceVehicle;
using Object                = Game.Objects.     Object;
using Park                  = Game.Buildings.   Park;
using ParkingFacility       = Game.Buildings.   ParkingFacility;
using ParkingLane           = Game.Net.         ParkingLane;
using PoliceCar             = Game.Vehicles.    PoliceCar;
using PoliceStation         = Game.Buildings.   PoliceStation;
using PostFacility          = Game.Buildings.   PostFacility;
using PostVan               = Game.Vehicles.    PostVan;
using Prison                = Game.Buildings.   Prison;
using ResearchFacility      = Game.Buildings.   ResearchFacility;
using Resources             = Game.Economy.     Resources;
using School                = Game.Buildings.   School;
using SewageOutlet          = Game.Buildings.   SewageOutlet;
using Student               = Game.Buildings.   Student;
using SubArea               = Game.Areas.       SubArea;
using SubLane               = Game.Net.         SubLane;
using SubNet                = Game.Net.         SubNet;
using SubObject             = Game.Objects.     SubObject;
using Taxi                  = Game.Vehicles.    Taxi;
using TelecomFacility       = Game.Buildings.   TelecomFacility;
using Transformer           = Game.Buildings.   Transformer;
using TransportDepot        = Game.Buildings.   TransportDepot;
using TransportStation      = Game.Buildings.   TransportStation;
using UtilityObject         = Game.Objects.     UtilityObject;
using WaterPumpingStation   = Game.Buildings.   WaterPumpingStation;
using WelfareOffice         = Game.Buildings.   WelfareOffice;

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
        /// Subtotal of used and capacity data for a building status type.
        /// </summary>
        private struct SubtotalUsedCapacity
        {
            public BUBuildingStatusType BuildingStatusType;
            public long Used;
            public long Capacity;
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
            [ReadOnly] public ComponentLookup<Ambulance                     > ComponentLookupAmbulance;
            [ReadOnly] public ComponentLookup<BatteryData                   > ComponentLookupBatteryData;
            [ReadOnly] public ComponentLookup<BuildingData                  > ComponentLookupBuildingData;
            [ReadOnly] public ComponentLookup<BuildingPropertyData          > ComponentLookupBuildingPropertyData;
            [ReadOnly] public ComponentLookup<Citizen                       > ComponentLookupCitizen;
            [ReadOnly] public ComponentLookup<CompanyData                   > ComponentLookupCompanyData;
            [ReadOnly] public ComponentLookup<Curve                         > ComponentLookupCurve;
            [ReadOnly] public ComponentLookup<DeathcareFacilityData         > ComponentLookupDeathcareFacilityData;
            [ReadOnly] public ComponentLookup<DeliveryTruck                 > ComponentLookupDeliveryTruck;
            [ReadOnly] public ComponentLookup<EmergencyShelterData          > ComponentLookupEmergencyShelterData;
            [ReadOnly] public ComponentLookup<EvacuatingTransport           > ComponentLookupEvacuatingTransport;
            [ReadOnly] public ComponentLookup<FireEngine                    > ComponentLookupFireEngine;
            [ReadOnly] public ComponentLookup<FireStationData               > ComponentLookupFireStationData;
            [ReadOnly] public ComponentLookup<GarageLane                    > ComponentLookupGarageLane;
            [ReadOnly] public ComponentLookup<GarbageFacilityData           > ComponentLookupGarbageFacilityData;
            [ReadOnly] public ComponentLookup<GarbageTruck                  > ComponentLookupGarbageTruck;
            [ReadOnly] public ComponentLookup<Geometry                      > ComponentLookupGeometry;
            [ReadOnly] public ComponentLookup<HealthProblem                 > ComponentLookupHealthProblem;
            [ReadOnly] public ComponentLookup<Hearse                        > ComponentLookupHearse;
            [ReadOnly] public ComponentLookup<Helicopter                    > ComponentLookupHelicopter;
            [ReadOnly] public ComponentLookup<HospitalData                  > ComponentLookupHospitalData;
            [ReadOnly] public ComponentLookup<MailBoxData                   > ComponentLookupMailBoxData;
            [ReadOnly] public ComponentLookup<MaintenanceDepotData          > ComponentLookupMaintenanceDepotData;
            [ReadOnly] public ComponentLookup<MaintenanceVehicle            > ComponentLookupMaintenanceVehicle;
            [ReadOnly] public ComponentLookup<ParkedCar                     > ComponentLookupParkedCar;
            [ReadOnly] public ComponentLookup<ParkedTrain                   > ComponentLookupParkedTrain;
            [ReadOnly] public ComponentLookup<ParkingLane                   > ComponentLookupParkingLane;
            [ReadOnly] public ComponentLookup<ParkingLaneData               > ComponentLookupParkingLaneData;
            [ReadOnly] public ComponentLookup<PoliceCar                     > ComponentLookupPoliceCar;
            [ReadOnly] public ComponentLookup<PoliceStationData             > ComponentLookupPoliceStationData;
            [ReadOnly] public ComponentLookup<PostFacilityData              > ComponentLookupPostFacilityData;
            [ReadOnly] public ComponentLookup<PostVan                       > ComponentLookupPostVan;
            [ReadOnly] public ComponentLookup<PrefabRef                     > ComponentLookupPrefabRef;
            [ReadOnly] public ComponentLookup<PrisonData                    > ComponentLookupPrisonData;
            [ReadOnly] public ComponentLookup<PrisonerTransport             > ComponentLookupPrisonerTransport;
            [ReadOnly] public ComponentLookup<PropertyRenter                > ComponentLookupPropertyRenter;
            [ReadOnly] public ComponentLookup<PublicTransportVehicleData    > ComponentLookupPublicTransportVehicleData;
            [ReadOnly] public ComponentLookup<SchoolData                    > ComponentLookupSchoolData;
            [ReadOnly] public ComponentLookup<SpawnableBuildingData         > ComponentLookupSpawnableBuildingData;
            [ReadOnly] public ComponentLookup<Storage                       > ComponentLookupStorage;
            [ReadOnly] public ComponentLookup<StorageAreaData               > ComponentLookupStorageAreaData;
            [ReadOnly] public ComponentLookup<StorageLimitData              > ComponentLookupStorageLimitData;
            [ReadOnly] public ComponentLookup<Taxi                          > ComponentLookupTaxi;
            [ReadOnly] public ComponentLookup<TransportCompanyData          > ComponentLookupTransportCompanyData;
            [ReadOnly] public ComponentLookup<TransportDepotData            > ComponentLookupTransportDepotData;
            [ReadOnly] public ComponentLookup<WorkplaceData                 > ComponentLookupWorkplaceData;
            [ReadOnly] public ComponentLookup<WorkProvider                  > ComponentLookupWorkProvider;

            // Component type handles for buildings.
            [ReadOnly] public ComponentTypeHandle<AdminBuilding             > ComponentTypeHandleAdminBuilding;
            [ReadOnly] public ComponentTypeHandle<Battery                   > ComponentTypeHandleBattery;
            [ReadOnly] public ComponentTypeHandle<CommercialProperty        > ComponentTypeHandleCommercialProperty;
            [ReadOnly] public ComponentTypeHandle<DeathcareFacility         > ComponentTypeHandleDeathcareFacility;
            [ReadOnly] public ComponentTypeHandle<DisasterFacility          > ComponentTypeHandleDisasterFacility;
            [ReadOnly] public ComponentTypeHandle<ElectricityProducer       > ComponentTypeHandleElectricityProducer;
            [ReadOnly] public ComponentTypeHandle<EmergencyShelter          > ComponentTypeHandleEmergencyShelter;
            [ReadOnly] public ComponentTypeHandle<FireStation               > ComponentTypeHandleFireStation;
            [ReadOnly] public ComponentTypeHandle<GarbageFacility           > ComponentTypeHandleGarbageFacility;
            [ReadOnly] public ComponentTypeHandle<Hospital                  > ComponentTypeHandleHospital;
            [ReadOnly] public ComponentTypeHandle<IndustrialProperty        > ComponentTypeHandleIndustrialProperty;
            [ReadOnly] public ComponentTypeHandle<OfficeProperty            > ComponentTypeHandleOfficeProperty;
            [ReadOnly] public ComponentTypeHandle<Park                      > ComponentTypeHandlePark;
            [ReadOnly] public ComponentTypeHandle<ParkingFacility           > ComponentTypeHandleParkingFacility;
            [ReadOnly] public ComponentTypeHandle<ParkMaintenance           > ComponentTypeHandleParkMaintenance;
            [ReadOnly] public ComponentTypeHandle<PoliceStation             > ComponentTypeHandlePoliceStation;
            [ReadOnly] public ComponentTypeHandle<PostFacility              > ComponentTypeHandlePostFacility;
            [ReadOnly] public ComponentTypeHandle<Prison                    > ComponentTypeHandlePrison;
            [ReadOnly] public ComponentTypeHandle<ResearchFacility          > ComponentTypeHandleResearchFacility;
            [ReadOnly] public ComponentTypeHandle<ResidentialProperty       > ComponentTypeHandleResidentialProperty;
            [ReadOnly] public ComponentTypeHandle<RoadMaintenance           > ComponentTypeHandleRoadMaintenance;
            [ReadOnly] public ComponentTypeHandle<School                    > ComponentTypeHandleSchool;
            [ReadOnly] public ComponentTypeHandle<SewageOutlet              > ComponentTypeHandleSewageOutlet;
            [ReadOnly] public ComponentTypeHandle<TelecomFacility           > ComponentTypeHandleTelecomFacility;
            [ReadOnly] public ComponentTypeHandle<Transformer               > ComponentTypeHandleTransformer;
            [ReadOnly] public ComponentTypeHandle<TransportDepot            > ComponentTypeHandleTransportDepot;
            [ReadOnly] public ComponentTypeHandle<TransportStation          > ComponentTypeHandleTransportStation;
            [ReadOnly] public ComponentTypeHandle<WaterPumpingStation       > ComponentTypeHandleWaterPumpingStation;
            [ReadOnly] public ComponentTypeHandle<WelfareOffice             > ComponentTypeHandleWelfareOffice;

            // Component type handles for miscellaneous.
            [ReadOnly] public ComponentTypeHandle<CurrentDistrict               > ComponentTypeHandleCurrentDistrict;
            [ReadOnly] public ComponentTypeHandle<Destroyed                     > ComponentTypeHandleDestroyed;
            [ReadOnly] public ComponentTypeHandle<InfomodeActive                > ComponentTypeHandleInfomodeActive;
            [ReadOnly] public ComponentTypeHandle<InfoviewBuildingStatusData    > ComponentTypeHandleInfoviewBuildingStatusData;
            [ReadOnly] public ComponentTypeHandle<MailBox                       > ComponentTypeHandleMailBox;
            [ReadOnly] public ComponentTypeHandle<PrefabRef                     > ComponentTypeHandlePrefabRef;
            [ReadOnly] public ComponentTypeHandle<TransportCompany              > ComponentTypeHandleTransportCompany;
            [ReadOnly] public ComponentTypeHandle<UnderConstruction             > ComponentTypeHandleUnderConstruction;

            // Entity type handle.
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;

            // Active infoview.
            [ReadOnly] public BUInfoview ActiveInfoview;

            // List of active building status data chunks.
            [ReadOnly] public NativeList<ArchetypeChunk> ActiveBuildingStatusDataChunks;

            // Mod settings used in the job.
            [ReadOnly] public bool CountVehiclesInUse;
            [ReadOnly] public bool CountVehiclesInMaintenance;
            [ReadOnly] public bool EfficiencyMaxColor200Percent;

            // Selected district.
            [ReadOnly] public Entity SelectedDistrict;
            [ReadOnly] public bool SelectedDistrictIsEntireCity;

            // Array of lists to return total used and capacity to the BuildingColorSystem.
            // The outer array is one for each possible thread.
            // The inner list is one for each subtotal computed in that thread.
            // Even though the outer array is read only, entries can still be added to the inner lists.
            [ReadOnly] public NativeArray<NativeList<SubtotalUsedCapacity>> TotalUsedCapacity;

            /// <summary>
            /// Job execution.
            /// </summary>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // Get colors to set.
                NativeArray<Color> colors = chunk.GetNativeArray(ref ComponentTypeHandleColor);

                // Get applicable building status types based on active infoview.
                // Because more than one building status type can be applicable to a single building
                // (for example, a mixed use residential building has both residential and commercial),
                // a list is needed so that the building use and capacity are obtained for all building status types
                // even if the building is not colored for corresponding inactive infomodes.
                NativeList<BUBuildingStatusType> applicableBuildingStatusTypes = new NativeList<BUBuildingStatusType>(0, Allocator.TempJob);
                switch (ActiveInfoview)
                {
                    case BUInfoview.Employees:  GetApplicableBuildingStatusTypesEmployees   (chunk, ref applicableBuildingStatusTypes); break;
                    case BUInfoview.Visitors:   GetApplicableBuildingStatusTypesVisitors    (chunk, ref applicableBuildingStatusTypes); break;
                    case BUInfoview.Storage:    GetApplicableBuildingStatusTypesStorage     (chunk, ref applicableBuildingStatusTypes); break;
                    case BUInfoview.Vehicles:   GetApplicableBuildingStatusTypesVehicles    (chunk, ref applicableBuildingStatusTypes); break;
                    case BUInfoview.Efficiency: GetApplicableBuildingStatusTypesEfficiency  (chunk, ref applicableBuildingStatusTypes); break;
                    case BUInfoview.Processing: GetApplicableBuildingStatusTypesProcessing  (chunk, ref applicableBuildingStatusTypes); break;
                }

                // Do each applicable building status type, if any.
                // Do in reverse order so the first building status type is the last one processed and defines the building color.
                for (int i = applicableBuildingStatusTypes.Length - 1; i >= 0; i--)
                {
                    // Get the applicable building status type.
                    BUBuildingStatusType applicableBuildingStatusType = applicableBuildingStatusTypes[i];

                    // Do each active building status data chunk to determine if the applicable building status type is active.
                    bool infomodeActive = false;
                    int infomodeIndex = 0;
                    foreach (ArchetypeChunk activeBuildingStatusDataChunk in ActiveBuildingStatusDataChunks)
                    {
                        // Do each active building status data.
                        NativeArray<InfoviewBuildingStatusData> activeBuildingStatusDatas = activeBuildingStatusDataChunk.GetNativeArray(ref ComponentTypeHandleInfoviewBuildingStatusData);
                        for (int j = 0; j < activeBuildingStatusDatas.Length; j++)
                        {
                            // Check if the applicable building status type is active.
                            InfoviewBuildingStatusData activeBuildingStatusData = activeBuildingStatusDatas[j];
                            BUBuildingStatusType activeBuildingStatusType = (BUBuildingStatusType)activeBuildingStatusData.m_Type;
                            if (applicableBuildingStatusType == activeBuildingStatusType)
                            {
                                // Get active infomodes.
                                // Empty     means no    infomode in this building status data chunk is active.
                                // Non-empty means every infomode in this building status data chunk is active.
                                NativeArray<InfomodeActive> activeInfomodes = activeBuildingStatusDataChunk.GetNativeArray(ref ComponentTypeHandleInfomodeActive);

                                // Fact that active building status type was found means infomode is active.
                                infomodeActive = true;

                                // Get index from corresponding active infomode.
                                infomodeIndex = activeInfomodes[j].m_Index;

                                break;
                            }
                        }

                        // Once found, also break out of enclosing loop.
                        if (infomodeActive)
                        {
                            break;
                        }
                    }

                    // Set building colors based on the active infoview.
                    // This also gets used and capacity data.
                    switch (ActiveInfoview)
                    {
                        case BUInfoview.Employees:  SetBuildingColorsEmployees  (chunk, infomodeActive, infomodeIndex, colors, applicableBuildingStatusType); break;
                        case BUInfoview.Visitors:   SetBuildingColorsVisitors   (chunk, infomodeActive, infomodeIndex, colors, applicableBuildingStatusType); break;
                        case BUInfoview.Storage:    SetBuildingColorsStorage    (chunk, infomodeActive, infomodeIndex, colors, applicableBuildingStatusType); break;
                        case BUInfoview.Vehicles:   SetBuildingColorsVehicles   (chunk, infomodeActive, infomodeIndex, colors, applicableBuildingStatusType); break;
                        case BUInfoview.Efficiency: SetBuildingColorsEfficiency (chunk, infomodeActive, infomodeIndex, colors, applicableBuildingStatusType); break;
                        case BUInfoview.Processing: SetBuildingColorsProcessing (chunk, infomodeActive, infomodeIndex, colors, applicableBuildingStatusType); break;
                    }
                }

                // Dispose of native list that was created above.
                applicableBuildingStatusTypes.Dispose();

                // Check if should set SubColor flag on any colors.
                // Adapted from Game.Rendering.ObjectColorSystem.CheckColors().
                NativeArray<Destroyed        > destroyeds         = chunk.GetNativeArray(ref ComponentTypeHandleDestroyed);
                NativeArray<UnderConstruction> underConstructions = chunk.GetNativeArray(ref ComponentTypeHandleUnderConstruction);
                NativeArray<PrefabRef        > prefabRefs         = chunk.GetNativeArray(ref ComponentTypeHandlePrefabRef);
                for (int i = 0; i < prefabRefs.Length; i++)
                {
                    if ((ComponentLookupBuildingData[prefabRefs[i].m_Prefab].m_Flags & BuildingFlags.ColorizeLot) != 0 || 
                        (CollectionUtils.TryGet(destroyeds,         i, out Destroyed         destroyed        ) && destroyed.m_Cleared >= 0f) || 
                        (CollectionUtils.TryGet(underConstructions, i, out UnderConstruction underConstruction) && underConstruction.m_NewPrefab == Entity.Null))
                    {
                        // Set SubColor flag on the color.
                        // Not sure what the SubColor flag does.
                        Color color = colors[i];
                        color.m_SubColor = true;
                        colors[i] = color;
                    }
                }
            }

            /// <summary>
            /// Get whether or not a building is in the selected district.
            /// </summary>
            private bool BuildingInSelectedDistrict(Entity buildingDistrict)
            {
                // If the selected district is entire city, then building is in the selected district.
                if (SelectedDistrictIsEntireCity)
                {
                    return true;
                }

                // Return if building is in the selected district.
                return buildingDistrict == SelectedDistrict;
            }

            /// <summary>
            /// Get component data from prefab (if any) with upgrades from entity (if any).
            /// The building could have the component data on the prefab or on installed upgrades or on both.
            /// </summary>
            private bool TryGetComponentDataWithUpgrades<T>
            (
                Entity entity,
                Entity prefab,
                ref ComponentLookup<T> componentLookup,
                out T componentData
            ) where T : unmanaged, IComponentData, ICombineData<T>
            {
                // Logic adapted from Game.UI.InGame.InfoSectionBase.TryGetComponentWithUpgrades
                // which simply calls Game.Prefabs.UpgradeUtils.TryGetCombinedComponent.

                // Try to get the component data directly from the prefab.
                bool hasComponentData = componentLookup.TryGetComponent(prefab, out componentData);

                // Check if entity has any installed upgrades.
                // Logic adapted from Game.Prefabs.UpgradeUtils.TryCombineData.
                bool hasInstalledUpgrade = false;
                if (BufferLookupInstalledUpgrade.TryGetBuffer(entity, out DynamicBuffer<InstalledUpgrade> installedUpgrades))
                {
                    // Do each installed upgrade.
                    for (int i = 0; i < installedUpgrades.Length; i++)
                    {
                        // Installed upgrade must not be inactive and prefab of installed upgrade must have component data type.
                        InstalledUpgrade installedUpgrade = installedUpgrades[i];
                        if (!BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive) && 
                            ComponentLookupPrefabRef.TryGetComponent(installedUpgrade.m_Upgrade, out PrefabRef installedUpgradePrefabRef) && 
                            componentLookup.TryGetComponent(installedUpgradePrefabRef.m_Prefab, out T installedUpgradeComponentData))
                        {
                            // Combine previous component data with component data from the installed upgrade.
                            componentData.Combine(installedUpgradeComponentData);
                            hasInstalledUpgrade = true;
                        }
                    }
                }
                
                // Return whether or not there is component data.
                return hasComponentData || hasInstalledUpgrade;
            }

            /// <summary>
            /// Update entity color.
            /// </summary>
            private void UpdateEntityColor(long used, long capacity, bool infomodeActive, int infomodeIndex, NativeArray<Color> colors, int colorsIndex)
            {
                // If infomode is active, set color for this entity.
                // Otherwise, leave entity color as the default color set earlier, which is the grayish/off-white color.
                if (infomodeActive)
                {
                    // All infomodes for this mod have a range from 0 to 255 which represents 0% to 100%.
                    float useRatio = capacity > 0 ? (float)used / capacity : 0f;
                    colors[colorsIndex] = new Color((byte)infomodeIndex, (byte)math.clamp(Mathf.RoundToInt(255f * useRatio), 0, 255)); 
                }
            }

            /// <summary>
            /// Update total used and capacity data.
            /// </summary>
            private void UpdateTotalUsedCapacity(BUBuildingStatusType buildingStatusType, long used, long capacity)
            {
                // Add only if either value is not zero.
                if (used != 0L || capacity != 0L)
                {
                    // Add an entry of used and capacity for this thread.
                    // By having a separate entry for each thread, parallel threads will never access the same inner list at the same time.
                    TotalUsedCapacity[JobsUtility.ThreadIndex].Add(
                        new SubtotalUsedCapacity() { BuildingStatusType = buildingStatusType, Used = used, Capacity = capacity });
                }
            }

            /// <summary>
            /// Get length of dynamic buffer from a buffer lookup.
            /// </summary>
            private int GetDynamicBufferLength<T>(Entity entity, ref BufferLookup<T> bufferLookup) where T : unmanaged, IBufferElementData
            {
                // Check if entity has buffer.
                if (bufferLookup.TryGetBuffer(entity, out DynamicBuffer<T> dynamicBuffer))
                {
                    // Dynamic buffer must be created.
                    if (dynamicBuffer.IsCreated)
                    {
                        // Return length of dynamic buffer.
                        return dynamicBuffer.Length;
                    }
                }

                // No buffer length.
                return 0;
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
            [ReadOnly] public ComponentLookup<GateData              > ComponentLookupGateData;
            [ReadOnly] public ComponentLookup<PrefabRef             > ComponentLookupPrefabRef;
            [ReadOnly] public ComponentLookup<StorageCompanyData    > ComponentLookupStorageCompanyData;

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
                    // Get the entity and owner for this building.
                    Entity entity = entities[i];
                    Entity ownerEntity = owners[i].m_Owner;

                    // Check for port middle building.
                    // All port middle buildings have an Owner that is the main port gate building.
                    // A main port gate building's prefab has the GateData component.
                    if (ComponentLookupPrefabRef.TryGetComponent(ownerEntity, out PrefabRef ownerPrefabRef) &&
                        ComponentLookupGateData.HasComponent(ownerPrefabRef.m_Prefab))
                    {
                        // Set the color of this port middle building.
                        SetPortBuildingColor(entity, prefabRefs[i].m_Prefab, ownerEntity);
                    }
                    else
                    {
                        // This is not a port middle building.
                        // Set this building color same as the owner building.
                        SetBuildingColorToOwnerColor(entity, ownerEntity);
                    }
                }
            }

            /// <summary>
            /// Set color for a port middle building.
            /// </summary>
            private void SetPortBuildingColor(Entity entity, Entity prefab, Entity ownerEntity)
            {
                // Port middle buildings are the port buildings placed in the port's area and include:
                //      Auxiliary Port Gate.
                //      Employee Canteen, Port Security, Emergency Response.
                //      Container Crane.
                //      Passenger Terminal.
                //      Intermodal Train Terminal.
                //      Container Yard, Cargo Warehouse, Tank Farm, Bulk Storage Yard (collectively "storage").

                // Check for auxiliary port gate building.
                // An auxiliary port gate building's prefab has GateData component.
                if (ComponentLookupGateData.HasComponent(prefab))
                {
                    // Set auxiliary port gate building color same as the main port gate building.
                    SetBuildingColorToOwnerColor(entity, ownerEntity);
                    return;
                }

                // Check for port storage building.
                // All port storage buildings have a prefab with StorageCompanyData that defines stored resources.
                // Note that the Container Crane's prefab has StorageCompanyData but does not define stored resources.
                if (ComponentLookupStorageCompanyData.TryGetComponent(prefab, out StorageCompanyData storageCompanyData) &&
                    storageCompanyData.m_StoredResources != Resource.NoResource)
                {
                    // Set port storage building color same as the main port gate building.
                    SetBuildingColorToOwnerColor(entity, ownerEntity);

                    // Determine whether or not this building's lot should be colorized.
                    // This is specified in Color.m_SubColor.
                    // But for unknown reasons, the Bulk Storage Yards start with m_SubColor = false
                    // when the Bulk Storage Yards should have m_SubColor = true;
                    // So need to set m_SubColor here according to the building prefab's ColorizeLot flag.
                    // For simplicity, set SubColor for all port storage buildings, not just Bulk Storage Yards.
                    Color color = ComponentLookupColor[entity];
                    color.m_SubColor =
                        ComponentLookupBuildingData.TryGetComponent(prefab, out BuildingData buildingData) &&
                        (buildingData.m_Flags & BuildingFlags.ColorizeLot) != 0;
                    ComponentLookupColor[entity] = color;

                    return;
                }

                // If get here without setting building color, then building simply remains default color.
            }

            /// <summary>
            /// Set building color same as owner building.
            /// </summary>
            private void SetBuildingColorToOwnerColor(Entity entity, Entity ownerEntity)
            {
                // Get color of owner building.
                if (ComponentLookupColor.TryGetComponent(ownerEntity, out Color ownerColor))
                {
                    // Set color of this entity to color of owner entity.
                    // Building's SubColor remains unchanged.
                    Color color = ComponentLookupColor[entity];
                    color.m_Index = ownerColor.m_Index;
                    color.m_Value = ownerColor.m_Value;
                    ComponentLookupColor[entity] = color;
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
        private ToolSystem _toolSystem;
        private BuildingUseUISystem _buildingUseUISystem;

        // Entity queries.
        private EntityQuery _queryDefault;
        private EntityQuery _queryMainBuilding;
        private EntityQuery _queryAttachmentBuilding;
        private EntityQuery _queryMiddleBuilding;
        private EntityQuery _queryTempObject;
        private EntityQuery _querySubObject;
        private EntityQuery _queryActiveBuildingStatusData;
        
        // Harmony ID.
        private const string HarmonyID = "rcav8tr." + ModAssemblyInfo.Name;

        // Create nested array of lists for used and capacity data.
        // The outer array is one for each possible thread.
        // The inner list is one for each subtotal of used and capacity amounts.
        NativeArray<NativeList<SubtotalUsedCapacity>> _totalUsedCapacity;

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
            _toolSystem          = base.World.GetOrCreateSystemManaged<ToolSystem>();
            _buildingUseUISystem = base.World.GetOrCreateSystemManaged<BuildingUseUISystem>();

            // Query to get default objects (i.e. every object that has a color).
            _queryDefault = GetEntityQuery
            (
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly <Object>(),
                        ComponentType.ReadWrite<Color>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Hidden>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            );

            // Query to get main buildings.
            // Adapted from Game.Rendering.ObjectColorSystem.
            _queryMainBuilding = GetEntityQuery
            (
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Object>(),
                        ComponentType.ReadWrite<Color>(),
                    },
                    Any = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Building>(),
                        ComponentType.ReadOnly<MailBox>(),
                    },
                    None = new ComponentType[]
                    {
                        // Do not exclude hidden buildings because they must be included in the total used and capacity data.
                        //ComponentType.ReadOnly<Hidden>(),

                        ComponentType.ReadOnly<Abandoned>(),    // Exclude abandoned buildings. 
                        ComponentType.ReadOnly<Condemned>(),    // Exclude condemned buildings.
                        ComponentType.ReadOnly<Deleted>(),      // Exclude deleted   buildings.
                        ComponentType.ReadOnly<Destroyed>(),    // Exclude destroyed buildings.
                        ComponentType.ReadOnly<Owner>(),        // Exclude subbuildings (see middle buildings query below).
                        ComponentType.ReadOnly<Attachment>(),   // Exclude attachments (see attachments query below).
                        ComponentType.ReadOnly<Temp>(),         // Exclude temp (see temp objects query below).
                    }
                }
            );

            // Query to get attachment buildings.
            // Attachments are the lots attached to specialized industry.
            _queryAttachmentBuilding = GetEntityQuery
            (
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Building>(),
                        ComponentType.ReadOnly<Attachment>(),
                        ComponentType.ReadWrite<Color>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Owner>(),    // Exclude middle buildings (see middle buildings query below).
                        ComponentType.ReadOnly<Hidden>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            );

            // Query to get middle buildings.
            // Middle buildings include sub buildings (i.e. building upgrades placed around the perimeter of the main building).
            // Copied exactly from Game.Rendering.ObjectColorSystem except Vehicles with Controllers and attachments are excluded.
            _queryMiddleBuilding = GetEntityQuery
            (
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Building>(),
                        ComponentType.ReadOnly<Owner>(),
                        ComponentType.ReadWrite<Color>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Attachment>(),   // Exclude attachments (see attachment buildings query above).
                        ComponentType.ReadOnly<Hidden>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            );

            // Query to get Temp objects.
            // Temp objects are when cursor is hovered over an object.
            // The original object gets hidden and a temp object is placed over the original.
            // Copied exactly from Game.Rendering.ObjectColorSystem.
            _queryTempObject = GetEntityQuery
            (
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Object>(),
                        ComponentType.ReadWrite<Color>(),
                        ComponentType.ReadOnly<Temp>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Hidden>(),
                        ComponentType.ReadOnly<Deleted>(),
                    }
                }
            );

            // Query that will get building extensions (i.e. the building upgrades attached to the main building).
            // This query will likely also get other objects.
            // Copied exactly from Game.Rendering.ObjectColorSystem.
            _querySubObject = GetEntityQuery
            (
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly <Object>(),
                        ComponentType.ReadOnly <Owner>(),
                        ComponentType.ReadWrite<Color>(),
                    },
                    None = new ComponentType[]
                    {
                        // Exclude all same things as base game logic.
                        ComponentType.ReadOnly<Hidden>(),
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Vehicle>(),
                        ComponentType.ReadOnly<Creature>(),
                        ComponentType.ReadOnly<Building>(),
                        ComponentType.ReadOnly<UtilityObject>(),
                    }
                }
            );

            // Query to get active building status datas.
            // All infomodes for this mod are BuildingStatusInfomodePrefab which generates InfoviewBuildingStatusData.
            // So there is no need to check for other datas.
            _queryActiveBuildingStatusData = GetEntityQuery
            (
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<InfoviewBuildingStatusData>(),
                        ComponentType.ReadOnly<InfomodeActive>(),
                    }
                }
            );

            // Create nested arrays and lists to hold used and capacity amounts.
            // Arrays and lists are persistent so they do not need to be recreated each time a job runs.
            int threadCount = JobsUtility.ThreadIndexCount;
            _totalUsedCapacity = new NativeArray<NativeList<SubtotalUsedCapacity>>(threadCount, Allocator.Persistent);
            for (int i = 0; i < threadCount; i++)
            {
                // Each thread array entry is a list to hold used and capacity subtotals.
                _totalUsedCapacity[i] = new NativeList<SubtotalUsedCapacity>(32, Allocator.Persistent);
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
            for (int i = 0; i < _totalUsedCapacity.Length; i++)
            {
                _totalUsedCapacity[i].Dispose();
            }
            _totalUsedCapacity.Dispose();

            base.OnDestroy();
        }

        /// <summary>
        /// Called every frame, even when at the main menu.
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

            // Define a job to get active building status types.
            NativeList<ArchetypeChunk> activeBuildingStatusDataChunks =
                _queryActiveBuildingStatusData.ToArchetypeChunkListAsync(Allocator.TempJob, out JobHandle activeBuildingStatusDataJobHandle);


            // Create a job to update default colors.
            UpdateColorsJobDefault updateColorsJobDefault = new UpdateColorsJobDefault()
            {
                ComponentTypeHandleColor = SystemAPI.GetComponentTypeHandle<Color>(false),
            };


            // Create a job to update main building colors.
            // All of the buffers and components are set even though not all of them will be used for the active infoview.
            UpdateColorsJobMainBuilding updateColorsJobMainBuilding = new UpdateColorsJobMainBuilding()
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
                
                ComponentLookupAmbulance                        = SystemAPI.GetComponentLookup<Ambulance                    >(true),
                ComponentLookupBatteryData                      = SystemAPI.GetComponentLookup<BatteryData                  >(true),
                ComponentLookupBuildingData                     = SystemAPI.GetComponentLookup<BuildingData                 >(true),
                ComponentLookupBuildingPropertyData             = SystemAPI.GetComponentLookup<BuildingPropertyData         >(true),
                ComponentLookupCitizen                          = SystemAPI.GetComponentLookup<Citizen                      >(true),
                ComponentLookupCompanyData                      = SystemAPI.GetComponentLookup<CompanyData                  >(true),
                ComponentLookupCurve                            = SystemAPI.GetComponentLookup<Curve                        >(true),
                ComponentLookupDeathcareFacilityData            = SystemAPI.GetComponentLookup<DeathcareFacilityData        >(true),
                ComponentLookupDeliveryTruck                    = SystemAPI.GetComponentLookup<DeliveryTruck                >(true),
                ComponentLookupEmergencyShelterData             = SystemAPI.GetComponentLookup<EmergencyShelterData         >(true),
                ComponentLookupEvacuatingTransport              = SystemAPI.GetComponentLookup<EvacuatingTransport          >(true),
                ComponentLookupFireEngine                       = SystemAPI.GetComponentLookup<FireEngine                   >(true),
                ComponentLookupFireStationData                  = SystemAPI.GetComponentLookup<FireStationData              >(true),
                ComponentLookupGarageLane                       = SystemAPI.GetComponentLookup<GarageLane                   >(true),
                ComponentLookupGarbageFacilityData              = SystemAPI.GetComponentLookup<GarbageFacilityData          >(true),
                ComponentLookupGarbageTruck                     = SystemAPI.GetComponentLookup<GarbageTruck                 >(true),
                ComponentLookupGeometry                         = SystemAPI.GetComponentLookup<Geometry                     >(true),
                ComponentLookupHealthProblem                    = SystemAPI.GetComponentLookup<HealthProblem                >(true),
                ComponentLookupHearse                           = SystemAPI.GetComponentLookup<Hearse                       >(true),
                ComponentLookupHelicopter                       = SystemAPI.GetComponentLookup<Helicopter                   >(true),
                ComponentLookupHospitalData                     = SystemAPI.GetComponentLookup<HospitalData                 >(true),
                ComponentLookupMailBoxData                      = SystemAPI.GetComponentLookup<MailBoxData                  >(true),
                ComponentLookupMaintenanceDepotData             = SystemAPI.GetComponentLookup<MaintenanceDepotData         >(true),
                ComponentLookupMaintenanceVehicle               = SystemAPI.GetComponentLookup<MaintenanceVehicle           >(true),
                ComponentLookupParkedCar                        = SystemAPI.GetComponentLookup<ParkedCar                    >(true),
                ComponentLookupParkedTrain                      = SystemAPI.GetComponentLookup<ParkedTrain                  >(true),
                ComponentLookupParkingLane                      = SystemAPI.GetComponentLookup<ParkingLane                  >(true),
                ComponentLookupParkingLaneData                  = SystemAPI.GetComponentLookup<ParkingLaneData              >(true),
                ComponentLookupPoliceCar                        = SystemAPI.GetComponentLookup<PoliceCar                    >(true),
                ComponentLookupPoliceStationData                = SystemAPI.GetComponentLookup<PoliceStationData            >(true),
                ComponentLookupPostFacilityData                 = SystemAPI.GetComponentLookup<PostFacilityData             >(true),
                ComponentLookupPostVan                          = SystemAPI.GetComponentLookup<PostVan                      >(true),
                ComponentLookupPrefabRef                        = SystemAPI.GetComponentLookup<PrefabRef                    >(true),
                ComponentLookupPrisonData                       = SystemAPI.GetComponentLookup<PrisonData                   >(true),
                ComponentLookupPrisonerTransport                = SystemAPI.GetComponentLookup<PrisonerTransport            >(true),
                ComponentLookupPropertyRenter                   = SystemAPI.GetComponentLookup<PropertyRenter               >(true),
                ComponentLookupPublicTransportVehicleData       = SystemAPI.GetComponentLookup<PublicTransportVehicleData   >(true),
                ComponentLookupSchoolData                       = SystemAPI.GetComponentLookup<SchoolData                   >(true),
                ComponentLookupSpawnableBuildingData            = SystemAPI.GetComponentLookup<SpawnableBuildingData        >(true),
                ComponentLookupStorage                          = SystemAPI.GetComponentLookup<Storage                      >(true),
                ComponentLookupStorageAreaData                  = SystemAPI.GetComponentLookup<StorageAreaData              >(true),
                ComponentLookupStorageLimitData                 = SystemAPI.GetComponentLookup<StorageLimitData             >(true),
                ComponentLookupTaxi                             = SystemAPI.GetComponentLookup<Taxi                         >(true),
                ComponentLookupTransportCompanyData             = SystemAPI.GetComponentLookup<TransportCompanyData         >(true),
                ComponentLookupTransportDepotData               = SystemAPI.GetComponentLookup<TransportDepotData           >(true),
                ComponentLookupWorkplaceData                    = SystemAPI.GetComponentLookup<WorkplaceData                >(true),
                ComponentLookupWorkProvider                     = SystemAPI.GetComponentLookup<WorkProvider                 >(true),
                
                ComponentTypeHandleAdminBuilding                = SystemAPI.GetComponentTypeHandle<AdminBuilding            >(true),
                ComponentTypeHandleBattery                      = SystemAPI.GetComponentTypeHandle<Battery                  >(true),
                ComponentTypeHandleCommercialProperty           = SystemAPI.GetComponentTypeHandle<CommercialProperty       >(true),
                ComponentTypeHandleDeathcareFacility            = SystemAPI.GetComponentTypeHandle<DeathcareFacility        >(true),
                ComponentTypeHandleDisasterFacility             = SystemAPI.GetComponentTypeHandle<DisasterFacility         >(true),
                ComponentTypeHandleElectricityProducer          = SystemAPI.GetComponentTypeHandle<ElectricityProducer      >(true),
                ComponentTypeHandleEmergencyShelter             = SystemAPI.GetComponentTypeHandle<EmergencyShelter         >(true),
                ComponentTypeHandleFireStation                  = SystemAPI.GetComponentTypeHandle<FireStation              >(true),
                ComponentTypeHandleGarbageFacility              = SystemAPI.GetComponentTypeHandle<GarbageFacility          >(true),
                ComponentTypeHandleHospital                     = SystemAPI.GetComponentTypeHandle<Hospital                 >(true),
                ComponentTypeHandleIndustrialProperty           = SystemAPI.GetComponentTypeHandle<IndustrialProperty       >(true),
                ComponentTypeHandleOfficeProperty               = SystemAPI.GetComponentTypeHandle<OfficeProperty           >(true),
                ComponentTypeHandlePark                         = SystemAPI.GetComponentTypeHandle<Park                     >(true),
                ComponentTypeHandleParkingFacility              = SystemAPI.GetComponentTypeHandle<ParkingFacility          >(true),
                ComponentTypeHandleParkMaintenance              = SystemAPI.GetComponentTypeHandle<ParkMaintenance          >(true),
                ComponentTypeHandlePoliceStation                = SystemAPI.GetComponentTypeHandle<PoliceStation            >(true),
                ComponentTypeHandlePostFacility                 = SystemAPI.GetComponentTypeHandle<PostFacility             >(true),
                ComponentTypeHandlePrison                       = SystemAPI.GetComponentTypeHandle<Prison                   >(true),
                ComponentTypeHandleResearchFacility             = SystemAPI.GetComponentTypeHandle<ResearchFacility         >(true),
                ComponentTypeHandleResidentialProperty          = SystemAPI.GetComponentTypeHandle<ResidentialProperty      >(true),
                ComponentTypeHandleRoadMaintenance              = SystemAPI.GetComponentTypeHandle<RoadMaintenance          >(true),
                ComponentTypeHandleSchool                       = SystemAPI.GetComponentTypeHandle<School                   >(true),
                ComponentTypeHandleSewageOutlet                 = SystemAPI.GetComponentTypeHandle<SewageOutlet             >(true),
                ComponentTypeHandleTelecomFacility              = SystemAPI.GetComponentTypeHandle<TelecomFacility          >(true),
                ComponentTypeHandleTransformer                  = SystemAPI.GetComponentTypeHandle<Transformer              >(true),
                ComponentTypeHandleTransportDepot               = SystemAPI.GetComponentTypeHandle<TransportDepot           >(true),
                ComponentTypeHandleTransportStation             = SystemAPI.GetComponentTypeHandle<TransportStation         >(true),
                ComponentTypeHandleWaterPumpingStation          = SystemAPI.GetComponentTypeHandle<WaterPumpingStation      >(true),
                ComponentTypeHandleWelfareOffice                = SystemAPI.GetComponentTypeHandle<WelfareOffice            >(true),

                ComponentTypeHandleCurrentDistrict              = SystemAPI.GetComponentTypeHandle<CurrentDistrict              >(true),
                ComponentTypeHandleDestroyed                    = SystemAPI.GetComponentTypeHandle<Destroyed                    >(true),
                ComponentTypeHandleInfomodeActive               = SystemAPI.GetComponentTypeHandle<InfomodeActive               >(true),
                ComponentTypeHandleInfoviewBuildingStatusData   = SystemAPI.GetComponentTypeHandle<InfoviewBuildingStatusData   >(true),
                ComponentTypeHandleMailBox                      = SystemAPI.GetComponentTypeHandle<MailBox                      >(true),
                ComponentTypeHandlePrefabRef                    = SystemAPI.GetComponentTypeHandle<PrefabRef                    >(true),
                ComponentTypeHandleTransportCompany             = SystemAPI.GetComponentTypeHandle<TransportCompany             >(true),
                ComponentTypeHandleUnderConstruction            = SystemAPI.GetComponentTypeHandle<UnderConstruction            >(true),
                
                EntityTypeHandle                                = SystemAPI.GetEntityTypeHandle(),
                
                ActiveInfoview                                  = activeInfoview,
                ActiveBuildingStatusDataChunks                  = activeBuildingStatusDataChunks,
                
                CountVehiclesInUse                              = Mod.ModSettings.CountVehiclesInUse,
                CountVehiclesInMaintenance                      = Mod.ModSettings.CountVehiclesInMaintenance,
                EfficiencyMaxColor200Percent                    = Mod.ModSettings.EfficiencyMaxColor200Percent,

                SelectedDistrict                                = _buildingUseUISystem.selectedDistrict,
                SelectedDistrictIsEntireCity                    = _buildingUseUISystem.selectedDistrict == BuildingUseUISystem.EntireCity,
                
                TotalUsedCapacity                               = _totalUsedCapacity,
            };


            // Create a job to update attachment building colors.
            UpdateColorsJobAttachmentBuilding updateColorsJobAttachmentBuilding = new UpdateColorsJobAttachmentBuilding()
            {
                ComponentLookupColor            = SystemAPI.GetComponentLookup<Color>(false),
                
                ComponentTypeHandleAttachment   = SystemAPI.GetComponentTypeHandle<Attachment>(true),
                
                EntityTypeHandle                = SystemAPI.GetEntityTypeHandle(),
            };


            // Create a job to update middle building colors.
            UpdateColorsJobMiddleBuilding updateColorsJobMiddleBuilding = new UpdateColorsJobMiddleBuilding()
            {
                ComponentLookupColor                = SystemAPI.GetComponentLookup<Color                >(false),

                ComponentLookupBuildingData         = SystemAPI.GetComponentLookup<BuildingData         >(true),
                ComponentLookupGateData             = SystemAPI.GetComponentLookup<GateData             >(true),
                ComponentLookupPrefabRef            = SystemAPI.GetComponentLookup<PrefabRef            >(true),
                ComponentLookupStorageCompanyData   = SystemAPI.GetComponentLookup<StorageCompanyData   >(true),
                
                ComponentTypeHandleOwner            = SystemAPI.GetComponentTypeHandle<Owner            >(true),
                ComponentTypeHandlePrefabRef        = SystemAPI.GetComponentTypeHandle<PrefabRef        >(true),

                EntityTypeHandle                    = SystemAPI.GetEntityTypeHandle(),
            };


            // Create a job to update temp object colors.
            UpdateColorsJobTempObject updateColorsJobTempObject = new UpdateColorsJobTempObject()
            {
                ComponentLookupColor    = SystemAPI.GetComponentLookup<Color>(false),
                
                ComponentTypeHandleTemp = SystemAPI.GetComponentTypeHandle<Temp>(true),
                
                EntityTypeHandle        = SystemAPI.GetEntityTypeHandle(),
            };

            
            // Create a job to update sub object colors.
            UpdateColorsJobSubObject updateColorsJobSubObject = new UpdateColorsJobSubObject()
            {
                ComponentLookupColor            = SystemAPI.GetComponentLookup<Color>(false),
                
                ComponentLookupBuilding         = SystemAPI.GetComponentLookup<Building         >(true),
                ComponentLookupElevation        = SystemAPI.GetComponentLookup<Elevation        >(true),
                ComponentLookupOwner            = SystemAPI.GetComponentLookup<Owner            >(true),
                ComponentLookupVehicle          = SystemAPI.GetComponentLookup<Vehicle          >(true),
                
                ComponentTypeHandleElevation    = SystemAPI.GetComponentTypeHandle<Elevation    >(true),
                ComponentTypeHandleOwner        = SystemAPI.GetComponentTypeHandle<Owner        >(true),
                ComponentTypeHandleTree         = SystemAPI.GetComponentTypeHandle<Tree         >(true),
                
                EntityTypeHandle                = SystemAPI.GetEntityTypeHandle(),
            };


            // Schedule the jobs with dependencies so the jobs run in order.
            // Schedule each job to execute in parallel (i.e. job uses multiple threads, if available).
            // Parallel threads execute much faster than a single thread.
            // Do attachment buildings before middle buildings because some middle buildings have an attachment building as owner.
            JobHandle jobHandleDefault            = JobChunkExtensions.ScheduleParallel(updateColorsJobDefault,            _queryDefault,            base.Dependency);
            JobHandle jobHandleMainBuilding       = JobChunkExtensions.ScheduleParallel(updateColorsJobMainBuilding,       _queryMainBuilding,       JobHandle.CombineDependencies(jobHandleDefault, activeBuildingStatusDataJobHandle));
            JobHandle jobHandleNext = jobHandleMainBuilding;
            if (Mod.ModSettings.ColorSpecializedIndustryLots)
            {
                jobHandleNext                     = JobChunkExtensions.ScheduleParallel(updateColorsJobAttachmentBuilding, _queryAttachmentBuilding, jobHandleMainBuilding);
            }
            JobHandle jobHandleMiddleBuilding     = JobChunkExtensions.ScheduleParallel(updateColorsJobMiddleBuilding,     _queryMiddleBuilding,     jobHandleNext);
            JobHandle jobHandleTempObject         = JobChunkExtensions.ScheduleParallel(updateColorsJobTempObject,         _queryTempObject,         jobHandleMiddleBuilding);
            JobHandle jobHandleSubObject          = JobChunkExtensions.ScheduleParallel(updateColorsJobSubObject,          _querySubObject,          jobHandleTempObject);

            // Prevent these jobs from running again until last job is complete.
            base.Dependency = jobHandleSubObject;

            // Wait for the main building job to complete before accessing total used and capacity.
            jobHandleMainBuilding.Complete();

            // Dispose of native collections no longer needed once the main building job is complete.
            activeBuildingStatusDataChunks.Dispose();

            // Jobs scheduled after and dependent on the main building job could still be executing at this point, which is okay.
            // Those jobs only set building color based on main building color.
            // All the data needed to update the UI has been collected by the main building job.
            // So proceed now with updating the UI while those subsequent jobs might still be executing.

            // For the active infoview, get building status type datas and first building status type.
            BUBuildingStatusTypeDatas buildingStatusTypeDatas = BUInfoviewDatas.instance[activeInfoview].buildingStatusTypeDatas;
            BUBuildingStatusType buildingStatusTypeFirst = buildingStatusTypeDatas.buildingStatusTypeFirst;

            // Compute total used and capacity by building status type.
            // Totals are double because some data values can exceed the max value of an int.
            // Do each thread entry in the total used and capacity array.
            double[] totalUsed     = new double[buildingStatusTypeDatas.Count];
            double[] totalCapacity = new double[buildingStatusTypeDatas.Count];
            for (int i = 0; i < _totalUsedCapacity.Length; i++)
            {
                // Do each used and capacity subtotal entry in the subtotal list.
                NativeList<SubtotalUsedCapacity> subtotalList = _totalUsedCapacity[i];
                for (int j = 0; j < subtotalList.Length; j++)
                {
                    // Add used and capacity from this entry to totals.
                    // Index into the total arrays is the building status type minus the first building status type.
                    // This assumes the building status types are in sequential numerical order in the enum, which they should always be.
                    SubtotalUsedCapacity subtotalUsedCapacity = subtotalList[j];
                    int totalIndex = subtotalUsedCapacity.BuildingStatusType - buildingStatusTypeFirst;
                    totalUsed    [totalIndex] += subtotalUsedCapacity.Used;
                    totalCapacity[totalIndex] += subtotalUsedCapacity.Capacity;
                }
            }

            // Update building status type data values.
            buildingStatusTypeDatas.UpdateDataValues(totalUsed, totalCapacity);

            // Wait for the middle building job to complete to help reduce building flicker.
            jobHandleMiddleBuilding.Complete();

            // This system handled building colors for one of this mod's infoviews.
            // Do not execute the original game logic.
            return false;
        }
    }
}
