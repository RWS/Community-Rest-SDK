# REST SDK for Zimbra Social
##Social SDK for REST and OAuthentication
###System Requirements
- Zimbra Social 8.0 or higher
- .NET Framework version 4.5

###Executing the Tests
To execute all the tests you must modify some items. 
- In your Community site, create an Oauth Client that is confidential using Client Credentials
- In the test project, locate Setup.cs in the root
- At the top of th file modify the Url, user and Oauth variables to be appropriate to your site.

>Note that these tests are meant to test the communication infrastrature and SDK logic, not the REST Apis themselves.  If you are having issues with a specific API not related to the SDK please contact support.
