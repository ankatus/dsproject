# dsproject

Distributed Systems Fall 2020 programming task

A console-based Uno-game following the rules found on [Wikipedia](https://en.wikipedia.org/wiki/Uno_\(card_game\)#Official_rules), with the following exceptions:  
* The "WildDrawFour" -card is not included
* There is no points-counting, every round is a self-contained game with a single winner

## Usage
### Installation
#### Windows
1. Make sure the .[NET 5.0 runtime](https://dotnet.microsoft.com/download/dotnet/current/runtime) is installed
2. Download the .exe
3. Launch the .exe

#### Linux
1. Make sure the .[NET 5.0 runtime](https://dotnet.microsoft.com/download/dotnet/current/runtime) is installed
2. Download the .dll
3. Make sure your terminal is at least 150 columns wide and 50 columns high
3. Run the .dll using dotnet

### Joining a game
1. Input your display name when prompted
2. Select your network adapter
3. Override the default group address and port if you wish
4. Select how many players your game will have (the game will not start before this amount of players are found)
5. Wait for the game to start

### Playing the game
Follow the instructions on the screen. Left and right arrow keys scroll through your hand if you have more than five cards

## Other stuff
* A log file is created in the same directory that the application .exe or .dll is in.
