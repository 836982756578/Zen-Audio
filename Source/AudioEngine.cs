// Zen Audio - Görev Çubuğu Ses Yöneticisi & Karıştırıcı
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ZenAudio
{
    [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ISimpleAudioVolume
    {
        int SetMasterVolume(float fLevel, Guid EventContext);
        int GetMasterVolume(out float pfLevel);
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, Guid EventContext);
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
    }

    public class AudioSessionItem
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public float Volume { get; set; }
        public bool IsMuted { get; set; }
        public ISimpleAudioVolume SimpleAudioVolume { get; set; }
    }

    public static class AudioEngine
    {
        private static IAudioEndpointVolume _masterVolume;

        static AudioEngine()
        {
            InitMasterVolume();
        }

        private static void InitMasterVolume()
        {
            try
            {
                IMMDeviceEnumerator enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
                IMMDevice speakers;
                enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

                Guid iid = typeof(IAudioEndpointVolume).GUID;
                object obj;
                speakers.Activate(ref iid, CLSCTX.ALL, IntPtr.Zero, out obj);
                _masterVolume = (IAudioEndpointVolume)obj;
            }
            catch { }
        }

        public static float GetMasterVolume()
        {
            try
            {
                if (_masterVolume == null) InitMasterVolume();
                if (_masterVolume != null)
                {
                    float level;
                    _masterVolume.GetMasterVolumeLevelScalar(out level);
                    return level;
                }
            }
            catch { }
            return 0.5f;
        }

        public static void SetMasterVolume(float level)
        {
            try
            {
                if (_masterVolume == null) InitMasterVolume();
                if (_masterVolume != null)
                {
                    level = Math.Max(0.0f, Math.Min(1.0f, level));
                    _masterVolume.SetMasterVolumeLevelScalar(level, Guid.Empty);
                }
            }
            catch { }
        }

        public static void AdjustMasterVolume(float delta)
        {
            float current = GetMasterVolume();
            SetMasterVolume(current + delta);
        }

        public static bool GetMasterMute()
        {
            try
            {
                if (_masterVolume == null) InitMasterVolume();
                if (_masterVolume != null)
                {
                    bool isMuted;
                    _masterVolume.GetMute(out isMuted);
                    return isMuted;
                }
            }
            catch { }
            return false;
        }

        public static bool ToggleMasterMute()
        {
            try
            {
                if (_masterVolume == null) InitMasterVolume();
                if (_masterVolume != null)
                {
                    bool current = GetMasterMute();
                    _masterVolume.SetMute(!current, Guid.Empty);
                    return !current;
                }
            }
            catch { }
            return false;
        }

        public static List<AudioSessionItem> GetAppSessions()
        {
            List<AudioSessionItem> items = new List<AudioSessionItem>();

            try
            {
                IMMDeviceEnumerator enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
                IMMDevice speakers;
                enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

                Guid iidSessionManager2 = typeof(IAudioSessionManager2).GUID;
                object obj;
                speakers.Activate(ref iidSessionManager2, CLSCTX.ALL, IntPtr.Zero, out obj);
                IAudioSessionManager2 mgr = (IAudioSessionManager2)obj;

                IAudioSessionEnumerator sessionEnum;
                mgr.GetSessionEnumerator(out sessionEnum);

                int count;
                sessionEnum.GetCount(out count);

                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        IAudioSessionControl ctl;
                        sessionEnum.GetSession(i, out ctl);

                        IAudioSessionControl2 ctl2 = ctl as IAudioSessionControl2;
                        if (ctl2 != null)
                        {
                            uint pid;
                            ctl2.GetProcessId(out pid);

                            if (pid > 0)
                            {
                                ISimpleAudioVolume sav = ctl as ISimpleAudioVolume;
                                if (sav != null)
                                {
                                    float vol;
                                    bool isMuted;
                                    sav.GetMasterVolume(out vol);
                                    sav.GetMute(out isMuted);

                                    string name = "Bilinmeyen Program";
                                    try
                                    {
                                        Process p = Process.GetProcessById((int)pid);
                                        name = p.ProcessName;
                                        if (!string.IsNullOrEmpty(p.MainWindowTitle))
                                        {
                                            name = p.MainWindowTitle;
                                        }
                                    }
                                    catch { }

                                    items.Add(new AudioSessionItem
                                    {
                                        ProcessId = (int)pid,
                                        ProcessName = name,
                                        Volume = vol,
                                        IsMuted = isMuted,
                                        SimpleAudioVolume = sav
                                    });
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return items;
        }

        // COM Interop definitions
        [ComImport]
        [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumerator { }

        private enum EDataFlow { eRender, eCapture, eAll }
        private enum ERole { eConsole, eMultimedia, eCommunications }
        private const uint CLSCTX_ALL = 23;
        private static class CLSCTX { public const uint ALL = 23; }

        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            int EnumAudioEndpoints(EDataFlow dataFlow, uint dwStateMask, out IntPtr ppDevices);
            int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);
        }

        [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            int Activate(ref Guid iid, uint dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
        }

        [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAudioEndpointVolume
        {
            int RegisterControlChangeNotify(IntPtr pNotify);
            int UnregisterControlChangeNotify(IntPtr pNotify);
            int GetChannelCount(out uint pnChannelCount);
            int SetMasterVolumeLevel(float fLevelDB, Guid pguidEventContext);
            int SetMasterVolumeLevelScalar(float fLevel, Guid pguidEventContext);
            int GetMasterVolumeLevel(out float pfLevelDB);
            int GetMasterVolumeLevelScalar(out float pfLevel);
            int SetChannelVolumeLevel(uint nChannel, float fLevelDB, Guid pguidEventContext);
            int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, Guid pguidEventContext);
            int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
            int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
            int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, Guid pguidEventContext);
            int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
        }

        [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionManager2
        {
            int GetAudioSessionControl(IntPtr AudioSessionGuid, uint StreamFlags, out IntPtr NewSessionControl);
            int GetSimpleAudioVolume(IntPtr AudioSessionGuid, uint StreamFlags, out IntPtr AudioVolume);
            int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);
        }

        [Guid("E2F5EE11-2070-4E59-AC81-E5E583E1A1A2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionEnumerator
        {
            int GetCount(out int SessionCount);
            int GetSession(int SessionIndex, out IAudioSessionControl Session);
        }

        [Guid("F4B41553-E703-4CF0-B942-A8BA868CE570"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionControl
        {
            int GetState(out int pRetVal);
            int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
            int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, Guid EventContext);
            int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
            int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, Guid EventContext);
            int GetGroupingParam(out Guid pRetVal);
            int SetGroupingParam(Guid Override, Guid EventContext);
            int RegisterAudioSessionNotification(IntPtr NewNotifications);
            int UnregisterAudioSessionNotification(IntPtr NewNotifications);
        }

        [Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionControl2 : IAudioSessionControl
        {
            new int GetState(out int pRetVal);
            new int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
            new int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, Guid EventContext);
            new int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
            new int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, Guid EventContext);
            new int GetGroupingParam(out Guid pRetVal);
            new int SetGroupingParam(Guid Override, Guid EventContext);
            new int RegisterAudioSessionNotification(IntPtr NewNotifications);
            new int UnregisterAudioSessionNotification(IntPtr NewNotifications);
            int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
            int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
            int GetProcessId(out uint pRetVal);
            int IsSystemSoundsSession();
            int SetDuckingPreference(bool optOut);
        }
    }
}
