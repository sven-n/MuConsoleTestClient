# OpenMU Console Test Client #

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

I wrote this tool to demonstrate how easy it is to write network based tools with the nuget package ```MUnique.OpenMU.Network.Packets``` of the [OpenMU Project](https://github.com/MUnique/OpenMU/).

It encapsulates most of the packet creation and parsing for you, so you don't have to mess around with bits and bytes yourself.

Additionally, it's written for the best network performance you can get on .NET. It makes heavy use of ```Span<T>``` and ref structs for packets. The package ```MUnique.OpenMU.Network``` makes use of ```System.IO.Pipelines```.

## Licensing ##
This project is released under the MIT license (see LICENSE file).

Feel free to use this demo project as a base for your own projects.

## Contributions ##
It's not going to be actively maintained or extended. Consider it done for the purpose it serves.

It's also not perfect code - I wrote this in a matter of minutes.

## How to use ##
It supports a start parameter which takes the IP Address and a Port. The default is "127.0.0.1:55901", if nothing is specified.

It will connect to the specified address using the default network encryption of a MU Online Season 6 Episode 3 Client (English, GMO).

The application will then ask for the username, password and character name.
It wont do further actions after it entered the game with the character.
