# RetroLottis Magic: The Gathering - Proxy Drafter aka MgcPrxyDrftr
I wrote this console application to create ready to print PDF files of Magic: The Gathering booster packs.
Highly inspired by [MtGen](https://github.com/copperdogma/mtgen) I figured that I'd like to create a booster pack for printing and using the cards in proxy drafting.
Data is base on two main sources:
- [Scryfall](https://scryfall.com/)
- [MTGJSON](https://mtgjson.com/)

<strike>PDF files are generated via [nanDeck](https://www.nandeck.com/).</strike>
<strike>PDF files are now generated using [FreeSpire.PDF](https://www.nuget.org/packages/FreeSpire.PDF).</strike>
PDF files are finally generated with a good library that I found [QuestPDF]https://github.com/QuestPDF/QuestPDF.
<strike>here are limitations to the free version but at the moment these do not apply to my case.</strike>
There is no limit on page count and whatnot. The version I use is only free for community applications.
Maybe I switch to the latest really free (older) version but at the moment everything is fine.

Also I wrote the API, that provides boosters, myself with PHP. At the moment I it located on a small server that has almost not capacity at all.
No domain, no SSL so it is totally unsecure and will probably rejected by everything. :)
Maybe I change that in the future.