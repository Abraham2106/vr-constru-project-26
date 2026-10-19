using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Vivienda.EditorTools
{
    // Editor lock compartido entre agentes (swarm).
    public static class Agente4EditorLock
    {
        const string LockRelative = "Assets/_Project/Docs/.locks/editor.lock";
        const double MaxAgeMinutes = 30;

        public static string LockPath => Path.Combine(Application.dataPath, "_Project/Docs/.locks/editor.lock");

        public static bool TryAcquire(string agentName, out string reason)
        {
            reason = null;
            var dir = Path.GetDirectoryName(LockPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (File.Exists(LockPath))
            {
                var text = File.ReadAllText(LockPath);
                if (!IsLockAbandoned(text, out var idleMin))
                {
                    reason = $"Lock activo (inactivo {idleMin:F1} min, umbral {MaxAgeMinutes} min): {text.Trim()}";
                    return false;
                }
            }

            var stamp = $"{agentName} | UTC {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z";
            File.WriteAllText(LockPath, stamp);
            return true;
        }

        public static void Release()
        {
            if (File.Exists(LockPath))
                File.Delete(LockPath);
        }

        static bool IsLockAbandoned(string lockText, out double idleMinutes)
        {
            idleMinutes = 0;
            if (!TryParseAgeMinutes(lockText, out var parsedAge))
                parsedAge = (DateTime.UtcNow - File.GetLastWriteTimeUtc(LockPath)).TotalMinutes;
            var mtimeAge = (DateTime.UtcNow - File.GetLastWriteTimeUtc(LockPath)).TotalMinutes;
            idleMinutes = Math.Max(parsedAge, mtimeAge);

            var editorLog = Path.Combine(Directory.GetCurrentDirectory(), "Logs/Editor.log");
            if (File.Exists(editorLog))
            {
                var logAge = (DateTime.UtcNow - File.GetLastWriteTimeUtc(editorLog)).TotalMinutes;
                if (logAge < MaxAgeMinutes)
                    return false;
            }

            return idleMinutes >= MaxAgeMinutes;
        }

        static bool TryParseAgeMinutes(string text, out double minutes)
        {
            minutes = double.MaxValue;
            var idx = text.IndexOf("UTC ", StringComparison.Ordinal);
            if (idx >= 0)
            {
                var ts = text.Substring(idx + 4).Trim();
                if (DateTime.TryParseExact(ts, "yyyy-MM-dd HH:mm:ss'Z'", CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var thenUtc))
                {
                    minutes = (DateTime.UtcNow - thenUtc).TotalMinutes;
                    return true;
                }
            }

            foreach (var line in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var thenIso))
                {
                    minutes = (DateTime.UtcNow - thenIso).TotalMinutes;
                    return true;
                }
            }

            return false;
        }
    }
}
