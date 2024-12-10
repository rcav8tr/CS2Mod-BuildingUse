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
        public Entity entity { get; set; }
        public string name { get; set; }

        public DistrictInfo(Entity entity, string name)
        {
            this.entity = entity;
            this.name = name;
        }

        /// <summary>
        /// Write district info to the UI.
        /// </summary>
        public void Write(IJsonWriter writer)
        {
			writer.TypeBegin(ModAssemblyInfo.Name + ".DistrictInfo");
			writer.PropertyName("entity");
			writer.Write(entity);
			writer.PropertyName("name");
			writer.Write(name);
			writer.TypeEnd();
        }

        /// <summary>
        /// Compare the names of two districts.
        /// </summary>
        public int CompareTo(DistrictInfo other)
        {
            return String.Compare(this.name, other.name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
