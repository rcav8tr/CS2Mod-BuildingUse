import { bindValue, useValue            } from "cs2/api";
import { useLocalization                } from "cs2/l10n";

import { InfomodeCheckBox               } from "infomodeCheckBox";
import   styles                           from "infomodeCheckBoxRow.module.scss";
import   mod                              from "../mod.json";
import { ModuleResolver                 } from "moduleResolver";
import { uiBindingNames, uiEventNames   } from "uiBindings";
import { UITranslationKey               } from "uiTranslationKey";

// Custom infmode item for efficiency max color check boxes.
export const EfficiencyMaxColor = () =>
{
    // Translations.
    const { translate } = useLocalization();
    const labelMaxColor100Percent   = translate(UITranslationKey.MaxColor100Percent);
    const labelMaxColor200Percent   = translate(UITranslationKey.MaxColor200Percent);
    const tooltipMaxColor           = translate(UITranslationKey.MaxColorTooltip);

    // Get data binding.
    const bindingEfficiencyMaxColor200Percent = bindValue<boolean>(mod.id, uiBindingNames.EfficiencyMaxColor200Percent, false);

    // Get data value from data binding.
    const valueEfficiencyMaxColor200Percent: boolean = useValue(bindingEfficiencyMaxColor200Percent);

    // A row with two check boxes.
    return (
        <ModuleResolver.instance.Tooltip
            direction="right"
            tooltip={<ModuleResolver.instance.FormattedParagraphs children={tooltipMaxColor} />}
            theme={ModuleResolver.instance.TooltipClasses}
            children=
            {
                <div className={styles.buildingUseInfomodeCheckBoxRow}>
                    <InfomodeCheckBox onClickEventName={uiEventNames.EfficiencyMaxColorClicked} isChecked={!valueEfficiencyMaxColor200Percent} label={labelMaxColor100Percent} />
                    <InfomodeCheckBox onClickEventName={uiEventNames.EfficiencyMaxColorClicked} isChecked={ valueEfficiencyMaxColor200Percent} label={labelMaxColor200Percent} />
                </div>
            }
        />
    );
}
