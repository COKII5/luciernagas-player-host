using UnityEditor;

namespace Fireflies.EditorTools
{
    public static class AssetFolders
    {
        public static void Ensure(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            int separator = folderPath.LastIndexOf('/');
            string parent = folderPath.Substring(0, separator);
            Ensure(parent);
            AssetDatabase.CreateFolder(parent, folderPath.Substring(separator + 1));
        }
    }
}
