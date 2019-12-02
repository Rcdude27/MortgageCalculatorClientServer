using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Pipes;
using System.Diagnostics;
using System.Threading;

namespace CompoundingTwo 
{
    public partial class Form1 : Form 
    {

        private NamedPipeClientStream npClient;
        public Form1() 
        {
            InitializeComponent();
        }

        private void cboCompMeth_SelectedIndexChanged(object sender, EventArgs e) 
        {
            btnCalc.Enabled = (cboCompMeth.SelectedIndex >= 0);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Start the kernel process, then connect to the named pipe.
            Process pKernel = new Process();
            pKernel.StartInfo.FileName = "CompoundingKernel.exe";
            pKernel.Start();
            Thread.Sleep(500);
            npClient = new NamedPipeClientStream("CompoundingKernel");
            npClient.Connect();
            // Receive the message from the kernel telling us what compounding methods are allowed.
            vGetMethods();
        }

        private void vGetMethods()
        {
            // Retrieve message from the kernel.
            (string strCommand, string strArgs) = tstrReceiveMsg();
            // Parse the argument into the method names.
            string[] astrMethods = strArgs.Split(',');
            // Put each method name into the combo box item list.
            foreach (string strMethod in astrMethods)
            {
                cboCompMeth.Items.Add(strMethod);
            }
        }

        private (string strCommand, string strArgs) tstrReceiveMsg()
        {
            // Receive the message from the pipe, convert to string.
            byte[] byBuffer = new byte[100];
            int iBytesRead = npClient.Read(byBuffer, 0, byBuffer.Length);
            string strMessage = Encoding.ASCII.GetString(byBuffer);
            // Split on the colon in the string. The aprt before the colon is command, the part after the colon is the argument.
            string[] astrSplit = strMessage.Split(':');

            return (astrSplit[0],astrSplit[1]);
        }

        // Send a mesage out over the pipe, given the command and argument.

        private void vSendMessage(string strCommand, string strArgument) 
        {
            // Combine command and argument with colon in between.
            string strMessage = strCommand + ":" + strArgument;
            // Convert to array of bytes and send out over the pipe.
            byte[] byMessage = Encoding.ASCII.GetBytes(strMessage);
            npClient.Write(byMessage, 9, byMessage.Length);
        }

        private void btnCalc_Click(object sender, EventArgs e)
        {
            // Get the principal, interest rate, and compounding method and send them to the kernel.
            vSendMessage("principal", txtPrinc.Text);
            // Convert interest rate to decimal (from a percentage) before sending to the kernel.
            double dRateAsDecimal = double.Parse(txtInterest.Text) / 100.0;
            vSendMessage("interest", dRateAsDecimal.ToString());
            vSendMessage("method", cboCompMeth.SelectedItem.ToString());
            // Tell the kernel to do that calculation and send the result back.
            vSendMessage("calculate", "");

            // Receive the response from the kernal and display it.
            (string strCommand, string strArgument) = tstrReceiveMsg();
            // To be safe, check that the command is "result" before using the argument.
            if (strCommand.ToLower() == "result") 
            {
                // NOt sure how many decimal places in the result. Convert to a double, then to currency format.
                double dResult = double.Parse(strArgument);
                txtIntEarned.Text = dResult.ToString("C2");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Send a "quit" message to the kernel, then dispose of the pipe.
            vSendMessage("quit", "");
            npClient.Dispose();
        }
    }
}
