Walkthrough of MIP SDK demo on a console based app C#.
Objective:
Create a C# console-based application which can perform the following activities: 
•	List all the labels.
•	Apply a label to the document.
•	Read the label from the document and display its metadata. 
Prerequisites:
•	Licencing requirement - Microsoft Information Protection (MIP) SDK setup and configuration | Microsoft Learn
•	At least 1 sensitivity label defined and published - Create and publish sensitivity labels | Microsoft Learn
•	Visual Studio installation on the machine - Microsoft Information Protection (MIP) SDK setup and configuration | Microsoft Learn
Implementation walkthrough: 
Step 1: Create an Azure AD App Registration
Authentication against the Azure AD tenant requires creating a native application registration. The client ID created in this step is used in a later step to generate an OAuth2 token.
Skip this step if you've already created a registration for previous sample. You may continue to use that client ID.
1.	Go to https://portal.azure.com and log in as a global/application admin.
Your tenant may permit standard users to register applications. If you aren't a global admin, you can attempt these steps, but may need to work with a tenant administrator to have an application registered or be granted access to register applications.
2.	Select Azure Active Directory, then App Registrations on the left side menu.
3.	Select New registration
4.	For name, enter MipSdk-Sample-Apps
5.	Under Supported account types set Accounts in this organizational directory only
Optionally, set this to Accounts in any organizational directory.
6.	Select Register
The Application registration screen should now be displaying your new application.
Step 2: Add API Permissions to the registered application
1.	Select API Permissions
2.	Select Add a permission
3.	Select Azure Rights Management Services
4.	Select Delegated permissions
5.	Check user_impersonation and select Add permissions at the bottom of the screen.
6.	Select Add a permission
7.	Select APIs my organization uses
8.	In the search box, type Microsoft Information Protection Sync Service then select the service.
9.	Select Delegated permissions
10.	Check UnifiedPolicy.User.Read then select Add permissions
11.	In the API permissions menu, select Grant admin consent for and confirm.
Set Redirect URI
1.	Select Authentication.
2.	Select Add a platform.
3.	Select Mobile and desktop applications
4.	Select the default native client redirect URI, which should look similar to https://login.microsoftonline.com/common/oauth2/nativeclient.
You may have to add additional redirect URI starting with http://localhost:PORT depending on the port used by your sample application 
5.	Select configure and be sure to save and changes if required.
Step 3 : Create console application 
1.	Create a C# console project 
2.	Right-click the project and select Manage NuGet Packages
3.	On the Browse tab, search for Microsoft.InformationProtection.File
4.	Select the package and click Install
5.	Same way, please install the package Microsoft.Identity.Client & System.Configuration.ConfigurationManager
Step 4: Configure App registration details in the console project. 
1.	Right click and add ApplicationConfiguration file with the name App.Config
2.	Add the below appsettings under the configuration section

<appSettings>
	<add key="ClientId" value="APPID"/>		
	<add key="TenantGuid" value="Tenant Id"/>
	<add key="Authority" value="https://login.microsoftonline.com/"/>
	<add key="app:Name" value="MIP File SDK Sample App"/>
	<add key="app:Version" value="1.11"/>
</appSettings>

Step 5: Implement an authclass to manage the MSAL objects.
1.	Right-click the project name in Visual Studio, select Add then Class.
2.	Enter "AuthClass" in the Name field. Click Add.
3.	Add using statements for the Microsoft Authentication Library (MSAL) and the MIP library:
using Microsoft.Identity.Client;
using System.Configuration;
4.	Add the below implementation of Authclass to initialize the MSAL instance and sign-in and acquire the token.
internal static class AuthClass
{
    private static IPublicClientApplication application;

    // Initialize the MSAL library by building a public client application
    internal static void InitializeMSAL()
    {
        string authority = string.Concat(ConfigurationManager.AppSettings.Get("Authority"), ConfigurationManager.AppSettings.Get("TenantGuid"));
        	application = PublicClientApplicationBuilder.Create(ConfigurationManager.AppSettings.Get("ClientId"))
                                                .WithAuthority(authority)
                                                .WithDefaultRedirectUri()
                                                .Build();
    }
    
    // Sign in and return the access token 
    internal static async Task<string> SignInUserAndGetTokenUsingMSAL(string[] scopes)
    {
        AuthenticationResult result;
        try
        {
            var accounts = await application.GetAccountsAsync();
            result = await application.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
             .ExecuteAsync();
        }
        catch (MsalUiRequiredException ex)
        {
            result = await application.AcquireTokenInteractive(scopes)
             .WithClaims(ex.Claims)
             .ExecuteAsync();
        }
        return result.AccessToken;
    }

        // Sign in and return the account 
        internal static async Task<IAccount> SignInUserAndGetAccountUsingMSAL(string[] scopes)
        {
            AuthenticationResult result;
            try
            {
                var accounts = await application.GetAccountsAsync();
                result = await application.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                 .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                result = await application.AcquireTokenInteractive(scopes)
                 .WithClaims(ex.Claims)
                 .ExecuteAsync();
            }
            return result.Account;
        }
}

Step 6: Implement an authentication delegate 
The MIP SDK implements authentication using class extensibility, which provides a mechanism to share authentication work with the client application. The client must acquire a suitable OAuth2 access token, and provide to the MIP SDK at runtime.
Now create an implementation for an authentication delegate, by extending the SDK's Microsoft.InformationProtection.IAuthDelegate interface, and overriding/implementing the IAuthDelegate.AcquireToken() virtual function. The authentication delegate is instantiated and used later by the FileProfile and FileEngine objects.
1.	Right-click the project name in Visual Studio, select Add then Class.
2.	Enter "AuthDelegateImplementation" in the Name field. Click Add.
3.	Add using statements for the Microsoft Authentication Library (MSAL) and the MIP library:
using Microsoft.InformationProtection;
using Microsoft.Identity.Client;
using System.Configuration;
4.	Set AuthDelegateImplementation to inherit Microsoft.InformationProtection.IAuthDelegate and implement a private variable of Microsoft.InformationProtection.ApplicationInfo and a constructor that accepts the same type.
public class AuthDelegateImplementation : IAuthDelegate
 {
     private ApplicationInfo _appInfo;
     // Microsoft Authentication Library IPublicClientApplication
     private IPublicClientApplication _app;
     public AuthDelegateImplementation(ApplicationInfo appInfo)
     {
         _appInfo = appInfo;
     }
 }
5.	The ApplicationInfo object contains three properties. The _appInfo.ApplicationId will be used in the AuthDelegateImplementation class to provide the client ID to the auth library. ApplicationName and ApplicationVersion will be surfaced in Azure Information Protection Analytics reports.
6.	Add the public string AcquireToken() method. This method should accept Microsoft.InformationProtection.Identity and three strings: authority URL, resource URI, and claims, if required. These string variables will be passed in to the authentication library by the API and shouldn't be manipulated. Please input Tenant GUID from Azure portal for your tenant. Editing strings other than the Tenant GUID may result in a failure to authenticate.
     public string AcquireToken(Identity identity, string authority, string resource, string claims)
     {
         string[] scopes = new string[] { resource[resource.Length - 1].Equals('/') ? $"{resource}.default" : $"{resource}/.default" };
         return AuthClass.SignInUserAndGetTokenUsingMSAL(scopes).Result;
     }
Step 7: Implement consent delegate
Now create an implementation for a consent delegate, by extending the SDK's Microsoft.InformationProtection.IConsentDelegate interface, and overriding/implementing GetUserConsent(). The consent delegate is instantiated and used later, by the File profile and File engine objects. The consent delegate is provided with the address of the service the user must consent to using in the url parameter. The delegate should generally provide some flow that allows the user to accept or reject to consent to accessing the service. For this quickstart hard code Consent.Accept.
1.	Using the same Visual Studio "Add Class" feature we used previously, add another class to your project. This time, enter "ConsentDelegateImplementation" in the Class Name field.
2.	Add using statements for the Microsoft Authentication Library (MSAL) and the MIP library:
using Microsoft.InformationProtection;

3.	Now update ConsentDelegateImpl.cs to implement your new consent delegate class. Add the using statement for Microsoft.InformationProtection and set the class to inherit IConsentDelegate.
public class ConsentDelegateImplementation : IConsentDelegate
 {
     public Consent GetUserConsent(string url)
     {
         return Consent.Accept;
     }
 }
4.	Optionally, attempt to build the solution to ensure that it compiles with no errors.
Step 8: Initialize the MSAL for sign-in & MIP SDK Managed Wrapper
1.	From Solution Explorer, open the program.cs file in your project that contains the implementation of the Main() method. It defaults to the same name as the project containing it, which you specified during project creation.
2.	Remove the generated implementation of main().
3.	The managed wrapper includes a static class, Microsoft.InformationProtection.MIP used for initialization, creating a MipContext, loading profiles, and releasing resources. To initialize the wrapper for File SDK operations, call MIP.Initialize(), passing in MipComponent.File to load the libraries necessary for file operations.
4.	In the Program.cs add the following using statements
class Program
{
    static void Main(string[] args)
    {
    }
}
5.	In Main() in Program.cs add the following
  // Initialize MSAL and trigger sign-in .
  AuthClass.InitializeMSAL();
var username = AuthClass.SignInUserAndGetAccountUsingMSAL(new string[] { "user.read"}).Result;
  Console.WriteLine(string.Format("Logged in user {0}", username.Username));
  // Initialize Wrapper for File SDK operations.

  MIP.Initialize(MipComponent.File);

  // Create ApplicationInfo, setting the clientID from Microsoft Entra App Registration as the ApplicationId.
  ApplicationInfo appInfo = new ApplicationInfo()
  {
      ApplicationId = ConfigurationManager.AppSettings.Get("ClientId"),
      ApplicationName = ConfigurationManager.AppSettings.Get("app:Name"),
      ApplicationVersion = ConfigurationManager.AppSettings.Get("app:Version")
  };

  // Instantiate the AuthDelegateImpl object, passing in AppInfo.
  AuthDelegateImplementation authDelegate = new AuthDelegateImplementation(appInfo);
Step 9: Construct a MIPContext instance and load File Profile and Engine
As mentioned, profile and engine objects are required for SDK clients using MIP APIs. Complete the coding portion of this Quickstart, by adding code to load the native DLLs then instantiate the profile and engine objects.
1.	Toward the end of the main() function add the below code.
// Create MipConfiguration Object
 MipConfiguration mipConfiguration = new MipConfiguration(appInfo, "mip_data", Microsoft.InformationProtection.LogLevel.Trace, false);

 // Create MipContext using Configuration
 MipContext mipContext = MIP.CreateMipContext(mipConfiguration);

 // Initialize and instantiate the File Profile.
 // Create the FileProfileSettings object.
 // Initialize file profile settings to create/use local state.
 var profileSettings = new FileProfileSettings(mipContext,
                          CacheStorageType.OnDiskEncrypted,
                          new ConsentDelegateImplementation());

 // Load the Profile async and wait for the result.
 var fileProfile = Task.Run(async () => await MIP.LoadFileProfileAsync(profileSettings)).Result;

 // Create a FileEngineSettings object, then use that to add an engine to the profile.
 // This pattern sets the engine ID to user1@tenant.com, then sets the identity used to create the engine.
 var engineSettings = new FileEngineSettings("user1@tenant.com", authDelegate, "", "en-US");
 engineSettings.Identity = new Identity("user1@tenant.com");

 var fileEngine = Task.Run(async () => await fileProfile.AddEngineAsync(engineSettings)).Result;

Step 10: Add logic to list the sensitivity labels
Add logic to list your organization's sensitivity labels, using the File engine object.
1.	Toward the end of the Main() body (where you left off in the previous Quickstart), insert the following code:
// List sensitivity labels from fileEngine and display name and id
 foreach (var label in fileEngine.SensitivityLabels)
 {
     Console.WriteLine(string.Format("{0} : {1}", label.Name, label.Id));

     if (label.Children.Count != 0)
     {
         foreach (var child in label.Children)
         {
             Console.WriteLine(string.Format("{0}{1} : {2}", "\t", child.Name, child.Id));
         }
     }
 }

Step 11: Set a sensitivity label
Add logic to set and get a sensitivity label on a file, using the File engine object.
1.	Toward the end of the Main() body, insert the following code:

 //Set paths and label ID
 //string inputFilePath = "<input-file-path>";
 //string labelId = "<label-id>";
 //string outputFilePath = "<output-file-path>";


 Console.Write("Enter a label identifier from above: ");
 var labelId = Console.ReadLine();

 // Prompt for path inputs
 Console.Write("Enter an input file path: ");
 string inputFilePath = Console.ReadLine();

 Console.Write("Enter an output file path: ");
 string outputFilePath = Console.ReadLine();

 string actualOutputFilePath = outputFilePath;
 string actualFilePath = inputFilePath;

 //Create a file handler for that file
 //Note: the 2nd inputFilePath is used to provide a human-readable content identifier for admin auditing.
 var handler = Task.Run(async () => await fileEngine.CreateFileHandlerAsync(inputFilePath, actualFilePath, true)).Result;

 //Set Labeling Options
 LabelingOptions labelingOptions = new LabelingOptions()
 {
     AssignmentMethod = AssignmentMethod.Standard
 };

 // Set a label on input file	
 handler.SetLabel(fileEngine.GetLabelById(labelId), labelingOptions, new ProtectionSettings());

 // Commit changes, save as outputFilePath
 var result = Task.Run(async () => await handler.CommitAsync(outputFilePath)).Result;
Step 12: Get a sensitivity label metadata
Add logic to get a sensitivity label on a file, using the File engine object.
1.	Toward the end of the Main() body, insert the following code:
// Create a new handler to read the labeled file metadata
var handlerModified = Task.Run(async () => await fileEngine.CreateFileHandlerAsync(outputFilePath, actualOutputFilePath, true)).Result;

// Get the label from output file
var contentLabel = handlerModified.Label;
Console.WriteLine(string.Format("Getting the label committed to file: {0}", outputFilePath));
Console.WriteLine(string.Format("File Label: {0} \r\nIsProtected: {1}", contentLabel.Label.Name, contentLabel.IsProtectionAppliedFromLabel.ToString()));
Console.WriteLine("Press a key to continue.");
Console.ReadKey();
// Application Shutdown
handler = null; // This will be used in later quick starts.
fileEngine = null;
fileProfile = null;
mipContext.ShutDown();
mipContext = null;
Step 13: Output

 
