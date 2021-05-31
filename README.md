# Introduction
This project is a playground to try the following technologies:
* Azure
* Microsoft Orleans
* SinglaR
* Flutter

# Project structure

```
./
|-- /OrleanPG - Backend application
|   |-- /OrleanPG.Cient - Console client application. Functionality: create game, list games, play game, etc.
|   |-- /OrleanPG.Grains - Grains implementation
|   |   |-- /Game - Game Grain and state storage model. Implements logic for game and game intialization.
|   |   |-- /GameBot - Game Bot Grain and state storage model. Implements logic for playing with bot (choose random position on each turn).
|   |   *-- /GameLobbyGrain - Game Lobby Grain and state storage models. Implements logic for pseudo authorization, game creation and adding bot to game.
|   |-- /OrleanPG.Grains.Interfaces - Grain interfaces
|   |-- /OrleanPG.Grains.UnitTests - unit tests for grains. Yep, I cover my code with tests even for pet-projects ;)
|   |-- /OrleanPG.Silo - grains host
|   *-- /SinglaR_PG.WebAPI - API endpoint for clients
|-- /clientapp - Mobile client written in Flutter
```

# How to run
* Replace connection string with your Azure storage access key (yep, this should be moved to config obviously). Hardcoded access key is revoked already.
* Run Flutter client
* Run OrleanPG.Silo
* Run SinglaR_PG.WebAPI
At that moment you can "login" in mobile app, create game and play with a bot.

Optionally you may run console client (OrleanPG.Cient), join the game and play versus yourself. 

# General note
Please don't judge strictly, project was written in spare time and frozen in the middle.
