// This entire file is only for creating UI files when in DEBUG.
#if DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BuildingUse
{
    /// <summary>
    /// Create the files for UI:
    ///     One file for building status types.
    ///     One file for the value bindings for used and capacity data for each infomode.
    ///     One file for UI translation keys for C#.  Includes settings.
    ///     One file for UI translation keys for UI.  Excludes settings.
    /// By creating the files from the C# enums and lists, C# and UI are ensured to be the same as each other.
    /// </summary>
    public static class CreateUIFiles
    {
        // Shortcut for UI constants dictionary.
        // Dictionary key is the constant name.
        // Dictionary value is the constant value.
        private class UIConstants : Dictionary<string, string> { }

        // Shortcut for translation keys list.
        // Entry is used for constant name and constant value suffix.
        private class TranslationKeys : List<string> { }


        /// <summary>
        /// Create the UI files.
        /// </summary>
        public static void Create()
        {
            CreateFileUIConstants();
            CreateFileUIBindings();
            CreateFileUITranslationKeys(true);
            CreateFileUITranslationKeys(false);
        }

        /// <summary>
        /// Create the file for UI constants.
        /// </summary>
        private static void CreateFileUIConstants()
        {
            // Start with the do not modify instructions.
            StringBuilder sb = new StringBuilder();
            sb.Append(DoNotModify());

            // Include building status types.
            sb.AppendLine("// Define building status types.");
            sb.AppendLine("export enum " + nameof(BUBuildingStatusType));
            sb.AppendLine("{");
            foreach (BUBuildingStatusType buildingStatusType in Enum.GetValues(typeof(BUBuildingStatusType)))
            {
                sb.AppendLine($"    {buildingStatusType.ToString().PadRight(32)} = {(int)buildingStatusType},");
            }
            sb.AppendLine("}");

            // Write the file to the UI/src folder.
            string uiConstantsPath = Path.Combine(GetSourceCodePath(), "UI", "src", "uiConstants.tsx");
            File.WriteAllText(uiConstantsPath, sb.ToString());
        }

        /// <summary>
        /// Create the file for UI bindings.
        /// </summary>
        private static void CreateFileUIBindings()
        {
            // Start with the do not modify instructions.
            StringBuilder sb = new StringBuilder();
            sb.Append(DoNotModify());

            // Include imports.
            sb.AppendLine("import { bindValue } from \"cs2/api\";");
            sb.AppendLine("import mod from \"../mod.json\";");

            // Do each building status type.
            sb.AppendLine();
            sb.AppendLine("// Define bindings for C# to UI for used and capacity data for each building status type.");
            sb.AppendLine("export const uiBindings: any =");
            sb.AppendLine("{");
            foreach (BUInfoviewData infoviewData in BUInfoviewDatas.instance.Values)
            {
                foreach (BUBuildingStatusTypeData buildingStatusTypeData in infoviewData.buildingStatusTypeDatas.Values)
                {
                    // Skip special cases.
                    if (!buildingStatusTypeData.isSpecialCase)
                    {
                        // Include both a used and a capacity binding.
                        BUBuildingStatusType buildingStatusType = buildingStatusTypeData.buildingStatusType;
                        string used     = buildingStatusType + "Used";
                        string capacity = buildingStatusType + "Capacity";
                        sb.AppendLine($"    {used    .PadRight(40)} : bindValue<number>(mod.id, {("\"" + used     + "\",").PadRight(48)} 0),");
                        sb.AppendLine($"    {capacity.PadRight(40)} : bindValue<number>(mod.id, {("\"" + capacity + "\",").PadRight(48)} 0),");
                    }
                }
            }
            sb.AppendLine("}");

            // Include binding names.
            sb.AppendLine();
            sb.AppendLine("// Define binding names for C# to UI.");
            sb.AppendLine("export class uiBindingNames");
            sb.AppendLine("{");
            sb.AppendLine($"    public static {BuildingUseUISystem.BindingNameCountVehiclesInUse          .PadRight(40)} : string = \"{BuildingUseUISystem.BindingNameCountVehiclesInUse          }\";");
            sb.AppendLine($"    public static {BuildingUseUISystem.BindingNameCountVehiclesInMaintenance  .PadRight(40)} : string = \"{BuildingUseUISystem.BindingNameCountVehiclesInMaintenance  }\";");
            sb.AppendLine($"    public static {BuildingUseUISystem.BindingNameEfficiencyMaxColor200Percent.PadRight(40)} : string = \"{BuildingUseUISystem.BindingNameEfficiencyMaxColor200Percent}\";");
            sb.AppendLine($"    public static {BuildingUseUISystem.BindingNameSelectedDistrict            .PadRight(40)} : string = \"{BuildingUseUISystem.BindingNameSelectedDistrict            }\";");
            sb.AppendLine($"    public static {BuildingUseUISystem.BindingNameDistrictInfos               .PadRight(40)} : string = \"{BuildingUseUISystem.BindingNameDistrictInfos               }\";");
            sb.AppendLine("}");

            // Include event names.
            sb.AppendLine();
            sb.AppendLine("// Define event names for UI to C#.");
            sb.AppendLine("export class uiEventNames");
            sb.AppendLine("{");
            sb.AppendLine($"    public static {BuildingUseUISystem.EventNameCountVehiclesInUseClicked        .PadRight(40)} : string = \"{BuildingUseUISystem.EventNameCountVehiclesInUseClicked        }\";");
            sb.AppendLine($"    public static {BuildingUseUISystem.EventNameCountVehiclesInMaintenanceClicked.PadRight(40)} : string = \"{BuildingUseUISystem.EventNameCountVehiclesInMaintenanceClicked}\";");
            sb.AppendLine($"    public static {BuildingUseUISystem.EventNameEfficiencyMaxColorClicked        .PadRight(40)} : string = \"{BuildingUseUISystem.EventNameEfficiencyMaxColorClicked        }\";");
            sb.AppendLine($"    public static {BuildingUseUISystem.EventNameSelectedDistrictChanged          .PadRight(40)} : string = \"{BuildingUseUISystem.EventNameSelectedDistrictChanged          }\";");
            sb.AppendLine("}");

            // Write the file to the UI/src folder.
            string uiBindingsPath = Path.Combine(GetSourceCodePath(), "UI", "src", "uiBindings.tsx");
            File.WriteAllText(uiBindingsPath, sb.ToString());
        }

        /// <summary>
        /// Create the file for UI transtion keys.
        /// One file for C# (i.e. CS) and one file for UI.
        /// </summary>
        private static void CreateFileUITranslationKeys(bool csFile)
        {
            // Start with the do not modify instructions.
            StringBuilder sb = new StringBuilder();
            sb.Append(DoNotModify());

            // For C# file, include namespace.
            if (csFile)
            {
                sb.AppendLine($"namespace {ModAssemblyInfo.Name}");
                sb.AppendLine("{");
            }

            // Start class.
            const string className = "UITranslationKey";
            if (csFile)
            {
                sb.AppendLine("    // Define UI translation keys.");
                sb.AppendLine("    public class " + className);
                sb.AppendLine("    {");
            }
            else
            {
                sb.AppendLine("// Define UI translation keys.");
                sb.AppendLine("export class " + className);
                sb.AppendLine("{");
            }

            // Include title and description.
            TranslationKeys titleDescription = new TranslationKeys()
            {
                "Title",
                "Description",
            };
            sb.Append(GetTranslationsContent(csFile, "Mod title and description.", titleDescription));

            // Include infoview titles.
            UIConstants infoviewTitles = new UIConstants();
            foreach (BUInfoviewData infoviewData in BUInfoviewDatas.instance.Values)
            {
                infoviewTitles.Add("InfoviewTitle" + infoviewData.infoview.ToString(), $"Infoviews.INFOVIEW[{infoviewData.infoviewName}]");
            }
            sb.AppendLine();
            sb.Append(GetTranslationsContent(csFile, "Infoview titles.", infoviewTitles));

            // Include infomode titles and tooltips.
            UIConstants infomodeTitles   = new UIConstants();
            UIConstants infomodeTooltips = new UIConstants();
            foreach (BUInfoviewData infoviewData in BUInfoviewDatas.instance.Values)
            {
                foreach (BUBuildingStatusTypeData buildingStatusTypeData in infoviewData.buildingStatusTypeDatas.Values)
                {
                    // Skip special cases.
                    if (!buildingStatusTypeData.isSpecialCase)
                    {
                        BUBuildingStatusType buildingStatusType = buildingStatusTypeData.buildingStatusType;
                        string buildingStatusTypeName = BUBuildingStatusTypeData.GetBuildingStatusTypeName(buildingStatusType);
                        infomodeTitles  .Add("InfomodeTitle"   + buildingStatusType.ToString(), $"Infoviews.INFOMODE[{        buildingStatusTypeName}]");
                        infomodeTooltips.Add("InfomodeTooltip" + buildingStatusType.ToString(), $"Infoviews.INFOMODE_TOOLTIP[{buildingStatusTypeName}]");
                    }
                }
            }
            sb.AppendLine();
            sb.Append(GetTranslationsContent(csFile, "Infomode titles.", infomodeTitles));
            sb.AppendLine();
            sb.Append(GetTranslationsContent(csFile, "Infomode tooltips.", infomodeTooltips));

            // Include district selector text.
            TranslationKeys districtSelectorText = new TranslationKeys() { "EntireCity", "DistrictSelectorTooltip", };
            sb.AppendLine();
            sb.Append(GetTranslationsContent(csFile, "District selector text.", districtSelectorText));

            // Include select/deselect text.
            TranslationKeys selectDeselectText = new TranslationKeys() { "SelectAll", "DeselectAll", "SelectDeselectTooltip", };
            sb.AppendLine();
            sb.Append(GetTranslationsContent(csFile, "Select/deselect text.", selectDeselectText));

            // Include vehicles in use/in maintenance text.
            TranslationKeys vehiclesInUseInMaintenanceText = new TranslationKeys() { "InUse", "InMaintenance", "InUseInMaintenanceTooltip", };
            sb.AppendLine();
            sb.Append(GetTranslationsContent(csFile, "Vehicles in use/in maintenance text.", vehiclesInUseInMaintenanceText));

            // Include max color text.
            TranslationKeys maxColorText = new TranslationKeys() { "MaxColor100Percent", "MaxColor200Percent", "MaxColorTooltip", };
            sb.AppendLine();
            sb.Append(GetTranslationsContent(csFile, "Max color text.", maxColorText));

            // Include unit of measure text.
            TranslationKeys unitOfMeasureText = new TranslationKeys() { "UnitOfMeasurePrefixKilo", "UnitOfMeasurePrefixMega", "GigaWattHour", };
            sb.AppendLine();
            sb.Append(GetTranslationsContent(csFile, "Unit of measure text.", unitOfMeasureText));

            // Include general text.
            TranslationKeys generalText = new TranslationKeys() { "NoBuildings", };
            sb.AppendLine();
            sb.Append(GetTranslationsContent(csFile, "General text.", generalText));

            // For C# file only, include settings translations.
            if (csFile)
            {
                // Construct settings.
                UIConstants _translationKeySettings = new UIConstants()
                {
                    { "SettingTitle",                                   Mod.ModSettings.GetSettingsLocaleID()                                                         },
                                                                                                                                                                         
                                                                                                                                                                         
                    { "SettingGroupGeneral",                            Mod.ModSettings.GetOptionGroupLocaleID(ModSettings.GroupGeneral)                              },
                                                                                                                                                                         
                    { "SettingZonedBuildingColorLabel",                 Mod.ModSettings.GetOptionLabelLocaleID(nameof(ModSettings.ZonedBuildingColor               )) },
                    { "SettingZonedBuildingColorDesc",                  Mod.ModSettings.GetOptionDescLocaleID (nameof(ModSettings.ZonedBuildingColor               )) },
                    { "SettingZonedBuildingColorChoiceThreeColors",     Mod.ModSettings.GetEnumValueLocaleID  (ModSettings.ZonedBuildingColorChoice.ThreeColors     ) },
                    { "SettingZonedBuildingColorChoiceZoneColor",       Mod.ModSettings.GetEnumValueLocaleID  (ModSettings.ZonedBuildingColorChoice.ZoneColor       ) },

                    { "SettingServiceBuildingColorLabel",               Mod.ModSettings.GetOptionLabelLocaleID(nameof(ModSettings.ServiceBuildingColor             )) },
                    { "SettingServiceBuildingColorDesc",                Mod.ModSettings.GetOptionDescLocaleID (nameof(ModSettings.ServiceBuildingColor             )) },
                    { "SettingServiceBuildingColorChoiceThreeColors",   Mod.ModSettings.GetEnumValueLocaleID  (ModSettings.ServiceBuildingColorChoice.ThreeColors   ) },
                    { "SettingServiceBuildingColorChoiceRed",           Mod.ModSettings.GetEnumValueLocaleID  (ModSettings.ServiceBuildingColorChoice.Red           ) },
                    { "SettingServiceBuildingColorChoiceVarious",       Mod.ModSettings.GetEnumValueLocaleID  (ModSettings.ServiceBuildingColorChoice.Various       ) },
                                                                                                                                                                          
                    { "SettingColorSpecializedIndustryLotsLabel",       Mod.ModSettings.GetOptionLabelLocaleID(nameof(ModSettings.ColorSpecializedIndustryLots     )) },
                    { "SettingColorSpecializedIndustryLotsDesc",        Mod.ModSettings.GetOptionDescLocaleID (nameof(ModSettings.ColorSpecializedIndustryLots     )) },
                                                                                                                                                                          
                    { "SettingReverseColorsLabel",                      Mod.ModSettings.GetOptionLabelLocaleID(nameof(ModSettings.ReverseColors                    )) },
                    { "SettingReverseColorsDesc",                       Mod.ModSettings.GetOptionDescLocaleID (nameof(ModSettings.ReverseColors                    )) },
                                                                                                                                                                          
                                                                                                                                                                          
                    { "SettingGroupAbout",                              Mod.ModSettings.GetOptionGroupLocaleID(ModSettings.GroupAbout)                                },
                                                                                                                                                                          
                    { "SettingModVersionLabel",                         Mod.ModSettings.GetOptionLabelLocaleID(nameof(ModSettings.ModVersion                       )) },
                    { "SettingModVersionDesc",                          Mod.ModSettings.GetOptionDescLocaleID (nameof(ModSettings.ModVersion                       )) },
                };

                // Append settings to the file.
                sb.AppendLine();
                sb.Append(GetTranslationsContent(csFile, "Settings.", _translationKeySettings));
            }

            // End class.
            sb.AppendLine(csFile ? "    }": "}");

            // For C# file, end namespace.
            if (csFile)
            {
                sb.AppendLine("}");
            }

            // Write the file.
            string uiBindingsPath;
            if (csFile)
            {
                // Write the file to the Localization folder.
                uiBindingsPath = Path.Combine(GetSourceCodePath(), "Localization", "UITranslationKey.cs");
            }
            else
            {
                // Write the file to the UI/src folder.
                uiBindingsPath = Path.Combine(GetSourceCodePath(), "UI", "src", "uiTranslationKey.tsx");
            }
            File.WriteAllText(uiBindingsPath, sb.ToString());
        }

        /// <summary>
        /// Get instructions for do not modify.
        /// </summary>
        /// <returns></returns>
        private static string DoNotModify()
        {
            StringBuilder sb = new StringBuilder();
            // Include do not modify instructions.
            sb.AppendLine($"// DO NOT MODIFY THIS FILE.");
            sb.AppendLine($"// This entire file was automatically generated by class {nameof(CreateUIFiles)}.");
            sb.AppendLine($"// Make any needed changes in class {nameof(CreateUIFiles)}.");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// Get the constants content.
        /// </summary>
        private static string GetTranslationsContent(bool csFile, string comment, UIConstants constants)
        {
            string indentation = (csFile ? "        " : "    ");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{indentation}// {comment}");
            foreach (var key in constants.Keys)
            {
                if (csFile)
                {
                    sb.AppendLine($"{indentation}public const string {key.PadRight(50)} = \"{constants[key]}\";");
                }
                else
                {
                    sb.AppendLine($"{indentation}public static {key.PadRight(50)}: string = \"{constants[key]}\";");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Get the translations content.
        /// </summary>
        private static string GetTranslationsContent(bool csFile, string comment, TranslationKeys translationKeys)
        {
            string indentation = (csFile ? "        " : "    ");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{indentation}// {comment}");
            foreach (var translationKey in translationKeys)
            {
                if (csFile)
                {
                    sb.AppendLine($"{indentation}public const string {translationKey.PadRight(50)} = \"{ModAssemblyInfo.Name}.{translationKey}\";");
                }
                else
                {
                    sb.AppendLine($"{indentation}public static {translationKey.PadRight(50)}: string = \"{ModAssemblyInfo.Name}.{translationKey}\";");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Get the full path of this C# source code file.
        /// </summary>
        private static string GetSourceCodePath([System.Runtime.CompilerServices.CallerFilePath] string sourceFile = "")
        {
            return Path.GetDirectoryName(sourceFile);
        }
    }
}

#endif
