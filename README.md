# Bifrost

How to use:
Prepare a ApplicationConfig.json file: 

```
{
  "logDirectory": "",
  "backupDirectory": "",
  "backupLengthDays": 7,
  "services": [
    {
      "serviceType": "local",
      "serviceName": "developer",
      "hostname": "",
      "scp_username": "",
      "scp_password": "",
      "scp_RemoteRoot": "",
      "scp_hostkey": "",
      "localFolder": "",
      "filetypes": [
        ".txt",
        ".doc"
      ],
      "developerInstance": true,
      "rename": true,
      "filenameAddition": "DEVELOPER"
    }
  ]
}
```

Build the the Bifrost solution and then build the SetupFileMover solution.

On the client, run the msi file that you made earlier with building the SetupFileMover solution, then use installutil located in the installation directory to install the 
Bifrost.exe file as a service. Run the service and check windows event logs for any errors you might encounter.


## TODO
* [DONE] Add config file that is read on startup, not build
* Better logging module
* Email notifications
