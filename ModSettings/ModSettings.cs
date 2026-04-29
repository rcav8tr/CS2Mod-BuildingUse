using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;

namespace BuildingUse
{
    /// <summary>
    /// The settings for this mod.
    /// </summary>
    [FileLocation("ModsSettings/" + nameof(BuildingUse) + "/" + nameof(BuildingUse))]
    [SettingsUIGroupOrder(GroupGeneral, GroupAbout)]
    [SettingsUIShowGroupName(GroupGeneral, GroupAbout)]
    public class ModSettings : ModSetting
    {
        // Group constants.
        public const string GroupGeneral = "General";
        public const string GroupAbout   = "About";

        // Whether or not settings are loaded.
        private bool _loaded = false;

        // Constructor.
        public ModSettings(IMod mod) : base(mod)
        {
            Mod.log.Info($"{nameof(ModSettings)}.{nameof(ModSettings)}");

            SetDefaults();
        }

        /// <summary>
        /// Set a default value for every setting that has a value that can change.
        /// </summary>
        public override void SetDefaults()
        {
            // It is important to set a default for every value.
            ZonedBuildingColor           = ZonedBuildingColorChoice.ThreeColors;
            ServiceBuildingColor         = ServiceBuildingColorChoice.ThreeColors;
            ProductionInfoviewColor      = ProductionInfoviewColorChoice.ThreeColors;
            ColorSpecializedIndustryLots = false;
            ReverseColors                = false;

            CountVehiclesInUse           = true;
            CountVehiclesInMaintenance   = true;
            EfficiencyMaxColor200Percent = false;
            ProductionMaxColor200Percent = false;
        }

        /// <summary>
        /// Set loaded flag.
        /// </summary>
        public void Loaded()
        {
            _loaded = true;
        }

        // Gradient color choices for zoned buildings (i.e. residential, commercial, industrial, and office).
        public enum ZonedBuildingColorChoice
        {
            ThreeColors,
            ZoneColor,
        }

        // The selected gradient color choice for zoned buildings.
        // When this setting changes, need to set infomode colors.
        private ZonedBuildingColorChoice _zonedBuildingColorChoice;
        [SettingsUISection(GroupGeneral)]
        public ZonedBuildingColorChoice ZonedBuildingColor
        {
            get { return _zonedBuildingColorChoice; }
            set { _zonedBuildingColorChoice = value; SetInfomodeColors(); }
        }

        // Gradient color choices for service buildings.
        public enum ServiceBuildingColorChoice
        {
            ThreeColors,
            Red,
            Various
        }

        // The selected gradient color choice for service buildings.
        // When this setting changes, need to set infomode colors.
        private ServiceBuildingColorChoice _serviceBuildingColorChoice;
        [SettingsUISection(GroupGeneral)]
        public ServiceBuildingColorChoice ServiceBuildingColor
        {
            get { return _serviceBuildingColorChoice; }
            set { _serviceBuildingColorChoice = value; SetInfomodeColors(); }
        }

        // Gradient color choices for Production infoview.
        public enum ProductionInfoviewColorChoice
        {
            ThreeColors,
            VariousGame,
            VariousResource,
        }

        // The selected gradient color choice for Production infoview.
        // When this setting changes, need to set infomode colors.
        private ProductionInfoviewColorChoice _productionInfoviewColorChoice;
        [SettingsUISection(GroupGeneral)]
        public ProductionInfoviewColorChoice ProductionInfoviewColor
        {
            get { return _productionInfoviewColorChoice; }
            set { _productionInfoviewColorChoice = value; SetInfomodeColors(); }
        }

        // Color specialized industry lots.
        [SettingsUISection(GroupGeneral)]
        public bool ColorSpecializedIndustryLots { get; set; }

        // Whether or not to reverse the colors.
        // When this setting changes, need to set infomode colors.
        private bool _reverseColors;
        [SettingsUISection(GroupGeneral)]
        public bool ReverseColors
        {
            get { return _reverseColors; }
            set { _reverseColors = value; SetInfomodeColors(); }
        }

        // Display mod version in settings.
        [SettingsUISection(GroupAbout)]
        public string ModVersion { get { return ModAssemblyInfo.Version; } }


        // Whether or not to count vehicles in use and in maintenance.
        [SettingsUIHidden]
        public bool CountVehiclesInUse { get; set; }
        [SettingsUIHidden]
        public bool CountVehiclesInMaintenance { get; set; }

        // Efficiency and Production max color 200 percent.
        // True means max color is 200 percent.
        // False means default of 100 percent.
        [SettingsUIHidden]
        public bool EfficiencyMaxColor200Percent { get; set; }
        [SettingsUIHidden]
        public bool ProductionMaxColor200Percent { get; set; }

        /// <summary>
        /// Set infomode colors.
        /// </summary>
        private void SetInfomodeColors()
        {
            // Settings must be loaded.
            // This prevents setting infomode colors while defaults are being set and while settings are loading.
            if (_loaded)
            {
                BUInfoviewDatas.instance.SetInfomodeColors();
            }
        }
    }
}
