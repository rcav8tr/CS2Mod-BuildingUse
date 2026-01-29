import { useLocalization        } from "cs2/l10n";

import   styles                   from "heading.module.scss";
import { BUBuildingStatusType   } from "uiConstants";

// Props for Heading.
export interface HeadingProps
{
    buildingStatusType: BUBuildingStatusType;
}

// Custom infomode item for headings.
export const Heading = ({ buildingStatusType }: HeadingProps) =>
{
    // Get translation for heading text.
    const { translate } = useLocalization();
    let headingText: string;
    switch (buildingStatusType)
    {
        case BUBuildingStatusType.ProductionHeadingRawMaterials:    headingText = "" + translate("EconomyPanel.RESOURCE_CATEGORY[Raw Materials]"   ); break;
        case BUBuildingStatusType.ProductionHeadingProcessedGoods:  headingText = "" + translate("EconomyPanel.RESOURCE_CATEGORY[Processed Goods]" ); break;
        case BUBuildingStatusType.ProductionHeadingImmaterialGoods: headingText = "" + translate("EconomyPanel.RESOURCE_CATEGORY[Immaterial Goods]"); break;
        default:                                                    headingText = "Unhandled heading type."; break;
    }

    // A horizontal line with the heading text below.
    return (
        <>
            <hr className={styles.buildingUseHeadingLine} />
            <div className={styles.buildingUseHeadingText}>{headingText}</div>
        </>
    );
}