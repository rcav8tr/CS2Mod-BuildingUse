import { CSSProperties                  } from "react";

import { bindValue, trigger, useValue   } from "cs2/api";
import { infoviewTypes                  } from "cs2/bindings";
import { LocalizedNumber, Unit, UnitSettings, useLocalization } from "cs2/l10n";
import { FormattedParagraphs, Tooltip   } from "cs2/ui";

import   styles                           from "infomodeItem.module.scss";
import   mod                              from "../mod.json";
import { ModuleResolver                 } from "moduleResolver";
import { uiBindings, uiBindingNames     } from "uiBindings";
import { BUBuildingStatusType           } from "uiConstants";
import { UITranslationKey               } from "uiTranslationKey";

// Props for InfomodeItem.
export interface InfomodeItemProps
{
    infomode: infoviewTypes.Infomode;
    buildingStatusType: BUBuildingStatusType;
}

// Custom infmode item.
// Adapted from the base game's infomode logic.
export const InfomodeItem = ({ infomode, buildingStatusType }: InfomodeItemProps) =>
{
    // Translations.
    const { translate } = useLocalization();
    const infomodeTitle:      string = "" + translate("Infoviews.INFOMODE[" + infomode.id + "]");
    let   infomodeTooltip:    string = "" + translate("Infoviews.INFOMODE_TOOLTIP[" + infomode.id + "]");
    const buildingColor:      string = "" + translate("Infoviews.INFOMODE_TYPE[BuildingColor]");
    const buildings:          string = "" + translate("EconomyPanel.SERVICES_TITLE_BUILDINGS");
    const thousandsSeparator: string = "" + translate("Common.THOUSANDS_SEPARATOR", ",");
    const noBuildings:        string = "" + translate(UITranslationKey.NoBuildings, "No Buildings");

    // Get icon based on building status type.
    let icon: string;
    switch (buildingStatusType)
    {
        case BUBuildingStatusType.EmployeesResidential:
        case BUBuildingStatusType.VehiclesResidentialCar:
        case BUBuildingStatusType.EfficiencyResidential:
            icon = "Media/Game/Icons/ZoneResidential.svg";
            break;

        case BUBuildingStatusType.EmployeesCommercial:
        case BUBuildingStatusType.StorageCommercial:
        case BUBuildingStatusType.VehiclesCommercialTruck:
        case BUBuildingStatusType.EfficiencyCommercial:
            icon = "Media/Game/Icons/ZoneCommercial.svg";
            break;

        case BUBuildingStatusType.EmployeesIndustrial:
        case BUBuildingStatusType.StorageIndustrial:
        case BUBuildingStatusType.VehiclesIndustrialTruck:
        case BUBuildingStatusType.EfficiencyIndustrial:
            icon = "Media/Game/Icons/ZoneIndustrial.svg";
            break;

        case BUBuildingStatusType.EmployeesOffice:
        case BUBuildingStatusType.StorageOffice:
        case BUBuildingStatusType.VehiclesOfficeTruck:
        case BUBuildingStatusType.EfficiencyOffice:
            icon = "Media/Game/Icons/ZoneOffice.svg";
            break;

        case BUBuildingStatusType.EmployeesParking:
        case BUBuildingStatusType.VehiclesParkedCar:
        case BUBuildingStatusType.VehiclesParkedBike:
        case BUBuildingStatusType.VehiclesParkedOther:
        case BUBuildingStatusType.EfficiencyParking:
            icon = "Media/Game/Icons/Parking.svg";
            break;

        case BUBuildingStatusType.EmployeesRoadMaintenance:
        case BUBuildingStatusType.VehiclesRoadMaintenance:
        case BUBuildingStatusType.EfficiencyRoadMaintenance:
            icon = "Media/Game/Icons/RoadsServices.svg";
            break;

        case BUBuildingStatusType.EmployeesElectricity:
        case BUBuildingStatusType.StorageBatteryCharge:
        case BUBuildingStatusType.StoragePowerPlantFuel:
        case BUBuildingStatusType.EfficiencyElectricity:
        case BUBuildingStatusType.ProcessingElectricityProduction:
            icon = "Media/Game/Icons/Electricity.svg";
            break;

        case BUBuildingStatusType.EmployeesWater:
        case BUBuildingStatusType.EfficiencyWater:
        case BUBuildingStatusType.ProcessingWaterOutput:
            icon = "Media/Game/Icons/Water.svg";
            break;

        case BUBuildingStatusType.EmployeesSewage:
        case BUBuildingStatusType.EfficiencySewage:
        case BUBuildingStatusType.ProcessingSewageTreatment:
            icon = "Media/Game/Icons/Sewage.svg";
            break;

        case BUBuildingStatusType.EmployeesHealthcare:
        case BUBuildingStatusType.VisitorsHealthcare:
        case BUBuildingStatusType.StorageHealthcare:
        case BUBuildingStatusType.VehiclesAmbulance:
        case BUBuildingStatusType.VehiclesMedicalHelicopter:
        case BUBuildingStatusType.EfficiencyHealthcare:
            icon = "Media/Game/Icons/Healthcare.svg";
            break;

        case BUBuildingStatusType.EmployeesDeathcare:
        case BUBuildingStatusType.VisitorsCemetery:
        case BUBuildingStatusType.VisitorsCrematorium:
        case BUBuildingStatusType.VehiclesHearse:
        case BUBuildingStatusType.EfficiencyDeathcare:
        case BUBuildingStatusType.ProcessingCrematoriumProcessing:
            icon = "Media/Game/Icons/Deathcare.svg";
            break;

        case BUBuildingStatusType.EmployeesGarbageManagement:
        case BUBuildingStatusType.StorageLandfill:
        case BUBuildingStatusType.StorageGarbageManagement:
        case BUBuildingStatusType.VehiclesGarbageTruck:
        case BUBuildingStatusType.EfficiencyGarbageManagement:
        case BUBuildingStatusType.ProcessingGarbageProcessing:
            icon = "Media/Game/Icons/Garbage.svg";
            break;

        case BUBuildingStatusType.EmployeesEducation:
        case BUBuildingStatusType.VisitorsElementarySchool:
        case BUBuildingStatusType.VisitorsHighSchool:
        case BUBuildingStatusType.VisitorsCollege:
        case BUBuildingStatusType.VisitorsUniversity:
        case BUBuildingStatusType.EfficiencyEducation:
            icon = "Media/Game/Icons/Education.svg";
            break;

        case BUBuildingStatusType.EmployeesResearch:
        case BUBuildingStatusType.EfficiencyResearch:
            icon = "Media/Game/Icons/Research.svg";
            break;

        case BUBuildingStatusType.EmployeesFireRescue:
        case BUBuildingStatusType.VehiclesFireEngine:
        case BUBuildingStatusType.VehiclesFireHelicopter:
        case BUBuildingStatusType.EfficiencyFireRescue:
            icon = "Media/Game/Icons/FireSafety.svg";
            break;

        case BUBuildingStatusType.EmployeesDisasterControl:
        case BUBuildingStatusType.VisitorsEmergencyShelter:
        case BUBuildingStatusType.StorageEmergencyShelter:
        case BUBuildingStatusType.VehiclesDisasterResponse:
        case BUBuildingStatusType.VehiclesEvacuationBus:
        case BUBuildingStatusType.EfficiencyDisasterControl:
            icon = "Media/Game/Icons/DisasterControl.svg";
            break;

        case BUBuildingStatusType.EmployeesPolice:
        case BUBuildingStatusType.VisitorsPoliceStation:
        case BUBuildingStatusType.VisitorsPrison:
        case BUBuildingStatusType.VehiclesPoliceCar:
        case BUBuildingStatusType.VehiclesPoliceHelicopter:
        case BUBuildingStatusType.VehiclesPrisonVan:
        case BUBuildingStatusType.EfficiencyPolice:
            icon = "Media/Game/Icons/Police.svg";
            break;

        case BUBuildingStatusType.EmployeesAdministration:
        case BUBuildingStatusType.EfficiencyAdministration:
            icon = "Media/Game/Icons/Administration.svg";
            break;

        case BUBuildingStatusType.EmployeesTransportation:
        case BUBuildingStatusType.EfficiencyTransportation:
            icon = "Media/Game/Icons/Transportation.svg";
            break;

        case BUBuildingStatusType.EmployeesParkMaintenance:
        case BUBuildingStatusType.VehiclesParkMaintenance:
        case BUBuildingStatusType.EfficiencyParkMaintenance:
            icon = "Media/Game/Icons/ParkMaintenance.svg";
            break;

        case BUBuildingStatusType.EmployeesParksRecreation:
        case BUBuildingStatusType.EfficiencyParksRecreation:
            icon = "Media/Game/Icons/ParksAndRecreation.svg";
            break;

        case BUBuildingStatusType.EmployeesPost:
        case BUBuildingStatusType.StoragePost:
        case BUBuildingStatusType.StorageMailbox:
        case BUBuildingStatusType.VehiclesPost:
        case BUBuildingStatusType.EfficiencyPost:
        case BUBuildingStatusType.ProcessingMailSortingSpeed:
            icon = "Media/Game/Icons/PostService.svg";
            break;

        case BUBuildingStatusType.EmployeesTelecom:
        case BUBuildingStatusType.EfficiencyTelecom:
            icon = "Media/Game/Icons/Communications.svg";
            break;

        case BUBuildingStatusType.VehiclesBus:
            icon = "Media/Game/Icons/Bus.svg";
            break;

        case BUBuildingStatusType.VehiclesTaxi:
            icon = "Media/Game/Icons/Taxi.svg";
            break;

        case BUBuildingStatusType.VehiclesTrain:
            icon = "Media/Game/Icons/Train.svg";
            break;

        case BUBuildingStatusType.VehiclesTram:
            icon = "Media/Game/Icons/Tram.svg";
            break;

        case BUBuildingStatusType.VehiclesSubway:
            icon = "Media/Game/Icons/Subway.svg";
            break;

        case BUBuildingStatusType.VehiclesFerry:
            icon = "Media/Game/Icons/Ferry.svg";
            break;

        case BUBuildingStatusType.StorageCargoTransportation:
        case BUBuildingStatusType.VehiclesCargoStationTruck:
            icon = "Media/Game/Icons/CargoTruck.svg";
            break;

        default:
            // Check for production building status types.
            const buildingStatusTypeEnumName: string = BUBuildingStatusType[buildingStatusType];
            if (buildingStatusTypeEnumName.startsWith("Production"))
            {
                // Get icon based on production building status type.
                // This logic assumes all building status type enum names following "Production" are the same as the resource file names.
                const resourceName = buildingStatusTypeEnumName.substring("Production".length);
                icon = "Media/Game/Resources/" + resourceName + ".svg";
            }
            else
            {
                // Use dummy icon.
                icon = "Media/Game/Icons/Zones.svg";
            }
            break;
    }

    // Get data bindings for this building status type.
    const buildingStatusTypeEnumName: string = BUBuildingStatusType[buildingStatusType];
    const bindingUsed     = uiBindings[buildingStatusTypeEnumName + "Used"];
    const bindingCapacity = uiBindings[buildingStatusTypeEnumName + "Capacity"];
    const bindingCount    = uiBindings[buildingStatusTypeEnumName + "Count"];

    // Get used, capacity, and count values from data bindings.
    let used:     number = useValue(bindingUsed);
    let capacity: number = useValue(bindingCapacity);
    let count:    number = useValue(bindingCount);

    // If count is more than zero, then the building status type has buildings.
    const hasBuildings: boolean = count > 0;

    // Compute used as a percent of capacity, not more than 999%.
    const percent: number = capacity > 0 ? Math.min(Math.round(100 * used / capacity), 999) : 0;

    // Style to set gradient background based on gradient colors in the infomode.
    const gradientStyle: Partial<CSSProperties> =
    {
        background: infomode.gradientLegend && infomode.gradientLegend.gradient && infomode.gradientLegend.gradient.stops ?
            ModuleResolver.instance.BuildCssLinearGradient(infomode.gradientLegend.gradient) :
            "linear-gradient(to right, rgba(128, 128, 128, 1) 0%, rgba(128, 128, 128, 1) 100%)"
    }

    // Get whether max color is 100% or 200%.
    let maxColorPercent: number = 100;
    const isEfficiency: boolean = buildingStatusTypeEnumName.startsWith("Efficiency");
    const isProduction: boolean = buildingStatusTypeEnumName.startsWith("Production");
    if (isEfficiency || isProduction)
    {
        // Get max color setting.
        const bindingName: string = isEfficiency ? uiBindingNames.EfficiencyMaxColor200Percent : uiBindingNames.ProductionMaxColor200Percent;
        const bindingMaxColor200Percent = bindValue<boolean>(mod.id, bindingName, false);
        const valueMaxColor200Percent: boolean = useValue(bindingMaxColor200Percent);
        if (valueMaxColor200Percent)
        {
            maxColorPercent = 200;
        }
    }

    // Style to move pointer to correct position based on percent, but no more than 100%.
    const pointerStyle: Partial<CSSProperties> =
    {
        left: Math.min(100, 100 * percent / maxColorPercent) + "%"
    }

    // For building status types that include a unit of measure, adjust used and capacity manually and append unit of measure (UOM) to tooltip.
    // Used and capacity are adjusted manually here because in some cases the game's default for the UOM does not have a value reduction (e.g. kilo, mega).
    // UOM is appended to the tooltip because the text of most UOMs is too long to include with the value (e.g. m3/month).
    // Furthermore, as of game version 1.1.11f1, some of the game's number formatting has errors when used with UOMs.
    const UOMPlaceholder: string = "(UOM)";
    if (infomodeTooltip?.includes(UOMPlaceholder))
    {
        // Get whether or not unit system is metric.
        const bindingUnitSettings = bindValue<UnitSettings>("options", "unitSettings");
        const unitSettings = useValue(bindingUnitSettings);
        const unitSystemMetric: number = 0;  // UnitSystem.Metric is 0.
        const isMetric: boolean = (unitSettings.unitSystem === unitSystemMetric);

        // Define pounds per kilogram.
        const poundsPerKilogram = 2.204622622;

        // For each building status type, get scaling factor, UOM prefix, and UOM text.
        // These cases must match the infomode tooltips that have the UOM placeholder.
        let scalingFactor: number = 1;
        let uomPrefix: string | null = null;
        let uomText: string | null = null;
        switch (buildingStatusType)
        {
            case BUBuildingStatusType.StorageCommercial:
            case BUBuildingStatusType.StorageIndustrial:
            case BUBuildingStatusType.StorageOffice:
            case BUBuildingStatusType.StoragePowerPlantFuel:
            case BUBuildingStatusType.StorageHealthcare:
            case BUBuildingStatusType.StorageLandfill:
            case BUBuildingStatusType.StorageGarbageManagement:
            case BUBuildingStatusType.StorageEmergencyShelter:
            case BUBuildingStatusType.StorageCargoTransportation:
                // Logic adapted from index.js for displaying weight.
                if (isMetric)
                {
                    // Check scale.
                    if (capacity < 100000)
                    {
                        // No scaling needed.
                        uomText = translate(ModuleResolver.instance.Loc.Common.VALUE_KILOGRAM.displayName);
                    }
                    else if (capacity < 100000000)
                    {
                        // Convert kg to tons.
                        scalingFactor = 1000;
                        uomText = translate(ModuleResolver.instance.Loc.Common.VALUE_TON.displayName);
                    }
                    else
                    {
                        // Convert kg to kilo tons.
                        scalingFactor = 1000000;
                        uomText = translate(ModuleResolver.instance.Loc.Common.VALUE_KILOTON.displayName);
                    }
                }
                else
                {
                    // Convert kg to pounds.
                    used     *= poundsPerKilogram;
                    capacity *= poundsPerKilogram;

                    // Check scale.
                    if (capacity < 100000)
                    {
                        // No scaling needed.
                        uomText = translate(ModuleResolver.instance.Loc.Common.VALUE_POUND.displayName);
                    }
                    else if (capacity < 200000000)
                    {
                        // Convert pounds to short tons.
                        scalingFactor = 2000;
                        uomText = translate(ModuleResolver.instance.Loc.Common.VALUE_SHORT_TON.displayName);
                    }
                    else
                    {
                        // Convert pounds to kilo short tons.
                        scalingFactor = 2000000;
                        uomText = translate(ModuleResolver.instance.Loc.Common.VALUE_SHORT_KILOTON.displayName);
                    }
                }
                uomText = "" + uomText?.replace("{SIGN}{VALUE}", "");
                break;

            case BUBuildingStatusType.StorageBatteryCharge:
                // Logic adapted from index.js for displaying energy.
                used     /= 10000;
                capacity /= 10000;
                if (capacity < 100000)
                {
                    uomText = translate(ModuleResolver.instance.Loc.Common.VALUE_MEGAWATT_HOURS.displayName);
                    uomText = "" + uomText?.replace("{SIGN}{VALUE}", "");
                }
                else
                {
                    scalingFactor = 1000;
                    uomText = translate(UITranslationKey.GigaWattHour);
                }
                break;

            case BUBuildingStatusType.ProcessingElectricityProduction:
                // Logic adapted from index.js for displaying power.
                if (Math.abs(capacity) < 100000)
                {
                    scalingFactor = 10;
                    uomText = translate(ModuleResolver.instance.Loc.Common.VALUE_KILOWATT.displayName);
                }
                else
                {
                    scalingFactor = 10000;
                    uomText = translate(ModuleResolver.instance.Loc.Common.VALUE_MEGAWATT.displayName);
                }
                uomText = "" + uomText?.replace("{SIGN}{VALUE}", "");
                break;

            case BUBuildingStatusType.ProcessingWaterOutput:
            case BUBuildingStatusType.ProcessingSewageTreatment:
                // Logic adapted from index.js for displaying volume per month.
                if (isMetric)
                {
                    uomText = translate(ModuleResolver.instance.Loc.Common.VALUE_CUBIC_METER_PER_MONTH.displayName);
                }
                else
                {
                    uomText = translate(ModuleResolver.instance.Loc.Common.VALUE_GALLON_PER_MONTH.displayName);

                    // Convert cubic meters per month to gallons per month.
                    const gallonsPerCubicMeter: number = 264.172;
                    used     *= gallonsPerCubicMeter;
                    capacity *= gallonsPerCubicMeter;
                }
                uomText = "" + uomText?.replace("{SIGN}{VALUE}", "");

                // Check for scaling.
                if (capacity < 100000)
                {
                    // No scaling needed.
                }
                else if (capacity < 100000000)
                {
                    scalingFactor = 1000;
                    uomPrefix = translate(UITranslationKey.UnitOfMeasurePrefixKilo);
                }
                else
                {
                    scalingFactor = 1000000;
                    uomPrefix = translate(UITranslationKey.UnitOfMeasurePrefixMega);
                }
                break;

            case BUBuildingStatusType.ProcessingCrematoriumProcessing:
                // Logic adapted from index.js for displaying bodies per month.
                uomText = translate(ModuleResolver.instance.Loc.Common.FRACTION_BODIES_PER_MONTH.displayName);
                uomText = "" + uomText?.replace("{VALUE} / {TOTAL}", "").replace("{VALUE}/{TOTAL}", "");

                // Check for scaling.
                if (capacity < 100000)
                {
                    // No scaling needed.
                }
                else
                {
                    scalingFactor = 1000;
                    uomPrefix = translate(UITranslationKey.UnitOfMeasurePrefixKilo);
                }
                break;

            case BUBuildingStatusType.ProcessingGarbageProcessing:
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
                // Logic adapted from index.js for displaying weight per month.
                if (isMetric)
                {
                    // Check scale.
                    if (capacity < 100000)
                    {
                        // No scaling needed.
                        uomText = translate(ModuleResolver.instance.Loc.Common.VALUE_KG_PER_MONTH.displayName);
                    }
                    else if (capacity < 100000000)
                    {
                        // Convert kg to tons.
                        scalingFactor = 1000;
                        uomText = translate(ModuleResolver.instance.Loc.Common.VALUE_TON_PER_MONTH.displayName);
                    }
                    else
                    {
                        // Convert kg to kilo tons.
                        scalingFactor = 1000000;
                        uomPrefix = translate(UITranslationKey.UnitOfMeasurePrefixKilo);
                        uomText = translate(ModuleResolver.instance.Loc.Common.VALUE_TON_PER_MONTH.displayName);
                    }
                }
                else
                {
                    // Convert kg to pounds.
                    used     *= poundsPerKilogram;
                    capacity *= poundsPerKilogram;

                    // Check scale.
                    if (capacity < 100000)
                    {
                        // No scaling needed.
                        uomText = translate(ModuleResolver.instance.Loc.Common.VALUE_POUND_PER_MONTH.displayName);
                    }
                    else if (capacity < 200000000)
                    {
                        // Convert pounds to short tons.
                        scalingFactor = 2000;
                        uomText = translate(ModuleResolver.instance.Loc.Common.VALUE_SHORT_TON_PER_MONTH.displayName);
                    }
                    else
                    {
                        // Convert pounds to kilo short tons.
                        scalingFactor = 2000000;
                        uomPrefix = translate(UITranslationKey.UnitOfMeasurePrefixKilo);
                        uomText = translate(ModuleResolver.instance.Loc.Common.VALUE_SHORT_TON_PER_MONTH.displayName);
                    }
                }
                uomText = "" + uomText?.replace("{SIGN}{VALUE}", "");
                break;

            case BUBuildingStatusType.ProcessingMailSortingSpeed:
            case BUBuildingStatusType.ProductionSoftware:
            case BUBuildingStatusType.ProductionTelecom:
            case BUBuildingStatusType.ProductionFinancial:
            case BUBuildingStatusType.ProductionMedia:
                // Logic adapted from index.js for displaying integer per month.
                uomText = translate(ModuleResolver.instance.Loc.Common.VALUE_PER_MONTH.displayName);
                uomText = "" + uomText?.replace("{SIGN}{VALUE}", "");

                // Check for scaling.
                if (capacity < 100000)
                {
                    // No scaling needed.
                }
                else if (capacity < 100000000)
                {
                    scalingFactor = 1000;
                    uomPrefix = translate(UITranslationKey.UnitOfMeasurePrefixKilo);
                }
                else
                {
                    scalingFactor = 1000000;
                    uomPrefix = translate(UITranslationKey.UnitOfMeasurePrefixMega);
                }
                break;
        }

        // Apply scaling factor to used and capacity.
        used     = Math.round(used     / scalingFactor);
        capacity = Math.round(capacity / scalingFactor);

        // Replace the UOM placeholder with UOM prefix and UOM text.
        if (uomText)
        {
            infomodeTooltip = infomodeTooltip.replace(UOMPlaceholder, "(" + (uomPrefix && uomPrefix.length > 0 ? uomPrefix + " " : "") + uomText?.trim() + ")")
        }
    }

    // Append buildings count to tooltip.
    // Formatting logic adapted from the game's index.js for localized numbers.
    const regexReplacement = /\B(?=(\d{3})+(?!\d))/g;
    const formattedCount: string = count.toFixed(0).replace(regexReplacement, thousandsSeparator);
    infomodeTooltip += "\n" + buildings + ": " + formattedCount;

    // Function to join classes.
    function joinClasses(...classes: any) { return classes.join(" "); }

    // Handle button click.
    function onButtonClick()
    {
        trigger("audio", "playSound", ModuleResolver.instance.UISound.toggleInfoMode, 1);
        trigger("infoviews", "setInfomodeActive", infomode.entity, !infomode.active, infomode.priority);
    }

    // Mostly adapted from the base game building status color infomode with the following general changes:
    //      Use element styles to override default base game appearance, mostly for making the infomode more compact.
    //      Add an icon before the title.
    //      Add a pointer on the gradient to indicate current city-wide building use level.
    //      Add text for percent, used, and capacity.
    //      Replace gradient, percent, used, and capacity with text when there are no buildings.
    //      Show building count in tool tip.
    return (
        <Tooltip direction="right" tooltip={<FormattedParagraphs children={infomodeTooltip} />} >
            <button
                className={joinClasses(ModuleResolver.instance.TransparentButtonClasses.button,
                                       ModuleResolver.instance.InfomodeItemClasses.infomodeItem,
                                       (infomode.active ? ModuleResolver.instance.InfomodeItemClasses.active : ""),
                                       styles.buildingUseInfomodeButton)}
                onClick={() => onButtonClick()}
            >
                <div className={ModuleResolver.instance.InfomodeItemClasses.header}>
                    <div className={ModuleResolver.instance.InfomodeItemClasses.title}>
                        <img className={styles.buildingUseInfomodeIcon} src={icon} />
                        <div className={ModuleResolver.instance.InfomodeItemClasses.titleText}>{infomodeTitle}</div>
                    </div>
                    <div className={ModuleResolver.instance.InfomodeItemClasses.type}>
                        {buildingColor}
                        <div className={joinClasses(ModuleResolver.instance.CheckboxClasses.toggle,
                                                    ModuleResolver.instance.InfomodeItemClasses.checkbox,
                                                    (infomode.active ? "checked" : "unchecked"))}>
                            <div className={joinClasses(ModuleResolver.instance.CheckboxClasses.checkmark,
                                                        (infomode.active ? "checked" : ""))}>
                            </div>
                        </div>
                    </div>
                </div>
                <div className={joinClasses(ModuleResolver.instance.InfomodeItemClasses.legend,
                                            styles.buildingUseInfomodeLegend)}>
                    {
                        hasBuildings &&
                        (
                            <>
                                <div className={joinClasses(ModuleResolver.instance.InfomodeItemClasses.gradient,
                                                            styles.buildingUseInfomodeGradient)} style={gradientStyle}>
                                    <div className={styles.buildingUseInfomodePointer} style={pointerStyle}>
                                        <img className={styles.buildingUseInfomodePointerIcon} src="Media/Misc/IndicatorBarPointer.svg" />
                                    </div>
                                </div>
                                <div className={joinClasses(ModuleResolver.instance.InfomodeItemClasses.label,
                                                            styles.buildingUseInfomodePercent)}>
                                    {percent + "%"}
                                </div>
                                <div className={joinClasses(ModuleResolver.instance.InfomodeItemClasses.label,
                                                            styles.buildingUseInfomodeValue)}>
                                    <LocalizedNumber value={used} unit={Unit.Integer} />
                                </div>
                                <div className={joinClasses(ModuleResolver.instance.InfomodeItemClasses.label,
                                                            styles.buildingUseInfomodeValue)}>
                                    <LocalizedNumber value={capacity} unit={Unit.Integer} />
                                </div>
                            </>
                        )
                    }
                    {
                        hasBuildings ||
                        (
                            <div className={joinClasses(ModuleResolver.instance.InfomodeItemClasses.label,
                                                        styles.buildingUseInfomodeValueNoBuildings)}>
                                {noBuildings}
                            </div>
                        )
                    }
                </div>
            </button>
        </Tooltip>
    );
}
