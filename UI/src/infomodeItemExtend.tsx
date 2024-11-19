import { infoviewTypes              } from "cs2/bindings";
import { ModuleRegistryExtend       } from "cs2/modding";

import { EfficiencyMaxColor         } from "efficiencyMaxColor";
import { InfomodeItem               } from "infomodeItem";
import   mod                          from "../mod.json";
import { SelectDeselect             } from "selectDeselect";
import { BUBuildingStatusType       } from "uiConstants";
import { VehiclesInUseInMaintenance } from "vehiclesInUseInMaintenance";

// InfomodeItem extension.
export const InfomodeItemExtend: ModuleRegistryExtend = (Component: any) =>
{
    return (props) =>
    {
        // Get building status type from the infomode.
        const infomode: infoviewTypes.Infomode = props.infomode;
        const buildingStatusTypeName: string = infomode.id;
        var buildingStatusType: BUBuildingStatusType = BUBuildingStatusType.None;
        if (buildingStatusTypeName && buildingStatusTypeName.startsWith(mod.id))
        {
            const buildingStatusTypeNameSuffix: string = buildingStatusTypeName.substring(mod.id.length);
            buildingStatusType = BUBuildingStatusType[buildingStatusTypeNameSuffix as keyof typeof BUBuildingStatusType];
        }

        // Check if building status type is for this mod.
        if (buildingStatusType !== BUBuildingStatusType.None)
        {
            // Building status type is for this mod.

            // Check for special case for select/deselect buttons.
            if (buildingStatusTypeName.endsWith("SelectDeselect"))
            {
                return (<SelectDeselect />);
            }
            // Check for special case for vehicles in use/in maintenance.
            else if (buildingStatusType === BUBuildingStatusType.VehiclesInUseInMaintenance)
            {
                return (<VehiclesInUseInMaintenance />);
            }
            // Check for special case for efficiency max color.
            else if (buildingStatusType === BUBuildingStatusType.EfficiencyMaxColor)
            {
                return (<EfficiencyMaxColor />);
            }
            else
            {
                // Not a special case.
                // Return this mod's custom infomode item.
                return (<InfomodeItem infomode={infomode} buildingStatusType={buildingStatusType} />);
            }
        }

        // Building status type is not for this mod.
        // Return original InfomodeItem unchanged.
        return (<Component {...props} />);
    }
}
