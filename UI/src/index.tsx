import { ModRegistrar       } from "cs2/modding";

import { InfomodeItemExtend } from "infomodeItemExtend";
import   mod                  from "../mod.json";

const register: ModRegistrar = (moduleRegistry) =>
{
    // Extend the InfomodeItem module so this mod can implement its own custom infomodes.
    moduleRegistry.extend("game-ui/game/components/infoviews/active-infoview-panel/components/infomode-item/infomode-item.tsx", "InfomodeItem", InfomodeItemExtend);

    // Registration is complete.
    console.log(mod.id + " registration complete.");
}

export default register;
