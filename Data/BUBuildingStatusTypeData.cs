using Colossal.UI.Binding;

namespace BuildingUse
{
    /// <summary>
    /// Class to hold data for one of this mod's building status types.
    /// The "BU" (i.e. Building Use) prefix differentiates this class from any game objects.
    /// </summary>
    public class BUBuildingStatusTypeData
    {
        // The building status type this data is for.
        public BUBuildingStatusType BuildingStatusType { get; private set; }
        public string BuildingStatusTypeName { get; private set; }

        // Whether or not this is a special case.
        public bool IsSpecialCase { get; private set; }

        // Data bindings.
        private readonly ValueBinding<double> _bindingUsed;
        private readonly ValueBinding<double> _bindingCapacity;
        private readonly ValueBinding<int   > _bindingCount;

        // Total city-wide data values.
        // City-wide data used and capacity values are double because:
        //      An int max value is exceeded by some city-wide totals.
        //      A long is not supported by ValueBinding.
        //      A float does not have enough precision.
        private double _used;
        private double _capacity;
        private int    _count;

        // Lock for accessing data values.
        private readonly object _dataValuesLock = new object();

        // Can construct only with parameters.
        private BUBuildingStatusTypeData() { }

        /// <summary>
        /// Constructor for new instance.
        /// </summary>
        public BUBuildingStatusTypeData(BUBuildingStatusType type)
        {
            // Save building status type.
            BuildingStatusType = type;
            BuildingStatusTypeName = ModAssemblyInfo.Name + BuildingStatusType.ToString();

            // Determine whether or not building status type is a special case.
            IsSpecialCase =
                BuildingStatusType == BUBuildingStatusType.None ||
                BuildingStatusType == BUBuildingStatusType.VehiclesInUseInMaintenance ||
                BuildingStatusType == BUBuildingStatusType.EfficiencyMaxColor ||
                BuildingStatusType == BUBuildingStatusType.ProductionMaxColor ||
                BuildingStatusTypeName.EndsWith("District") ||
                BuildingStatusTypeName.EndsWith("SelectDeselect") ||
                BuildingStatusTypeName.Contains("Heading");

            // Create data bindings, except for special cases.
            if (!IsSpecialCase)
            {
                _bindingUsed     = new ValueBinding<double>(ModAssemblyInfo.Name, BuildingStatusType + "Used",     0d);
                _bindingCapacity = new ValueBinding<double>(ModAssemblyInfo.Name, BuildingStatusType + "Capacity", 0d);
                _bindingCount    = new ValueBinding<int   >(ModAssemblyInfo.Name, BuildingStatusType + "Count",    0 );
            }
        }

        /// <summary>
        /// Get the data bindings.
        /// </summary>
        public void GetDataBindings(
            out ValueBinding<double> bindingUsed,
            out ValueBinding<double> bindingCapacity,
            out ValueBinding<int>    bindingCount)
        {
            bindingUsed     = _bindingUsed;
            bindingCapacity = _bindingCapacity;
            bindingCount    = _bindingCount;
        }

        /// <summary>
        /// Update data bindings with data values.
        /// </summary>
        public void UpdateDataBindings()
        {
            // Do not update special cases.
            if (!IsSpecialCase)
            {
                // Allow only one thread at a time to access data values.
                lock (_dataValuesLock)
                {
                    _bindingUsed    .Update(_used);
                    _bindingCapacity.Update(_capacity);
                    _bindingCount   .Update(_count);
                }
            }
        }

        /// <summary>
        /// Update data values.
        /// </summary>
        public void UpdateDataValues(double used, double capacity, int count)
        {
            // Allow only one thread at a time to access data values.
            lock (_dataValuesLock)
            {
                _used     = used;
                _capacity = capacity;
                _count    = count;
            }
        }

        /// <summary>
        /// Reset data values.
        /// </summary>
        public void ResetDataValues()
        {
            // Allow only one thread at a time to access data values.
            lock (_dataValuesLock)
            {
                _used     = 0d;
                _capacity = 0d;
                _count    = 0;
            }
        }
    }
}
