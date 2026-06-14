using UnityEngine;

namespace Monstrology
{
    public enum CloudSaveStatus
    {
        LocalOnly,
        MissingPluginModules,
        IntegrationRequired,
        Ready
    }

    public class CloudSaveCoordinator : MonoBehaviour
    {
        public CloudSaveStatus Status { get; private set; }
        public string StatusMessage { get; private set; }

        public void Initialize()
        {
#if Storage_yg && Authorization_yg
            Status = CloudSaveStatus.IntegrationRequired;
            StatusMessage =
                "PluginYG2 Storage and Authorization are installed, but " +
                "the project-specific save binding still requires verification.";
#else
            Status = CloudSaveStatus.MissingPluginModules;
            StatusMessage =
                "PluginYG2 Storage and Authorization modules are not installed. " +
                "Local PlayerPrefs saves remain active.";
#endif
            Debug.Log("[CloudSave] " + StatusMessage);
        }

        public void NotifyLocalSave()
        {
            // Local persistence remains authoritative until official cloud
            // modules are installed and their concrete API can be verified.
        }

        public static int CompareSaveJson(
            string localJson,
            string remoteJson)
        {
            GameProgress local;
            GameProgress remote;
            if (!SaveSystem.TryDeserialize(localJson, out local))
            {
                return SaveSystem.TryDeserialize(remoteJson, out remote)
                    ? -1
                    : 0;
            }

            if (!SaveSystem.TryDeserialize(remoteJson, out remote))
            {
                return 1;
            }

            return SaveSystem.CompareFreshness(local, remote);
        }
    }
}
