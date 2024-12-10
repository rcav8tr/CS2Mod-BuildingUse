using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace BuildingUse
{
    /// <summary>
    /// System to send building data to UI.
    /// </summary>
    public partial class BuildingUseUISystem : InfoviewUISystemBase
    {
        // Variables to track when updates should occur.
        private BUInfoview _previousActiveInfoview;
        private long _previousBindingUpdateTicks;

        // Variables to track when waiting for data values to be updated.
        private bool __waitDataValuesUpdated;   // Do not access this field directly.  This is the backing field for private property _waitDataValuesUpdated.
        private bool _waitDataValuesUpdated
        {
            get { lock (_waitDataValuesUpdatedLock) { return __waitDataValuesUpdated;  } }
            set { lock (_waitDataValuesUpdatedLock) { __waitDataValuesUpdated = value; } }
        }
        private readonly object _waitDataValuesUpdatedLock = new object();

        // Other systems.
        private NameSystem _nameSystem;
        private ToolSystem _toolSystem;

        // C# to UI binding names.
        public const string BindingNameCountVehiclesInUse           = "CountVehiclesInUse";
        public const string BindingNameCountVehiclesInMaintenance   = "CountVehiclesInMaintenance";
        public const string BindingNameEfficiencyMaxColor200Percent = "EfficiencyMaxColor200Percent";
        public const string BindingNameSelectedDistrict             = "SelectedDistrict";
        public const string BindingNameDistrictInfos                = "DistrictInfos";

        // C# to UI bindings.
        private ValueBinding<bool>  _bindingCountVehiclesInUse;
        private ValueBinding<bool>  _bindingCountVehiclesInMaintenance;
        private ValueBinding<bool>  _bindingEfficiencyMaxColor200Percent;
        private ValueBinding<Entity>_bindingSelectedDistrict;
        private RawValueBinding     _bindingDistrictInfos;

        // UI to C# event names.
        public const string EventNameCountVehiclesInUseClicked          = "CountVehiclesInUseClicked";
        public const string EventNameCountVehiclesInMaintenanceClicked  = "CountVehiclesInMaintenanceClicked";
        public const string EventNameEfficiencyMaxColorClicked          = "EfficiencyMaxColorClicked";
        public const string EventNameSelectedDistrictChanged            = "SelectedDistrictChanged";

        // Districts.
        private EntityQuery _districtQuery;
        private DistrictInfos _districtInfos = new DistrictInfos();
        public static Entity EntireCity { get; } = Entity.Null;
        public Entity selectedDistrict { get; set; } = EntireCity;

        /// <summary>
        /// Do one-time initialization of the system.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();
            LogUtil.Info($"{nameof(BuildingUseUISystem)}.{nameof(OnCreate)}");
            
            try
            {
                // Get other systems.
                _nameSystem = base.World.GetOrCreateSystemManaged<NameSystem>();
                _toolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();

                // Add data bindings for used and capacity values.
                foreach (BUInfoviewData infoviewData in BUInfoviewDatas.instance.Values)
                {
                    foreach (BUBuildingStatusTypeData buildingStatusTypeData in infoviewData.buildingStatusTypeDatas.Values)
                    {
                        // Skip special cases.
                        if (!buildingStatusTypeData.isSpecialCase)
                        {
                            buildingStatusTypeData.GetDataBindings(out ValueBinding<double> bindingUsed, out ValueBinding<double> bindingCapacity);
                            AddBinding(bindingUsed);
                            AddBinding(bindingCapacity);
                        }
                    }
                }

                // Add bindings for C# to UI.
                AddBinding(_bindingCountVehiclesInUse           = new ValueBinding<bool  >(ModAssemblyInfo.Name, BindingNameCountVehiclesInUse,           Mod.ModSettings.CountVehiclesInUse          ));
                AddBinding(_bindingCountVehiclesInMaintenance   = new ValueBinding<bool  >(ModAssemblyInfo.Name, BindingNameCountVehiclesInMaintenance,   Mod.ModSettings.CountVehiclesInMaintenance  ));
                AddBinding(_bindingEfficiencyMaxColor200Percent = new ValueBinding<bool  >(ModAssemblyInfo.Name, BindingNameEfficiencyMaxColor200Percent, Mod.ModSettings.EfficiencyMaxColor200Percent));
                AddBinding(_bindingSelectedDistrict             = new ValueBinding<Entity>(ModAssemblyInfo.Name, BindingNameSelectedDistrict,             EntireCity                                  ));
                AddBinding(_bindingDistrictInfos                = new RawValueBinding     (ModAssemblyInfo.Name, BindingNameDistrictInfos,                UpdateDistrictInfos                         ));

                // Add bindings for UI to C#.
                AddBinding(new TriggerBinding        (ModAssemblyInfo.Name, EventNameCountVehiclesInUseClicked,         CountVehiclesInUseClicked        ));
                AddBinding(new TriggerBinding        (ModAssemblyInfo.Name, EventNameCountVehiclesInMaintenanceClicked, CountVehiclesInMaintenanceClicked));
                AddBinding(new TriggerBinding        (ModAssemblyInfo.Name, EventNameEfficiencyMaxColorClicked,         EfficiencyMaxColorClicked        ));
                AddBinding(new TriggerBinding<Entity>(ModAssemblyInfo.Name, EventNameSelectedDistrictChanged,           SelectedDistrictChanged          ));

                // Define entity query to get districts.
                _districtQuery = GetEntityQuery(ComponentType.ReadOnly<District>());
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex);
            }
        }
        
        /// <summary>
        /// Write district infos to the UI.
        /// </summary>
        private void UpdateDistrictInfos(IJsonWriter writer)
        {
            _districtInfos.Write(writer);
        }

        /// <summary>
        /// Check for any change in districts.
        /// </summary>
        private void CheckForDistrictChange()
        {
            // Get district infos and find selected district.
            bool foundSelectedDistrict = (selectedDistrict == EntireCity);
            DistrictInfos districtInfos = new DistrictInfos();
            NativeArray<Entity> districtEntities = _districtQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity districtEntity in districtEntities)
            {
                // Skip the special district that the game creates while a new district is being drawn.
                string districtName = _nameSystem.GetRenderedLabelName(districtEntity);
                if (districtName != "Assets.DISTRICT_NAME")
                {
                    // Add a district info for this district.
                    districtInfos.Add(new DistrictInfo(districtEntity, districtName));

                    // Check if this is the selected district.
                    if (districtEntity == selectedDistrict)
                    {
                        foundSelectedDistrict = true;
                    }
                }
            }

            // Check if selected district was not found.
            if (!foundSelectedDistrict)
            {
                // Selected district was not found, most likely because the selected district was deleted.
                // Change selected district to entire city.
                SelectedDistrictChanged(EntireCity);
            }

            // Sort district infos by name.
            districtInfos.Sort();

            // First district info is always for entire city.
            districtInfos.Insert(0, new DistrictInfo(EntireCity, Translation.Get(UITranslationKey.EntireCity)));

            // Check if district infos have changed.
            bool districtsChanged = false;
            if (districtInfos.Count != _districtInfos.Count)
            {
                districtsChanged = true;
            }
            else
            {
                // Compare each district info.
                for (int i = 0; i < districtInfos.Count; i++)
                {
                    if (districtInfos[i].entity != _districtInfos[i].entity || districtInfos[i].name != _districtInfos[i].name)
                    {
                        districtsChanged = true;
                        break;
                    }
                }
            }
            
            // Check if a district info change was found.
            if (districtsChanged)
            {
                // Write district infos to the UI.
                _districtInfos = districtInfos;
                _bindingDistrictInfos.Update();
            }
        }

        /// <summary>
        /// Event callback for count vehicles in use clicked.
        /// </summary>
        private void CountVehiclesInUseClicked()
        {
            // Toggle the settings value.
            Mod.ModSettings.CountVehiclesInUse = !Mod.ModSettings.CountVehiclesInUse;

            // If both are now unchecked, then set the other.
            if (!Mod.ModSettings.CountVehiclesInUse && !Mod.ModSettings.CountVehiclesInMaintenance)
            {
                Mod.ModSettings.CountVehiclesInMaintenance = true;
            }

            // Update and save.
            UpdateCountVehicles();
        }
        
        /// <summary>
        /// Event callback for count vehicles in maintenance clicked.
        /// </summary>
        private void CountVehiclesInMaintenanceClicked()
        {
            // Toggle the settings value.
            Mod.ModSettings.CountVehiclesInMaintenance = !Mod.ModSettings.CountVehiclesInMaintenance;

            // If both are now unchecked, then set the other.
            if (!Mod.ModSettings.CountVehiclesInUse && !Mod.ModSettings.CountVehiclesInMaintenance)
            {
                Mod.ModSettings.CountVehiclesInUse = true;
            }

            // Update and save.
            UpdateCountVehicles();
        }

        /// <summary>
        /// Update count vehicles in UI.
        /// </summary>
        private void UpdateCountVehicles()
        {
            // Update count vehicle bindings to UI.
            _bindingCountVehiclesInUse        .Update(Mod.ModSettings.CountVehiclesInUse);
            _bindingCountVehiclesInMaintenance.Update(Mod.ModSettings.CountVehiclesInMaintenance);

            // Update and save.
            UpdateDataValuesImmediate();
            Mod.ModSettings.ApplyAndSave();
        }
        
        /// <summary>
        /// Event callback for efficiency max color clicked.
        /// </summary>
        private void EfficiencyMaxColorClicked()
        {
            // Toggle the settings value.
            Mod.ModSettings.EfficiencyMaxColor200Percent = !Mod.ModSettings.EfficiencyMaxColor200Percent;

            // Update and save.
            _bindingEfficiencyMaxColor200Percent.Update(Mod.ModSettings.EfficiencyMaxColor200Percent);
            UpdateDataValuesImmediate();
            Mod.ModSettings.ApplyAndSave();
        }
        
        /// <summary>
        /// Event callback for selected district changed.
        /// </summary>
        private void SelectedDistrictChanged(Entity newDistrict)
        {
            // Save selected district.
            selectedDistrict = newDistrict;

            // Immediately send the selected district back to the UI.
            _bindingSelectedDistrict.Update(selectedDistrict);

            // Immediately update data values.
            UpdateDataValuesImmediate();
        }

        /// <summary>
        /// Called when a game is done being loaded.
        /// </summary>
        protected override void OnGameLoadingComplete(Purpose purpose, Game.GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            // Initialize only for game mode.
            if (mode == Game.GameMode.Game)
            {
                // Reset all data values to ensure nothing remains from a previous game.
                BUInfoviewDatas.instance.ResetDataValues();

                // Initialize updating variables.
                _previousActiveInfoview = BUInfoview.None;
                _previousBindingUpdateTicks = 0;
                _waitDataValuesUpdated = false;

                // Selected district is entire city.
                SelectedDistrictChanged(EntireCity);

                // Initialize districts.
                _districtInfos.Clear();
                CheckForDistrictChange();
            }
        }

        /// <summary>
        /// Called when the game determines that an update is needed.
        /// </summary>
        [Preserve]
        protected override void PerformUpdate()
        {
            // Nothing to do here, but implementation is required.
            // Updates are performed in OnUpdate.
        }

        /// <summary>
        /// Called every frame, even when at the main menu.
        /// </summary>
        protected override void OnUpdate()
        {
            base.OnUpdate();

            // An infoview must be active.
            if (_toolSystem.activeInfoview == null)
            {
                return;
            }

            // Active infoview must be for this mod.
            BUInfoview activeInfoview = BUInfoviewData.GetInfoview(_toolSystem.activeInfoview.name);
            if (activeInfoview != BUInfoview.None)
            {
                // Active infoview is for this mod.

                // Check for a change in districts.
                // Note that if the districts change while there is no active infoview or the active infoview is not for this mod
                // (e.g. the last selected district is deleted), then it will take a frame for the district infos to be updated.
                // So for one frame, the district dropdown might be blank.  This is acceptable.
                CheckForDistrictChange();

                // For a change in active infoview, update data values immediately.
                // This includes changes from a game infoview to one of this mod's infoviews and between this mod's infoviews.
                if (activeInfoview != _previousActiveInfoview)
                {
                    UpdateDataValuesImmediate();
                }

                // Check if waiting for data values to be updated.
                if (_waitDataValuesUpdated)
                {
                    // Check if data values are updated.
                    if (BUInfoviewDatas.instance[activeInfoview].buildingStatusTypeDatas.DataValuesUpdated)
                    {
                        // No longer waiting for data values to be updated.
                        _waitDataValuesUpdated = false;

                        // Update data bindings for active infoview.
                        BUInfoviewDatas.instance[activeInfoview].buildingStatusTypeDatas.UpdateDataBindings();
                    }
                }
                else
                {
                    // Update bindings once per second.
                    long currentTicks = DateTime.Now.Ticks;
                    if (currentTicks - _previousBindingUpdateTicks >= TimeSpan.TicksPerSecond)
                    {
                        // Save binding update ticks.
                        _previousBindingUpdateTicks = currentTicks;

                        // Update data bindings for active infoview.
                        BUInfoviewDatas.instance[activeInfoview].buildingStatusTypeDatas.UpdateDataBindings();
                    }
                }
            }

            // Save active infoview.
            _previousActiveInfoview = activeInfoview;
        }

        /// <summary>
        /// Update data values immediately.
        /// </summary>
        public void UpdateDataValuesImmediate()
        {
            // An infoview must be active.
            if (_toolSystem.activeInfoview != null)
            {
                // Active infoview must be for this mod.
                BUInfoview activeInfoview = BUInfoviewData.GetInfoview(_toolSystem.activeInfoview.name);
                if (activeInfoview != BUInfoview.None)
                {
                    // Wait for data values to be updated for active infoview.
                    // Should take only 1-2 frames.
                    BUInfoviewDatas.instance[activeInfoview].buildingStatusTypeDatas.DataValuesUpdated = false;
                    _waitDataValuesUpdated = true;
                }
            }
        }
    }
}
