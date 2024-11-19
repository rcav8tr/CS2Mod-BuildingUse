using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;

namespace BuildingUse
{
    /// <summary>
    /// The settings for this mod.
    /// </summary>
    [FileLocation(nameof(BuildingUse))]
    [SettingsUIGroupOrder(GroupGeneral, GroupAbout)]
    [SettingsUIShowGroupName(GroupGeneral, GroupAbout)]
    public class ModSettings : ModSetting
    {
        // Group constants.
        public const string GroupGeneral = "General";
        public const string GroupAbout   = "About";
        
        // Constructor.
        public ModSettings(IMod mod) : base(mod)
        {
            LogUtil.Info($"{nameof(ModSettings)}.{nameof(ModSettings)}");

            SetDefaults();
        }
        
        /// <summary>
        /// Set a default value for every setting that has a value that can change.
        /// </summary>
        public override void SetDefaults()
        {
            // It is important to set a default for every value.
            ZonedBuildingColor = ZonedBuildingColorChoice.ThreeColors;
            ServiceBuildingColor = ServiceBuildingColorChoice.ThreeColors;
            ColorSpecializedIndustryLots = false;

            CountVehiclesInUse           = true;
            CountVehiclesInMaintenance   = true;
            EfficiencyMaxColor200Percent = false;
        }

        // Gradient color choices for zoned buildings (i.e. residential, commercial, industrial, and office).
        public enum ZonedBuildingColorChoice
        {
            ThreeColors,
            ZoneColor,
        }

        // The selected gradient color choice for zoned buildings.
        private ZonedBuildingColorChoice _zonedBuildingColorChoice;
        [SettingsUISection(GroupGeneral)]
        public ZonedBuildingColorChoice ZonedBuildingColor
        {
            get { return _zonedBuildingColorChoice; }
            set { _zonedBuildingColorChoice = value; BUInfoviewDatas.instance.SetInfomodeColors(); ApplyAndSave(); }
        }

        // Gradient color choices for service buildings.
        public enum ServiceBuildingColorChoice
        {
            ThreeColors,
            Red,
            Various
        }

        // The selected gradient color choice for service buildings.
        private ServiceBuildingColorChoice _serviceBuildingColorChoice;
        [SettingsUISection(GroupGeneral)]
        public ServiceBuildingColorChoice ServiceBuildingColor
        {
            get { return _serviceBuildingColorChoice; }
            set { _serviceBuildingColorChoice = value; BUInfoviewDatas.instance.SetInfomodeColors(); ApplyAndSave(); }
        }

        // Color specialized industry lots.
        [SettingsUISection(GroupGeneral)]
        public bool ColorSpecializedIndustryLots { get; set; }

        // Display mod version in settings.
        [SettingsUISection(GroupAbout)]
        public string ModVersion { get { return ModAssemblyInfo.Version; } }


        // Whether or not to count vehicles in use.
        [SettingsUIHidden]
        public bool CountVehiclesInUse { get; set; }

        // Whether or not to count vehicles in maintenance.
        [SettingsUIHidden]
        public bool CountVehiclesInMaintenance { get; set; }

        // Efficiency max color 200 percent.
        // True means max color is 200 percent.
        // False means default of 100 percent.
        [SettingsUIHidden]
        public bool EfficiencyMaxColor200Percent { get; set; }
    }
}
