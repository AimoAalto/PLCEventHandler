How TO :

Activate : C:\Lavaus\Asetukset\Conf.json
EventHandler_InUse = true/false
EventHandler_ListenerPort
  0 = programs uses default port = 2000
  > 0 = port to listen

Example :
{
  "PanelNo": 1,
  "NumberOfPLC": 1,
  "EventHandler_InUse": false,
  "EventHandler_ListenerPort": 0
}

**************************************************************************************************

Data structure
Machine - AutoArea - Module - Function - Device - Identification

Data is saved to text files on C:\Orfer\Events\ directory
directory tree is constructed from event data [Machine - AutoArea - Module - Function - Device]
directory name is taken from value on each level

[Orfer]
  [1]
    [1]
      [1]
        [1]
          [1]


**************************************************************************************************


Format directory names
configuration file : C:\Lavaus\Asetukset\EventConfig.json

{
  "RootFile": "Root.txt",
  "RootDirectory": "C:\\Orfer\\Events\\",
  "Children": {
    "0": {
      "FileName": "zero.txt",
      "FileDirectory": "0",
      "Children": {}
    },
    "9999": {
      "FileName": "Machine.txt",
      "FileDirectory": "Machine",
      "Children": {
        "1": {
          "FileName": "AutoArea.txt",
          "FileDirectory": "AutoArea",
          "Children": {
            "1": {
              "FileName": "Module.txt",
              "FileDirectory": "Module",
              "Children": {
                "1": {
                  "FileName": "Function.txt",
                  "FileDirectory": "Function",
                  "Children": {
                    "1": {
                      "FileName": "Device.txt",
                      "FileDirectory": "Device",
                      "Children": {}
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}

**************************************************************************************************

log file cleanup 
forfiles /p "c:\Orfer\Events" /s /m *.txt /d -120 /c "cmd /c del @path"
