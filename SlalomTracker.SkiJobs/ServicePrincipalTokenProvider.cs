/*
SOURCE: https://tsmatz.wordpress.com/2017/03/03/azure-rest-api-with-certificate-and-service-principal-by-silent-backend-daemon-service/

This class really should be here: https://github.com/Azure/azure-sdk-for-net/tree/master/sdk/mgmtcommon/ClientRuntime/ClientRuntime

This article: https://docs.microsoft.com/en-us/dotnet/azure/dotnet-sdk-azure-authenticate?view=azure-dotnet 
    should talk about alternatives to the Fluent SDK (i.e. this below)

 curl -i -H "Content-Type: application/x-www-form-urlencoded" -d "resource=https%3A%2F%2Fmanagement.core.windows.net%2F&client_id=$clientid&client_secret=$secret&grant_type=client_credentials" https://login.microsoftonline.com/delormejhotmail364.onmicrosoft.com/oauth2/token  

 HTTP/1.1 200 OK
Cache-Control: no-cache, no-store
Pragma: no-cache
Content-Type: application/json; charset=utf-8
Expires: -1
Strict-Transport-Security: max-age=31536000; includeSubDomains
X-Content-Type-Options: nosniff
x-ms-request-id: 048a4772-532e-41d4-a810-837b6c029500
x-ms-ests-server: 2.1.9624.12 - CHI ProdSlices
P3P: CP="DSP CUR OTPi IND OTRi ONL FIN"
Set-Cookie: fpc=Arx2MnaErD9GtId2RBTVHUxeBeoXAQAAAEoCVNUOAAAA; expires=Thu, 05-Dec-2019 23:52:42 GMT; path=/; secure; HttpOnly; SameSite=None
Set-Cookie: x-ms-gateway-slice=prod; path=/; SameSite=None; secure; HttpOnly
Set-Cookie: stsservicecookie=ests; path=/; SameSite=None; secure; HttpOnly
Date: Tue, 05 Nov 2019 23:52:42 GMT
Content-Length: 1354

{"token_type":"Bearer","expires_in":"3600","ext_expires_in":"3600","expires_on":"1573001562","not_before":"1572997662","resource":"https://management.core.windows.net/","access_token":"eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6ImFQY3R3X29kdlJPb0VOZzNWb09sSWgydGlFcyIsImtpZCI6ImFQY3R3X29kdlJPb0VOZzNWb09sSWgydGlFcyJ9.eyJhdWQiOiJodHRwczovL21hbmFnZW1lbnQuY29yZS53aW5kb3dzLm5ldC8iLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83MTU3ODdmYi01NjA5LTRhZmMtYjY3ZS03Zjg3MThkOGI1NmMvIiwiaWF0IjoxNTcyOTk3NjYyLCJuYmYiOjE1NzI5OTc2NjIsImV4cCI6MTU3MzAwMTU2MiwiYWlvIjoiNDJWZ1lJajV1WU5iOHV4M3J3dm5qVS9NdlBJb0hnQT0iLCJhcHBpZCI6Ijg4N2YxNTZkLThmZDgtNDNlOC1iNmVlLWVjMTVjMDJlMzdlZCIsImFwcGlkYWNyIjoiMSIsImlkcCI6Imh0dHBzOi8vc3RzLndpbmRvd3MubmV0LzcxNTc4N2ZiLTU2MDktNGFmYy1iNjdlLTdmODcxOGQ4YjU2Yy8iLCJvaWQiOiI0MWQ2OGM5ZC1lMWJhLTQ5NWYtOTYyNy0yZjIwZmViMzEyMGIiLCJzdWIiOiI0MWQ2OGM5ZC1lMWJhLTQ5NWYtOTYyNy0yZjIwZmViMzEyMGIiLCJ0aWQiOiI3MTU3ODdmYi01NjA5LTRhZmMtYjY3ZS03Zjg3MThkOGI1NmMiLCJ1dGkiOiJja2VLQkM1VDFFR29FSU43YkFLVkFBIiwidmVyIjoiMS4wIn0.nW0-lQlt2fh6YIM3_rRA1npyTL82wNYbkj7p_vdYYMexijbtBsrqEthc7L0ZuAV9Tu2qKbT9lCnAtr5gORhaN1mha1xIX6nCJIPyXqJoF8LaiePZvCNqU15EVFokG2W193f6ir7USdQABwilrzVYsKet5E8bfQwwLvMXoaWFxWVZJo6XAwLDrwDCjYUI-MuQClrAS-joull-rbcALy_64J61it8pEABHQpIUZADc2wZXn6NfMukenbWA9DYUSwBB10N9pk3Z45T6gynq5qwIPYEwbaxGeLBykIago1o7RNGq8Bh-zBiQzPRq5g5e6vNtEoe-96JZpyLY-C9moE1Gqg"}%
*/