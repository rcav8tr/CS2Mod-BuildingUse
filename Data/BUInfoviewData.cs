using Colossal.Mathematics;
using Game.Economy;
using Game.Prefabs;
using System;
using Unity.Entities;
using UnityEngine;

namespace BuildingUse
{
    /// <summary>
    /// Class to hold data for one of this mod's infoviews.
    /// The "BU" (i.e. Building Use) prefix differentiates this class from Game.Prefabs.InfoviewData.
    /// </summary>
    public class BUInfoviewData
    {
        // The infoview this data is for.
        public BUInfoview Infoview { get; set; }
        public string InfoviewName { get; set; }

        // Building status type datas.
        public BUBuildingStatusTypeDatas buildingStatusTypeDatas { get; set; }

        // Infoview prefab and prefab entity.
        public InfoviewPrefab InfoviewPrefab { get; set; }
        public Entity InfoviewPrefabEntity { get; set; }

        // Can construct only with parameters.
        private BUInfoviewData() { }

        /// <summary>
        /// Constructor for new instance.
        /// </summary>
        public BUInfoviewData(BUInfoview infoview, int infoviewGroupNumber, int infoviewPriority)
        {
            // Save the infoview.
            Infoview = infoview;
            InfoviewName = ModAssemblyInfo.Name + infoview.ToString();

            // Create new building status type datas.
            buildingStatusTypeDatas = new BUBuildingStatusTypeDatas(infoview);

            // Create the infoview prefab.
            CreateInfoviewPrefab(infoviewGroupNumber, infoviewPriority);
        }

        /// <summary>
        /// Create a prefab for the infoview.
        /// </summary>
        private void CreateInfoviewPrefab(int infoviewGroupNumber, int infoviewPriority)
        {
            // Create the infoview prefab.
            InfoviewPrefab = ScriptableObject.CreateInstance<InfoviewPrefab>();

            // Set prefab properties.
            InfoviewPrefab.name = InfoviewName;
            InfoviewPrefab.m_Group = infoviewGroupNumber;
            InfoviewPrefab.m_Priority = infoviewPriority;
            InfoviewPrefab.m_Editor = false;
            InfoviewPrefab.m_IconPath = $"coui://{Mod.ImagesURI}/Infoview{Infoview}.svg";

            // Set prefab's infomodes.
            // Priority determines infomode sort order.
            InfoviewPrefab.m_Infomodes = new InfomodeInfo[buildingStatusTypeDatas.Count];
            int infomodePriority = 1;
            int index = 0;
            foreach (BUBuildingStatusTypeData buildingStatusTypeData in buildingStatusTypeDatas.Values)
            {
                InfoviewPrefab.m_Infomodes[index++] = CreateInfomodeInfo(buildingStatusTypeData, infomodePriority++);
            }

            // Add the infoview prefab to the prefab system.
            PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
            prefabSystem.AddPrefab(InfoviewPrefab);

            // Save the infoview prefab entity.
            InfoviewPrefabEntity = prefabSystem.GetEntity(InfoviewPrefab);
        }

        /// <summary>
        /// Create an InfomodeInfo for a building status type.
        /// </summary>
        private InfomodeInfo CreateInfomodeInfo(BUBuildingStatusTypeData buildingStatusTypeData, int priority)
        {
            // Create a new building status infomode prefab.
            // All infomodes in this mod are of type BuildingStatusInfomodePrefab.
            // BuildingStatusInfomodePrefab results in InfoviewBuildingStatusData being generated.
            BuildingStatusInfomodePrefab infomodePrefab = ScriptableObject.CreateInstance<BuildingStatusInfomodePrefab>();

            // Set infomode prefab properties.
            infomodePrefab.m_Type = (BuildingStatusType)buildingStatusTypeData.BuildingStatusType;
            infomodePrefab.name = buildingStatusTypeData.BuildingStatusTypeName;
            infomodePrefab.m_Range = new Bounds1(0f, 255f);        // Range of 0 to 255 represents 0% to 100%.
            infomodePrefab.m_LegendType = GradientLegendType.Gradient;

            // Return a new infomode based on the infomode prefab.
            return new InfomodeInfo() { m_Mode = infomodePrefab, m_Priority = priority };
        }

        /// <summary>
        /// Set colors for all infomodes of this infoview.
        /// </summary>
        public void SetInfomodeColors()
        {
            // Do each of this infoview prefab's infomodes.
            foreach (InfomodeInfo infomodeInfo in InfoviewPrefab.m_Infomodes)
            {
                // Get the infomode's building status type.
                BuildingStatusInfomodePrefab buildingStatusInfomodePrefab = (BuildingStatusInfomodePrefab)infomodeInfo.m_Mode;
                BUBuildingStatusType buildingStatusType = (BUBuildingStatusType)buildingStatusInfomodePrefab.m_Type;

                // Set low color based on building status type.
                Color lowColor = default;
                bool useThreeColors = false;
                switch (buildingStatusType)
                {
                    // The four zone colors are always the same regardless of the setting.
                    // The colors are manually taken from the corresponding zone icons.
                    case BUBuildingStatusType.EmployeesResidential:
                    case BUBuildingStatusType.VehiclesResidentialCar:
                    case BUBuildingStatusType.EfficiencyResidential:
                        if (Mod.ModSettings.ZonedBuildingColor == ModSettings.ZonedBuildingColorChoice.ThreeColors)
                        {
                            useThreeColors = true;
                        }
                        else
                        {
                            // Media/Game/Icons/ZoneResidential.svg
                            lowColor = GetColor(0, 255, 38);
                        }
                        break;

                    case BUBuildingStatusType.EmployeesCommercial:
                    case BUBuildingStatusType.StorageCommercial:
                    case BUBuildingStatusType.VehiclesCommercialTruck:
                    case BUBuildingStatusType.EfficiencyCommercial:
                        if (Mod.ModSettings.ZonedBuildingColor == ModSettings.ZonedBuildingColorChoice.ThreeColors)
                        {
                            useThreeColors = true;
                        }
                        else
                        {
                            // Media/Game/Icons/ZoneCommercial.svg
                            lowColor = GetColor(0, 191, 255);
                        }
                        break;

                    case BUBuildingStatusType.EmployeesIndustrial:
                    case BUBuildingStatusType.StorageIndustrial:
                    case BUBuildingStatusType.VehiclesIndustrialTruck:
                    case BUBuildingStatusType.EfficiencyIndustrial:
                        if (Mod.ModSettings.ZonedBuildingColor == ModSettings.ZonedBuildingColorChoice.ThreeColors)
                        {
                            useThreeColors = true;
                        }
                        else
                        {
                            // Media/Game/Icons/ZoneIndustrial.svg
                            lowColor = GetColor(255, 217, 0);
                        }
                        break;

                    case BUBuildingStatusType.EmployeesOffice:
                    case BUBuildingStatusType.StorageOffice:
                    case BUBuildingStatusType.VehiclesOfficeTruck:
                    case BUBuildingStatusType.EfficiencyOffice:
                        if (Mod.ModSettings.ZonedBuildingColor == ModSettings.ZonedBuildingColorChoice.ThreeColors)
                        {
                            useThreeColors = true;
                        }
                        else
                        {
                            // Media/Game/Icons/ZoneOffice.svg
                            lowColor = GetColor(179, 0, 255);
                        }
                        break;

                    // Check for Production.
                    case BUBuildingStatusType.ProductionWood:
                    case BUBuildingStatusType.ProductionGrain:
                    case BUBuildingStatusType.ProductionLivestock:
                    case BUBuildingStatusType.ProductionFish:
                    case BUBuildingStatusType.ProductionVegetables:
                    case BUBuildingStatusType.ProductionCotton:
                    case BUBuildingStatusType.ProductionOil:
                    case BUBuildingStatusType.ProductionOre:
                    case BUBuildingStatusType.ProductionCoal:
                    case BUBuildingStatusType.ProductionStone:

                    case BUBuildingStatusType.ProductionMetals:
                    case BUBuildingStatusType.ProductionSteel:
                    case BUBuildingStatusType.ProductionMinerals:
                    case BUBuildingStatusType.ProductionConcrete:
                    case BUBuildingStatusType.ProductionMachinery:
                    case BUBuildingStatusType.ProductionPetrochemicals:
                    case BUBuildingStatusType.ProductionChemicals:
                    case BUBuildingStatusType.ProductionPlastics:
                    case BUBuildingStatusType.ProductionPharmaceuticals:
                    case BUBuildingStatusType.ProductionElectronics:
                    case BUBuildingStatusType.ProductionVehicles:
                    case BUBuildingStatusType.ProductionBeverages:
                    case BUBuildingStatusType.ProductionConvenienceFood:
                    case BUBuildingStatusType.ProductionFood:
                    case BUBuildingStatusType.ProductionTextiles:
                    case BUBuildingStatusType.ProductionTimber:
                    case BUBuildingStatusType.ProductionPaper:
                    case BUBuildingStatusType.ProductionFurniture:

                    case BUBuildingStatusType.ProductionSoftware:
                    case BUBuildingStatusType.ProductionTelecom:
                    case BUBuildingStatusType.ProductionFinancial:
                    case BUBuildingStatusType.ProductionMedia:
                        if (Mod.ModSettings.ProductionInfoviewColor == ModSettings.ProductionInfoviewColorChoice.ThreeColors)
                        {
                            useThreeColors = true;
                        }
                        else if (Mod.ModSettings.ProductionInfoviewColor == ModSettings.ProductionInfoviewColorChoice.VariousGame)
                        {
                            // Convert building status type to a resource.
                            string buildingStatusTypeName = buildingStatusType.ToString();
                            string resourceName = buildingStatusTypeName.Substring("Production".Length);
                            if (Enum.TryParse<Resource>(resourceName, out Resource resource))
                            {
                                // Get the resource prefab.
                                ResourceSystem resourceSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ResourceSystem>();
                                Entity resourcePrefabEntity = resourceSystem.GetPrefab(resource);
                                PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
                                if (prefabSystem.TryGetPrefab(resourcePrefabEntity, out ResourcePrefab resourcePrefab))
                                {
                                    // Get the color from the resource prefab.
                                    lowColor = resourcePrefab.m_Color;
                                }
                            }
                        }
                        else
                        {
                            // Get low color based on building status type.
                            // Except where noted, each color was taken manually from the corresponding resource icon.
                            // These same colors are also used in Resource Locator mod.
                            switch (buildingStatusType)
                            {
                                case BUBuildingStatusType.ProductionWood:               lowColor = GetColor(136,  77,  31); break;  // Dark part of the wood.
                                case BUBuildingStatusType.ProductionGrain:              lowColor = GetColor(244, 195, 110); break;  // Light part of the grain.
                                case BUBuildingStatusType.ProductionLivestock:          lowColor = GetColor(194,  54,   2); break;  // Top of the chicken head.
                                case BUBuildingStatusType.ProductionFish:               lowColor = GetColor(107, 136, 157); break;  // Eye of fish.
                                case BUBuildingStatusType.ProductionVegetables:         lowColor = GetColor(252, 158,  51); break;  // Center of carrot.
                                case BUBuildingStatusType.ProductionCotton:             lowColor = GetColor(227, 235, 242); break;  // Top cotton ball.
                                case BUBuildingStatusType.ProductionOil:                lowColor = GetColor(132, 120, 109); break;  // Middle of 3 colors.
                                case BUBuildingStatusType.ProductionOre:                lowColor = GetColor( 82, 240, 218); break;  // Blue from ore.
                                case BUBuildingStatusType.ProductionCoal:               lowColor = GetColor( 76,  79,  85); break;  // Middle of 3 colors.
                                case BUBuildingStatusType.ProductionStone:              lowColor = GetColor(139, 131, 131); break;  // Darker of 2 colors.

                                case BUBuildingStatusType.ProductionMetals:             lowColor = GetColor(138, 141, 143); break;  // Lighter of 2 colors.
                                case BUBuildingStatusType.ProductionSteel:              lowColor = GetColor(182, 213, 236); break;  // Lighter of 2 colors.
                                case BUBuildingStatusType.ProductionMinerals:           lowColor = GetColor( 10, 176, 153); break;  // Darkest of colors.
                                case BUBuildingStatusType.ProductionConcrete:           lowColor = GetColor(117,   0, 143); break;  // Ignore icon, use purple.
                                case BUBuildingStatusType.ProductionMachinery:          lowColor = GetColor(150, 172, 183); break;  // Darker of 2 gears.
                                case BUBuildingStatusType.ProductionPetrochemicals:     lowColor = GetColor(255, 221, 186); break;  // Lightest of 3 colors.
                                case BUBuildingStatusType.ProductionChemicals:          lowColor = GetColor(147, 226,  44); break;  // Green from flask.
                                case BUBuildingStatusType.ProductionPlastics:           lowColor = GetColor(117, 164, 255); break;  // Main bottle color.
                                case BUBuildingStatusType.ProductionPharmaceuticals:    lowColor = GetColor(233, 129, 129); break;  // Light color from red pill.
                                case BUBuildingStatusType.ProductionElectronics:        lowColor = GetColor( 59,  65,  70); break;  // 50% of center color.
                                case BUBuildingStatusType.ProductionVehicles:           lowColor = GetColor(241, 105,  35); break;  // Car body.
                                case BUBuildingStatusType.ProductionBeverages:          lowColor = GetColor(243, 141,  52); break;  // Darkest color from bottle.
                                case BUBuildingStatusType.ProductionConvenienceFood:    lowColor = GetColor(207, 194, 160); break;  // Front food item.
                                case BUBuildingStatusType.ProductionFood:               lowColor = GetColor(250, 220, 162); break;  // Basket.
                                case BUBuildingStatusType.ProductionTextiles:           lowColor = GetColor(240, 236, 236); break;  // Only one color.
                                case BUBuildingStatusType.ProductionTimber:             lowColor = GetColor(198, 133, 100); break;  // Middle of 3 colors.
                                case BUBuildingStatusType.ProductionPaper:              lowColor = GetColor(220,   0, 180); break;  // Ignore icon, use pink.
                                case BUBuildingStatusType.ProductionFurniture:          lowColor = GetColor(175, 110,  46); break;  // Chair right side.

                                case BUBuildingStatusType.ProductionSoftware:           lowColor = GetColor(193, 255, 104); break;  // The only green.
                                case BUBuildingStatusType.ProductionTelecom:            lowColor = GetColor(230, 221, 199); break;  // Back conversation bubble.
                                case BUBuildingStatusType.ProductionFinancial:          lowColor = GetColor(114, 171,  95); break;  // Right side of money stack.
                                case BUBuildingStatusType.ProductionMedia:              lowColor = GetColor(135, 151, 156); break;  // Lighter of 2 colors.
                            }
                        }

                        // If not using three colors, make adjustments to the low color.
                        if (!useThreeColors)
                        {
                            // Make low color a bit brighter so middle of gradient is closer to original color.
                            lowColor = MultiplyColor(lowColor, 1.15f);

                            // Make sure color has a minimum perceived luminance.
                            const float MinimumPerceivedLuminance = 0.4f;
                            float perceivedLuminance = 0.2126f * lowColor.r + 0.7152f * lowColor.g + 0.0722f * lowColor.b;
                            if (perceivedLuminance > 0f && perceivedLuminance < MinimumPerceivedLuminance)
                            {
                                lowColor = MultiplyColor(lowColor, MinimumPerceivedLuminance / perceivedLuminance);
                            }
                        }

                        break;

                    default:
                        // Everything else is service.

                        // Check mod setting for service building color.
                        if (Mod.ModSettings.ServiceBuildingColor == ModSettings.ServiceBuildingColorChoice.ThreeColors)
                        {
                            // Use three colors.
                            useThreeColors = true;
                        }
                        else if (Mod.ModSettings.ServiceBuildingColor == ModSettings.ServiceBuildingColorChoice.Red)
                        {
                            // All service is red.
                            lowColor = GetColor(255, 26, 0);
                        }
                        else
                        {
                            // Service building color setting is Various.

                            // Set low color different for each building status type.
                            // In most cases, the color is manually taken from the corresponding service icon.
                            // Where the service icon is mostly white/gray, the icon color is ignored and a different color is used.
                            switch (buildingStatusType)
                            {
                                case BUBuildingStatusType.EmployeesParking:
                                case BUBuildingStatusType.VehiclesParkedCar:
                                case BUBuildingStatusType.VehiclesParkedBike:
                                case BUBuildingStatusType.VehiclesParkedOther:
                                case BUBuildingStatusType.EfficiencyParking:
                                    // Media/Game/Icons/Parking.svg
                                    // Ignore icon color (white/gray) and use blue.
                                    lowColor = GetColor(32, 128, 255);
                                    break;

                                case BUBuildingStatusType.EmployeesRoadMaintenance:
                                case BUBuildingStatusType.VehiclesRoadMaintenance:
                                case BUBuildingStatusType.EfficiencyRoadMaintenance:
                                    // Media/Game/Icons/RoadsServices.svg
                                    // Ignore icon color (white/gray) and use blue.
                                    lowColor = GetColor(96, 96, 255);
                                    break;

                                case BUBuildingStatusType.EmployeesElectricity:
                                case BUBuildingStatusType.StorageBatteryCharge:
                                case BUBuildingStatusType.StoragePowerPlantFuel:
                                case BUBuildingStatusType.EfficiencyElectricity:
                                case BUBuildingStatusType.ProcessingElectricityProduction:
                                    // Media/Game/Icons/Electricity.svg
                                    lowColor = GetColor(255, 184, 14);
                                    break;

                                case BUBuildingStatusType.EmployeesWater:
                                case BUBuildingStatusType.EfficiencyWater:
                                case BUBuildingStatusType.ProcessingWaterOutput:
                                    // Media/Game/Icons/Water.svg
                                    lowColor = GetColor(57, 194, 255);
                                    break;

                                case BUBuildingStatusType.EmployeesSewage:
                                case BUBuildingStatusType.EfficiencySewage:
                                case BUBuildingStatusType.ProcessingSewageTreatment:
                                    // Media/Game/Icons/Sewage.svg
                                    lowColor = GetColor(161, 136, 108);
                                    break;

                                case BUBuildingStatusType.EmployeesHealthcare:
                                case BUBuildingStatusType.VisitorsHealthcare:
                                case BUBuildingStatusType.StorageHealthcare:
                                case BUBuildingStatusType.VehiclesAmbulance:
                                case BUBuildingStatusType.VehiclesMedicalHelicopter:
                                case BUBuildingStatusType.EfficiencyHealthcare:
                                    // Media/Game/Icons/Healthcare.svg
                                    lowColor = GetColor(229, 99, 51);
                                    break;

                                case BUBuildingStatusType.EmployeesDeathcare:
                                case BUBuildingStatusType.VisitorsCemetery:
                                case BUBuildingStatusType.VisitorsCrematorium:
                                case BUBuildingStatusType.VehiclesHearse:
                                case BUBuildingStatusType.EfficiencyDeathcare:
                                case BUBuildingStatusType.ProcessingCrematoriumProcessing:
                                    // Media/Game/Icons/Deathcare.svg
                                    lowColor = GetColor(131, 231, 0);
                                    break;

                                case BUBuildingStatusType.EmployeesGarbageManagement:
                                case BUBuildingStatusType.StorageGarbageManagement:
                                case BUBuildingStatusType.StorageLandfill:
                                case BUBuildingStatusType.VehiclesGarbageTruck:
                                case BUBuildingStatusType.EfficiencyGarbageManagement:
                                case BUBuildingStatusType.ProcessingGarbageProcessing:
                                    // Media/Game/Icons/Garbage.svg
                                    lowColor = GetColor( 49, 207, 0);
                                    break; 

                                case BUBuildingStatusType.EmployeesEducation:
                                case BUBuildingStatusType.VisitorsElementarySchool:
                                case BUBuildingStatusType.VisitorsHighSchool:
                                case BUBuildingStatusType.VisitorsCollege:
                                case BUBuildingStatusType.VisitorsUniversity:
                                case BUBuildingStatusType.EfficiencyEducation:
                                    // Media/Game/Icons/Education.svg made lighter
                                    lowColor = GetColor(129, 155, 173) * 1.3f;
                                    break;

                                case BUBuildingStatusType.EmployeesResearch:
                                case BUBuildingStatusType.EfficiencyResearch:
                                    // Media/Game/Icons/Research.svg
                                    lowColor = GetColor(105, 200, 224);
                                    break;

                                case BUBuildingStatusType.EmployeesFireRescue:
                                case BUBuildingStatusType.VehiclesFireEngine:
                                case BUBuildingStatusType.VehiclesFireHelicopter:
                                case BUBuildingStatusType.EfficiencyFireRescue:
                                    // Media/Game/Icons/FireSafety.svg
                                    lowColor = GetColor(229, 99, 51);
                                    break;

                                case BUBuildingStatusType.EmployeesDisasterControl:
                                case BUBuildingStatusType.VisitorsEmergencyShelter:
                                case BUBuildingStatusType.StorageEmergencyShelter:
                                case BUBuildingStatusType.VehiclesDisasterResponse:
                                case BUBuildingStatusType.VehiclesEvacuationBus:
                                case BUBuildingStatusType.EfficiencyDisasterControl:
                                    // Media/Game/Icons/DisasterControl.svg
                                    // Ignore icon color (white/gray) and use red.
                                    lowColor = GetColor(255, 128, 64);
                                    break;

                                case BUBuildingStatusType.EmployeesPolice:
                                case BUBuildingStatusType.VisitorsPoliceStation:
                                case BUBuildingStatusType.VisitorsPrison:
                                case BUBuildingStatusType.VehiclesPoliceCar:
                                case BUBuildingStatusType.VehiclesPoliceHelicopter:
                                case BUBuildingStatusType.VehiclesPrisonVan:
                                case BUBuildingStatusType.EfficiencyPolice:
                                    // Media/Game/Icons/Police.svg
                                    lowColor = GetColor(255, 184, 14);
                                    break;

                                case BUBuildingStatusType.EmployeesAdministration:
                                case BUBuildingStatusType.EfficiencyAdministration:
                                    // Media/Game/Icons/Administration.svg
                                    // Ignore icon color (white/gray) and use yellow.
                                    lowColor = GetColor(224, 224, 0);
                                    break;

                                case BUBuildingStatusType.EmployeesTransportation:
                                case BUBuildingStatusType.EfficiencyTransportation:
                                    // Media/Game/Icons/Transportation.svg
                                    lowColor = GetColor(84, 182, 11);
                                    break;

                                case BUBuildingStatusType.EmployeesParkMaintenance:
                                case BUBuildingStatusType.VehiclesParkMaintenance:
                                case BUBuildingStatusType.EfficiencyParkMaintenance:
                                    // Media/Game/Icons/ParkMaintenance.svg
                                    lowColor = GetColor(224, 185, 140);
                                    break;

                                case BUBuildingStatusType.EmployeesParksRecreation:
                                case BUBuildingStatusType.EfficiencyParksRecreation:
                                    // Media/Game/Icons/ParksAndRecreation.svg
                                    lowColor = GetColor(204, 153, 94);
                                    break;

                                case BUBuildingStatusType.EmployeesPost:
                                case BUBuildingStatusType.StoragePost:
                                case BUBuildingStatusType.StorageMailbox:
                                case BUBuildingStatusType.VehiclesPost:
                                case BUBuildingStatusType.EfficiencyPost:
                                case BUBuildingStatusType.ProcessingMailSortingSpeed:
                                    // Media/Game/Icons/PostService.svg
                                    // Ignore icon color (white/gray) and use purple.
                                    lowColor = GetColor(255, 0, 179);
                                    break;

                                case BUBuildingStatusType.EmployeesTelecom:
                                case BUBuildingStatusType.EfficiencyTelecom:
                                    // Media/Game/Icons/Communications.svg
                                    // Ignore icon color (white/gray) and use purple.
                                    lowColor = GetColor(180, 64, 255);
                                    break;
                                                                                                
                                case BUBuildingStatusType.VehiclesBus:
                                    // Media/Game/Icons/Bus.svg
                                    lowColor = GetColor(58, 151, 187);
                                    break;

                                case BUBuildingStatusType.VehiclesTaxi:
                                    // Media/Game/Icons/Taxi.svg
                                    lowColor = GetColor(247, 205, 51);
                                    break;

                                case BUBuildingStatusType.VehiclesTrain:
                                    // Media/Game/Icons/Train.svg
                                    lowColor = GetColor(60, 148, 66);
                                    break;

                                case BUBuildingStatusType.VehiclesTram:
                                    // Media/Game/Icons/Tram.svg
                                    lowColor = GetColor(221, 113, 248);
                                    break;

                                case BUBuildingStatusType.VehiclesSubway:
                                    // Media/Game/Icons/Subway.svg
                                    lowColor = GetColor(218, 111, 72);
                                    break;

                                case BUBuildingStatusType.VehiclesFerry:
                                    // Media/Game/Icons/Ferry.svg
                                    lowColor = GetColor(75, 158, 241);
                                    break;

                                case BUBuildingStatusType.StorageCargoTransportation:
                                case BUBuildingStatusType.VehiclesCargoStationTruck:
                                    // Media/Game/Icons/CargoTruck.svg
                                    lowColor = GetColor(85, 187, 226);
                                    break;

                                default:
                                    // Set to black to be noticeable.
                                    lowColor = GetColor(0, 0, 0);
                                    break;
                            }
                        }
                        break;
                }

                // Check for three colors.
                if (useThreeColors)
                {
                    // Set low, medium, high colors to red, yellow, green respectively.
                    buildingStatusInfomodePrefab.m_Low    = GetColor(255,  32, 32);
                    buildingStatusInfomodePrefab.m_Medium = GetColor(224, 224,  0);
                    buildingStatusInfomodePrefab.m_High   = GetColor( 32, 255, 32);
                }
                else
                {
                    // Set low color.
                    buildingStatusInfomodePrefab.m_Low = lowColor;

                    // Set high color to a darker version of low color.
                    const float ColorMultiplier = 0.45f;
                    buildingStatusInfomodePrefab.m_High = MultiplyColor(lowColor, ColorMultiplier);

                    // Set medium color to average of low and high colors.
                    buildingStatusInfomodePrefab.m_Medium = new Color((lowColor.r + buildingStatusInfomodePrefab.m_High.r) * 0.5f,
                                                                      (lowColor.g + buildingStatusInfomodePrefab.m_High.g) * 0.5f,
                                                                      (lowColor.b + buildingStatusInfomodePrefab.m_High.b) * 0.5f,
                                                                      1f);
                }

                // Check for reverse colors.
                if (Mod.ModSettings.ReverseColors)
                {
                    // Reverse low and high colors.
                    // Medium color stays the same.
                    (buildingStatusInfomodePrefab.m_High, buildingStatusInfomodePrefab.m_Low) = (buildingStatusInfomodePrefab.m_Low, buildingStatusInfomodePrefab.m_High);
                }
            }
        }

        /// <summary>
        /// Get a color based on bytes, not floats.
        /// </summary>
        private Color GetColor(byte r, byte g, byte b)
        {
            return new Color(Mathf.Clamp01(r / 255f), Mathf.Clamp01(g / 255f), Mathf.Clamp01(b / 255f), 1f);
        }

        /// <summary>
        /// Multiply a color by a factor.
        /// </summary>
        private Color MultiplyColor(Color color, float multiplier)
        {
            return new Color(Mathf.Clamp01(color.r * multiplier),
                             Mathf.Clamp01(color.g * multiplier),
                             Mathf.Clamp01(color.b * multiplier),
                             1f);
        }

        /// <summary>
        /// Convert infoview name to infoview enum.
        /// </summary>
        public static BUInfoview GetInfoview(string infoviewName)
        {
            // There are few infoview datas, so just loop over them to find the one.
            foreach (BUInfoviewData infoviewData in BUInfoviewDatas.instance.Values)
            {
                if (infoviewData.InfoviewName == infoviewName)
                {
                    return infoviewData.Infoview;
                }
            }

            // Name is not for an infoview in this mod.
            return BUInfoview.None;
        }

        /// <summary>
        /// Reset data values.
        /// </summary>
        public void ResetDataValues()
        {
            buildingStatusTypeDatas.ResetDataValues();
        }
    }
}
