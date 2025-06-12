using Game.Prefabs;
using Game.Tools;
using Game.UI.InGame;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace BuildingUse
{
    /// <summary>
    /// Collection of infoview data.
    /// </summary>
    public class BUInfoviewDatas : Dictionary<BUInfoview, BUInfoviewData>
    {
        // Use singleton pattern:  there can be only one BUInfoviewDatas in the mod.
        private static readonly BUInfoviewDatas _instance = new BUInfoviewDatas();
        public static BUInfoviewDatas instance { get { return _instance; } }
        private BUInfoviewDatas() { Initialize(); }

        /// <summary>
        /// Initialize infoview datas.
        /// </summary>
        private void Initialize()
        {
            Mod.log.Info($"{nameof(BUInfoviewDatas)}.{nameof(Initialize)}");
            
            try
            {
                // The game's infoviews must be created before this mod's infoviews.
                // That will be normal case, but perform the check anyway just in case.
                World defaultWorld = World.DefaultGameObjectInjectionWorld;
                InfoviewInitializeSystem infoviewInitializeSystem = defaultWorld.GetOrCreateSystemManaged<InfoviewInitializeSystem>();
                if (infoviewInitializeSystem == null || infoviewInitializeSystem.infoviews.Count() == 0)
                {
                    Mod.log.Error("The game's infoviews must be created before this mod's infoviews.");
                    return;
                }

                // Get list of infoview group numbers already used by the base game or
                // by other mods that added their own infoview group(s) before this mod.
                List<int> usedInfoGroupNumbers = new List<int>();
                foreach (InfoviewPrefab infoviewPrefab in infoviewInitializeSystem.infoviews)
                {
                    if (!usedInfoGroupNumbers.Contains(infoviewPrefab.m_Group))
                    {
                        usedInfoGroupNumbers.Add(infoviewPrefab.m_Group);
                    }
                }

                // Get the first infoview group number not already used.
                int infoviewGroupNumber = 1;
                while (usedInfoGroupNumbers.Contains(infoviewGroupNumber))
                {
                    infoviewGroupNumber++;
                }

                // Do each infoview.
                int infoviewPriority = 1;
                foreach (BUInfoview infoview in Enum.GetValues(typeof(BUInfoview)))
                {
                    // Skip None.
                    if (infoview != BUInfoview.None)
                    {
                        // Add a new infoview data.
                        Add(infoview, new BUInfoviewData(infoview, infoviewGroupNumber, infoviewPriority++));

                        // Start a new infoview group after Vehicles.
                        if (infoview == BUInfoview.Vehicles)
                        {
                            // Add the previous infoview group number to the used list.
                            usedInfoGroupNumbers.Add(infoviewGroupNumber);

                            // Get the next infoview group number not already used.
                            infoviewGroupNumber++;
                            while (usedInfoGroupNumbers.Contains(infoviewGroupNumber))
                            {
                                infoviewGroupNumber++;
                            }

                            // Restart infoview priority for this new group.
                            infoviewPriority = 1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Mod.log.Error(ex);
            }
        }

        /// <summary>
        /// Set colors for all infoviews of this mod.
        /// </summary>
        public void SetInfomodeColors()
        {
            // Do each of this mod's infoviews.
            foreach (BUInfoviewData infoviewData in Values)
            {
                infoviewData.SetInfomodeColors();
            }

            // Check for an active infoview.
            World defaultWorld = World.DefaultGameObjectInjectionWorld;
            ToolSystem toolSystem = defaultWorld.GetOrCreateSystemManaged<ToolSystem>();
            if (toolSystem.activeInfoview != null)
            {
                // Check if active infoview is for this mod.
                BUInfoview activeInfoview = BUInfoviewData.GetInfoview(toolSystem.activeInfoview.name);
                if (activeInfoview != BUInfoview.None)
                {
                    // Get the entity for the active infoview.
                    Entity entityActiveInfoview = instance[activeInfoview].infoviewPrefabEntity;

                    // Set the active infoview to Null and immediately back to the active infoview.
                    // This causes the infoviews UI system to resend the infomode data to the UI where the new colors will be applied.
                    InfoviewsUISystem infoviewsUISystem = defaultWorld.GetOrCreateSystemManaged<InfoviewsUISystem>();
                    infoviewsUISystem.SetActiveInfoview(Entity.Null);
                    infoviewsUISystem.SetActiveInfoview(entityActiveInfoview);
                }
            }
        }

        /// <summary>
        /// Reset data values.
        /// </summary>
        public void ResetDataValues()
        {
            // Do each infoview data.
            foreach (BUInfoviewData infoviewData in Values)
            {
                infoviewData.ResetDataValues();
            }
        }
    }
}
