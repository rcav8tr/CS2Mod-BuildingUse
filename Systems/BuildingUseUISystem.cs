using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using System;
using UnityEngine.Scripting;

namespace BuildingUse
{
    /// <summary>
    /// System to send building data to UI.
    /// </summary>
    public partial class BuildingUseUISystem : Game.UI.InGame.InfoviewUISystemBase
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
        private Game.Tools.ToolSystem _toolSystem;

        // C# to UI binding names.
        public const string BindingNameCountVehiclesInUse           = "CountVehiclesInUse";
        public const string BindingNameCountVehiclesInMaintenance   = "CountVehiclesInMaintenance";
        public const string BindingNameEfficiencyMaxColor200Percent = "EfficiencyMaxColor200Percent";

        // C# to UI bindings.
        private ValueBinding<bool> _bindingCountVehiclesInUse;
        private ValueBinding<bool> _bindingCountVehiclesInMaintenance;
        private ValueBinding<bool> _bindingEfficiencyMaxColor200Percent;

        // UI to C# event names.
        public const string EventNameCountVehiclesInUseClicked          = "CountVehiclesInUseClicked";
        public const string EventNameCountVehiclesInMaintenanceClicked  = "CountVehiclesInMaintenanceClicked";
        public const string EventNameEfficiencyMaxColorClicked          = "EfficiencyMaxColorClicked";

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
                _toolSystem = base.World.GetOrCreateSystemManaged<Game.Tools.ToolSystem>();

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
                AddBinding(_bindingCountVehiclesInUse           = new ValueBinding<bool>(ModAssemblyInfo.Name, BindingNameCountVehiclesInUse,           Mod.ModSettings.CountVehiclesInUse          ));
                AddBinding(_bindingCountVehiclesInMaintenance   = new ValueBinding<bool>(ModAssemblyInfo.Name, BindingNameCountVehiclesInMaintenance,   Mod.ModSettings.CountVehiclesInMaintenance  ));
                AddBinding(_bindingEfficiencyMaxColor200Percent = new ValueBinding<bool>(ModAssemblyInfo.Name, BindingNameEfficiencyMaxColor200Percent, Mod.ModSettings.EfficiencyMaxColor200Percent));

                // Add bindings for UI to C#.
                AddBinding(new TriggerBinding(ModAssemblyInfo.Name, EventNameCountVehiclesInUseClicked,         CountVehiclesInUseClicked        ));
                AddBinding(new TriggerBinding(ModAssemblyInfo.Name, EventNameCountVehiclesInMaintenanceClicked, CountVehiclesInMaintenanceClicked));
                AddBinding(new TriggerBinding(ModAssemblyInfo.Name, EventNameEfficiencyMaxColorClicked,         EfficiencyMaxColorClicked        ));
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex);
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
        /// Called when a game is done being loaded.
        /// </summary>
        protected override void OnGameLoadingComplete(Purpose purpose, Game.GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            // Reset all data values to ensure nothing remains from a previous game.
            if (mode == Game.GameMode.Game)
            {
                BUInfoviewDatas.instance.ResetDataValues();
            }

            // Initialize updating variables.
            _previousActiveInfoview = BUInfoview.None;
            _previousBindingUpdateTicks = 0;
            _waitDataValuesUpdated = false;
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
            if (_toolSystem.activeInfoview != null)
            {
                // Active infoview must be for this mod.
                BUInfoview activeInfoview = BUInfoviewData.GetInfoview(_toolSystem.activeInfoview.name);
                if (activeInfoview != BUInfoview.None)
                {
                    // Active infoview is for this mod.

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
