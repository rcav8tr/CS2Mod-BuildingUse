using Colossal.UI.Binding;
using System;
using Unity.Entities;

namespace BuildingUse
{
    /// <summary>
    /// Info for a single district needed by the UI.
    /// </summary>
    public class DistrictInfo : IJsonWritable, IComparable<DistrictInfo>
    {
        // Entity and name of the district.
        public Entity District { get; set; }
        public string Name { get; set; }

        // Can construct only with parameters.
        private DistrictInfo() { }

        /// <summary>
        /// Constructor for new instance.
        /// </summary>
        public DistrictInfo(Entity district, string name)
        {
            District = district;
            Name = name;
        }

        /// <summary>
        /// Write district info to the UI.
        /// </summary>
        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(ModAssemblyInfo.Name + ".DistrictInfo");
            writer.PropertyName("district");
            writer.Write(District);
            writer.PropertyName("name");
            writer.Write(Name);
            writer.TypeEnd();
        }

        /// <summary>
        /// Compare the names of two districts.
        /// </summary>
        public int CompareTo(DistrictInfo other)
        {
            return String.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
