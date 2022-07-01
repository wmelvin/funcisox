# Kusto Queries

## Queries Used

These queries were used looking at the logs for FunciSox:

```
traces
| project
    timestamp,
    message
```

```
traces
| project
    timestamp,
    message
| order by timestamp desc
```

```
traces
| where message has "RunProcess"
| project
    timestamp,
    message
| order by timestamp desc
```

```
traces
| where message has "SendDownloadAvailableEmail"
| project
    timestamp,
    message
| order by timestamp desc
```

```
traces
| where message has "Download" or message has "END "
| project
    timestamp,
    message
| order by timestamp desc
```

## Microsoft Docs

[Kusto Query Language (KQL)](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/query/)

[Tutorial - Kusto queries](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/query/tutorial?pivots=azuredataexplorer)
