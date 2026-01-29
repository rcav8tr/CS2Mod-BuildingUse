using System;
using System.Collections.Generic;

namespace BuildingUse
{
    /// <summary>
    /// Collection of building status type data.
    /// </summary>
    public class BUBuildingStatusTypeDatas : Dictionary<BUBuildingStatusType, BUBuildingStatusTypeData>
    {
        // First building status type in the datas.
        public BUBuildingStatusType BuildingStatusTypeFirst { get; private set; }

        // Whether or not data values have been updated.
        private bool _dataValuesUpdated;
        public bool DataValuesUpdated
        {
            get
            {
                lock (_dataValuesUpdatedLock)
                {
                    return _dataValuesUpdated;
                }
            }
            set
            {
                lock (_dataValuesUpdatedLock)
                {
                    _dataValuesUpdated = value;
                }
            }
        }

        // Lock for accessing data values updated.
        private readonly object _dataValuesUpdatedLock = new object();

        // Can construct only with parameters.
        private BUBuildingStatusTypeDatas() { }

        /// <summary>
        /// Populate building status type datas.
        /// </summary>
        public BUBuildingStatusTypeDatas(BUInfoview infoview)
        {
            // Get infoview name.
            string infoviewName = infoview.ToString();

            // Populate the building status type datas.
            // Get first building status type.
            BuildingStatusTypeFirst = BUBuildingStatusType.None;
            foreach (BUBuildingStatusType buildingStatusType in Enum.GetValues(typeof(BUBuildingStatusType)))
            {
                // Check if enum is for this infoview.
                if (buildingStatusType.ToString().StartsWith(infoviewName))
                {
                    // Save first only once.
                    if (BuildingStatusTypeFirst == BUBuildingStatusType.None)
                    {
                        BuildingStatusTypeFirst = buildingStatusType;
                    }

                    // Add a new building status type data.
                    Add(buildingStatusType, new BUBuildingStatusTypeData(buildingStatusType));
                }
            }
        }

        /// <summary>
        /// Update data bindings.
        /// </summary>
        public void UpdateDataBindings()
        {
            // Do each building status type data.
            foreach (BUBuildingStatusTypeData buildingStatusTypeData in Values)
            {
                buildingStatusTypeData.UpdateDataBindings();
            }
        }

        /// <summary>
        /// Update data values.
        /// </summary>
        public void UpdateDataValues(double[] used, double[] capacity, int[] count)
        {
            // Do each building status type data.
            int index = 0;
            foreach (BUBuildingStatusTypeData buildingStatusTypeData in Values)
            {
                buildingStatusTypeData.UpdateDataValues(used[index], capacity[index], count[index]);
                index++;
            }

            // Data values have been updated.
            DataValuesUpdated = true;
        }

        /// <summary>
        /// Reset data values.
        /// </summary>
        public void ResetDataValues()
        {
            // Do each building status type data.
            foreach (BUBuildingStatusTypeData buildingStatusTypeData in Values)
            {
                buildingStatusTypeData.ResetDataValues();
            }
        }
    }
}
