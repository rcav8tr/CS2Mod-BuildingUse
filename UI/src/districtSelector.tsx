import { bindValue, useValue, trigger           } from "cs2/api";
import { useLocalization                        } from "cs2/l10n";
import { Dropdown, DropdownItem, DropdownToggle } from "cs2/ui";
import { Entity                                 } from "cs2/utils";

import   styles                                   from "districtSelector.module.scss";
import   mod                                      from "../mod.json";
import { ModuleResolver                         } from "moduleResolver";
import { uiBindingNames, uiEventNames           } from "uiBindings";
import { UITranslationKey                       } from "uiTranslationKey";

// Define district info passed from C#.
type DistrictInfo =
    {
        entity: Entity;
        name: string;
    }

// Define district bindings.
const bindingDistrictInfos    = bindValue<DistrictInfo[]>(mod.id, uiBindingNames.DistrictInfos);
const bindingSelectedDistrict = bindValue<Entity        >(mod.id, uiBindingNames.SelectedDistrict, { index: 0, version: 0 });

// Custom infomode item for selecting a district.
export const DistrictSelector = () =>
{
    // Get district infos and selected district.
    const districtInfos: DistrictInfo[]  = useValue(bindingDistrictInfos);
    const selectedDistrictEntity: Entity = useValue(bindingSelectedDistrict)

    // If there are no districts in the city besides the entire city entry, then do not display the district selector.
    if (districtInfos.length <= 1)
    {
        return (<></>);
    }

    // Translations.
    const { translate } = useLocalization();
    const districtSelectorTooltip = translate(UITranslationKey.DistrictSelectorTooltip);

    // Create a dropdown item for each district info and get the name of the selected district.
    var selectedDistrictName: string = "";
    const districtDropdownItems: JSX.Element[] = districtInfos.map
    (
        (districtInfo: DistrictInfo) =>
        {
            // Get district entity and name from the district info.
            const districtEntity: Entity = districtInfo.entity;
            const districtName:   string = districtInfo.name;

            // Check if this district info is for the selected district.
            const selected: boolean =
                districtEntity.index   === selectedDistrictEntity.index &&
                districtEntity.version === selectedDistrictEntity.version;

            // Get the name of the selected district.
            if (selected)
            {
                selectedDistrictName = districtName;
            }

            // Return a dropdown item.
            return (
                <DropdownItem
                    theme={ModuleResolver.instance.DropdownClasses}
                    value={districtName}
                    closeOnSelect={true}
                    selected={selected}
                    className={selected ? styles.selectedDistrictDropdownItem : ""}
                    onChange={() => trigger(mod.id, uiEventNames.SelectedDistrictChanged, districtEntity) }
                >
                    {districtName}
                </DropdownItem>
            );
        }
    );

    // Entire row is a single dropdown of districts.
    return (
        <ModuleResolver.instance.Tooltip
            direction="right"
            tooltip={<ModuleResolver.instance.FormattedParagraphs children={districtSelectorTooltip} />}
            theme={ModuleResolver.instance.TooltipClasses}
            children=
            {
                <div className={styles.districtDropdownRow}>
                    <Dropdown
                        theme={ModuleResolver.instance.DropdownClasses}
                        content={districtDropdownItems}>
                        <DropdownToggle>
                            {selectedDistrictName}
                        </DropdownToggle>
                    </Dropdown>
                </div>
            }
        />
    );
}