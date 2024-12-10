import { trigger, useValue          } from "cs2/api";
import { ActiveInfoview, infoview   } from "cs2/bindings";
import { useLocalization            } from "cs2/l10n";

import { ModuleResolver             } from "moduleResolver";
import   styles                       from "selectDeselect.module.scss";
import { UITranslationKey           } from "uiTranslationKey";

// Custom infomode item for select all and deselect all buttons.
export const SelectDeselect = () =>
{
    // Translations.
    const { translate } = useLocalization();
    const selectAllLabel        = translate(UITranslationKey.SelectAll);
    const deselectAllLabel      = translate(UITranslationKey.DeselectAll);
    const selectDeselectTooltip = translate(UITranslationKey.SelectDeselectTooltip);

    // Get active infoview (must be outside of onButtonClick handler).
    const activeInfoview: ActiveInfoview | null = useValue(infoview.activeInfoview$);

    // Handle button click.
    function onButtonClick(select: boolean)
    {
        // Check active infoview.
        if (activeInfoview)
        {
            trigger("audio", "playSound", ModuleResolver.instance.UISound.toggleInfoMode, 1);

            // Do each infomode in the active infoview.
            // This select/deselect infomode will also be set to active or inactive.
            // This is okay because the operation of this infomode is not dependent on its active status.
            for (let i: number = 0; i < activeInfoview.infomodes.length; i++)
            {
                // Trigger only infomodes that will change status.
                const infomode = activeInfoview.infomodes[i];
                if (infomode.active != select)
                {
                    trigger("infoviews", "setInfomodeActive", infomode.entity, select, infomode.priority)
                }
            }
        }
    }

    // A row with two buttons.
    return (
        <ModuleResolver.instance.Tooltip
            direction="right"
            tooltip={<ModuleResolver.instance.FormattedParagraphs children={selectDeselectTooltip} />}
            theme={ModuleResolver.instance.TooltipClasses}
            children=
            {
                <div className={styles.buildingUseSelectDeselectRow}>
                    <button className={styles.buildingUseSelectDeselectButton} onClick={() => onButtonClick(true )}>{selectAllLabel  }</button>
                    <button className={styles.buildingUseSelectDeselectButton} onClick={() => onButtonClick(false)}>{deselectAllLabel}</button>
                </div>
            }
        />
    );
}