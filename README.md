# FunciSox

Use [Azure Durable Functions](https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-overview) to process audio with the [SoX](http://sox.sourceforge.net/) utility.

**NOTE: This is a work-in-progress learning project. It is not ready for consumption (unless you want a bellyache).**

## Background

After completing a Pluralsight course called [Azure Durable Functions Fundamentals](https://app.pluralsight.com/library/courses/azure-durable-functions-fundamentals/table-of-contents) by [Mark Heath](https://markheath.net/), I wanted to do my own project to build my understanding of the topic. I have been using a set of shell scripts that run open source audio utilities to automate processing audio from MP3 files. The files are downloads of a few podcasts where I think the audio levels could use a boost. My scripts also produce a couple *faster* (increased tempo) versions of the audio (in case the pace of the podcast could use a boost too). I wrote these scripts years ago, but they still work.

This local automation process seemed like a good candidate for an Azure Functions project. Because it processes audio using external tools, the workflow could be similar to Mark's [Durable Functions Video Processor Demo](https://github.com/markheath/durable-functions-video-processor-v2) from his course.

My *FunciSox* application was built following a lot of the patterns from Mark's course. I did not use any of his code directly, though much of mine will be similar.

**Why this project is not ready for consumption:**

I am using the same versions of the audio utilities (sox.exe, lame.exe, id3.exe) as in my local process. I had trouble running the current versions of those executables in the Azure Functions environment (actually, I had trouble running the old versions, but eventually sorted that out). I am not going to make my copies of the executables available for download. Anyone trying to use the FunciSox project, as it stands, will have to find versions of those utilities and incorporate them into their project.


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


## Notes

**2022-04-13**

Just having the Table attribute in **SendDownloadAvailableEmail** did not result in the *Downloads* table being created automatically. The workflow would fail because the table did not exist. I used [Microsoft Azure Storage Explorer](https://azure.microsoft.com/en-us/features/storage-explorer/) to create an empty *Downloads* table. After that the workflow would successfully add rows to the table.

[Table Support in Azurite](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio#table-support) is in *PREVIEW*. Maybe it behaves differently from the (now deprecated) [Azure Storage Emulator](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator). Maybe I misunderstood when I expected using the Table attribute would cause the table to be created if it did not exist.

**2022-04-14**

For `sox.exe` to convert **from mp3** format `libmad.dll` is required. That DLL is not part of the SoX project and is not provided in the download. I found a version of `libmad.dll` that I complied from source in 2009. I copied it to the **Tools** directory but it did not work with the more recent `sox.exe`. I had to use the version of `sox.exe` (SoX v14.3.0) from the same era as the found `libmad.dll`.
