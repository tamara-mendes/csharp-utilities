using System;
using System.Diagnostics;

namespace Adadev.Util {
	
    public class RunExternalProgram {

        public static bool SuccesfulExecute(string programPath, string arguments) {
            try {
                ProcessStartInfo startInfo = new ProcessStartInfo(programPath, arguments) {
                    UseShellExecute = false,
                    RedirectStandardError = true,
                };
                Process Process = Process.Start(startInfo);

                string result = Process?.StandardError.ReadToEnd();
                Process?.Dispose();
                Process = null;

                if(!string.IsNullOrEmpty(result)) {
                    if(result.ToLower().Contains("error")) {
                        return false;
                    }
                }

                return true;
            } catch(Exception) {
                return false;
            }
        }
    }
}