ORMAP Arrow Tools ArcGIS Pro Add-In

1/10/22, 09:30

Add-In File Name
	The add-in name format is 
		ORMAPArrowTools-[YYYYMMDD]-[HHMM].esriAddinX

	where YYYYMMDD and HHMM correspond to date and time respectively and serve as a version stamp, which is also displayed in the add-in details on the corresponding ArcGIS Pro Add-In Manager page.


Feature Creation
	The tools create and modify features during construction rather than creating temporary graphics features as in the ArcMap add-in.


Tool Access
	The arrow tools are ArcGIS Pro construction tools.
	To access a tool, open the Create Features pane and find the tools available for any line feature class.


Standard Tool Context Menu Options
	Switch arrowheads - Shift-S
		Toggles arrowhead switch state for current session.
		A checkmark is shown for switched (i.e., arrowhead at start of feature(s)).
		Unchecked initially.
		Selected value is remembered within current session.

	Select new arrows
		Toggles selection state for current session.
		A checkmark is shown for selection.
		Checked initially.
		Works exactly as in the ArcMap add-in.
		Selected value is remembered within current session.


Scales
	The tools use three types of scale:
		Map scale
			The value specified in the MapScale field for the MapIndex layer.
	
		Arrow scale
			The scale used in arrow feature construction.

		Map reference scale
			The map reference scale.
			See Tool Options below.

	Arrow scale
		Applies to tools other than the Single Arrow tool.
	
		On clicking the start point for an arrow or arrows, the arrow scale is set as follows:
			Get the MapIndex polygon containing the point.
			Get the map scale from the MapScale field.
				If no polygon is found, map scale is set to 1.
		
		The user may change the initial arrow scale by selecting a value from a tool context menu.
			The feature under construction will be updated accordingly.
		
		The standard context menu scale items are:
			10, 20, 30, 40, 50, 100, 200, 400, 800, 1000, 2000

			This list of values may be limited using the Required Scales option - see below.

		The arrow scale is the selected context menu value divided by 100.

		On first opening the context menu after creating the start point for an arrow feature or arrow features,
			If a menu item corresponds to the initial arrow scale multiplied by 100, the item will be checked.
			Otherwise, no item will be checked.


Tool Options
	Click right arrow in top right corner of feature template panel.

	Select any arrow tool - options are the same for all tools.

	Options
		Reference Scale - select a value to control the map reference scale.

		Required Scales
			Set
				Set the required scales list.
				Click to upload a text file containing integer values that specify the subset of arrow tools scales to appear in the context menus.
				If the file is empty, no scale values are shown in the context menus.
				Otherwise, the selected file is checked to ensure that it contains one integer per line and no invalid characters.
				The selected file is copied to the ArcGIS Pro project folder and given the name "ArrowTools_Required_Scales.txt".
				If present, the stored file is checked and read each time a tool is activated.

			Clear
				Clear the required scales list and show all standard scales.
				If applicable, deletes the file "ArrowTools_Required_Scales.txt" in the ArcGIS Pro project folder.


Tools
	Single Arrow Tool
		Default context menu items
			Straight arrow style

			Leader arrow style

			Zigzag arrow style

			Switch arrowheads - Shift-S
			
			Select new arrows

		Current style is indicated by a checkmark next to the corresponding menu item.

		If a feature is under construction when a new style is chosen from the context menu, a warning appears along with the option to abandon the current feature construction or continue to change the style.
			
		Straight style
			Default style.

			Click on map to create start point.
				Context menu item is added:
					Cancel
						Reset tool.
			
			Click on map to create end point.
				Context menu items are added:
					Update end point
						Toggles update end point mode.
						Item is checked as required.
						Unchecked initially.
						If checked, click on map to define new end point.
						Tool remains in update end point mode until menu item is unchecked or construction is finished.

					Finish arrow - Shift-W
						Finish construction.
					
		Leader
			Click on map to create start point.
				Context menu item is added:
					Cancel
						Reset tool.

			Click on map to create second point.

			Click on map to create end point.
				Context menu items are added:
					Update end point
						As for straight arrow tool.

					Finish arrow - Shift-W
						Finish construction.
			
		Zigzag
			Click on map to create start point.
				Context menu item is added:
					Cancel
						Reset tool.
			
			Click on map to create end point.
				Context menu items are added:
					Update end point
						As for straight arrow tool.

					Finish arrow - Shift-W
						Finish construction.

					Slide toward start
					
					Slide toward end

					Narrower

					Wider

					Less curve

					More curve

					Use default settings

					Flip arrow - Shift-F
						Toggles flip mode.
						Item is checked as required.
						Unchecked initially.

			Shape adjustment options are applied immediately.
			Shape settings are remembered for current session.


	Dimension Arrows Tool
		Click on map to create start point.
			Context menu items are added:
				Scales
					Scale options for current features.
					
				Cancel
					Reset tool.
		
		Click on map to create end point.
			Context menu items are added:
				Shorter - Space
				
				Longer - Shift-Space

		Click on map to define offset position.
			Context menu items are added:
				Update end point
					As for straight arrow tool.

				Update offset
					Toggles update offset mode.
					Item is checked as required.
					Checked initially.
					If checked, click on map to define new offset point.
					Tool remains in update offset mode until menu item is unchecked or construction is finished.
				
				Finish arrows - Shift-W
					Finish construction.
			
		Cursor indicates the number of dashes.
			Default: 0
			Options: 0 - 4

			Click number keys 0 - 4 to control number of dashes.
				Cursor changes accordingly on mouse move.


	Straight Arrows Tool
		Click on map to create start point.
			Context menu items are added:
				Scales
					Scale options for current features.
					
				Cancel
					Reset tool.

		Click on map to create end point.
			Context menu items are added:
				Update end point
					As for straight arrow tool.

				Finish arrows - Shift-W
					Finish construction.

				Shorter - Space
				
				Longer - Shift-Space

				Flip arrows - Shift-F
					Toggles flip mode.
					Item is checked as required.
					Unchecked initially.
					State is remembered within current session.


	Land Hook Tool
		Details as for Straight Arrows.


	Road Tic Tool
		Click on map to create start point.
			Context menu items are added:
				Scales
					Scale options for current features.
					
				Cancel
					Reset tool.

		Click on map to create end point.
			Context menu items are added:
				Update end point
					As for straight arrow tool.

				Finish arrow - Shift-W
					Finish construction.


Developer Notes
	The tools are point construction tools made available for line feature classes in the Create Features pane.
		This is necessary to collect click locations and allow for snapping.
		The point locations are then used in the creation or adjustment of a line feature or line features.
	
	Mouse clicks are recorded as map points which are added to the ArrowContext.MapPoints context variable, of type ObservableCollection<MapPoint>.
	
	Each tool has a CollectionChangedMethod event handler, which is added on tool activation and removed on tool deactivation.
	
	Feature creation is controlled by changes in the contents of ArrowContext.MapPoints caused by left clicking on the map and, if applicable, context menu options being checked:
		First point added
			Feature of features creation is initialized.
		Second point added
			Feature is or features are created.
		Second point replaced
			Applies for all feature types except Dimension Arrows and Leader Arrow when Update end point context menu item is checked.
			Feature is or features are modified.
		Third point added
			Applies for Dimension Arrows offset and Leader Arrow third point.
		Third point replaced
			Applies for Dimension Arrows Update offset option and Leader Arrow Update end point option.

	Except for single arrow features, feature creation is based on settings in the XML file Resources/ArrowSettings.xml (same as in ArcMap add-in).


	Developer Options
		The ArrowContext class includes properties for three options for use in development:
			DevShowDiagnosticPoints
				If true, point features are created at mouse click locations.
				The option requires the presence of a point layer called TempPoints anywhere in the table of contents.
				The TempPoints feature class can be stored anywhere.
				The spatial reference must be "NAD 1983 HARN StatePlane Oregon North FIPS 3601 (Intl Feet)".
				No attributes are required.
				Any symbol can be used.

			DevShowDiagnosticLines
				If true, diagnostic line features are created in the current line layer.

			DevShowMenuPointCount
				If true, the ArrowContext.MapPoints.Count property value is shown in the tool context menus.
