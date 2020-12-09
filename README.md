# Echobox is hiring! 
If you'd like to build and/or improve a poker AI (or similar), as part of an amazing team, please get in touch via our [careers page](https://careers.echobox.com/) :mortar_board:.

# OpenPokerAI 

[![Build Status](https://dev.azure.com/OpenPokerAI/OpenPokerAI/_apis/build/status/MarcFletcher.OpenPokerAI?branchName=main)](https://dev.azure.com/OpenPokerAI/OpenPokerAI/_build/latest?definitionId=1&branchName=main) [![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=OpenPokerAI_OpenPokerAI&metric=ncloc)](https://sonarcloud.io/dashboard?id=OpenPokerAI_OpenPokerAI)

## What is it?

This repo contains everything required to train and play against 'reasonably sophisticated' poker AIs. It is shared with the intention of being used as a learning playground for aspiring and also seasoned (if you fancy a change of problem domain) AI/ML engineers and researchers.

## Licensing

This project is released under the terms of the Apache 2.0 License.

## Where does this work come from?

This was one of many side projects by [MattDean](https://github.com/MattDean) and [MarcFletcher](https://www.linkedin.com/in/marcpfletcher/) between 2009 and 2013. Since then we got distracted with life and the code has sat on a shelf ever since.

Back then the field of ML was orders of magnitude less sophisticated than it is today so some of the ML implementations demonstrated here might now be considered out-of-date. Since 2013 we've also learnt a lot about how to create large software projects so please don't judge what still looks like a 'hacky' project, e.g. we don't have many tests which should really exist. "What's a style check?". We'd like to share this sooner, in its current form warts and all, rather than later or never.

## Getting started

There is quite a lot to digest here but the simplest way to start exploring this project is by playing the AIs themselves:

1. Clone the repo.
2. Download the data files that were too large for git (most even [LFS](https://git-lfs.github.com/)) using this torrent file. We recommend putting these files in the `Resources` folder of the repo. If you put them somewhere else you'll have to change some hardcoded path values in a subsequent step (see below).
3. Open the `OpenPokerAI.sln` file from the base of the repo using [Visual Studio](https://visualstudio.microsoft.com/vs/community/).
4. Set the `PokerGame` project as your startup project. It should now appear in bold.
5. If you downloaded the large data files somewhere other than `./Resources/` you need to edit `handRanksAbsoluteDir` and `wpLookupTablesAbsoluteDir` parameters within the `PokerGame > BotGame.cs` file, near the top.
6. Run/execute the `PokerGame` project.
7. Select the combination of opponents you would like to play against, up to a total of 9, and click _Play Poker_. If you want to play yourself remember to include atleast one _NoAi_Human_ player. See [`AIGenerations.cs`](https://github.com/MarcFletcher/OpenPokerAI/blob/main/PokerDefinitions/AIGenerations.cs) for information about the different players you can pick from.
8. Have fun playing the AIs.
9. Start breakpointing code to work out how their insides work and start to make changes!

Note: If things don't appear to be working check for error log files in your execution path (i.e. Debug/..). If you're still having problems feel free to raise an issue.

## Getting in touch

**[GitHub Issues](https://github.com/MarcFletcher/OpenPokerAI/issues/new)**: If you have ideas, bugs, 
or problems with this project just open a new issue.

## Contributing

We'd love your contributions, even if it's just to help minimise project build warnings. It's as simple as creating a pull request! 
