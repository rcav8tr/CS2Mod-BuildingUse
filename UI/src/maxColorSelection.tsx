import { bindValue, useValue            } from "cs2/api";
import { useLocalization                } from "cs2/l10n";
import { FormattedParagraphs, Tooltip   } from "cs2/ui";

import { InfomodeCheckBox               } from "infomodeCheckBox";
import   styles                           from "infomodeCheckBoxRow.module.scss";
import   mod                              from "../mod.json";
import { uiBindingNames, uiEventNames   } from "uiBindings";
import { BUBuildingStatusType           } from "uiConstants";
import { UITranslationKey               } from "uiTranslationKey";

// Props for MaxColorSelection.
export interface MaxColorSelectionProps
{
    buildingStatusType: BUBuildingStatusType;
}

// Custom infmode item for max color check boxes.
export const MaxColorSelection = ({ buildingStatusType }: MaxColorSelectionProps) =>
{
    // Translations.
    const { translate } = useLocalization();
    const labelMaxColor100Percent   = translate(UITranslationKey.MaxColor100Percent);
    const labelMaxColor200Percent   = translate(UITranslationKey.MaxColor200Percent);
    const tooltipMaxColor           = translate(UITranslationKey.MaxColorTooltip);

    // Get data binding.
    const bindingName: string = buildingStatusType === BUBuildingStatusType.EfficiencyMaxColor ?
        uiBindingNames.EfficiencyMaxColor200Percent :
        uiBindingNames.ProductionMaxColor200Percent;
    const bindingMaxColor200Percent = bindValue<boolean>(mod.id, bindingName, false);

    // Get data value from data binding.
    const valueMaxColor200Percent: boolean = useValue(bindingMaxColor200Percent);

    // Get event name.
    const eventName: string = buildingStatusType === BUBuildingStatusType.EfficiencyMaxColor ?
        uiEventNames.EfficiencyMaxColorClicked :
        uiEventNames.ProductionMaxColorClicked;

    // A row with two check boxes.
    return (
        <Tooltip direction="right" tooltip={<FormattedParagraphs children={tooltipMaxColor} />} >
            <div className={styles.buildingUseInfomodeCheckBoxRow}>
                <InfomodeCheckBox onClickEventName={eventName} isChecked={!valueMaxColor200Percent} label={labelMaxColor100Percent} />
                <InfomodeCheckBox onClickEventName={eventName} isChecked={ valueMaxColor200Percent} label={labelMaxColor200Percent} />
            </div>
        </Tooltip>
    );
}
