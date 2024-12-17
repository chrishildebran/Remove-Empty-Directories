namespace RED2.Lib
{

    using System;

    /// <summary>
    ///     Icon names (Warning: Entries are case sensitive)
    /// </summary>
    public enum DirectoryIcons
    {

        Home,

        Deleted,

        ProtectedIcon,

        FolderWarning

    }

    /// <summary>
    ///     List box container class thingy
    /// </summary>
    public class DeleteModeItem
    {

        public DeleteModeItem(DeleteModes mode)
        {
            this.DeleteMode = mode;
        }

        public DeleteModes DeleteMode{get; set;}

        public static DeleteModes[] GetList()
        {
            return new[] { DeleteModes.RecycleBin, DeleteModes.RecycleBinShowErrors, DeleteModes.RecycleBinWithQuestion, DeleteModes.Direct, DeleteModes.Simulate };
        }

        public override string ToString()
        {
            switch (this.DeleteMode)
            {
                case DeleteModes.RecycleBin:

                    return "Delete to recycle bin and ignore errors (safer but slower, default setting)";

                case DeleteModes.Direct:

                    return "Bypass recycle bin and directly delete dirs (more dangerous but faster)";

                case DeleteModes.RecycleBinShowErrors:

                    return "Delete to recycle bin and show all errors (can be annoying)";

                case DeleteModes.RecycleBinWithQuestion:

                    return "Delete to recycle bin and ask before every deletion";

                case DeleteModes.Simulate:

                    return "Simulate deletion (just pretend doing it, for testing)";


                // TODO: Idea -> Move files instead of deleting?

                default:

                    throw new Exception("Unknown delete mode");
            }
        }

    }

}