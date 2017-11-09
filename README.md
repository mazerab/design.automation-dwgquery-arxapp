# DWG Query AutoCAD Plugin

# Description

This sample C# application creates a plugin for AutoCAD that lets users query information (layers, blocks and dependents) in AutoCAD drawing files and store that information into a JSON output file.

# Setup

- Rebuild current project in Release mode


### Run locally

- Copy ArxApp.dll to a machine with AutoCAD 2018 installed
- Launch AutoCAD 2018
- Run the NETLOAD command and load the ArxApp.dll
- Run the DWGQUERY command
- Review the output file in ~/Documents/results.json

## Packages used

- [Newtonsoft JSON](https://www.newtonsoft.com/json)
- [AutoCAD .NET Core](https://git.autodesk.com/AutoCAD/Packages)
- [AutoCAD .NET Model](https://git.autodesk.com/AutoCAD/Packages)

# License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT).
Please see the [LICENSE](LICENSE) file for full details.

## Written by
Bastien Mazeran, [@bastien-mazeran](https://www.linkedin.com/in/bastien-mazeran-01200414/), [Autodesk Enterprise Priority Support](https://enterprisehub.autodesk.com/)

