using Colossal.UI.Binding;
using System.Collections.Generic;

namespace BuildingUse
{
    /// <summary>
    /// Info for all districts needed by UI.
    /// </summary>
    public class DistrictInfos : List<DistrictInfo>, IJsonWritable
    {
        /// <summary>
        /// Write district infos to the UI.
        /// </summary>
        public void Write(IJsonWriter writer)
        {
			writer.ArrayBegin(this.Count);
			foreach (DistrictInfo districtInfo in this)
			{
				districtInfo.Write(writer);
			}
			writer.ArrayEnd();
        }
    }
}
