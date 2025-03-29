using UnityEngine;
using System.Diagnostics; // Used for starting external processes

public class ShutdownComputer : MonoBehaviour
{
    private void Start()
    {
        Shutdown();
    }

    public void Shutdown()
    {
        try
        {
            // Set up the process start info to call the Windows shutdown command
            ProcessStartInfo psi = new ProcessStartInfo("shutdown", "/s /t 0");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false; // Ensure no shell is used for process execution

            // Start the process which triggers the shutdown
            Process.Start(psi);

            // Optionally log the shutdown event for debugging purposes
            UnityEngine.Debug.Log("Shutdown command issued.");
        }
        catch (System.Exception ex)
        {
            // Log any errors if the shutdown command fails
            UnityEngine.Debug.LogError("Failed to shutdown the computer: " + ex.Message);
        }
    }
}
