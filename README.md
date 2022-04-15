# FunciSox

Do some audio processsing with [SoX](http://sox.sourceforge.net/) using [Azure Durable Functions](https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-overview).


## Notes

**2022-04-13**

Just having the Table attribute in **SendDownloadAvailableEmail** did not result in the *Downloads* table being created automatically. The workflow would fail because the table did not exist. I used [Microsoft Azure Storage Explorer](https://azure.microsoft.com/en-us/features/storage-explorer/) to create an empty *Downloads* table. After that the workflow would successfully add rows to the table.

[Table Support in Azurite](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio#table-support) is in *PREVIEW*. Maybe it behaves differently from the (now deprecated) [Azure Storage Emulator](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator). Maybe I misunderstood when I expected using the Table attribute would cause the table to be created if it did not exist.

**2022-04-14**

For `sox.exe` to convert **from mp3** format `libmad.dll` is required. That DLL is not part of the SoX project and is not provided in the download. I found a version of `libmad.dll` that I complied from source in 2009. I copied it to the **Tools** directory but it did not work with the more recent `sox.exe`. I had to use the version of `sox.exe` (SoX v14.3.0) from the same era as the found `libmad.dll`.

## Links

### Azure Functions

[Azure Durable Functions](https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-overview)

Mark Heath's [Azure Functions Links](https://github.com/markheath/azure-functions-links)

### Audio Tools

[Sox - Homepage](http://sox.sourceforge.net/)

[SoX - Wikipedia](https://en.wikipedia.org/wiki/SoX)

[LAME MP3 Encoder](https://lame.sourceforge.io/)

[ID3 - Wikipedia](https://en.wikipedia.org/wiki/ID3#Editing_ID3_tags)

[id3mtag](https://squell.github.io/id3/)

[GitHub - squell/id3](https://github.com/squell/id3)
