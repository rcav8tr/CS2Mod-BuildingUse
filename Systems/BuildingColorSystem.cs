using Colossal.Collections;
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

namespace BuildingUse
{
    /// <summary>
    /// System to set building colors.
    /// Adapted from Game.Rendering.ObjectColorSystem.
    /// This system replaces the game's ObjectColorSystem logic when one of this mod's infoviews is selected.
    /// </summary>
    public partial class BuildingColorSystem : Game.GameSystemBase
    {
        /// <summary>
        /// Subtotal of used and capacity data for a building status type.
        /// </summary>
        private struct SubtotalUsedCapacity
        {
            public BUBuildingStatusType buildingStatusType;
            public long used;
            public long capacity;
        }

        /// <summary>
        /// Job to set the color to default on all objects that have a color.
        /// In this way, any object not set by subsequent jobs is assured to be the default color.
        /// </summary>
        [BurstCompile]
        private partial struct UpdateColorsJobDefault : IJobChunk
        {
            // Color component type to update.
            public ComponentTypeHandle<Game.Objects.Color> ComponentTypeHandleColor;

            /// <summary>
            /// Job execution.
            /// </summary>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // Set color to default for all objects.
                NativeArray<Game.Objects.Color> colors = chunk.GetNativeArray(ref ComponentTypeHandleColor);
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = default;
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
            public ComponentTypeHandle<Game.Objects.Color> ComponentTypeHandleColor;

            // Buffer lookups.
            [ReadOnly] public BufferLookup<Game.Buildings.          Efficiency                  > BufferLookupEfficiency;
            [ReadOnly] public BufferLookup<Game.Companies.          Employee                    > BufferLookupEmployee;
            [ReadOnly] public BufferLookup<Game.Citizens.           HouseholdCitizen            > BufferLookupHouseholdCitizen;
    		[ReadOnly] public BufferLookup<Game.Buildings.          InstalledUpgrade            > BufferLookupInstalledUpgrade;
            [ReadOnly] public BufferLookup<Game.Net.                LaneObject                  > BufferLookupLaneObject;
            [ReadOnly] public BufferLookup<Game.Buildings.          Occupant                    > BufferLookupOccupant;
            [ReadOnly] public BufferLookup<Game.Vehicles.           OwnedVehicle                > BufferLookupOwnedVehicle;
            [ReadOnly] public BufferLookup<Game.Buildings.          Patient                     > BufferLookupPatient;
            [ReadOnly] public BufferLookup<Game.Buildings.          Renter                      > BufferLookupRenter;
            [ReadOnly] public BufferLookup<Game.Economy.            Resources                   > BufferLookupResources;
            [ReadOnly] public BufferLookup<Game.Buildings.          Student                     > BufferLookupStudent;
            [ReadOnly] public BufferLookup<Game.Areas.              SubArea                     > BufferLookupSubArea;
            [ReadOnly] public BufferLookup<Game.Net.                SubLane                     > BufferLookupSubLane;
            [ReadOnly] public BufferLookup<Game.Net.                SubNet                      > BufferLookupSubNet;
            [ReadOnly] public BufferLookup<Game.Objects.            SubObject                   > BufferLookupSubObject;

            // Component lookups.
            [ReadOnly] public ComponentLookup<Game.Vehicles.        Ambulance                   > ComponentLookupAmbulance;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         BatteryData                 > ComponentLookupBatteryData;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         BuildingData                > ComponentLookupBuildingData;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         BuildingPropertyData        > ComponentLookupBuildingPropertyData;
            [ReadOnly] public ComponentLookup<Game.Citizens.        Citizen                     > ComponentLookupCitizen;
            [ReadOnly] public ComponentLookup<Game.Companies.       CompanyData                 > ComponentLookupCompanyData;
            [ReadOnly] public ComponentLookup<Game.Net.             Curve                       > ComponentLookupCurve;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         DeathcareFacilityData       > ComponentLookupDeathcareFacilityData;
            [ReadOnly] public ComponentLookup<Game.Vehicles.        DeliveryTruck               > ComponentLookupDeliveryTruck;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         EmergencyShelterData        > ComponentLookupEmergencyShelterData;
            [ReadOnly] public ComponentLookup<Game.Vehicles.        EvacuatingTransport         > ComponentLookupEvacuatingTransport;
            [ReadOnly] public ComponentLookup<Game.Vehicles.        FireEngine                  > ComponentLookupFireEngine;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         FireStationData             > ComponentLookupFireStationData;
            [ReadOnly] public ComponentLookup<Game.Net.             GarageLane                  > ComponentLookupGarageLane;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         GarbageFacilityData         > ComponentLookupGarbageFacilityData;
            [ReadOnly] public ComponentLookup<Game.Vehicles.        GarbageTruck                > ComponentLookupGarbageTruck;
            [ReadOnly] public ComponentLookup<Game.Areas.           Geometry                    > ComponentLookupGeometry;
            [ReadOnly] public ComponentLookup<Game.Citizens.        HealthProblem               > ComponentLookupHealthProblem;
            [ReadOnly] public ComponentLookup<Game.Vehicles.        Hearse                      > ComponentLookupHearse;
            [ReadOnly] public ComponentLookup<Game.Vehicles.        Helicopter                  > ComponentLookupHelicopter;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         HospitalData                > ComponentLookupHospitalData;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         MailBoxData                 > ComponentLookupMailBoxData;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         MaintenanceDepotData        > ComponentLookupMaintenanceDepotData;
            [ReadOnly] public ComponentLookup<Game.Vehicles.        MaintenanceVehicle          > ComponentLookupMaintenanceVehicle;
            [ReadOnly] public ComponentLookup<Game.Vehicles.        ParkedCar                   > ComponentLookupParkedCar;
            [ReadOnly] public ComponentLookup<Game.Vehicles.        ParkedTrain                 > ComponentLookupParkedTrain;
            [ReadOnly] public ComponentLookup<Game.Net.             ParkingLane                 > ComponentLookupParkingLane;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         ParkingLaneData             > ComponentLookupParkingLaneData;
            [ReadOnly] public ComponentLookup<Game.Vehicles.        PoliceCar                   > ComponentLookupPoliceCar;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         PoliceStationData           > ComponentLookupPoliceStationData;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         PostFacilityData            > ComponentLookupPostFacilityData;
            [ReadOnly] public ComponentLookup<Game.Vehicles.        PostVan                     > ComponentLookupPostVan;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         PrefabRef                   > ComponentLookupPrefabRef;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         PrisonData                  > ComponentLookupPrisonData;
            [ReadOnly] public ComponentLookup<Game.Vehicles.        PrisonerTransport           > ComponentLookupPrisonerTransport;
            [ReadOnly] public ComponentLookup<Game.Buildings.       PropertyRenter              > ComponentLookupPropertyRenter;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         PublicTransportVehicleData  > ComponentLookupPublicTransportVehicleData;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         SchoolData                  > ComponentLookupSchoolData;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         SpawnableBuildingData       > ComponentLookupSpawnableBuildingData;
            [ReadOnly] public ComponentLookup<Game.Areas.           Storage                     > ComponentLookupStorage;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         StorageAreaData             > ComponentLookupStorageAreaData;
            [ReadOnly] public ComponentLookup<Game.Companies.       StorageLimitData            > ComponentLookupStorageLimitData;
            [ReadOnly] public ComponentLookup<Game.Companies.       TransportCompanyData        > ComponentLookupTransportCompanyData;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         TransportDepotData          > ComponentLookupTransportDepotData;
            [ReadOnly] public ComponentLookup<Game.Vehicles.        Taxi                        > ComponentLookupTaxi;
            [ReadOnly] public ComponentLookup<Game.Prefabs.         WorkplaceData               > ComponentLookupWorkplaceData;
            [ReadOnly] public ComponentLookup<Game.Companies.       WorkProvider                > ComponentLookupWorkProvider;

            // Component type handles for buildings.
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   AdminBuilding               > ComponentTypeHandleAdminBuilding;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   Battery                     > ComponentTypeHandleBattery;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   CommercialProperty          > ComponentTypeHandleCommercialProperty;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   DeathcareFacility           > ComponentTypeHandleDeathcareFacility;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   DisasterFacility            > ComponentTypeHandleDisasterFacility;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   ElectricityProducer         > ComponentTypeHandleElectricityProducer;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   EmergencyShelter            > ComponentTypeHandleEmergencyShelter;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   FireStation                 > ComponentTypeHandleFireStation;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   GarbageFacility             > ComponentTypeHandleGarbageFacility;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   Hospital                    > ComponentTypeHandleHospital;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   IndustrialProperty          > ComponentTypeHandleIndustrialProperty;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   OfficeProperty              > ComponentTypeHandleOfficeProperty;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   Park                        > ComponentTypeHandlePark;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   ParkingFacility             > ComponentTypeHandleParkingFacility;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   ParkMaintenance             > ComponentTypeHandleParkMaintenance;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   PoliceStation               > ComponentTypeHandlePoliceStation;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   PostFacility                > ComponentTypeHandlePostFacility;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   Prison                      > ComponentTypeHandlePrison;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   ResearchFacility            > ComponentTypeHandleResearchFacility;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   ResidentialProperty         > ComponentTypeHandleResidentialProperty;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   RoadMaintenance             > ComponentTypeHandleRoadMaintenance;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   School                      > ComponentTypeHandleSchool;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   SewageOutlet                > ComponentTypeHandleSewageOutlet;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   TelecomFacility             > ComponentTypeHandleTelecomFacility;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   Transformer                 > ComponentTypeHandleTransformer;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   TransportDepot              > ComponentTypeHandleTransportDepot;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   TransportStation            > ComponentTypeHandleTransportStation;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   WaterPumpingStation         > ComponentTypeHandleWaterPumpingStation;
            [ReadOnly] public ComponentTypeHandle<Game.Buildings.   WelfareOffice               > ComponentTypeHandleWelfareOffice;

            // Component type handles for miscellaneous.
            [ReadOnly] public ComponentTypeHandle<Game.Areas.       CurrentDistrict             > ComponentTypeHandleCurrentDistrict;
            [ReadOnly] public ComponentTypeHandle<Game.Common.      Destroyed                   > ComponentTypeHandleDestroyed;
            [ReadOnly] public ComponentTypeHandle<Game.Prefabs.     InfomodeActive              > ComponentTypeHandleInfomodeActive;
            [ReadOnly] public ComponentTypeHandle<Game.Prefabs.     InfoviewBuildingStatusData  > ComponentTypeHandleInfoviewBuildingStatusData;
            [ReadOnly] public ComponentTypeHandle<Game.Routes.      MailBox                     > ComponentTypeHandleMailBox;
            [ReadOnly] public ComponentTypeHandle<Game.Prefabs.     PrefabRef                   > ComponentTypeHandlePrefabRef;
            [ReadOnly] public ComponentTypeHandle<Game.Companies.   TransportCompany            > ComponentTypeHandleTransportCompany;
            [ReadOnly] public ComponentTypeHandle<Game.Objects.     UnderConstruction           > ComponentTypeHandleUnderConstruction;

            // Entity type handle.
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;

            // Active infoview.
            [ReadOnly] public BUInfoview ActiveInfoview;

            // List of active building status data chunks.
            [ReadOnly] public NativeList<ArchetypeChunk> ActiveBuildingStatusDataChunks;

            // Array of lists to return total used and capacity to the BuildingColorSystem.
            // The outer array is one for each possible thread.
            // The inner list is one for each subtotal computed in that thread.
            // Even though the outer array is read only, entries can still be added to the inner lists.
            [ReadOnly] public NativeArray<NativeList<SubtotalUsedCapacity>> TotalUsedCapacity;

            // Mod settings used in the job.
            [ReadOnly] public bool CountVehiclesInUse;
            [ReadOnly] public bool CountVehiclesInMaintenance;
            [ReadOnly] public bool EfficiencyMaxColor200Percent;

            // Selected district.
            [ReadOnly] public Entity SelectedDistrict;
            [ReadOnly] public bool SelectedDistrictIsEntireCity;

            /// <summary>
            /// Job execution.
            /// </summary>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // Get colors to set.
                NativeArray<Game.Objects.Color> colors = chunk.GetNativeArray(ref ComponentTypeHandleColor);

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
                        NativeArray<Game.Prefabs.InfoviewBuildingStatusData> activeBuildingStatusDatas = activeBuildingStatusDataChunk.GetNativeArray(ref ComponentTypeHandleInfoviewBuildingStatusData);
                        for (int j = 0; j < activeBuildingStatusDatas.Length; j++)
                        {
                            // Check if the applicable building status type is active.
                            Game.Prefabs.InfoviewBuildingStatusData activeBuildingStatusData = activeBuildingStatusDatas[j];
                            BUBuildingStatusType activeBuildingStatusType = (BUBuildingStatusType)activeBuildingStatusData.m_Type;
                            if (applicableBuildingStatusType == activeBuildingStatusType)
                            {
                                // Get active infomodes.
                                // Empty     means no    infomode in this building status data chunk is active.
                                // Non-empty means every infomode in this building status data chunk is active.
                                NativeArray<Game.Prefabs.InfomodeActive> activeInfomodes = activeBuildingStatusDataChunk.GetNativeArray(ref ComponentTypeHandleInfomodeActive);

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
                NativeArray<Game.Common.Destroyed         > destroyeds           = chunk.GetNativeArray(ref ComponentTypeHandleDestroyed);
                NativeArray<Game.Objects.UnderConstruction> underConstructions   = chunk.GetNativeArray(ref ComponentTypeHandleUnderConstruction);
                NativeArray<Game.Prefabs.PrefabRef        > prefabRefs           = chunk.GetNativeArray(ref ComponentTypeHandlePrefabRef);
                for (int i = 0; i < prefabRefs.Length; i++)
                {
                    if ((ComponentLookupBuildingData[prefabRefs[i].m_Prefab].m_Flags & Game.Prefabs.BuildingFlags.ColorizeLot) != 0 || 
                        (CollectionUtils.TryGet(destroyeds,         i, out Game.Common.Destroyed          destroyed        ) && destroyed.m_Cleared >= 0f) || 
                        (CollectionUtils.TryGet(underConstructions, i, out Game.Objects.UnderConstruction underConstruction) && underConstruction.m_NewPrefab == Entity.Null))
                    {
                        // Set SubColor flag on the color.
                        // Not sure what the SubColor flag does.
                        Game.Objects.Color color = colors[i];
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
            ) where T : unmanaged, IComponentData, Game.Prefabs.ICombineData<T>
            {
                // Logic adapted from Game.UI.InGame.InfoSectionBase.TryGetComponentWithUpgrades
                // which simply calls Game.Prefabs.UpgradeUtils.TryGetCombinedComponent.

                // Try to get the component data directly from the prefab.
		        bool hasComponentData = componentLookup.TryGetComponent(prefab, out componentData);

		        // Check if entity has any installed upgrades.
                // Logic adapted from Game.Prefabs.UpgradeUtils.TryCombineData.
		        bool hasInstalledUpgrade = false;
        		if (BufferLookupInstalledUpgrade.TryGetBuffer(entity, out DynamicBuffer<Game.Buildings.InstalledUpgrade> installedUpgrades))
                {
                    // Do each installed upgrade.
		            for (int i = 0; i < installedUpgrades.Length; i++)
		            {
                        // Installed upgrade must not be inactive and prefab of installed upgrade must have component data type.
			            Game.Buildings.InstalledUpgrade installedUpgrade = installedUpgrades[i];
			            if (!Game.Buildings.BuildingUtils.CheckOption(installedUpgrade, Game.Buildings.BuildingOption.Inactive) && 
                            ComponentLookupPrefabRef.TryGetComponent(installedUpgrade.m_Upgrade, out Game.Prefabs.PrefabRef installedUpgradePrefabRef) && 
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
            private void UpdateEntityColor(long used, long capacity, bool infomodeActive, int infomodeIndex, NativeArray<Game.Objects.Color> colors, int colorsIndex)
            {
                // If infomode is active, set color for this entity.
                // Otherwise, leave entity color as the default color set earlier, which is the grayish/off-white color.
                if (infomodeActive)
                {
                    // All infomodes for this mod have a range from 0 to 255 which represents 0% to 100%.
                    float useRatio = capacity > 0 ? (float)used / capacity : 0f;
                    colors[colorsIndex] = new Game.Objects.Color((byte)infomodeIndex, (byte)math.clamp(Mathf.RoundToInt(255f * useRatio), 0, 255)); 
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
                        new SubtotalUsedCapacity() { buildingStatusType = buildingStatusType, used = used, capacity = capacity });
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
        /// Job to set the color of each middle building to the color of its owner.
        /// Middle buildings include sub buildings (i.e. building upgrades placed around the perimeter of the main building).
        /// Logic is adapted from Game.Rendering.ObjectColorSystem except to handle only buildings and variables are renamed to improve readability.
        /// </summary>
        [BurstCompile]
        private struct UpdateColorsJobMiddleBuilding : IJobChunk
        {
            // Color component lookup to update.
            [NativeDisableParallelForRestriction] public ComponentLookup<Game.Objects.Color> ComponentLookupColor;

            // Component type handles.
            [ReadOnly] public ComponentTypeHandle<Game.Common.Owner> ComponentTypeHandleOwner;

            // Entity type handle.
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;

            /// <summary>
            /// Job execution.
            /// </summary>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // Do each entity.
                NativeArray<Game.Common.Owner> owners   = chunk.GetNativeArray(ref ComponentTypeHandleOwner);
                NativeArray<Entity           > entities = chunk.GetNativeArray(EntityTypeHandle);
                for (int i = 0; i < entities.Length; i++)
                {
                    // Get the color of the owner entity.
                    if (ComponentLookupColor.TryGetComponent(owners[i].m_Owner, out Game.Objects.Color ownerColor))
                    {
                        // Set color of this entity to color of owner entity.
                        Entity entity = entities[i];
                        Game.Objects.Color color = ComponentLookupColor[entity];
                        color.m_Index = ownerColor.m_Index;
                        color.m_Value = ownerColor.m_Value;
                        ComponentLookupColor[entity] = color;
                    }
                }
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
            [NativeDisableParallelForRestriction] public ComponentLookup<Game.Objects.Color> ComponentLookupColor;

            // Component type handles.
            [ReadOnly] public ComponentTypeHandle<Game.Objects.Attachment> ComponentTypeHandleAttachment;

            // Entity type handle.
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;

            /// <summary>
            /// Job execution.
            /// </summary>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // Do each attachment entity.
                NativeArray<Game.Objects.Attachment> attachments = chunk.GetNativeArray(ref ComponentTypeHandleAttachment);
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityTypeHandle);
                for (int i = 0; i < entities.Length; i++)
                {
                    // Get the color of the attached entity.
                    if (ComponentLookupColor.TryGetComponent(attachments[i].m_Attached, out Game.Objects.Color attachedColor))
                    {
                        // Set color of this attachment entity to the color of the attached entity.
                        Entity entity = entities[i];
                        Game.Objects.Color color = ComponentLookupColor[entity];
                        color.m_Index = attachedColor.m_Index;
                        color.m_Value = attachedColor.m_Value;
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
            [NativeDisableParallelForRestriction] public ComponentLookup<Game.Objects.Color> ComponentLookupColor;

            // Component type handles.
            [ReadOnly] public ComponentTypeHandle<Game.Tools.Temp> ComponentTypeHandleTemp;

            // Entity type handle.
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;

            /// <summary>
            /// Job execution.
            /// </summary>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // Set color of object to color of its original.
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityTypeHandle);
                NativeArray<Game.Tools.Temp> temps = chunk.GetNativeArray(ref ComponentTypeHandleTemp);
                for (int i = 0; i < temps.Length; i++)
                {
                    if (ComponentLookupColor.TryGetComponent(temps[i].m_Original, out Game.Objects.Color originalColor))
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
            [NativeDisableParallelForRestriction] public ComponentLookup<Game.Objects.Color> ComponentLookupColor;

            // Component lookups.
            [ReadOnly] public ComponentLookup<Game.Buildings.   Building    > ComponentLookupBuilding;
            [ReadOnly] public ComponentLookup<Game.Objects.     Elevation   > ComponentLookupElevation;
            [ReadOnly] public ComponentLookup<Game.Common.      Owner       > ComponentLookupOwner;
            [ReadOnly] public ComponentLookup<Game.Vehicles.    Vehicle     > ComponentLookupVehicle;

            // Component type handles.
            [ReadOnly] public ComponentTypeHandle<Game.Objects. Elevation   > ComponentTypeHandleElevation;
            [ReadOnly] public ComponentTypeHandle<Game.Common.  Owner       > ComponentTypeHandleOwner;
            [ReadOnly] public ComponentTypeHandle<Game.Objects. Tree        > ComponentTypeHandleTree;

            // Entity type handle.
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;

            /// <summary>
            /// Job execution.
            /// </summary>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Game.Common.Owner> owners = chunk.GetNativeArray(ref ComponentTypeHandleOwner);
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityTypeHandle);
                if (chunk.Has(ref ComponentTypeHandleTree))
                {
                    NativeArray<Game.Objects.Elevation> elevations = chunk.GetNativeArray(ref ComponentTypeHandleElevation);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        Entity entity = entities[i];
                        Game.Common.Owner owner = owners[i];
                        Game.Objects.Elevation elevation;
                        bool flag = CollectionUtils.TryGet(elevations, i, out elevation) && (elevation.m_Flags & Game.Objects.ElevationFlags.OnGround) == 0;
                        bool flag2 = flag && !ComponentLookupColor.HasComponent(owner.m_Owner);
                        Game.Common.Owner newOwner;
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
                                    flag &= ComponentLookupElevation.TryGetComponent(owner.m_Owner, out elevation) && (elevation.m_Flags & Game.Objects.ElevationFlags.OnGround) == 0;
                                }
                            }
                            owner = newOwner;
                        }
                        if (ComponentLookupColor.TryGetComponent(owner.m_Owner, out Game.Objects.Color color) && (flag || color.m_SubColor))
                        {
                            ComponentLookupColor[entity] = color;
                        }
                    }
                    return;
                }

                for (int j = 0; j < entities.Length; j++)
                {
                    Game.Common.Owner owner = owners[j];
                    Game.Common.Owner newOwner;
                    while (ComponentLookupOwner.TryGetComponent(owner.m_Owner, out newOwner) && !ComponentLookupBuilding.HasComponent(owner.m_Owner) && !ComponentLookupVehicle.HasComponent(owner.m_Owner))
                    {
                        owner = newOwner;
                    }
                    if (ComponentLookupColor.TryGetComponent(owner.m_Owner, out Game.Objects.Color color))
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
        private Game.Tools.ToolSystem _toolSystem;
        private BuildingUseUISystem _buildingUseUISystem;

        // Entity queries.
        private EntityQuery _queryDefault;
        private EntityQuery _queryMainBuilding;
        private EntityQuery _queryMiddleBuilding;
        private EntityQuery _queryAttachmentBuilding;
        private EntityQuery _queryTempObject;
        private EntityQuery _querySubObject;
        private EntityQuery _queryActiveBuildingStatusData;

        // The component lookup and type handle for color.
        // These are used to set building color.
        private ComponentLookup    <Game.Objects.   Color                       > _componentLookupColor;
        private ComponentTypeHandle<Game.Objects.   Color                       > _componentTypeHandleColor;

        // Buffer lookups.
        private BufferLookup<Game.Buildings.        Efficiency                  > _bufferLookupEfficiency;
        private BufferLookup<Game.Companies.        Employee                    > _bufferLookupEmployee;
        private BufferLookup<Game.Citizens.         HouseholdCitizen            > _bufferLookupHouseholdCitizen;
        private BufferLookup<Game.Buildings.        InstalledUpgrade            > _bufferLookupInstalledUpgrade;
        private BufferLookup<Game.Net.              LaneObject                  > _bufferLookupLaneObject;
        private BufferLookup<Game.Buildings.        Occupant                    > _bufferLookupOccupant;
        private BufferLookup<Game.Vehicles.         OwnedVehicle                > _bufferLookupOwnedVehicle;
        private BufferLookup<Game.Buildings.        Patient                     > _bufferLookupPatient;
        private BufferLookup<Game.Buildings.        Renter                      > _bufferLookupRenter;
        private BufferLookup<Game.Economy.          Resources                   > _bufferLookupResources;
        private BufferLookup<Game.Buildings.        Student                     > _bufferLookupStudent;
        private BufferLookup<Game.Areas.            SubArea                     > _bufferLookupSubArea;
        private BufferLookup<Game.Net.              SubLane                     > _bufferLookupSubLane;
        private BufferLookup<Game.Net.              SubNet                      > _bufferLookupSubNet;
        private BufferLookup<Game.Objects.          SubObject                   > _bufferLookupSubObject;

        // Component lookups.
        private ComponentLookup<Game.Vehicles.      Ambulance                   > _componentLookupAmbulance;
        private ComponentLookup<Game.Prefabs.       BatteryData                 > _componentLookupBatteryData;
        private ComponentLookup<Game.Buildings.     Building                    > _componentLookupBuilding;
        private ComponentLookup<Game.Prefabs.       BuildingData                > _componentLookupBuildingData;
        private ComponentLookup<Game.Prefabs.       BuildingPropertyData        > _componentLookupBuildingPropertyData;
        private ComponentLookup<Game.Citizens.      Citizen                     > _componentLookupCitizen;
        private ComponentLookup<Game.Companies.     CompanyData                 > _componentLookupCompanyData;
        private ComponentLookup<Game.Net.           Curve                       > _componentLookupCurve;
        private ComponentLookup<Game.Prefabs.       DeathcareFacilityData       > _componentLookupDeathcareFacilityData;
        private ComponentLookup<Game.Vehicles.      DeliveryTruck               > _componentLookupDeliveryTruck;
        private ComponentLookup<Game.Objects.       Elevation                   > _componentLookupElevation;
        private ComponentLookup<Game.Prefabs.       EmergencyShelterData        > _componentLookupEmergencyShelterData;
        private ComponentLookup<Game.Vehicles.      EvacuatingTransport         > _componentLookupEvacuatingTransport;
        private ComponentLookup<Game.Vehicles.      FireEngine                  > _componentLookupFireEngine;
        private ComponentLookup<Game.Prefabs.       FireStationData             > _componentLookupFireStationData;
        private ComponentLookup<Game.Net.           GarageLane                  > _componentLookupGarageLane;
        private ComponentLookup<Game.Prefabs.       GarbageFacilityData         > _componentLookupGarbageFacilityData;
        private ComponentLookup<Game.Vehicles.      GarbageTruck                > _componentLookupGarbageTruck;
        private ComponentLookup<Game.Areas.         Geometry                    > _componentLookupGeometry;
        private ComponentLookup<Game.Citizens.      HealthProblem               > _componentLookupHealthProblem;
        private ComponentLookup<Game.Vehicles.      Hearse                      > _componentLookupHearse;
        private ComponentLookup<Game.Vehicles.      Helicopter                  > _componentLookupHelicopter;
        private ComponentLookup<Game.Prefabs.       HospitalData                > _componentLookupHospitalData;
        private ComponentLookup<Game.Prefabs.       MailBoxData                 > _componentLookupMailBoxData;
        private ComponentLookup<Game.Prefabs.       MaintenanceDepotData        > _componentLookupMaintenanceDepotData;
        private ComponentLookup<Game.Vehicles.      MaintenanceVehicle          > _componentLookupMaintenanceVehicle;
        private ComponentLookup<Game.Common.        Owner                       > _componentLookupOwner;
        private ComponentLookup<Game.Vehicles.      ParkedCar                   > _componentLookupParkedCar;
        private ComponentLookup<Game.Vehicles.      ParkedTrain                 > _componentLookupParkedTrain;
        private ComponentLookup<Game.Net.           ParkingLane                 > _componentLookupParkingLane;
        private ComponentLookup<Game.Prefabs.       ParkingLaneData             > _componentLookupParkingLaneData;
        private ComponentLookup<Game.Vehicles.      PoliceCar                   > _componentLookupPoliceCar;
        private ComponentLookup<Game.Prefabs.       PoliceStationData           > _componentLookupPoliceStationData;
        private ComponentLookup<Game.Prefabs.       PostFacilityData            > _componentLookupPostFacilityData;
        private ComponentLookup<Game.Vehicles.      PostVan                     > _componentLookupPostVan;
        private ComponentLookup<Game.Prefabs.       PrefabRef                   > _componentLookupPrefabRef;
        private ComponentLookup<Game.Prefabs.       PrisonData                  > _componentLookupPrisonData;
        private ComponentLookup<Game.Vehicles.      PrisonerTransport           > _componentLookupPrisonerTransport;
        private ComponentLookup<Game.Buildings.     PropertyRenter              > _componentLookupPropertyRenter;
        private ComponentLookup<Game.Prefabs.       PublicTransportVehicleData  > _componentLookupPublicTransportVehicleData;
        private ComponentLookup<Game.Prefabs.       SchoolData                  > _componentLookupSchoolData;
        private ComponentLookup<Game.Prefabs.       SpawnableBuildingData       > _componentLookupSpawnableBuildingData;
        private ComponentLookup<Game.Areas.         Storage                     > _componentLookupStorage;
        private ComponentLookup<Game.Prefabs.       StorageAreaData             > _componentLookupStorageAreaData;
        private ComponentLookup<Game.Companies.     StorageLimitData            > _componentLookupStorageLimitData;
        private ComponentLookup<Game.Vehicles.      Taxi                        > _componentLookupTaxi;
        private ComponentLookup<Game.Companies.     TransportCompanyData        > _componentLookupTransportCompanyData;
        private ComponentLookup<Game.Prefabs.       TransportDepotData          > _componentLookupTransportDepotData;
        private ComponentLookup<Game.Vehicles.      Vehicle                     > _componentLookupVehicle;
        private ComponentLookup<Game.Prefabs.       WorkplaceData               > _componentLookupWorkplaceData;
        private ComponentLookup<Game.Companies.     WorkProvider                > _componentLookupWorkProvider;

        // Component type handles for buildings.
        // The presence of these on a building defines the building type.
        private ComponentTypeHandle<Game.Buildings. AdminBuilding               > _componentTypeHandleAdminBuilding;
        private ComponentTypeHandle<Game.Buildings. Battery                     > _componentTypeHandleBattery;
        private ComponentTypeHandle<Game.Buildings. CommercialProperty          > _componentTypeHandleCommercialProperty;
        private ComponentTypeHandle<Game.Buildings. DeathcareFacility           > _componentTypeHandleDeathcareFacility;
        private ComponentTypeHandle<Game.Buildings. DisasterFacility            > _componentTypeHandleDisasterFacility;
        private ComponentTypeHandle<Game.Buildings. ElectricityProducer         > _componentTypeHandleElectricityProducer;
        private ComponentTypeHandle<Game.Buildings. EmergencyShelter            > _componentTypeHandleEmergencyShelter;
        private ComponentTypeHandle<Game.Buildings. FireStation                 > _componentTypeHandleFireStation;
        private ComponentTypeHandle<Game.Buildings. GarbageFacility             > _componentTypeHandleGarbageFacility;
        private ComponentTypeHandle<Game.Buildings. Hospital                    > _componentTypeHandleHospital;
        private ComponentTypeHandle<Game.Buildings. IndustrialProperty          > _componentTypeHandleIndustrialProperty;
        private ComponentTypeHandle<Game.Buildings. OfficeProperty              > _componentTypeHandleOfficeProperty;
        private ComponentTypeHandle<Game.Buildings. Park                        > _componentTypeHandlePark;
        private ComponentTypeHandle<Game.Buildings. ParkingFacility             > _componentTypeHandleParkingFacility;
        private ComponentTypeHandle<Game.Buildings. ParkMaintenance             > _componentTypeHandleParkMaintenance;
        private ComponentTypeHandle<Game.Buildings. PoliceStation               > _componentTypeHandlePoliceStation;
        private ComponentTypeHandle<Game.Buildings. PostFacility                > _componentTypeHandlePostFacility;
        private ComponentTypeHandle<Game.Buildings. Prison                      > _componentTypeHandlePrison;
        private ComponentTypeHandle<Game.Buildings. ResearchFacility            > _componentTypeHandleResearchFacility;
        private ComponentTypeHandle<Game.Buildings. ResidentialProperty         > _componentTypeHandleResidentialProperty;
        private ComponentTypeHandle<Game.Buildings. RoadMaintenance             > _componentTypeHandleRoadMaintenance;
        private ComponentTypeHandle<Game.Buildings. School                      > _componentTypeHandleSchool;
        private ComponentTypeHandle<Game.Buildings. SewageOutlet                > _componentTypeHandleSewageOutlet;
        private ComponentTypeHandle<Game.Buildings. TelecomFacility             > _componentTypeHandleTelecomFacility;
        private ComponentTypeHandle<Game.Buildings. Transformer                 > _componentTypeHandleTransformer;
        private ComponentTypeHandle<Game.Buildings. TransportDepot              > _componentTypeHandleTransportDepot;
        private ComponentTypeHandle<Game.Buildings. TransportStation            > _componentTypeHandleTransportStation;
        private ComponentTypeHandle<Game.Buildings. WaterPumpingStation         > _componentTypeHandleWaterPumpingStation;
        private ComponentTypeHandle<Game.Buildings. WelfareOffice               > _componentTypeHandleWelfareOffice;

        // Component type handles for miscellaneous.
        private ComponentTypeHandle<Game.Objects.   Attachment                  > _componentTypeHandleAttachment;
        private ComponentTypeHandle<Game.Areas.     CurrentDistrict             > _componentTypeHandleCurrentDistrict;
        private ComponentTypeHandle<Game.Common.    Destroyed                   > _componentTypeHandleDestroyed;
        private ComponentTypeHandle<Game.Objects.   Elevation                   > _componentTypeHandleElevation;
        private ComponentTypeHandle<Game.Prefabs.   InfomodeActive              > _componentTypeHandleInfomodeActive;
        private ComponentTypeHandle<Game.Prefabs.   InfoviewBuildingStatusData  > _componentTypeHandleInfoviewBuildingStatusData;
        private ComponentTypeHandle<Game.Routes.    MailBox                     > _componentTypeHandleMailBox;
        private ComponentTypeHandle<Game.Common.    Owner                       > _componentTypeHandleOwner;
        private ComponentTypeHandle<Game.Prefabs.   PrefabRef                   > _componentTypeHandlePrefabRef;
        private ComponentTypeHandle<Game.Tools.     Temp                        > _componentTypeHandleTemp;
        private ComponentTypeHandle<Game.Companies. TransportCompany            > _componentTypeHandleTransportCompany;
        private ComponentTypeHandle<Game.Objects.   Tree                        > _componentTypeHandleTree;
        private ComponentTypeHandle<Game.Objects.   UnderConstruction           > _componentTypeHandleUnderConstruction;

        // Entity type handle.
        private EntityTypeHandle _entityTypeHandle;
        
        // Harmony ID.
        private const string HarmonyID = "rcav8tr." + ModAssemblyInfo.Name;

        // Max number of thread entries in the previous frame.
        private int _previousMaxThreadEntries = 8;

        /// <summary>
        /// Gets called right before OnCreate.
        /// </summary>
        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            LogUtil.Info($"{nameof(BuildingColorSystem)}.{nameof(OnCreateForCompiler)}");

            // Assign components for color.
            // These are the only ones that are read/write.
            _componentLookupColor                           = CheckedStateRef.GetComponentLookup    <Game.Objects.      Color                       >();
            _componentTypeHandleColor                       = CheckedStateRef.GetComponentTypeHandle<Game.Objects.      Color                       >();

            // Assign buffer lookups.
            _bufferLookupEfficiency                         = CheckedStateRef.GetBufferLookup<Game.Buildings.           Efficiency                  >(true);
            _bufferLookupEmployee                           = CheckedStateRef.GetBufferLookup<Game.Companies.           Employee                    >(true);
            _bufferLookupHouseholdCitizen                   = CheckedStateRef.GetBufferLookup<Game.Citizens.            HouseholdCitizen            >(true);
            _bufferLookupInstalledUpgrade                   = CheckedStateRef.GetBufferLookup<Game.Buildings.           InstalledUpgrade            >(true);
            _bufferLookupLaneObject                         = CheckedStateRef.GetBufferLookup<Game.Net.                 LaneObject                  >(true);
            _bufferLookupOccupant                           = CheckedStateRef.GetBufferLookup<Game.Buildings.           Occupant                    >(true);
            _bufferLookupOwnedVehicle                       = CheckedStateRef.GetBufferLookup<Game.Vehicles.            OwnedVehicle                >(true);
            _bufferLookupPatient                            = CheckedStateRef.GetBufferLookup<Game.Buildings.           Patient                     >(true);
            _bufferLookupRenter                             = CheckedStateRef.GetBufferLookup<Game.Buildings.           Renter                      >(true);
            _bufferLookupResources                          = CheckedStateRef.GetBufferLookup<Game.Economy.             Resources                   >(true);
            _bufferLookupStudent                            = CheckedStateRef.GetBufferLookup<Game.Buildings.           Student                     >(true);
            _bufferLookupSubArea                            = CheckedStateRef.GetBufferLookup<Game.Areas.               SubArea                     >(true);
            _bufferLookupSubLane                            = CheckedStateRef.GetBufferLookup<Game.Net.                 SubLane                     >(true);
            _bufferLookupSubNet                             = CheckedStateRef.GetBufferLookup<Game.Net.                 SubNet                      >(true);
            _bufferLookupSubObject                          = CheckedStateRef.GetBufferLookup<Game.Objects.             SubObject                   >(true);

            // Assign component lookups.
            _componentLookupAmbulance                       = CheckedStateRef.GetComponentLookup<Game.Vehicles.         Ambulance                   >(true);
            _componentLookupBatteryData                     = CheckedStateRef.GetComponentLookup<Game.Prefabs.          BatteryData                 >(true);
            _componentLookupBuilding                        = CheckedStateRef.GetComponentLookup<Game.Buildings.        Building                    >(true);
            _componentLookupBuildingData                    = CheckedStateRef.GetComponentLookup<Game.Prefabs.          BuildingData                >(true);
            _componentLookupBuildingPropertyData            = CheckedStateRef.GetComponentLookup<Game.Prefabs.          BuildingPropertyData        >(true);
            _componentLookupCitizen                         = CheckedStateRef.GetComponentLookup<Game.Citizens.         Citizen                     >(true);
            _componentLookupCompanyData                     = CheckedStateRef.GetComponentLookup<Game.Companies.        CompanyData                 >(true);
            _componentLookupCurve                           = CheckedStateRef.GetComponentLookup<Game.Net.              Curve                       >(true);
            _componentLookupDeathcareFacilityData           = CheckedStateRef.GetComponentLookup<Game.Prefabs.          DeathcareFacilityData       >(true);
            _componentLookupDeliveryTruck                   = CheckedStateRef.GetComponentLookup<Game.Vehicles.         DeliveryTruck               >(true);
            _componentLookupElevation                       = CheckedStateRef.GetComponentLookup<Game.Objects.          Elevation                   >(true);
            _componentLookupEmergencyShelterData            = CheckedStateRef.GetComponentLookup<Game.Prefabs.          EmergencyShelterData        >(true);
            _componentLookupEvacuatingTransport             = CheckedStateRef.GetComponentLookup<Game.Vehicles.         EvacuatingTransport         >(true);
            _componentLookupFireEngine                      = CheckedStateRef.GetComponentLookup<Game.Vehicles.         FireEngine                  >(true);
            _componentLookupFireStationData                 = CheckedStateRef.GetComponentLookup<Game.Prefabs.          FireStationData             >(true);
            _componentLookupGarageLane                      = CheckedStateRef.GetComponentLookup<Game.Net.              GarageLane                  >(true);
            _componentLookupGarbageFacilityData             = CheckedStateRef.GetComponentLookup<Game.Prefabs.          GarbageFacilityData         >(true);
            _componentLookupGarbageTruck                    = CheckedStateRef.GetComponentLookup<Game.Vehicles.         GarbageTruck                >(true);
            _componentLookupGeometry                        = CheckedStateRef.GetComponentLookup<Game.Areas.            Geometry                    >(true);
            _componentLookupHealthProblem                   = CheckedStateRef.GetComponentLookup<Game.Citizens.         HealthProblem               >(true);
            _componentLookupHearse                          = CheckedStateRef.GetComponentLookup<Game.Vehicles.         Hearse                      >(true);
            _componentLookupHelicopter                      = CheckedStateRef.GetComponentLookup<Game.Vehicles.         Helicopter                  >(true);
            _componentLookupHospitalData                    = CheckedStateRef.GetComponentLookup<Game.Prefabs.          HospitalData                >(true);
            _componentLookupMailBoxData                     = CheckedStateRef.GetComponentLookup<Game.Prefabs.          MailBoxData                 >(true);
            _componentLookupMaintenanceDepotData            = CheckedStateRef.GetComponentLookup<Game.Prefabs.          MaintenanceDepotData        >(true);
            _componentLookupMaintenanceVehicle              = CheckedStateRef.GetComponentLookup<Game.Vehicles.         MaintenanceVehicle          >(true);
            _componentLookupOwner                           = CheckedStateRef.GetComponentLookup<Game.Common.           Owner                       >(true);
            _componentLookupParkedCar                       = CheckedStateRef.GetComponentLookup<Game.Vehicles.         ParkedCar                   >(true);
            _componentLookupParkedTrain                     = CheckedStateRef.GetComponentLookup<Game.Vehicles.         ParkedTrain                 >(true);
            _componentLookupParkingLane                     = CheckedStateRef.GetComponentLookup<Game.Net.              ParkingLane                 >(true);
            _componentLookupParkingLaneData                 = CheckedStateRef.GetComponentLookup<Game.Prefabs.          ParkingLaneData             >(true);
            _componentLookupPoliceCar                       = CheckedStateRef.GetComponentLookup<Game.Vehicles.         PoliceCar                   >(true);
            _componentLookupPoliceStationData               = CheckedStateRef.GetComponentLookup<Game.Prefabs.          PoliceStationData           >(true);
            _componentLookupPostFacilityData                = CheckedStateRef.GetComponentLookup<Game.Prefabs.          PostFacilityData            >(true);
            _componentLookupPostVan                         = CheckedStateRef.GetComponentLookup<Game.Vehicles.         PostVan                     >(true);
            _componentLookupPrefabRef                       = CheckedStateRef.GetComponentLookup<Game.Prefabs.          PrefabRef                   >(true);
            _componentLookupPrisonData                      = CheckedStateRef.GetComponentLookup<Game.Prefabs.          PrisonData                  >(true);
            _componentLookupPrisonerTransport               = CheckedStateRef.GetComponentLookup<Game.Vehicles.         PrisonerTransport           >(true);
            _componentLookupPropertyRenter                  = CheckedStateRef.GetComponentLookup<Game.Buildings.        PropertyRenter              >(true);
            _componentLookupPublicTransportVehicleData      = CheckedStateRef.GetComponentLookup<Game.Prefabs.          PublicTransportVehicleData  >(true);
            _componentLookupSchoolData                      = CheckedStateRef.GetComponentLookup<Game.Prefabs.          SchoolData                  >(true);
            _componentLookupSpawnableBuildingData           = CheckedStateRef.GetComponentLookup<Game.Prefabs.          SpawnableBuildingData       >(true);
            _componentLookupStorage                         = CheckedStateRef.GetComponentLookup<Game.Areas.            Storage                     >(true);
            _componentLookupStorageAreaData                 = CheckedStateRef.GetComponentLookup<Game.Prefabs.          StorageAreaData             >(true);
            _componentLookupStorageLimitData                = CheckedStateRef.GetComponentLookup<Game.Companies.        StorageLimitData            >(true);
            _componentLookupTaxi                            = CheckedStateRef.GetComponentLookup<Game.Vehicles.         Taxi                        >(true);
            _componentLookupTransportCompanyData            = CheckedStateRef.GetComponentLookup<Game.Companies.        TransportCompanyData        >(true);
            _componentLookupTransportDepotData              = CheckedStateRef.GetComponentLookup<Game.Prefabs.          TransportDepotData          >(true);
            _componentLookupVehicle                         = CheckedStateRef.GetComponentLookup<Game.Vehicles.         Vehicle                     >(true);
            _componentLookupWorkplaceData                   = CheckedStateRef.GetComponentLookup<Game.Prefabs.          WorkplaceData               >(true);
            _componentLookupWorkProvider                    = CheckedStateRef.GetComponentLookup<Game.Companies.        WorkProvider                >(true);

            // Assign component type handles for buildings.
            _componentTypeHandleAdminBuilding               = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    AdminBuilding               >(true);
            _componentTypeHandleBattery                     = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    Battery                     >(true);
            _componentTypeHandleCommercialProperty          = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    CommercialProperty          >(true);
            _componentTypeHandleDeathcareFacility           = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    DeathcareFacility           >(true);
            _componentTypeHandleDisasterFacility            = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    DisasterFacility            >(true);
            _componentTypeHandleElectricityProducer         = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    ElectricityProducer         >(true);
            _componentTypeHandleEmergencyShelter            = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    EmergencyShelter            >(true);
            _componentTypeHandleFireStation                 = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    FireStation                 >(true);
            _componentTypeHandleGarbageFacility             = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    GarbageFacility             >(true);
            _componentTypeHandleHospital                    = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    Hospital                    >(true);
            _componentTypeHandleIndustrialProperty          = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    IndustrialProperty          >(true);
            _componentTypeHandleOfficeProperty              = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    OfficeProperty              >(true);
            _componentTypeHandlePark                        = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    Park                        >(true);
            _componentTypeHandleParkingFacility             = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    ParkingFacility             >(true);
            _componentTypeHandleParkMaintenance             = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    ParkMaintenance             >(true);
            _componentTypeHandlePoliceStation               = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    PoliceStation               >(true);
            _componentTypeHandlePostFacility                = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    PostFacility                >(true);
            _componentTypeHandlePrison                      = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    Prison                      >(true);
            _componentTypeHandleResearchFacility            = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    ResearchFacility            >(true);
            _componentTypeHandleResidentialProperty         = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    ResidentialProperty         >(true);
            _componentTypeHandleRoadMaintenance             = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    RoadMaintenance             >(true);
            _componentTypeHandleSchool                      = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    School                      >(true);
            _componentTypeHandleSewageOutlet                = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    SewageOutlet                >(true);
            _componentTypeHandleTelecomFacility             = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    TelecomFacility             >(true);
            _componentTypeHandleTransformer                 = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    Transformer                 >(true);
            _componentTypeHandleTransportDepot              = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    TransportDepot              >(true);
            _componentTypeHandleTransportStation            = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    TransportStation            >(true);
            _componentTypeHandleWaterPumpingStation         = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    WaterPumpingStation         >(true);
            _componentTypeHandleWelfareOffice               = CheckedStateRef.GetComponentTypeHandle<Game.Buildings.    WelfareOffice               >(true);

            // Assign component type handles for miscellaneous.
            _componentTypeHandleAttachment                  = CheckedStateRef.GetComponentTypeHandle<Game.Objects.      Attachment                  >(true);
            _componentTypeHandleCurrentDistrict             = CheckedStateRef.GetComponentTypeHandle<Game.Areas.        CurrentDistrict             >(true);
            _componentTypeHandleDestroyed                   = CheckedStateRef.GetComponentTypeHandle<Game.Common.       Destroyed                   >(true);
            _componentTypeHandleElevation                   = CheckedStateRef.GetComponentTypeHandle<Game.Objects.      Elevation                   >(true);
            _componentTypeHandleInfomodeActive              = CheckedStateRef.GetComponentTypeHandle<Game.Prefabs.      InfomodeActive              >(true);
            _componentTypeHandleInfoviewBuildingStatusData  = CheckedStateRef.GetComponentTypeHandle<Game.Prefabs.      InfoviewBuildingStatusData  >(true);
            _componentTypeHandleMailBox                     = CheckedStateRef.GetComponentTypeHandle<Game.Routes.       MailBox                     >(true);
            _componentTypeHandleOwner                       = CheckedStateRef.GetComponentTypeHandle<Game.Common.       Owner                       >(true);
            _componentTypeHandlePrefabRef                   = CheckedStateRef.GetComponentTypeHandle<Game.Prefabs.      PrefabRef                   >(true);
            _componentTypeHandleTemp                        = CheckedStateRef.GetComponentTypeHandle<Game.Tools.        Temp                        >(true);
            _componentTypeHandleTransportCompany            = CheckedStateRef.GetComponentTypeHandle<Game.Companies.    TransportCompany            >(true);
            _componentTypeHandleTree                        = CheckedStateRef.GetComponentTypeHandle<Game.Objects.      Tree                        >(true);
            _componentTypeHandleUnderConstruction           = CheckedStateRef.GetComponentTypeHandle<Game.Objects.      UnderConstruction           >(true);

            // Assign entity type handle.
            _entityTypeHandle = CheckedStateRef.GetEntityTypeHandle();
        }

        /// <summary>
        /// Initialize this system.
        /// </summary>
        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            LogUtil.Info($"{nameof(BuildingColorSystem)}.{nameof(OnCreate)}");

            // Save the game's instance of this system.
            _buildingColorSystem = this;

            // Get other systems.
            _toolSystem          = base.World.GetOrCreateSystemManaged<Game.Tools.ToolSystem>();
            _buildingUseUISystem = base.World.GetOrCreateSystemManaged<BuildingUseUISystem>();

            // Query to get default objects (i.e. every object that has a color).
		    _queryDefault = GetEntityQuery
            (
                new EntityQueryDesc
		        {
			        All = new ComponentType[]
			        {
				        ComponentType.ReadOnly <Game.Objects.   Object>(),
				        ComponentType.ReadWrite<Game.Objects.   Color>(),
			        },
			        None = new ComponentType[]
			        {
				        ComponentType.ReadOnly<Game.Tools.      Hidden>(),
				        ComponentType.ReadOnly<Game.Common.     Deleted>(),
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
				        ComponentType.ReadOnly <Game.Objects.   Object>(),
				        ComponentType.ReadWrite<Game.Objects.   Color>(),
			        },
                    Any = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Game.Buildings.  Building>(),
                        ComponentType.ReadOnly<Game.Routes.     MailBox>(),
                    },
			        None = new ComponentType[]
			        {
                        // Do not exclude hidden buildings because they must be included in the total used and capacity data.
				        //ComponentType.ReadOnly<Hidden>(),

                        ComponentType.ReadOnly<Game.Buildings.  Abandoned>(),   // Exclude abandoned buildings. 
                        ComponentType.ReadOnly<Game.Buildings.  Condemned>(),   // Exclude condemned buildings.
				        ComponentType.ReadOnly<Game.Common.     Deleted>(),     // Exclude deleted   buildings.
                        ComponentType.ReadOnly<Game.Common.     Destroyed>(),   // Exclude destroyed buildings.
				        ComponentType.ReadOnly<Game.Common.     Owner>(),       // Exclude subbuildings (see middle buildings query below).
                        ComponentType.ReadOnly<Game.Objects.    Attachment>(),  // Exclude attachments (see attachments query below).
				        ComponentType.ReadOnly<Game.Tools.      Temp>(),        // Exclude temp (see temp objects query below).
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
                        ComponentType.ReadOnly<Game.Buildings.  Building>(),
                        ComponentType.ReadOnly<Game.Common.     Owner>(),
                        ComponentType.ReadWrite<Game.Objects.   Color>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Game.Objects.    Attachment>(),  // Exclude attachments (see attachment buildings query below).
                        ComponentType.ReadOnly<Game.Tools.      Hidden>(),
                        ComponentType.ReadOnly<Game.Common.     Deleted>(),
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
                        ComponentType.ReadOnly <Game.Buildings. Building>(),
                        ComponentType.ReadOnly <Game.Objects.   Attachment>(),
                        ComponentType.ReadWrite<Game.Objects.   Color>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Game.Common.     Owner>(),       // Exclude middle buildings (see middle buildings query above).
                        ComponentType.ReadOnly<Game.Tools.      Hidden>(),
                        ComponentType.ReadOnly<Game.Common.     Deleted>(),
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
                        ComponentType.ReadOnly <Game.Objects.   Object>(),
                        ComponentType.ReadWrite<Game.Objects.   Color>(),
                        ComponentType.ReadOnly <Game.Tools.     Temp>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Game.Tools.      Hidden>(),
                        ComponentType.ReadOnly<Game.Common.     Deleted>(),
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
                        ComponentType.ReadOnly <Game.Objects.   Object>(),
                        ComponentType.ReadOnly <Game.Common.    Owner>(),
                        ComponentType.ReadWrite<Game.Objects.   Color>(),
                    },
                    None = new ComponentType[]
                    {
                        // Exclude all same things as base game logic.
                        ComponentType.ReadOnly<Game.Tools.      Hidden>(),
                        ComponentType.ReadOnly<Game.Common.     Deleted>(),
                        ComponentType.ReadOnly<Game.Vehicles.   Vehicle>(),
                        ComponentType.ReadOnly<Game.Creatures.  Creature>(),
                        ComponentType.ReadOnly<Game.Buildings.  Building>(),
                        ComponentType.ReadOnly<Game.Objects.    UtilityObject>(),
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
                        ComponentType.ReadOnly<Game.Prefabs.    InfoviewBuildingStatusData>(),
                        ComponentType.ReadOnly<Game.Prefabs.    InfomodeActive>(),
                    }
                }
            );

            // Use Harmony to patch ObjectColorSystem.OnUpdate with BuildingColorSystem.OnUpdatePrefix.
            // When one of this mod's infoviews is displayed, it is not necessary to execute ObjectColorSystem.OnUpdate.
            // By using a Harmony prefix, this system can prevent the execution of ObjectColorSystem.OnUpdate.
            // Note that ObjectColorSystem.OnUpdate can be patched, but the jobs in ObjectColorSystem cannot be patched because they are burst compiled.
            MethodInfo originalMethod = typeof(Game.Rendering.ObjectColorSystem).GetMethod("OnUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            if (originalMethod == null)
            {
                LogUtil.Error($"Unable to find original method {nameof(Game.Rendering.ObjectColorSystem)}.OnUpdate.");
                return;
            }
            MethodInfo prefixMethod = typeof(BuildingColorSystem).GetMethod(nameof(OnUpdatePrefix), BindingFlags.Static | BindingFlags.NonPublic);
            if (prefixMethod == null)
            {
                LogUtil.Error($"Unable to find patch prefix method {nameof(BuildingColorSystem)}.{nameof(OnUpdatePrefix)}.");
                return;
            }
            new Harmony(HarmonyID).Patch(originalMethod, new HarmonyMethod(prefixMethod), null);
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
            // Run jobs to set building colors and get used and capacity data.


            // Create a job to update default colors.
            _componentTypeHandleColor.Update(ref CheckedStateRef);
            UpdateColorsJobDefault updateColorsJobDefault = new UpdateColorsJobDefault()
            {
                ComponentTypeHandleColor = _componentTypeHandleColor,
            };


            // Define a job to get active building status types.
            NativeList<ArchetypeChunk> activeBuildingStatusDataChunks =
                _queryActiveBuildingStatusData.ToArchetypeChunkListAsync(Allocator.TempJob, out JobHandle activeBuildingStatusDataJobHandle);

            // Create array for used and capacity data, one entry for each possible parallel job thread.
            NativeArray<NativeList<SubtotalUsedCapacity>> totalUsedCapacity =
                new NativeArray<NativeList<SubtotalUsedCapacity>>(JobsUtility.ThreadIndexCount, Allocator.TempJob);
            for (int i = 0; i < totalUsedCapacity.Length; i++)
            {
                // Each thread array entry is a list to hold used and capacity subtotals.
                // When the list capacity needs to be expanded because a new entry is added, a new larger block of memory is allocated,
                // old list is copied there, then old list memory is released.  Doing all this every frame would reduce performance.
                // Initial list capacity is set to the max number of thread entries from the previous frame.
                // This is to try to minimize the number of times the list capacity needs to be expanded when adding new entries
                // while at the same time not using much more memory than is needed for the list.
                // It appears from empirical testing that:
                //      The actual initial list capacity is the power of 2 that is at least as large as the initial capacity.
                //      If the list capacity needs to be increased to add another entry, the list capacity is doubled.
                //      Therefore, list capacity is always a power of 2.
                totalUsedCapacity[i] = new NativeList<SubtotalUsedCapacity>(_previousMaxThreadEntries, Allocator.TempJob);
            }

            // Update buffers and components for main building colors job.
            // All of these are updated even though not all of them will be used for the active infoview.
            _componentTypeHandleColor                       .Update(ref CheckedStateRef);

            _bufferLookupEfficiency                         .Update(ref CheckedStateRef);
            _bufferLookupEmployee                           .Update(ref CheckedStateRef);
            _bufferLookupHouseholdCitizen                   .Update(ref CheckedStateRef);
            _bufferLookupInstalledUpgrade                   .Update(ref CheckedStateRef);
            _bufferLookupLaneObject                         .Update(ref CheckedStateRef);
            _bufferLookupOccupant                           .Update(ref CheckedStateRef);
            _bufferLookupOwnedVehicle                       .Update(ref CheckedStateRef);
            _bufferLookupPatient                            .Update(ref CheckedStateRef);
            _bufferLookupRenter                             .Update(ref CheckedStateRef);
            _bufferLookupResources                          .Update(ref CheckedStateRef);
            _bufferLookupStudent                            .Update(ref CheckedStateRef);
            _bufferLookupSubArea                            .Update(ref CheckedStateRef);
            _bufferLookupSubLane                            .Update(ref CheckedStateRef);
            _bufferLookupSubNet                             .Update(ref CheckedStateRef);
            _bufferLookupSubObject                          .Update(ref CheckedStateRef);

            _componentLookupAmbulance                       .Update(ref CheckedStateRef);
            _componentLookupBatteryData                     .Update(ref CheckedStateRef);
            _componentLookupBuildingData                    .Update(ref CheckedStateRef);
            _componentLookupBuildingPropertyData            .Update(ref CheckedStateRef);
            _componentLookupCitizen                         .Update(ref CheckedStateRef);
            _componentLookupCompanyData                     .Update(ref CheckedStateRef);
            _componentLookupCurve                           .Update(ref CheckedStateRef);
            _componentLookupDeathcareFacilityData           .Update(ref CheckedStateRef);
            _componentLookupDeliveryTruck                   .Update(ref CheckedStateRef);
            _componentLookupEmergencyShelterData            .Update(ref CheckedStateRef);
            _componentLookupEvacuatingTransport             .Update(ref CheckedStateRef);
            _componentLookupFireEngine                      .Update(ref CheckedStateRef);
            _componentLookupFireStationData                 .Update(ref CheckedStateRef);
            _componentLookupGarageLane                      .Update(ref CheckedStateRef);
            _componentLookupGarbageFacilityData             .Update(ref CheckedStateRef);
            _componentLookupGarbageTruck                    .Update(ref CheckedStateRef);
            _componentLookupGeometry                        .Update(ref CheckedStateRef);
            _componentLookupHospitalData                    .Update(ref CheckedStateRef);
            _componentLookupHealthProblem                   .Update(ref CheckedStateRef);
            _componentLookupHearse                          .Update(ref CheckedStateRef);
            _componentLookupHelicopter                      .Update(ref CheckedStateRef);
            _componentLookupMailBoxData                     .Update(ref CheckedStateRef);
            _componentLookupMaintenanceDepotData            .Update(ref CheckedStateRef);
            _componentLookupMaintenanceVehicle              .Update(ref CheckedStateRef);
            _componentLookupParkedCar                       .Update(ref CheckedStateRef);
            _componentLookupParkedTrain                     .Update(ref CheckedStateRef);
            _componentLookupParkingLane                     .Update(ref CheckedStateRef);
            _componentLookupParkingLaneData                 .Update(ref CheckedStateRef);
            _componentLookupPoliceCar                       .Update(ref CheckedStateRef);
            _componentLookupPoliceStationData               .Update(ref CheckedStateRef);
            _componentLookupPostFacilityData                .Update(ref CheckedStateRef);
            _componentLookupPostVan                         .Update(ref CheckedStateRef);
            _componentLookupPrefabRef                       .Update(ref CheckedStateRef);
            _componentLookupPrisonData                      .Update(ref CheckedStateRef);
            _componentLookupPrisonerTransport               .Update(ref CheckedStateRef);
            _componentLookupPropertyRenter                  .Update(ref CheckedStateRef);
            _componentLookupPublicTransportVehicleData      .Update(ref CheckedStateRef);
            _componentLookupSchoolData                      .Update(ref CheckedStateRef);
            _componentLookupSpawnableBuildingData           .Update(ref CheckedStateRef);
            _componentLookupStorage                         .Update(ref CheckedStateRef);
            _componentLookupStorageAreaData                 .Update(ref CheckedStateRef);
            _componentLookupStorageLimitData                .Update(ref CheckedStateRef);
            _componentLookupTaxi                            .Update(ref CheckedStateRef);
            _componentLookupTransportCompanyData            .Update(ref CheckedStateRef);
            _componentLookupTransportDepotData              .Update(ref CheckedStateRef);
            _componentLookupWorkplaceData                   .Update(ref CheckedStateRef);
            _componentLookupWorkProvider                    .Update(ref CheckedStateRef);

            _componentTypeHandleAdminBuilding               .Update(ref CheckedStateRef);
            _componentTypeHandleBattery                     .Update(ref CheckedStateRef);
            _componentTypeHandleCommercialProperty          .Update(ref CheckedStateRef);
            _componentTypeHandleDeathcareFacility           .Update(ref CheckedStateRef);
            _componentTypeHandleDisasterFacility            .Update(ref CheckedStateRef);
            _componentTypeHandleElectricityProducer         .Update(ref CheckedStateRef);
            _componentTypeHandleEmergencyShelter            .Update(ref CheckedStateRef);
            _componentTypeHandleFireStation                 .Update(ref CheckedStateRef);
            _componentTypeHandleGarbageFacility             .Update(ref CheckedStateRef);
            _componentTypeHandleHospital                    .Update(ref CheckedStateRef);
            _componentTypeHandleIndustrialProperty          .Update(ref CheckedStateRef);
            _componentTypeHandleOfficeProperty              .Update(ref CheckedStateRef);
            _componentTypeHandlePark                        .Update(ref CheckedStateRef);
            _componentTypeHandleParkingFacility             .Update(ref CheckedStateRef);
            _componentTypeHandleParkMaintenance             .Update(ref CheckedStateRef);
            _componentTypeHandlePoliceStation               .Update(ref CheckedStateRef);
            _componentTypeHandlePostFacility                .Update(ref CheckedStateRef);
            _componentTypeHandlePrison                      .Update(ref CheckedStateRef);
            _componentTypeHandleResearchFacility            .Update(ref CheckedStateRef);
            _componentTypeHandleResidentialProperty         .Update(ref CheckedStateRef);
            _componentTypeHandleRoadMaintenance             .Update(ref CheckedStateRef);
            _componentTypeHandleSchool                      .Update(ref CheckedStateRef);
            _componentTypeHandleSewageOutlet                .Update(ref CheckedStateRef);
            _componentTypeHandleTelecomFacility             .Update(ref CheckedStateRef);
            _componentTypeHandleTransformer                 .Update(ref CheckedStateRef);
            _componentTypeHandleTransportDepot              .Update(ref CheckedStateRef);
            _componentTypeHandleTransportStation            .Update(ref CheckedStateRef);
            _componentTypeHandleWaterPumpingStation         .Update(ref CheckedStateRef);  
            _componentTypeHandleWelfareOffice               .Update(ref CheckedStateRef);  

            _componentTypeHandleCurrentDistrict             .Update(ref CheckedStateRef);
            _componentTypeHandleDestroyed                   .Update(ref CheckedStateRef);
            _componentTypeHandleInfomodeActive              .Update(ref CheckedStateRef);
            _componentTypeHandleInfoviewBuildingStatusData  .Update(ref CheckedStateRef);
            _componentTypeHandleMailBox                     .Update(ref CheckedStateRef);
            _componentTypeHandlePrefabRef                   .Update(ref CheckedStateRef);
            _componentTypeHandleTransportCompany            .Update(ref CheckedStateRef);
            _componentTypeHandleUnderConstruction           .Update(ref CheckedStateRef);

            _entityTypeHandle                               .Update(ref CheckedStateRef);

            // Create a job to update main building colors.
            // All of the buffers and components are set even though not all of them will be used for the active infoview.
            UpdateColorsJobMainBuilding updateColorsJobMainBuilding = new UpdateColorsJobMainBuilding()
            {
                ComponentTypeHandleColor                        = _componentTypeHandleColor,

                BufferLookupEfficiency                          = _bufferLookupEfficiency,
                BufferLookupEmployee                            = _bufferLookupEmployee,
                BufferLookupHouseholdCitizen                    = _bufferLookupHouseholdCitizen,
                BufferLookupInstalledUpgrade                    = _bufferLookupInstalledUpgrade,
                BufferLookupLaneObject                          = _bufferLookupLaneObject,
                BufferLookupOccupant                            = _bufferLookupOccupant,
                BufferLookupOwnedVehicle                        = _bufferLookupOwnedVehicle,
                BufferLookupPatient                             = _bufferLookupPatient,
                BufferLookupRenter                              = _bufferLookupRenter,
                BufferLookupResources                           = _bufferLookupResources,
                BufferLookupStudent                             = _bufferLookupStudent,
                BufferLookupSubArea                             = _bufferLookupSubArea,
                BufferLookupSubLane                             = _bufferLookupSubLane,
                BufferLookupSubNet                              = _bufferLookupSubNet,
                BufferLookupSubObject                           = _bufferLookupSubObject,
                
                ComponentLookupAmbulance                        = _componentLookupAmbulance,
                ComponentLookupBatteryData                      = _componentLookupBatteryData,
                ComponentLookupBuildingData                     = _componentLookupBuildingData,
                ComponentLookupBuildingPropertyData             = _componentLookupBuildingPropertyData,
                ComponentLookupCitizen                          = _componentLookupCitizen,
                ComponentLookupCompanyData                      = _componentLookupCompanyData,
                ComponentLookupCurve                            = _componentLookupCurve,
                ComponentLookupDeathcareFacilityData            = _componentLookupDeathcareFacilityData,
                ComponentLookupDeliveryTruck                    = _componentLookupDeliveryTruck,
                ComponentLookupEmergencyShelterData             = _componentLookupEmergencyShelterData,
                ComponentLookupEvacuatingTransport              = _componentLookupEvacuatingTransport,
                ComponentLookupFireEngine                       = _componentLookupFireEngine,
                ComponentLookupFireStationData                  = _componentLookupFireStationData,
                ComponentLookupGarageLane                       = _componentLookupGarageLane,
                ComponentLookupGarbageFacilityData              = _componentLookupGarbageFacilityData,
                ComponentLookupGarbageTruck                     = _componentLookupGarbageTruck,
                ComponentLookupGeometry                         = _componentLookupGeometry,
                ComponentLookupHealthProblem                    = _componentLookupHealthProblem,
                ComponentLookupHearse                           = _componentLookupHearse,
                ComponentLookupHelicopter                       = _componentLookupHelicopter,
                ComponentLookupHospitalData                     = _componentLookupHospitalData,
                ComponentLookupMailBoxData                      = _componentLookupMailBoxData,
                ComponentLookupMaintenanceDepotData             = _componentLookupMaintenanceDepotData,
                ComponentLookupMaintenanceVehicle               = _componentLookupMaintenanceVehicle,
                ComponentLookupParkedCar                        = _componentLookupParkedCar,
                ComponentLookupParkedTrain                      = _componentLookupParkedTrain,
                ComponentLookupParkingLane                      = _componentLookupParkingLane,
                ComponentLookupParkingLaneData                  = _componentLookupParkingLaneData,
                ComponentLookupPoliceCar                        = _componentLookupPoliceCar,
                ComponentLookupPoliceStationData                = _componentLookupPoliceStationData,
                ComponentLookupPostFacilityData                 = _componentLookupPostFacilityData,
                ComponentLookupPostVan                          = _componentLookupPostVan,
                ComponentLookupPrefabRef                        = _componentLookupPrefabRef,
                ComponentLookupPrisonData                       = _componentLookupPrisonData,
                ComponentLookupPrisonerTransport                = _componentLookupPrisonerTransport,
                ComponentLookupPropertyRenter                   = _componentLookupPropertyRenter,
                ComponentLookupSchoolData                       = _componentLookupSchoolData,
                ComponentLookupPublicTransportVehicleData       = _componentLookupPublicTransportVehicleData,
                ComponentLookupSpawnableBuildingData            = _componentLookupSpawnableBuildingData,
                ComponentLookupStorage                          = _componentLookupStorage,
                ComponentLookupStorageAreaData                  = _componentLookupStorageAreaData,
                ComponentLookupStorageLimitData                 = _componentLookupStorageLimitData,
                ComponentLookupTaxi                             = _componentLookupTaxi,
                ComponentLookupTransportCompanyData             = _componentLookupTransportCompanyData,
                ComponentLookupTransportDepotData               = _componentLookupTransportDepotData,
                ComponentLookupWorkplaceData                    = _componentLookupWorkplaceData,
                ComponentLookupWorkProvider                     = _componentLookupWorkProvider,
                
                ComponentTypeHandleAdminBuilding                = _componentTypeHandleAdminBuilding,
                ComponentTypeHandleBattery                      = _componentTypeHandleBattery,
                ComponentTypeHandleCommercialProperty           = _componentTypeHandleCommercialProperty,
                ComponentTypeHandleDeathcareFacility            = _componentTypeHandleDeathcareFacility,
                ComponentTypeHandleDisasterFacility             = _componentTypeHandleDisasterFacility,
                ComponentTypeHandleElectricityProducer          = _componentTypeHandleElectricityProducer,
                ComponentTypeHandleEmergencyShelter             = _componentTypeHandleEmergencyShelter,
                ComponentTypeHandleFireStation                  = _componentTypeHandleFireStation,
                ComponentTypeHandleGarbageFacility              = _componentTypeHandleGarbageFacility,
                ComponentTypeHandleHospital                     = _componentTypeHandleHospital,
                ComponentTypeHandleIndustrialProperty           = _componentTypeHandleIndustrialProperty,
                ComponentTypeHandleOfficeProperty               = _componentTypeHandleOfficeProperty,
                ComponentTypeHandlePark                         = _componentTypeHandlePark,
                ComponentTypeHandleParkingFacility              = _componentTypeHandleParkingFacility,
                ComponentTypeHandleParkMaintenance              = _componentTypeHandleParkMaintenance,
                ComponentTypeHandlePoliceStation                = _componentTypeHandlePoliceStation,
                ComponentTypeHandlePostFacility                 = _componentTypeHandlePostFacility,
                ComponentTypeHandlePrison                       = _componentTypeHandlePrison,
                ComponentTypeHandleResearchFacility             = _componentTypeHandleResearchFacility,
                ComponentTypeHandleResidentialProperty          = _componentTypeHandleResidentialProperty,
                ComponentTypeHandleRoadMaintenance              = _componentTypeHandleRoadMaintenance,
                ComponentTypeHandleSchool                       = _componentTypeHandleSchool,
                ComponentTypeHandleSewageOutlet                 = _componentTypeHandleSewageOutlet,
                ComponentTypeHandleTelecomFacility              = _componentTypeHandleTelecomFacility,
                ComponentTypeHandleTransformer                  = _componentTypeHandleTransformer,
                ComponentTypeHandleTransportDepot               = _componentTypeHandleTransportDepot,
                ComponentTypeHandleTransportStation             = _componentTypeHandleTransportStation,
                ComponentTypeHandleWaterPumpingStation          = _componentTypeHandleWaterPumpingStation,
                ComponentTypeHandleWelfareOffice                = _componentTypeHandleWelfareOffice,

                ComponentTypeHandleCurrentDistrict              = _componentTypeHandleCurrentDistrict,
                ComponentTypeHandleDestroyed                    = _componentTypeHandleDestroyed,
                ComponentTypeHandleInfomodeActive               = _componentTypeHandleInfomodeActive,
                ComponentTypeHandleInfoviewBuildingStatusData   = _componentTypeHandleInfoviewBuildingStatusData,
                ComponentTypeHandleMailBox                      = _componentTypeHandleMailBox,
                ComponentTypeHandlePrefabRef                    = _componentTypeHandlePrefabRef,
                ComponentTypeHandleTransportCompany             = _componentTypeHandleTransportCompany,
                ComponentTypeHandleUnderConstruction            = _componentTypeHandleUnderConstruction,
                
                EntityTypeHandle                                = _entityTypeHandle,
                
                ActiveInfoview                                  = activeInfoview,
                ActiveBuildingStatusDataChunks                  = activeBuildingStatusDataChunks,
                
                TotalUsedCapacity                               = totalUsedCapacity,
                
                CountVehiclesInUse                              = Mod.ModSettings.CountVehiclesInUse,
                CountVehiclesInMaintenance                      = Mod.ModSettings.CountVehiclesInMaintenance,
                EfficiencyMaxColor200Percent                    = Mod.ModSettings.EfficiencyMaxColor200Percent,

                SelectedDistrict                                = _buildingUseUISystem.selectedDistrict,
                SelectedDistrictIsEntireCity                    = _buildingUseUISystem.selectedDistrict == BuildingUseUISystem.EntireCity
            };


            // Create a job to update middle building colors.
            _componentLookupColor       .Update(ref CheckedStateRef);
            _componentTypeHandleOwner   .Update(ref CheckedStateRef);
            _entityTypeHandle           .Update(ref CheckedStateRef);
            UpdateColorsJobMiddleBuilding updateColorsJobMiddleBuilding = new UpdateColorsJobMiddleBuilding()
            {
                ComponentLookupColor        = _componentLookupColor,
                ComponentTypeHandleOwner    = _componentTypeHandleOwner,
                EntityTypeHandle            = _entityTypeHandle,
            };


            // Create a job to update attachment building colors.
            _componentLookupColor           .Update(ref CheckedStateRef);
            _componentTypeHandleAttachment  .Update(ref CheckedStateRef);
            _entityTypeHandle               .Update(ref CheckedStateRef);
            UpdateColorsJobAttachmentBuilding updateColorsJobAttachmentBuilding = new UpdateColorsJobAttachmentBuilding()
            {
                ComponentLookupColor            = _componentLookupColor,
                ComponentTypeHandleAttachment   = _componentTypeHandleAttachment,
                EntityTypeHandle                = _entityTypeHandle,
            };


            // Create a job to update temp object colors.
            _componentLookupColor       .Update(ref CheckedStateRef);
            _componentTypeHandleTemp    .Update(ref CheckedStateRef);
            _entityTypeHandle           .Update(ref CheckedStateRef);
            UpdateColorsJobTempObject updateColorsJobTempObject = new UpdateColorsJobTempObject()
            {
                ComponentLookupColor    = _componentLookupColor,
                ComponentTypeHandleTemp = _componentTypeHandleTemp,
                EntityTypeHandle        = _entityTypeHandle,
            };

            
            // Create a job to update sub object colors.
            _componentLookupColor           .Update(ref CheckedStateRef);
            _componentLookupBuilding        .Update(ref CheckedStateRef);
            _componentLookupElevation       .Update(ref CheckedStateRef);
            _componentLookupOwner           .Update(ref CheckedStateRef);
            _componentLookupVehicle         .Update(ref CheckedStateRef);
            _componentTypeHandleElevation   .Update(ref CheckedStateRef);
            _componentTypeHandleOwner       .Update(ref CheckedStateRef);
            _componentTypeHandleTree        .Update(ref CheckedStateRef);
            _entityTypeHandle               .Update(ref CheckedStateRef);
            UpdateColorsJobSubObject updateColorsJobSubObject = new UpdateColorsJobSubObject()
            {
                ComponentLookupColor            = _componentLookupColor,
                ComponentLookupBuilding         = _componentLookupBuilding,
                ComponentLookupElevation        = _componentLookupElevation,
                ComponentLookupOwner            = _componentLookupOwner,
                ComponentLookupVehicle          = _componentLookupVehicle,
                ComponentTypeHandleElevation    = _componentTypeHandleElevation,
                ComponentTypeHandleOwner        = _componentTypeHandleOwner,
                ComponentTypeHandleTree         = _componentTypeHandleTree,
                EntityTypeHandle                = _entityTypeHandle,
            };


            // Schedule the jobs with dependencies so the jobs run in order.
            // Schedule each job to execute in parallel (i.e. job uses multiple threads, if available).
            // Parallel threads execute much faster than a single thread.
            JobHandle jobHandleDefault            = JobChunkExtensions.ScheduleParallel(updateColorsJobDefault,            _queryDefault,            base.Dependency);
            JobHandle jobHandleMainBuilding       = JobChunkExtensions.ScheduleParallel(updateColorsJobMainBuilding,       _queryMainBuilding,       JobHandle.CombineDependencies(jobHandleDefault, activeBuildingStatusDataJobHandle));
            JobHandle jobHandleMiddleBuilding     = JobChunkExtensions.ScheduleParallel(updateColorsJobMiddleBuilding,     _queryMiddleBuilding,     jobHandleMainBuilding);
            JobHandle jobHandleNext = jobHandleMiddleBuilding;
            if (Mod.ModSettings.ColorSpecializedIndustryLots)
            {
                jobHandleNext                     = JobChunkExtensions.ScheduleParallel(updateColorsJobAttachmentBuilding, _queryAttachmentBuilding, jobHandleMiddleBuilding);
            }
            JobHandle jobHandleTempObject         = JobChunkExtensions.ScheduleParallel(updateColorsJobTempObject,         _queryTempObject,         jobHandleNext);
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
            _previousMaxThreadEntries = 0;
            double[] totalUsed     = new double[buildingStatusTypeDatas.Count];
            double[] totalCapacity = new double[buildingStatusTypeDatas.Count];
            for (int i = 0; i < totalUsedCapacity.Length; i++)
            {
                // Get the thread entry, which is a list of subtotals.
                NativeList<SubtotalUsedCapacity> subtotalList = totalUsedCapacity[i];

                // Set new maximum thread entries.
                int subtotalListLength = subtotalList.Length;
                if (subtotalListLength > _previousMaxThreadEntries)
                {
                    _previousMaxThreadEntries = subtotalListLength;
                }

                // Do each used and capacity subtotal entry in the subtotal list.
                for (int j = 0; j < subtotalListLength; j++)
                {
                    // Add used and capacity from this entry to totals.
                    // Index into the total arrays is the building status type minus the first building status type.
                    // This assumes the building status types are in sequential numerical order in the enum, which they should always be.
                    SubtotalUsedCapacity subtotalUsedCapacity = subtotalList[j];
                    int totalIndex = subtotalUsedCapacity.buildingStatusType - buildingStatusTypeFirst;
                    totalUsed    [totalIndex] += subtotalUsedCapacity.used;
                    totalCapacity[totalIndex] += subtotalUsedCapacity.capacity;
                }

                // This subtotal list is no longer needed.
                subtotalList.Dispose();
            }

            // The total array is no longer needed.
            totalUsedCapacity.Dispose();

            // Update building status type data values.
            buildingStatusTypeDatas.UpdateDataValues(totalUsed, totalCapacity);

            // This system handled building colors for one of this mod's infoviews.
            // Do not execute the original game logic.
            return false;
        }
    }
}
