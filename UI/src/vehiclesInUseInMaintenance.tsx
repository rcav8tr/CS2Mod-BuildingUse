import { bindValue, useValue            } from "cs2/api";
import { useLocalization                } from "cs2/l10n";
import { FormattedParagraphs, Tooltip   } from "cs2/ui";

import { InfomodeCheckBox               } from "infomodeCheckBox";
import   styles                           from "infomodeCheckBoxRow.module.scss";
import   mod                              from "../mod.json";
import { uiBindingNames, uiEventNames   } from "uiBindings";
import { UITranslationKey               } from "uiTranslationKey";

// Get data bindings.
const bindingCountVehiclesInUse         = bindValue<boolean>(mod.id, uiBindingNames.CountVehiclesInUse,         true);
const bindingCountVehiclesInMaintenance = bindValue<boolean>(mod.id, uiBindingNames.CountVehiclesInMaintenance, true);

// Custom infmode item for vehicles in use/in maintenance check boxes.
export const VehiclesInUseInMaintenance = () =>
{
    // Translations.
    const { translate } = useLocalization();
    const labelInUse                = translate(UITranslationKey.InUse);
    const labelInMaintenance        = translate(UITranslationKey.InMaintenance);
    const tooltipInUseInMaintenance = translate(UITranslationKey.InUseInMaintenanceTooltip);

    // Get data values from data bindings.
    const valueCountVehiclesInUse:         boolean = useValue(bindingCountVehiclesInUse);
    const valueCountVehiclesInMaintenance: boolean = useValue(bindingCountVehiclesInMaintenance);

    // A row with two check boxes.
    return (
        <Tooltip direction="right" tooltip={<FormattedParagraphs children={tooltipInUseInMaintenance} />} >
            <div className={styles.buildingUseInfomodeCheckBoxRow}>
                <InfomodeCheckBox onClickEventName={uiEventNames.CountVehiclesInUseClicked        } isChecked={valueCountVehiclesInUse        } label={labelInUse        } />
                <InfomodeCheckBox onClickEventName={uiEventNames.CountVehiclesInMaintenanceClicked} isChecked={valueCountVehiclesInMaintenance} label={labelInMaintenance} />
            </div>
        </Tooltip>
    );
}
