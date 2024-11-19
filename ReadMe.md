﻿# Building Use
Six new infoviews show the use of buildings as a percent of their capacity.
You determine whether unused or fully used buildings are good or bad for your city for each building type.

### Building Colors
The color of each building indicates the use percent for that building.
For each building type, the gradient shows the color range for the building use.
See the settings below for Color of Zoned Buildings and Color of Service Buildings.

### Building Type Selection
- You can select all or deselect all building types.
- You can select and deselect individual building types.
- To show only one building type, deselect all buildings types and then select only the one building type you want see.
- The total for the entire city is still calculated and updated even when a building type is deselected.

### More Than One Building Type
If a building has more than one building type (e.g. Mixed Use Residential is both residential and commercial):
- If only one of the building types is selected, then the building color will be set according to the selected building type.
- If more than one building type is selected, then the building color will be set according to the topmost selected building type.
- The building used and capacity will be included in the total for the entire city for all applicable building types (i.e. double counted).
  For example, employees in a Water Treatment Plant will be included in both Water and Sewage totals because the building has both building types.

### Building Use Total
The total use of buildings for the entire city is shown for each building type as follows:
- Position of the triangle indicator over the color gradient.
- Percent number next to the color gradient.
- Used and capacity amounts next to the percent number.

### Deactivated Buildings
- Deactivated buildings are counted towards capacity.
- Deactivated building upgrades are not counted towards capacity.

### Buildings For Rent
For residential, commercial, industrial, and office buildings that are available to rent,
the capacity cannot be determined because the capacity comes from the renter.
Therefore, such buildings do not contribute to the total used or capacity for the entire city
(except that such buildings do count toward capacity for Efficiency because efficiency capacity is always 100%).
But the color of these buildings are set as if they are 0% used so the buildings can be identified.

### Excluded Buildings
Abandoned, condemned, deleted, and destroyed buildings are excluded.

# Infoviews
The following sections provide more information for each infoview.
Due to rounding, total used and capacity values may not exactly match the game's total values.
All building types for residential, commercial, industrial, and office include both growable buildings and signature buildings.

### Employees Infoview
- Number of employees as a percent of employee capacity.
- Residential building use is households instead of employees.
- The Employees infoview is also useful to identify the locations of zoned and service buildings because most buildings have employees.

### Visitors Infoview
- Number of visitors as a percent of visitor capacity.
- Visitors include citizens who are:  in a medical facility, deceased, students, in an emergency shelter, arrested, or in prison.

### Storage Infoview
- Amount of resources as a percent of resource capacity.
- Some resources have no weight (e.g. Software).
  The count of unweighted resources are combined with the amount of weighted resources.
- Processing buildings have both input and output resources.
  These are combined into one used total for the building because the game does not have separate capacities for inputs and outputs.

### Vehicles Infoview
- Number of vehicles in use, in maintenance, or both as a percent of vehicle capacity.
- Your selections for in use and in maintenance are saved and restored between infoviews and between games.
- The number of vehicles in maintenance is based on the building's efficiency.
  When efficiency is at least 100%, the building will have no vehicles in maintenance.
- Commercial Truck, Industrial Truck, Office Truck, and Parked Vehicle do not have vehicles in maintenance.
- Office buildings have capacity for delivery trucks, but they are rarely (if ever) used.
- For Fire Stations with a Disaster Response Unit upgrade, the game appears to incorrectly compute vehicles in maintenance.
  So this mod's in maintenance count will not match the game's in maintenance vehicle count.
- The Airport building has capacity for delivery trucks even if the Airport does not have the Cargo Terminal upgrade.
- The Space Rocket from the ChirpX Space Center is not shown because it is always 1 used of 1 capacity.

### Efficiency Infoview
- Building efficiency as a percent.
- Building efficiency can be and often is greater than 100%.
  You can choose to have the building color set for a maximum of 100% or 200%.
  Your selections are saved and restored between infoviews and between games.
- The color for buildings with an efficiency that exceeds the selected maximum of 100% or 200% will be set as if they are 100% or 200%.
- Deactivated buildings have 0% efficiency.
- Each building contributes its efficiency percent to the total used amount for the entire city.
  Each building contributes 100% to the total capacity amount for the entire city.
  Therefore, the total efficiency for the entire city for each building type is a simple average over the buildings of that type.
  Small and large buildings contribute equally to the average.
- Note that the total capacity divided by 100 is the count of buildings of that type.

### Processing Infoview
- Production or processing speed as a percent of the production or processing speed capacity.
- Crematoriums have a processing speed only when deceased are present.

# Options
The following settings are available on the Options screen.

### Color of Zoned Buildings
There are two choices for the color of zoned buildings, including signature buildings:
- 3 Colors (default):  Building colors are red, yellow, green for low, medium, high building use respectively.
  The colors green and red do not necessarily mean good and bad building use.
  You must decide whether low or high building use is good or bad.
- Zone Color:  Building colors are set according to their respective zone type:
  residential = green, commercial = blue, industrial = yellow, and office = purple.

For Zone Color, building use is indicated by the color brightness:  the lightest color is 0% used and the darkest color is 100% used.

### Color of Service Buildings
There are three choices for the color of service buildings:
- 3 Colors (default):  Building colors are red, yellow, green for low, medium, high building use respectively.
  The colors green and red do not necessarily mean good and bad building use.
  You must decide whether low or high building use is good or bad.
- Red:  All service buildings are red.
- Various: Different colors for different service building types.
  The colors are mostly based on the corresponding service icon.
  The colors for some building types will be similar to or the same as each other.
  For example, Electricity and Police are the same color because their icons are the same color.
  Some building types with white/gray icons use a different arbitrary color to differentiate them from the default off-white color.

For Red and Various, building use is indicated by the color brightness:  the lightest color is 0% used and the darkest color is 100% used.

### Specialized Industry Lots
- When checked, the color of the attached lot for each specialized industry is set the same as its hub building to make the building use easier to see.
- When unchecked (default), only the hub building color is set.

# Compatibility
The mod is translated into all the languages supported by the base game.

There are no known compatibility issues with any other mods.

This mod can be safely disabled or unsubscribed at any time.

# Possible Future Enhancements
Here are some possible future enhancements that were thought about during development but not included initially:
- Allow the player to choose a district and show the building use for only buildings in that district.
  "Entire city" would be one choice in the list of districts.
- Allow the player to locate buildings for selected building types by showing an icon above each building.
- By default, the game turns on all infomodes each time an infoview is displayed.
  Save the selections for each infoview to be used the next time the infoview is displayed.
  Possibly do this as a separate mod for all infoviews, not just for the infoviews for this mod.
- Create resource locator infoviews to show which buildings may hold which resources by:
  extraction, processing input, processing output, and warehouse.
  Possibly do this as a separate mod.

# Acknowledgements
The following mods were used extensively in the development of this mod:
- Scene Explorer by krzychu124
- Extended Tooltip by Mimonsi