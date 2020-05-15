using SharpShell.Attributes;
using SharpShell.SharpDropHandler;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace CSScript.ShellExtension
{
    [ComVisible(true)]
    [Guid("DA86D195-0C8D-477D-9038-98E53B5E29B0")]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".cssc")]
    public class ShellDropHandler : SharpDropHandler
    {
        protected override void DragEnter(DragEventArgs dragEventArgs)
        {
            dragEventArgs.Effect = DragDropEffects.Link;
        }

        protected override void Drop(DragEventArgs dragEventArgs)
        {
            StringBuilder args = new StringBuilder();
            foreach (string dragItem in DragItems)
            {
                if (args.Length > 0)
                {
                    args.Append(" ");
                }
                args.Append("\"" + dragItem + "\"");
            }
            Process.Start(SelectedItemPath, args.ToString());
        }
    }
}
