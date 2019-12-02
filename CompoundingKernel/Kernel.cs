using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.Threading;
using System.Collections;

namespace CompoundingKernel
{
    class Kernel
    {
        // Field for the named pipe.
        private static NamedPipeServerStream npUIPipe;

        static void Main(string[] args)
        {
            // Create a named pipe to communicate with the UI, then wait for hte UI process to connect.
            npUIPipe = new NamedPipeServerStream("CompoundingKernel", PipeDirection.InOut, 20, PipeTransmissionMode.Message);
            npUIPipe.WaitForConnection();
            // UI client has connected. Carry out interaction.
            // 5.2 Create and start a thread to exec. vInteract()
            Thread tInteract = new Thread(vInteract);
            tInteract.Start();
        }

        private static void vInteract()
        {
            // Step 1: Send a list of the coumpounding methods we can handle to the UI Client.
            vSendMethods();
            // Step 2: Set up dictionary to hold the computation data.
            Dictionary<string, string> dictCompInfo = dictInitDictionary();
            // Step 3: Loop while the UI is still running, receiving and processing messages.
            bool bRunning = true;
            while (bRunning == true) 
            {
                (string strCMD, string strArg) = tstrReceiveMsg();
                bRunning = bProcessMsg(strCMD, strArg, dictCompInfo);
            }

            // Connection closed. Dispose of the named pipe.
            npUIPipe.Dispose();

        }

        private static bool bProcessMsg(string strCMD, string strArg, Dictionary<string , string> dictCompInfo) 
        {
            if (strCMD == "quit")
                return false;
            else if (strCMD == "calculate") 
            {
                vCalculate(dictCompInfo);
                return true;
            }
            else
            {
                // All other commands put data into the dictionary, with the command as the key and the argument as the value.
                dictCompInfo[strCMD] = strArg.ToLower();
                return true;
            }
        }

        private static void vCalculate(Dictionary<string, string> dictCompInfo)
        {
            // Get the data for the calculatoion out of the dictionary.
            double dPrinc = double.Parse(dictCompInfo["principal"]);
            double dIntRate = double.Parse(dictCompInfo["interest"]);
            string strMethod = dictCompInfo["method"];

            // Do the calculation using the chosen method.
            double dResult;

            if (strMethod == "annual")
                dResult = dAnnual(dPrinc, dIntRate);
            else if (strMethod == "monthly")
                dResult = dMonthly(dPrinc, dIntRate);
            else
                dResult = dContinuous(dPrinc, dIntRate);

            vSendMessage("result", dResult.ToString());
        }

        private static Dictionary<string, string> dictInitDictionary()
        {
            // Create an empty dictionary, tne pu tin default calues for the principal, interest rate and compounding method.
            Dictionary<string, string> dictInitialDict = new Dictionary<string, string>();
            dictInitialDict["principal"] = "0";
            dictInitialDict["interest"] = "0";
            dictInitialDict["method"] = "annual";
            return dictInitialDict;
        }

        private static void vSendMethods()
        {
            // Create message listing the methods supported.
            string strMessage = "Methods:Annual,Monthly,Continious";
            // Convert to an array of bytes and send out over the pipe.
            byte[] byMessage = Encoding.ASCII.GetBytes(strMessage);
            npUIPipe.Write(byMessage, 0, byMessage.Length);
        }

        private static double dAnnual(double dPrincipal, double dIntRate)
        {
            return dPrincipal * dIntRate;
        }

        private static double dMonthly(double dPrincipal, double dIntRate)
        {
            return dPrincipal * (Math.Pow(1.0 + dIntRate / 12.0, 12) - 1.0);
        }

        private static double dContinuous(double dPrincipal, double dIntRate)
        {
            return dPrincipal * (Math.Exp(dIntRate) - 1.0);
        }

        private static (string strCommand, string strArgs) tstrReceiveMsg()
        {
            // Receive the message from the pipe, convert to string.
            byte[] byBuffer = new byte[100];
            int iBytesRead = npUIPipe.Read(byBuffer, 0, byBuffer.Length);
            string strMessage = Encoding.ASCII.GetString(byBuffer);
            // Split on the colon in the string. The aprt before the colon is command, the part after the colon is the argument.
            string[] astrSplit = strMessage.Split(':');

            return (astrSplit[0], astrSplit[1]);
        }

        // Send a mesage out over the pipe, given the command and argument.

        private static void vSendMessage(string strCommand, string strArgument)
        {
            // Combine command and argument with colon in between.
            string strMessage = strCommand + ":" + strArgument;
            // Convert to array of bytes and send out over the pipe.
            byte[] byMessage = Encoding.ASCII.GetBytes(strMessage);
            npUIPipe.Write(byMessage, 0, byMessage.Length);
        }
    }
}
