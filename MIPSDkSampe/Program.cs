using System;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.InformationProtection;
using Microsoft.InformationProtection.Exceptions;
using Microsoft.InformationProtection.File;
using Microsoft.InformationProtection.Protection;
using MIPSDkSampe;
using static System.Net.Mime.MediaTypeNames;
class Program
{

    
     static void Main(string[] args)
    {
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
    }
}
