# SmsControl application
Repo for the SmsControl application.

## Dependencies
1. .NET 9 

## Environments
1. Install .Net 9: https://dotnet.microsoft.com/en-us/download/dotnet/9.0
2. Run command to make sure SDK is installed and configured properly: dotnet --version

## Download source code
Get source code from Github Repo: https://github.com/aliciahuang22/SmsControl

## Building Application
1. Nevigate to project root directory: SmsControl
2. Type command: dotnet build
3. The build dirctory is at: SmsControl/bin/Debug/net9.0/SmsControl.dll

## Running Application
1. Nevigate to project root directory: SmsControl
2. Type command: dotnet run

## Testing application on local
Send request
curl -X POST "http://localhost:5000/api/sms/allow-send" -H "Content-Type: application/json" -d '{"phoneNumber": "1234567890"}'
curl -X POST "http://localhost:5000/api/sms/reset" -H "Content-Type: application/json"

curl -X GET "http://localhost:5000/api/sms/get-rate" \
-H "Content-Type: application/json" \
-d '{"phoneNumber": "1234567890", "from": "2025-02-11 15:45:30", "to": "2025-02-11 16:22:30"}'
 
## Running Unit Tests
1. Nevigate to project root directory: SmsControl
2. Type command: dotnet test