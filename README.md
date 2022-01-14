# ArrowTools
Cartographic Arrow Tools for ArcGIS Pro

This version was released January 10, 2022 at 09:30, so the version number is "20220110-0930".

This git repository contains a plaintext readme file called "Arrow Tools-README-20220110-0930.txt".
This README.md file was created using that file as a starting point.

## Set up

[Download the addin](https://github.com/ORMAPtools/ArrowTools/blob/main/ORMAPArrowTools-20220110-0930.esriAddinX) from github.

There are different ways to install it. One way is to put the esriAddinX file on a server accessible by anyone
using ArcGIS Pro, and then to set Pro to look there. In the startup screen click the "gear" icon to get to settings, and in the left navbar
choose "Add-in Manager", then click "Options" tab, then use "Add Folder" to point at the folder where you keep your esriAddinX file(s).
Restart ArcGIS Pro and you should now see "ORMAP Arrow Tools" in the Add-In Manager.

Complete ArcGIS Pro instructions on loading add-ins: [Manage add-ins](https://pro.arcgis.com/en/pro-app/latest/get-started/manage-add-ins.htm)

## Usage information

Once you have the add-in loading, the new collection of arrow tools will show up in the Create Features templates when you create line features.

![Create Features template](https://github.com/ORMAPtools/ArrowTools/blob/main/screenshots/arrow_tools_template.png)

I am still figuring usage out! (Brian).
For the moment, you should look in ["Arrow Tools-README-20220110-0930.txt"](https://github.com/ORMAPtools/ArrowTools/blob/main/Arrow%20Tools-README-20220110-0930.txt) for more instructions.

## Developer information

Visual Studio source for the project is in the "source" folder.

Building this project was tested using the "Visual Studio Community 2022 (64 bit), version 17.0.5"
When you install it, select the .NET desktop development option under "Desktop & Mobile".

## License

David Howes <david@dhowes.com> of David Howes, LLC wrote this tool for ORMAP.

Licensed under the GNU General Public License, version 3 (GPL-3.0). (See the file "LICENSE".)
