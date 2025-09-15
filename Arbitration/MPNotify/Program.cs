using MPNotify;
using System.Configuration;
using System.Net.Http.Headers;

var argListMessage = "Try GenerateBriefAssets or SendNSANotifications.";

// evaluate startup args
if(args.Length == 0)
{
    Console.WriteLine("Not enough arguments. " + argListMessage);
    return -1;
}
if (args.Length > 1)
{
    Console.WriteLine("Too many arguments. " + argListMessage);
    return -1;
}
if (Enum.TryParse<ArbitDaemonProcess>(args[0], true, out var process) == true)
{
    // Get MPArbitration token from Microsoft
    

    var appMain = new AppMain();
    var r = await appMain.Start(process);
    return r;

    // Get Unsent Notifications

    // Loop 'em
    // 1. Get ArbitrationCase
    // 2. Get Payor
    // 3. Get Attachments
    // 4. Build Email from HTML and Attachments
    // 5. Address TO, CC, BCC
    // 6. Send to SendGrid
    // 7. Sent WF request to Arb App (mark as sent)
    // 8. Log error to Notification record JSON "{_sendResult:"Success"}" or "{_sendResult:"SendGrid message"}"
}
else
{
    Console.WriteLine("Bad argument. " + argListMessage);
    return -1;
}

