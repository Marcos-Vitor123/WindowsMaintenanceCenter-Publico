using System.Media;

namespace WindowsMaintenanceCenter.Services;

public class SoundService
{
    public void PlaySuccess()
    {
        try { SystemSounds.Asterisk.Play(); } catch { }
    }

    public void PlayWarning()
    {
        try { SystemSounds.Exclamation.Play(); } catch { }
    }

    public void PlayError()
    {
        try { SystemSounds.Hand.Play(); } catch { }
    }

    public void PlayComplete()
    {
        try { SystemSounds.Asterisk.Play(); } catch { }
    }
}